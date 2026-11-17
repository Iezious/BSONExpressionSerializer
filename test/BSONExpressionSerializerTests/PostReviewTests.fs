namespace BSONExpressionSerializerTests

// Post-review tests: regression guards for the defects D1–D9 found in the code review of the library.
// Every test asserts the INTENDED behaviour (the cited story / acceptance criterion). Each defect test was written to
// fail against the code as reviewed and passes since its fix; the guards marked "Must hold before and after the fix"
// held throughout.
// The defect descriptions are copied verbatim from the review findings. "Today" describes the code as reviewed and
// the "Where" line numbers point into that version, not the current one.
// All source paths are under `src/Iezious.Libs.BSONExpressionSerializer/` unless they start with `.github/`.
//
// D9 is a CI-configuration defect and has no unit test:
//
// D9. The build workflow never runs (DEC-000-44)
// - Today: `build.yml` runs build and test but triggers only on pushes to `main`, a branch the repository
//   does not have. Building happens only on the release tag. No workflow has a pull-request trigger.
// - Intended: the build workflow runs on pushes to `master`.
// - Where: `.github/workflows/build.yml:1-15`; for reference, `.github/workflows/release.yml:1-24`.

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Linq
open System.Linq.Expressions
open System.Threading
open Iezious.Libs.BSONExpressionSerializer
open MongoDB.Bson
open MongoDB.Bson.IO
open MongoDB.Bson.Serialization
open MongoDB.Driver
open NUnit.Framework

/// Lambda → LINQ expression conversion only happens at method-call sites, so the render helpers are static members.
type private Render =
    static member Filter<'t>(f: Expression<Func<'t, bool>>) : BsonDocument =
        ExpressionFilterDefinition<'t>(f)
            .Render(RenderArgs<'t>(ExpressionDocumentSerializer<'t>(), BsonSerializer.SerializerRegistry))

    static member Set<'t, 'v>(field: Expression<Func<'t, 'v>>, value: 'v) : BsonValue =
        UpdateDefinitionBuilder<'t>()
            .Set(field, value)
            .Render(RenderArgs<'t>(ExpressionDocumentSerializer<'t>(), BsonSerializer.SerializerRegistry))

    /// Renders against a given serializer instance, so repeated renders go through its member cache.
    static member FilterWith<'t>(serializer: IBsonSerializer<'t>, f: Expression<Func<'t, bool>>) : BsonDocument =
        ExpressionFilterDefinition<'t>(f)
            .Render(RenderArgs<'t>(serializer, BsonSerializer.SerializerRegistry))

    static member SetWith<'t, 'v>(serializer: IBsonSerializer<'t>, field: Expression<Func<'t, 'v>>, value: 'v) : BsonValue =
        UpdateDefinitionBuilder<'t>()
            .Set(field, value)
            .Render(RenderArgs<'t>(serializer, BsonSerializer.SerializerRegistry))

    /// Renders a projection to one member and returns the serializer the store reads each returned document with.
    static member Project<'t, 'p>(projection: Expression<Func<'t, 'p>>) : IBsonSerializer<'p> =
        ProjectionDefinitionBuilder<'t>()
            .Expression(projection)
            .Render(RenderArgs<'t>(ExpressionDocumentSerializer<'t>(), BsonSerializer.SerializerRegistry))
            .ProjectionSerializer

    static member SortAscending<'t>(field: Expression<Func<'t, obj>>) : BsonDocument =
        SortDefinitionBuilder<'t>()
            .Ascending(field)
            .Render(RenderArgs<'t>(ExpressionDocumentSerializer<'t>(), BsonSerializer.SerializerRegistry))

[<TestFixture>]
module PostReviewTests =

    let private roundTrip<'t> (value: 't) : BsonDocument * 't =
        let doc = ExpressionWriter.CreateWriter<'t>().Invoke(value)
        let back = ExpressionReader.CreateReader<'t>().Invoke(doc)
        doc, back

    // =====================================================================
    // D1. Duplicate stored names pass creation (DEC-000-3)
    //
    // - Today: two members mapped to one stored name are accepted when the serializer is created.
    //   Every whole-document write of that type then fails with "Duplicate element name".
    // - Intended: creation refuses the type (US-005.AC-3).
    // - Where: `ExpressionDocumentSerializer.fs:16-17`; `ExpressionWriter.fs:224-233,272,276`;
    //   `ExpressionReader.fs:205-206,224`.
    // =====================================================================

    [<Test>]
    let ``D1 creating a serializer for a type with two members mapped to one stored name is refused``() =
        Assert.Catch(Action(fun () -> ExpressionDocumentSerializer<D1TwoMembersOneStoredName>() |> ignore))
        |> ignore

    [<Test>]
    let ``D1 creating a serializer for a type whose stored name clashes with another property name is refused``() =
        Assert.Catch(Action(fun () -> ExpressionDocumentSerializer<D1StoredNameClashesWithPropertyName>() |> ignore))
        |> ignore

    // =====================================================================
    // D2. A null text field is written but cannot be read back (DEC-000-15)
    //
    // - Today: a text member with no value is written as an explicit null. The read has no null guard,
    //   so the stored null fails the load at any depth (nested type, array element, dictionary value).
    // - Intended: the library reads back every document it writes (US-004.AC-3).
    // - Exposure: live through the hunt-payment records. A BackOffice history page that holds one fails to load.
    // - Where: `ExpressionWriter.fs:175-185`; `ExpressionReader.fs:152,175-176`.
    // =====================================================================

    [<Test>]
    let ``D2 null text at top level round-trips as null``() =
        let doc, back = roundTrip { TestFlatClass.Name = null; CountInt = 1; Date = DateTime.UtcNow }
        Assert.That(doc["Name"].IsBsonNull, Is.True, "writer stores an explicit null")
        Assert.That(back.Name, Is.Null)

    [<Test>]
    let ``D2 null text in a nested type round-trips as null``() =
        let value = {
            TestClassWithSubObject._id = ObjectId.GenerateNewId()
            Name = "outer"
            Count = 1L
            SubObject = { TestFlatClass.Name = null; CountInt = 2; Date = DateTime.UtcNow }
        }
        let _, back = roundTrip value
        Assert.That(back.SubObject.Name, Is.Null)

    [<Test>]
    let ``D2 null text as an array element round-trips as null``() =
        let value = { TestFlatClassWithArrayOfStringValues.Name = "a"; Count = 1; SubArray = [| "x"; null |] }
        let _, back = roundTrip value
        Assert.That(back.SubArray, Is.EqualTo(box [| "x"; null |]))

    [<Test>]
    let ``D2 null text as a dictionary value round-trips as null``() =
        let dict = Dictionary<string, string>()
        dict["k"] <- null
        let _, back = roundTrip { TestClassWithStringDictionary.Name = "a"; Dict = dict }
        Assert.That(back.Dict.ContainsKey "k", Is.True)
        Assert.That(back.Dict["k"], Is.Null)

    // ---------------------------------------------------------------------
    // D2 (extension) — omission of null reference members with BsonIgnoreIfDefault.
    // A member with no value and "omit when default" must not create the field: the document stays smaller and
    // a newly added field stays absent, exactly as in documents written before the field existed.
    // The document read back without the field must load the member as null.
    // ---------------------------------------------------------------------

    let private assertOmittedAndLoadsNull<'t> (value: 't) (getValue: 't -> obj) =
        let doc, back = roundTrip value
        Assert.That(doc.Contains "Value", Is.False, $"written: {doc}")
        Assert.That(getValue back, Is.Null)

    [<Test>]
    let ``D2 omit-when-default leaves out null text and it loads as null``() =
        assertOmittedAndLoadsNull { D2OmitNullText.Name = "a"; Value = null } (fun v -> box v.Value)

    [<Test>]
    let ``D2 omit-when-default leaves out a null nested object and it loads as null``() =
        assertOmittedAndLoadsNull { D2OmitNullSubObject.Name = "a"; Value = Unchecked.defaultof<_> } (fun v -> box v.Value)

    [<Test>]
    let ``D2 omit-when-default leaves out a null text array and it loads as null``() =
        assertOmittedAndLoadsNull { D2OmitNullTextArray.Name = "a"; Value = null } (fun v -> box v.Value)

    [<Test>]
    let ``D2 omit-when-default leaves out a null nested-object array and it loads as null``() =
        assertOmittedAndLoadsNull { D2OmitNullSubObjectArray.Name = "a"; Value = null } (fun v -> box v.Value)

    [<Test>]
    let ``D2 omit-when-default leaves out a null dictionary and it loads as null``() =
        assertOmittedAndLoadsNull { D2OmitNullDictionary.Name = "a"; Value = null } (fun v -> box v.Value)

    [<Test>]
    let ``D2 omit-when-default leaves out a null raw document and it loads as null``() =
        assertOmittedAndLoadsNull { D2OmitNullBsonDocument.Name = "a"; Value = null } (fun v -> box v.Value)

    [<Test>]
    let ``D2 omit-when-default leaves out a null binary and it loads as null``() =
        assertOmittedAndLoadsNull { D2OmitNullBinary.Name = "a"; Value = null } (fun v -> box v.Value)

    [<Test>]
    let ``D2 omit-when-default still writes text that has a value``() =
        let doc, back = roundTrip { D2OmitNullText.Name = "a"; Value = "x" }
        Assert.That(doc["Value"].AsString, Is.EqualTo "x")
        Assert.That(back.Value, Is.EqualTo "x")

    [<Test>]
    let ``D2 omit-when-default leaves out null text in a nested type and in array elements``() =
        let value = {
            D2OmitNullTextNested.Name = "a"
            Sub = { D2OmitNullText.Name = "s"; Value = null }
            Items = [| { D2OmitNullText.Name = "i"; Value = null } |]
        }
        let doc, back = roundTrip value
        Assert.That(doc["Sub"].AsBsonDocument.Contains "Value", Is.False, $"written: {doc}")
        Assert.That(doc["Items"].AsBsonArray[0].AsBsonDocument.Contains "Value", Is.False, $"written: {doc}")
        Assert.That(back.Sub.Value, Is.Null)
        Assert.That(back.Items[0].Value, Is.Null)

    // ---------------------------------------------------------------------
    // D2 (extension) — absent fields load as null / empty.
    // Existing documents do not have fields added to a type later. Every member that can hold "no value"
    // (text, nested object, arrays, dictionary, raw document, binary, nullable, option, value option)
    // must load as null / empty when its field is absent, at the top level, in nested types and in array elements.
    // ---------------------------------------------------------------------

    let private assertAbsentMembersEmpty (v: D2AbsentFields) =
        use _ = Assert.EnterMultipleScope()
        Assert.That(v.Text, Is.Null, "Text")
        Assert.That(v.Sub, Is.Null, "Sub")
        Assert.That(v.Texts, Is.Null, "Texts")
        Assert.That(v.Subs, Is.Null, "Subs")
        Assert.That(v.Dict, Is.Null, "Dict")
        Assert.That(v.Doc, Is.Null, "Doc")
        Assert.That(v.Payload, Is.Null, "Payload")
        Assert.That(v.CountNullable.HasValue, Is.False, "CountNullable")
        Assert.That(v.TextOpt, Is.EqualTo(None: string option), "TextOpt")
        Assert.That(v.TextVOpt, Is.EqualTo(ValueNone: string voption), "TextVOpt")

    let private onlyName (name: string) = BsonDocument([ BsonElement("Name", BsonString name) ])

    [<Test>]
    let ``D2 absent fields load as null or empty through the reader``() =
        let back = ExpressionReader.CreateReader<D2AbsentFields>().Invoke(onlyName "a")
        Assert.That(back.Name, Is.EqualTo "a")
        assertAbsentMembersEmpty back

    [<Test>]
    let ``D2 absent fields load as null or empty through the registered serializer``() =
        let ser = ExpressionDocumentSerializer<D2AbsentFields>() :> IBsonSerializer<D2AbsentFields>
        use reader = new BsonDocumentReader(onlyName "a")
        let back = ser.Deserialize(BsonDeserializationContext.CreateRoot(reader), BsonDeserializationArgs())
        Assert.That(back.Name, Is.EqualTo "a")
        assertAbsentMembersEmpty back

    [<Test>]
    let ``D2 absent fields load as null or empty in a nested type and in array elements``() =
        let doc =
            BsonDocument([
                BsonElement("Name", BsonString "a")
                BsonElement("Sub", onlyName "s")
                BsonElement("Items", BsonArray([ onlyName "i" ]))
            ])
        let back = ExpressionReader.CreateReader<D2AbsentFieldsNested>().Invoke(doc)
        Assert.That(back.Sub.Name, Is.EqualTo "s")
        assertAbsentMembersEmpty back.Sub
        Assert.That(back.Items.Length, Is.EqualTo 1)
        Assert.That(back.Items[0].Name, Is.EqualTo "i")
        assertAbsentMembersEmpty back.Items[0]

    // =====================================================================
    // D3. A binary field with no value cannot be stored (DEC-000-15)
    //
    // - Today: writing a null byte array fails unless "omit when null" or "omit when default" leaves it out.
    //   A null byte array inside an array always fails, because no attribute can omit an element.
    // - Intended: US-004.AC-4.
    // - Exposure: live through the hunt-payment records.
    // - Where: same as D2 (`ExpressionWriter.fs:175-185`; `ExpressionReader.fs:152,175-176`).
    // =====================================================================

    [<Test>]
    let ``D3 null binary with no omit choice is stored as an explicit null``() =
        let writer = ExpressionWriter.CreateWriter<D3BinaryNoOmit>()
        let doc = writer.Invoke({ D3BinaryNoOmit.Name = "a"; Payload = null })
        Assert.That(doc.Contains "Payload", Is.True)
        Assert.That(doc["Payload"].IsBsonNull, Is.True)

    [<Test>]
    let ``D3 null binary with no omit choice round-trips as null``() =
        let _, back = roundTrip { D3BinaryNoOmit.Name = "a"; Payload = null }
        Assert.That(back.Payload, Is.Null)

    [<Test>]
    let ``D3 null element in an array of binaries round-trips as null``() =
        let doc, back = roundTrip { D3ArrayOfBinary.Name = "a"; Payloads = [| [| 1uy; 2uy |]; null |] }
        Assert.That(doc["Payloads"].AsBsonArray[1].IsBsonNull, Is.True)
        Assert.That(back.Payloads.Length, Is.EqualTo 2)
        Assert.That(back.Payloads[0], Is.EqualTo(box [| 1uy; 2uy |]))
        Assert.That(back.Payloads[1], Is.Null)

    [<Test>]
    let ``D3 null binary with no omit choice in a nested type and in array elements round-trips as null``() =
        let value = {
            D3BinaryNested.Name = "a"
            Sub = { D3BinaryNoOmit.Name = "s"; Payload = null }
            Items = [| { D3BinaryNoOmit.Name = "i"; Payload = null } |]
        }
        let doc, back = roundTrip value
        Assert.That(doc["Sub"].["Payload"].IsBsonNull, Is.True, $"written: {doc}")
        Assert.That(doc["Items"].AsBsonArray[0].["Payload"].IsBsonNull, Is.True, $"written: {doc}")
        Assert.That(back.Sub.Payload, Is.Null)
        Assert.That(back.Items[0].Payload, Is.Null)

    [<Test>]
    let ``D3 null binary as a dictionary value round-trips as null``() =
        let dict = Dictionary<string, byte[]>()
        dict["k"] <- null
        let doc, back = roundTrip { D3BinaryDictionary.Name = "a"; Dict = dict }
        Assert.That(doc["Dict"].["k"].IsBsonNull, Is.True, $"written: {doc}")
        Assert.That(back.Dict.ContainsKey "k", Is.True)
        Assert.That(back.Dict["k"], Is.Null)

    [<Test>]
    let ``D3 binary with a value and no omit choice is written and round-trips``() =
        let doc, back = roundTrip { D3BinaryNoOmit.Name = "a"; Payload = [| 1uy; 2uy; 3uy |] }
        Assert.That(doc["Payload"].BsonType, Is.EqualTo BsonType.Binary)
        Assert.That(back.Payload, Is.EqualTo(box [| 1uy; 2uy; 3uy |]))

    // ---------------------------------------------------------------------
    // D3 (extension) — the same missing / absent / default behaviour as D2, for binary.
    // With "omit when null" or "omit when default", a null binary creates no field and loads as null.
    // An absent binary field (documents written before the field existed) loads as null.
    // ---------------------------------------------------------------------

    [<Test>]
    let ``D3 omit-when-null leaves out a null binary and it loads as null``() =
        let doc, back = roundTrip { TestClassBinaryData.Name = "a"; Payload = null }
        Assert.That(doc.Contains "Payload", Is.False, $"written: {doc}")
        Assert.That(back.Payload, Is.Null)

    [<Test>]
    let ``D3 omit-when-default leaves out a null binary in a nested type and in array elements``() =
        let value = {
            D3OmitNullBinaryNested.Name = "a"
            Sub = { D2OmitNullBinary.Name = "s"; Value = null }
            Items = [| { D2OmitNullBinary.Name = "i"; Value = null } |]
        }
        let doc, back = roundTrip value
        Assert.That(doc["Sub"].AsBsonDocument.Contains "Value", Is.False, $"written: {doc}")
        Assert.That(doc["Items"].AsBsonArray[0].AsBsonDocument.Contains "Value", Is.False, $"written: {doc}")
        Assert.That(back.Sub.Value, Is.Null)
        Assert.That(back.Items[0].Value, Is.Null)

    [<Test>]
    let ``D3 absent binary and absent binary array load as null``() =
        let single = ExpressionReader.CreateReader<D3BinaryNoOmit>().Invoke(onlyName "a")
        let many = ExpressionReader.CreateReader<D3ArrayOfBinary>().Invoke(onlyName "a")
        Assert.That(single.Payload, Is.Null)
        Assert.That(many.Payloads, Is.Null)

    [<Test>]
    let ``D3 absent binary loads as null in a nested type and in array elements``() =
        let doc =
            BsonDocument([
                BsonElement("Name", BsonString "a")
                BsonElement("Sub", onlyName "s")
                BsonElement("Items", BsonArray([ onlyName "i" ]))
            ])
        let back = ExpressionReader.CreateReader<D3BinaryNested>().Invoke(doc)
        Assert.That(back.Sub.Payload, Is.Null)
        Assert.That(back.Items[0].Payload, Is.Null)

    // =====================================================================
    // D4. The omission attribute's on/off argument is ignored (DEC-000-17)
    //
    // - Today: "omit when default" switched off still leaves the member out.
    // - Intended: switched off, the member is written (US-007.AC-4).
    // - Where: `ExpressionWriter.fs:224-256,263-278`.
    // =====================================================================

    [<Test>]
    let ``D4 omit-when-default switched off writes an int at its default``() =
        let doc = ExpressionWriter.CreateWriter<D4IgnoreDefaultOffInt>().Invoke({ D4IgnoreDefaultOffInt.Name = "a"; Count = 0 })
        Assert.That(doc.Contains "Count", Is.True)
        Assert.That(doc["Count"].AsInt32, Is.EqualTo 0)

    [<Test>]
    let ``D4 omit-when-default switched off writes a string at its declared default``() =
        let doc = ExpressionWriter.CreateWriter<D4IgnoreDefaultOffString>().Invoke({ D4IgnoreDefaultOffString.Name = "zz"; Count = 1 })
        Assert.That(doc.Contains "Name", Is.True)
        Assert.That(doc["Name"].AsString, Is.EqualTo "zz")

    [<Test>]
    let ``D4 omit-when-null with omit-when-default switched off still writes a value equal to the default``() =
        let doc = ExpressionWriter.CreateWriter<D4IgnoreNullAndDefaultOff>().Invoke({ D4IgnoreNullAndDefaultOff.Name = "zz"; Count = 1 })
        Assert.That(doc.Contains "Name", Is.True)
        Assert.That(doc["Name"].AsString, Is.EqualTo "zz")

    [<Test>]
    let ``D4 omit-when-default switched off writes a value at its default and it reads back``() =
        let doc, back = roundTrip { D4IgnoreDefaultOffInt.Name = "a"; Count = 0 }
        Assert.That(doc.Contains "Count", Is.True, $"written: {doc}")
        Assert.That(back.Count, Is.EqualTo 0)

    [<Test>]
    let ``D4 omit-when-default switched off writes null text with no declared default as an explicit null``() =
        let doc, back = roundTrip { D4IgnoreDefaultOffNullText.Name = "a"; Value = null }
        Assert.That(doc.Contains "Value", Is.True, $"written: {doc}")
        Assert.That(doc["Value"].IsBsonNull, Is.True)
        Assert.That(back.Value, Is.Null)

    [<Test>]
    let ``D4 omit-when-default switched off writes at its default in a nested type and in array elements``() =
        let value = {
            D4IgnoreDefaultOffNested.Name = "a"
            Sub = { D4IgnoreDefaultOffInt.Name = "s"; Count = 0 }
            Items = [| { D4IgnoreDefaultOffInt.Name = "i"; Count = 0 } |]
        }
        let doc = ExpressionWriter.CreateWriter<D4IgnoreDefaultOffNested>().Invoke(value)
        Assert.That(doc["Sub"].AsBsonDocument.Contains "Count", Is.True, $"written: {doc}")
        Assert.That(doc["Items"].AsBsonArray[0].AsBsonDocument.Contains "Count", Is.True, $"written: {doc}")

    // ---------------------------------------------------------------------
    // D4 (extension) — behaviour that must hold before and after the fix.
    // ---------------------------------------------------------------------

    [<Test>]
    let ``D4 omit-when-default switched off writes a value that is not the default``() =
        let doc = ExpressionWriter.CreateWriter<D4IgnoreDefaultOffInt>().Invoke({ D4IgnoreDefaultOffInt.Name = "a"; Count = 5 })
        Assert.That(doc["Count"].AsInt32, Is.EqualTo 5)

    [<Test>]
    let ``D4 omit-when-default switched off with omit-when-null still leaves out a null``() =
        let doc, back = roundTrip { D4IgnoreNullAndDefaultOff.Name = null; Count = 1 }
        Assert.That(doc.Contains "Name", Is.False, $"written: {doc}")
        Assert.That(back.Name, Is.EqualTo "zz", "absent field loads the declared default")

    [<Test>]
    let ``D4 omit-when-default switched on explicitly still leaves out a value at its default``() =
        let doc, back = roundTrip { D4IgnoreDefaultOnInt.Name = "a"; Count = 0 }
        Assert.That(doc.Contains "Count", Is.False, $"written: {doc}")
        Assert.That(back.Count, Is.EqualTo 0)

    [<Test>]
    let ``D4 omit-when-default switched off on an option follows the option's own rule``() =
        let docNone, backNone = roundTrip { D4IgnoreDefaultOffOption.Name = "a"; Value = None }
        let docSome, backSome = roundTrip { D4IgnoreDefaultOffOption.Name = "a"; Value = Some "x" }
        Assert.That(docNone.Contains "Value", Is.False, $"written: {docNone}")
        Assert.That(backNone.Value, Is.EqualTo(None: string option))
        Assert.That(docSome["Value"].AsString, Is.EqualTo "x")
        Assert.That(backSome.Value, Is.EqualTo(Some "x"))

    [<Test>]
    let ``D4 absent field with omit-when-default switched off loads the declared default``() =
        let back = ExpressionReader.CreateReader<D4IgnoreDefaultOffString>().Invoke(BsonDocument([ BsonElement("Count", BsonInt32 1) ]))
        Assert.That(back.Name, Is.EqualTo "zz")

    // ---------------------------------------------------------------------
    // D4 (BsonIgnoreIfNull) — "omit when null" switched off.
    // Not in the review findings; found while fixing D4: BsonIgnoreIfNull(false) had the same defect,
    // only the presence of the attribute was checked, so a null was still left out.
    // Intended: switched off, the member behaves as if the attribute were absent (a null is written as null).
    // ---------------------------------------------------------------------

    [<Test>]
    let ``D4 omit-when-null switched off writes null text as an explicit null and it reads back``() =
        let doc, back = roundTrip { D4IgnoreNullOffText.Name = "a"; Value = null }
        Assert.That(doc.Contains "Value", Is.True, $"written: {doc}")
        Assert.That(doc["Value"].IsBsonNull, Is.True)
        Assert.That(back.Value, Is.Null)

    [<Test>]
    let ``D4 omit-when-null switched off writes a null nested object as an explicit null and it reads back``() =
        let doc, back = roundTrip { D4IgnoreNullOffSubObject.Name = "a"; Value = Unchecked.defaultof<_> }
        Assert.That(doc.Contains "Value", Is.True, $"written: {doc}")
        Assert.That(doc["Value"].IsBsonNull, Is.True)
        Assert.That(back.Value, Is.Null)

    [<Test>]
    let ``D4 omit-when-null switched off writes a null binary as an explicit null and it reads back``() =
        let doc, back = roundTrip { D4IgnoreNullOffBinary.Name = "a"; Value = null }
        Assert.That(doc.Contains "Value", Is.True, $"written: {doc}")
        Assert.That(doc["Value"].IsBsonNull, Is.True)
        Assert.That(back.Value, Is.Null)

    [<Test>]
    let ``D4 omit-when-null switched off writes null in a nested type and in array elements``() =
        let value = {
            D4IgnoreNullOffNested.Name = "a"
            Sub = { D4IgnoreNullOffText.Name = "s"; Value = null }
            Items = [| { D4IgnoreNullOffText.Name = "i"; Value = null } |]
        }
        let doc = ExpressionWriter.CreateWriter<D4IgnoreNullOffNested>().Invoke(value)
        Assert.That(doc["Sub"].AsBsonDocument.Contains "Value", Is.True, $"written: {doc}")
        Assert.That(doc["Items"].AsBsonArray[0].AsBsonDocument.Contains "Value", Is.True, $"written: {doc}")

    [<Test>]
    let ``D4 omit-when-null switched off with omit-when-default on writes a null that is not the declared default``() =
        let doc, back = roundTrip { D4IgnoreNullOffDefaultOn.Name = "a"; Value = null }
        Assert.That(doc.Contains "Value", Is.True, $"written: {doc}")
        Assert.That(doc["Value"].IsBsonNull, Is.True)
        Assert.That(back.Value, Is.Null)

    [<Test>]
    let ``D4 omit-when-null and omit-when-default both switched off write null and the default``() =
        let docNull = ExpressionWriter.CreateWriter<D4IgnoreNullOffDefaultOff>().Invoke({ D4IgnoreNullOffDefaultOff.Name = "a"; Value = null })
        let docDefault = ExpressionWriter.CreateWriter<D4IgnoreNullOffDefaultOff>().Invoke({ D4IgnoreNullOffDefaultOff.Name = "a"; Value = "zz" })
        Assert.That(docNull.Contains "Value", Is.True, $"written: {docNull}")
        Assert.That(docNull["Value"].IsBsonNull, Is.True)
        Assert.That(docDefault.Contains "Value", Is.True, $"written: {docDefault}")
        Assert.That(docDefault["Value"].AsString, Is.EqualTo "zz")

    // Must hold before and after the fix.

    [<Test>]
    let ``D4 omit-when-null switched off writes a value``() =
        let doc, back = roundTrip { D4IgnoreNullOffText.Name = "a"; Value = "x" }
        Assert.That(doc["Value"].AsString, Is.EqualTo "x")
        Assert.That(back.Value, Is.EqualTo "x")

    [<Test>]
    let ``D4 omit-when-null switched on explicitly still leaves out a null``() =
        let doc, back = roundTrip { D4IgnoreNullOnText.Name = "a"; Value = null }
        Assert.That(doc.Contains "Value", Is.False, $"written: {doc}")
        Assert.That(back.Value, Is.Null)

    [<Test>]
    let ``D4 omit-when-null switched off with omit-when-default on still leaves out the declared default``() =
        let doc, back = roundTrip { D4IgnoreNullOffDefaultOn.Name = "a"; Value = "zz" }
        Assert.That(doc.Contains "Value", Is.False, $"written: {doc}")
        Assert.That(back.Value, Is.EqualTo "zz", "absent field loads the declared default")

    [<Test>]
    let ``D4 omit-when-null switched off on an option follows the option's own rule``() =
        let docNone, backNone = roundTrip { D4IgnoreNullOffOption.Name = "a"; Value = None }
        let docSome, backSome = roundTrip { D4IgnoreNullOffOption.Name = "a"; Value = Some "x" }
        Assert.That(docNone.Contains "Value", Is.False, $"written: {docNone}")
        Assert.That(backNone.Value, Is.EqualTo(None: string option))
        Assert.That(docSome["Value"].AsString, Is.EqualTo "x")
        Assert.That(backSome.Value, Is.EqualTo(Some "x"))

    [<Test>]
    let ``D4 absent field with omit-when-null switched off loads as null``() =
        let back = ExpressionReader.CreateReader<D4IgnoreNullOffText>().Invoke(onlyName "a")
        Assert.That(back.Value, Is.Null)

    // =====================================================================
    // D5. A by-name enumeration inside an option, nullable or list filters and updates as a number (DEC-000-29)
    //
    // - Today: the value is stored in its declared representation, but typed filters and updates translate it
    //   as a 32-bit integer. A by-name representation there never matches a filter. An update stores a number,
    //   and the read rules then refuse the record.
    // - Intended: US-009.AC-7, US-010.
    // - Where: `ExpressionDocumentSerializer.fs:9,30-91`; `HelperSerializers.fs:34-84`.
    // =====================================================================

    [<Test>]
    let ``D5 filter on by-name enum in an option matches by name``() =
        let b = Render.Filter<D5EnumStringInOption>(fun f -> f.EnumOpt = Some TestEnum.OptionB)
        Assert.That(b["EnumOpt"].BsonType, Is.EqualTo BsonType.String)
        Assert.That(b["EnumOpt"].AsString, Is.EqualTo(nameof TestEnum.OptionB))

    [<Test>]
    let ``D5 filter on by-name enum in a nullable matches by name``() =
        let b = Render.Filter<D5EnumStringInNullable>(fun f -> f.EnumNullable = Nullable TestEnum.OptionB)
        Assert.That(b["EnumNullable"].BsonType, Is.EqualTo BsonType.String)
        Assert.That(b["EnumNullable"].AsString, Is.EqualTo(nameof TestEnum.OptionB))

    [<Test>]
    let ``D5 filter on by-name enum in an array matches by name``() =
        let b = Render.Filter<D5EnumStringInArray>(fun f -> Enumerable.Contains(f.Enums, TestEnum.OptionB))
        Assert.That(b["Enums"].BsonType, Is.EqualTo BsonType.String)
        Assert.That(b["Enums"].AsString, Is.EqualTo(nameof TestEnum.OptionB))

    [<Test>]
    let ``D5 update of by-name enum in an option stores the name and the document loads``() =
        let u = Render.Set<D5EnumStringInOption, TestEnum option>(_.EnumOpt, Some TestEnum.OptionC)
        let stored = u["$set"]["EnumOpt"]
        Assert.That(stored.BsonType, Is.EqualTo BsonType.String)
        let doc = BsonDocument([ BsonElement("Name", BsonString "a"); BsonElement("EnumOpt", stored) ])
        let back = ExpressionReader.CreateReader<D5EnumStringInOption>().Invoke(doc)
        Assert.That(back.EnumOpt, Is.EqualTo(Some TestEnum.OptionC))

    [<Test>]
    let ``D5 update of by-name enum in a nullable stores the name and the document loads``() =
        let u = Render.Set<D5EnumStringInNullable, Nullable<TestEnum>>(_.EnumNullable, Nullable TestEnum.OptionC)
        let stored = u["$set"]["EnumNullable"]
        Assert.That(stored.BsonType, Is.EqualTo BsonType.String)
        let doc = BsonDocument([ BsonElement("Name", BsonString "a"); BsonElement("EnumNullable", stored) ])
        let back = ExpressionReader.CreateReader<D5EnumStringInNullable>().Invoke(doc)
        Assert.That(back.EnumNullable, Is.EqualTo(Nullable TestEnum.OptionC))

    [<Test>]
    let ``D5 update of by-name enum array stores names and the document loads``() =
        let u = Render.Set<D5EnumStringInArray, TestEnum[]>(_.Enums, [| TestEnum.OptionA; TestEnum.OptionC |])
        let stored = u["$set"]["Enums"]
        Assert.That(stored.AsBsonArray[0].BsonType, Is.EqualTo BsonType.String)
        let doc = BsonDocument([ BsonElement("Name", BsonString "a"); BsonElement("Enums", stored) ])
        let back = ExpressionReader.CreateReader<D5EnumStringInArray>().Invoke(doc)
        Assert.That(back.Enums, Is.EqualTo(box [| TestEnum.OptionA; TestEnum.OptionC |]))

    [<Test>]
    let ``D5 filter on by-name enum in a value option matches by name``() =
        let b = Render.Filter<TestClassWithEnumStringOption>(fun f -> f.EnumData = ValueSome TestEnum.OptionB)
        Assert.That(b["EnumData"].BsonType, Is.EqualTo BsonType.String, $"rendered: {b}")
        Assert.That(b["EnumData"].AsString, Is.EqualTo(nameof TestEnum.OptionB))

    [<Test>]
    let ``D5 update of by-name enum in a value option stores the name and the document loads``() =
        let u = Render.Set<TestClassWithEnumStringOption, TestEnum voption>(_.EnumData, ValueSome TestEnum.OptionC)
        let stored = u["$set"]["EnumData"]
        Assert.That(stored.BsonType, Is.EqualTo BsonType.String, $"rendered: {u}")
        let doc = BsonDocument([ BsonElement("Name", BsonString "a"); BsonElement("Count", BsonInt64 1L); BsonElement("EnumData", stored) ])
        let back = ExpressionReader.CreateReader<TestClassWithEnumStringOption>().Invoke(doc)
        Assert.That(back.EnumData, Is.EqualTo(ValueSome TestEnum.OptionC))

    // ---------------------------------------------------------------------
    // D5 (extension) — a typed update stores the same form as storing the whole value (US-010 story),
    // for every declared form, not only by-name.
    // ---------------------------------------------------------------------

    [<Test>]
    let ``D5 update of by-name enum in an option stores the same value as a whole-document write``() =
        let written = ExpressionWriter.CreateWriter<D5EnumStringInOption>().Invoke({ D5EnumStringInOption.Name = "a"; EnumOpt = Some TestEnum.OptionC })
        let u = Render.Set<D5EnumStringInOption, TestEnum option>(_.EnumOpt, Some TestEnum.OptionC)
        Assert.That(u["$set"]["EnumOpt"], Is.EqualTo written["EnumOpt"])

    [<Test>]
    let ``D5 update of by-name enum in a nullable stores the same value as a whole-document write``() =
        let written = ExpressionWriter.CreateWriter<D5EnumStringInNullable>().Invoke({ D5EnumStringInNullable.Name = "a"; EnumNullable = Nullable TestEnum.OptionC })
        let u = Render.Set<D5EnumStringInNullable, Nullable<TestEnum>>(_.EnumNullable, Nullable TestEnum.OptionC)
        Assert.That(u["$set"]["EnumNullable"], Is.EqualTo written["EnumNullable"])

    [<Test>]
    let ``D5 update of by-name enum array stores the same value as a whole-document write``() =
        let values = [| TestEnum.OptionA; TestEnum.OptionC |]
        let written = ExpressionWriter.CreateWriter<D5EnumStringInArray>().Invoke({ D5EnumStringInArray.Name = "a"; Enums = values })
        let u = Render.Set<D5EnumStringInArray, TestEnum[]>(_.Enums, values)
        Assert.That(u["$set"]["Enums"], Is.EqualTo written["Enums"])

    [<Test>]
    let ``D5 filter on 64-bit enum in an option matches by the declared form``() =
        let b = Render.Filter<D5EnumInt64InOption>(fun f -> f.EnumOpt = Some TestEnum.OptionB)
        Assert.That(b["EnumOpt"].BsonType, Is.EqualTo BsonType.Int64, $"rendered: {b}")
        Assert.That(b["EnumOpt"].AsInt64, Is.EqualTo(int64 TestEnum.OptionB))

    [<Test>]
    let ``D5 update of 64-bit enum array stores the same value as a whole-document write and loads``() =
        let values = [| TestEnum.OptionA; TestEnum.OptionC |]
        let written = ExpressionWriter.CreateWriter<D5EnumInt64InArray>().Invoke({ D5EnumInt64InArray.Name = "a"; Enums = values })
        let u = Render.Set<D5EnumInt64InArray, TestEnum[]>(_.Enums, values)
        let stored = u["$set"]["Enums"]
        Assert.That(stored, Is.EqualTo written["Enums"])
        let back = ExpressionReader.CreateReader<D5EnumInt64InArray>().Invoke(BsonDocument([ BsonElement("Name", BsonString "a"); BsonElement("Enums", stored) ]))
        Assert.That(back.Enums, Is.EqualTo(box values))

    // Must hold before and after the fix.

    [<Test>]
    let ``D5 filter on enum with no declared form in an option or array matches as a 32-bit number``() =
        let bOpt = Render.Filter<D5EnumNoFormInOption>(fun f -> f.EnumOpt = Some TestEnum.OptionB)
        let bArr = Render.Filter<D5EnumNoFormInArray>(fun f -> Enumerable.Contains(f.Enums, TestEnum.OptionB))
        Assert.That(bOpt["EnumOpt"].BsonType, Is.EqualTo BsonType.Int32, $"rendered: {bOpt}")
        Assert.That(bOpt["EnumOpt"].AsInt32, Is.EqualTo(int TestEnum.OptionB))
        Assert.That(bArr["Enums"].BsonType, Is.EqualTo BsonType.Int32, $"rendered: {bArr}")
        Assert.That(bArr["Enums"].AsInt32, Is.EqualTo(int TestEnum.OptionB))

    [<Test>]
    let ``D5 update of enum with no declared form in an option stores the same value as a whole-document write``() =
        let written = ExpressionWriter.CreateWriter<D5EnumNoFormInOption>().Invoke({ D5EnumNoFormInOption.Name = "a"; EnumOpt = Some TestEnum.OptionC })
        let u = Render.Set<D5EnumNoFormInOption, TestEnum option>(_.EnumOpt, Some TestEnum.OptionC)
        Assert.That(u["$set"]["EnumOpt"], Is.EqualTo written["EnumOpt"])

    [<Test>]
    let ``D5 filter for an empty by-name enum option matches null``() =
        let b = Render.Filter<D5EnumStringInOption>(fun f -> f.EnumOpt = None)
        Assert.That(b["EnumOpt"].IsBsonNull, Is.True, $"rendered: {b}")

    [<Test>]
    let ``D5 update that empties a by-name enum option stores null and the document loads empty``() =
        let u = Render.Set<D5EnumStringInOption, TestEnum option>(_.EnumOpt, None)
        let stored = u["$set"]["EnumOpt"]
        Assert.That(stored.IsBsonNull, Is.True, $"rendered: {u}")
        let back = ExpressionReader.CreateReader<D5EnumStringInOption>().Invoke(BsonDocument([ BsonElement("Name", BsonString "a"); BsonElement("EnumOpt", stored) ]))
        Assert.That(back.EnumOpt, Is.EqualTo(None: TestEnum option))

    // =====================================================================
    // D6. Typed filters and updates ignore stored names and exclusion (DEC-000-30)
    //
    // - Today: translation targets the property's own name even when a stored name is declared, so filters miss
    //   renamed fields and updates write under the wrong name. Excluded members are translated too.
    // - Intended: US-009.AC-6, US-010.
    // - Where: `ExpressionDocumentSerializer.fs:41-89,117`.
    // =====================================================================

    [<Test>]
    let ``D6 filter on a renamed field targets the stored name``() =
        let b = Render.Filter<D6RenamedAndExcluded>(fun f -> f.Name = "a")
        Assert.That(b.Contains "n", Is.True, $"rendered: {b}")
        Assert.That(b.Contains "Name", Is.False, $"rendered: {b}")

    [<Test>]
    let ``D6 update on a renamed field targets the stored name``() =
        let u = Render.Set<D6RenamedAndExcluded, string>((fun f -> f.Name), "a")
        let set = u["$set"].AsBsonDocument
        Assert.That(set.Contains "n", Is.True, $"rendered: {u}")
        Assert.That(set.Contains "Name", Is.False, $"rendered: {u}")

    [<Test>]
    let ``D6 update on an excluded field stores no value``() =
        Assert.Catch(Action(fun () -> Render.Set<D6RenamedAndExcluded, string>((fun f -> f.Excluded), "x") |> ignore))
        |> ignore

    [<Test>]
    let ``D6 filter on a renamed field targets the stored name on every use of one serializer``() =
        // The first use resolves the member; later uses are answered from the serializer's member cache.
        let ser = ExpressionDocumentSerializer<D6RenamedAndExcluded>() :> IBsonSerializer<D6RenamedAndExcluded>
        let first = Render.FilterWith<D6RenamedAndExcluded>(ser, fun f -> f.Name = "a")
        let second = Render.FilterWith<D6RenamedAndExcluded>(ser, fun f -> f.Name = "b")
        Assert.That(first.Contains "n", Is.True, $"first: {first}")
        Assert.That(second.Contains "n", Is.True, $"second: {second}")
        Assert.That(second.Contains "Name", Is.False, $"second: {second}")

    [<Test>]
    let ``D6 filter targets the same field name as a whole-document write``() =
        let written = ExpressionWriter.CreateWriter<D6RenamedAndExcluded>().Invoke({ D6RenamedAndExcluded.Name = "a"; Count = 1; Excluded = "x" })
        let b = Render.Filter<D6RenamedAndExcluded>(fun f -> f.Name = "a")
        let filterField = b.Names |> Seq.exactlyOne
        Assert.That(written.Contains filterField, Is.True, $"filter: {b}; written: {written}")

    [<Test>]
    let ``D6 update on a renamed field is loaded by the reader``() =
        let u = Render.Set<D6RenamedAndExcluded, string>(_.Name, "changed")
        let doc = BsonDocument([ BsonElement("Count", BsonInt32 1) ]).AddRange(u["$set"].AsBsonDocument)
        let back = ExpressionReader.CreateReader<D6RenamedAndExcluded>().Invoke(doc)
        Assert.That(back.Name, Is.EqualTo "changed", $"document: {doc}")

    [<Test>]
    let ``D6 sort on a renamed field targets the stored name``() =
        let s = Render.SortAscending<D6RenamedAndExcluded>(fun f -> f.Name :> obj)
        Assert.That(s.Contains "n", Is.True, $"rendered: {s}")
        Assert.That(s.Contains "Name", Is.False, $"rendered: {s}")

    [<Test>]
    let ``D6 filter on a renamed by-name enum targets the stored name in its declared form``() =
        let b = Render.Filter<D6RenamedKinds>(fun f -> f.Kind = TestEnum.OptionB)
        Assert.That(b.Contains "e", Is.True, $"rendered: {b}")
        Assert.That(b["e"].AsString, Is.EqualTo(nameof TestEnum.OptionB))

    [<Test>]
    let ``D6 filter and update on a renamed option target the stored name``() =
        let b = Render.Filter<D6RenamedKinds>(fun f -> f.Opt = Some "x")
        let u = Render.Set<D6RenamedKinds, string option>(_.Opt, Some "y")
        Assert.That(b.Contains "o", Is.True, $"rendered: {b}")
        Assert.That(u["$set"].AsBsonDocument.Contains "o", Is.True, $"rendered: {u}")

    [<Test>]
    let ``D6 filter on an excluded field is refused``() =
        // An excluded member has no stored form, so there is nothing a filter on it could match.
        Assert.Catch(Action(fun () -> Render.Filter<D6RenamedAndExcluded>(fun f -> f.Excluded = "x") |> ignore))
        |> ignore

    // Must hold before and after the fix.

    [<Test>]
    let ``D6 filter on a member with no stored name targets the property name``() =
        let b = Render.Filter<D6RenamedKinds>(fun f -> f.Plain = 3)
        Assert.That(b["Plain"].AsInt32, Is.EqualTo 3)

    [<Test>]
    let ``D6 filter on a member with a blank stored name targets the property name``() =
        let b = Render.Filter<D6RenamedKinds>(fun f -> f.Blank = "x")
        Assert.That(b["Blank"].AsString, Is.EqualTo "x")

    // =====================================================================
    // D7. Member resolution is not safe for simultaneous first use (DEC-000-33)
    //
    // - Today: the per-serializer member map has no lock, and its insert refuses duplicates. Requests that first
    //   use members of one type at the same moment can fail with "An item with the same key has already been
    //   added" or a concurrent-modification error. Occasionally the member, or every unresolved member of the
    //   type, keeps failing until restart.
    // - Intended: US-009.AC-8, US-010.AC-6.
    // - Exposure: latent; never seen in logs.
    // - Where: `ExpressionDocumentSerializer.fs:19,40,46,56,68,80,88,113`.
    // =====================================================================

    [<Test>]
    let ``D7 simultaneous first use of members on one serializer always succeeds``() =
        let names = typeof<D7ManyFields>.GetProperties() |> Array.map (fun p -> p.Name)
        let threads = 8
        let iterations = 200
        let failures = ConcurrentBag<string>()

        for _ in 1 .. iterations do
            let ser = ExpressionDocumentSerializer<D7ManyFields>() :> IBsonDocumentSerializer
            use barrier = new Barrier(threads)
            let workers =
                [| for _ in 1 .. threads ->
                    Thread(fun () ->
                        barrier.SignalAndWait()
                        for name in names do
                            try
                                let ok, info = ser.TryGetMemberSerializationInfo(name)
                                if not ok || isNull info then failures.Add($"{name}: not resolved")
                            with ex ->
                                failures.Add($"{name}: {ex.GetType().Name}: {ex.Message}")) |]
            workers |> Array.iter (fun t -> t.Start())
            workers |> Array.iter (fun t -> t.Join())

        Assert.That(failures, Is.Empty, $"{failures.Count} failures, first: {failures |> Seq.tryHead}")

    // ---------------------------------------------------------------------
    // D7 (extension) — the same race through the paths services use, and what must hold after it.
    // ---------------------------------------------------------------------

    /// Runs one round per iteration: newRound() creates fresh state (a new serializer, so every member is unresolved)
    /// and returns the per-thread work; all threads are released together by a barrier. Returns every failure seen.
    let private race (iterations: int) (threads: int) (newRound: unit -> (int -> unit)) : string list =
        let failures = ConcurrentBag<string>()
        for _ in 1 .. iterations do
            let work = newRound ()
            use barrier = new Barrier(threads)
            let workers =
                [| for t in 0 .. threads - 1 ->
                    Thread(fun () ->
                        barrier.SignalAndWait()
                        try work t
                        with ex -> failures.Add($"{ex.GetType().Name}: {ex.Message}")) |]
            workers |> Array.iter (fun t -> t.Start())
            workers |> Array.iter (fun t -> t.Join())
        List.ofSeq failures

    let private newD7Serializer () = ExpressionDocumentSerializer<D7ManyFields>() :> IBsonSerializer<D7ManyFields>

    let private failureSummary (failures: string list) =
        $"{failures.Length} failures, first: {List.tryHead failures}"

    [<Test>]
    let ``D7 simultaneous first filters on one serializer always succeed``() =
        let filters : (IBsonSerializer<D7ManyFields> -> BsonDocument) list = [
            fun s -> Render.FilterWith<D7ManyFields>(s, fun f -> f.F01 = "a")
            fun s -> Render.FilterWith<D7ManyFields>(s, fun f -> f.F02 = "b")
            fun s -> Render.FilterWith<D7ManyFields>(s, fun f -> f.F05 = 5)
            fun s -> Render.FilterWith<D7ManyFields>(s, fun f -> f.F06 > 6)
            fun s -> Render.FilterWith<D7ManyFields>(s, fun f -> f.F09 = 9L)
            fun s -> Render.FilterWith<D7ManyFields>(s, fun f -> f.F10 < 10L)
            fun s -> Render.FilterWith<D7ManyFields>(s, fun f -> f.F13 = true)
            fun s -> Render.FilterWith<D7ManyFields>(s, fun f -> f.F15 = 1.5)
        ]
        let failures =
            race 100 8 (fun () ->
                let ser = newD7Serializer ()
                fun _ -> for render in filters do render ser |> ignore)
        Assert.That(failures, Is.Empty, failureSummary failures)

    [<Test>]
    let ``D7 simultaneous first updates on one serializer always succeed``() =
        let updates : (IBsonSerializer<D7ManyFields> -> BsonValue) list = [
            fun s -> Render.SetWith<D7ManyFields, string>(s, _.F01, "a")
            fun s -> Render.SetWith<D7ManyFields, string>(s, _.F02, "b")
            fun s -> Render.SetWith<D7ManyFields, int32>(s, _.F05, 5)
            fun s -> Render.SetWith<D7ManyFields, int32>(s, _.F06, 6)
            fun s -> Render.SetWith<D7ManyFields, int64>(s, _.F09, 9L)
            fun s -> Render.SetWith<D7ManyFields, int64>(s, _.F10, 10L)
            fun s -> Render.SetWith<D7ManyFields, bool>(s, _.F13, true)
            fun s -> Render.SetWith<D7ManyFields, float>(s, _.F15, 1.5)
        ]
        let failures =
            race 100 8 (fun () ->
                let ser = newD7Serializer ()
                fun _ -> for render in updates do render ser |> ignore)
        Assert.That(failures, Is.Empty, failureSummary failures)

    [<Test>]
    let ``D7 threads racing on first use all get the same answer for a member``() =
        let names = typeof<D7ManyFields>.GetProperties() |> Array.map (fun p -> p.Name)
        let mismatches = ConcurrentBag<string>()
        let failures =
            race 200 8 (fun () ->
                let ser = ExpressionDocumentSerializer<D7ManyFields>() :> IBsonDocumentSerializer
                let answers = ConcurrentBag<string * BsonSerializationInfo>()
                let pending = ref 8
                fun _ ->
                    for name in names do
                        let ok, info = ser.TryGetMemberSerializationInfo(name)
                        if ok then answers.Add((name, info))
                    // the last thread to finish compares the answers of the round
                    if Interlocked.Decrement(&pending.contents) = 0 then
                        for name, infos in answers |> Seq.groupBy fst do
                            let serializers = infos |> Seq.map (snd >> _.Serializer) |> Seq.distinctBy LanguagePrimitives.PhysicalHash |> Seq.length
                            let elementNames = infos |> Seq.map (snd >> _.ElementName) |> Seq.distinct |> Seq.length
                            if serializers > 1 || elementNames > 1 then
                                mismatches.Add($"{name}: {serializers} serializer instances, {elementNames} element names"))
        Assert.That(failures, Is.Empty, failureSummary failures)
        Assert.That(mismatches, Is.Empty, $"{mismatches.Count} mismatches, first: {mismatches |> Seq.tryHead}")

    [<Test>]
    let ``D7 after a race on first use every member still resolves``() =
        let names = typeof<D7ManyFields>.GetProperties() |> Array.map (fun p -> p.Name)
        let afterwards = ConcurrentBag<string>()
        let _ =
            race 200 8 (fun () ->
                let ser = ExpressionDocumentSerializer<D7ManyFields>() :> IBsonDocumentSerializer
                let pending = ref 8
                fun _ ->
                    try
                        for name in names do ser.TryGetMemberSerializationInfo(name) |> ignore
                    finally
                        // the last thread to finish checks the serializer once the race is over
                        if Interlocked.Decrement(&pending.contents) = 0 then
                            for name in names do
                                try
                                    let ok, info = ser.TryGetMemberSerializationInfo(name)
                                    if not ok || isNull info then afterwards.Add($"{name}: not resolved after the race")
                                with ex ->
                                    afterwards.Add($"{name}: {ex.GetType().Name} after the race: {ex.Message}"))
        Assert.That(afterwards, Is.Empty, $"{afterwards.Count} failures after the race, first: {afterwards |> Seq.tryHead}")

    // Must hold before and after the fix.

    [<Test>]
    let ``D7 a member resolved once is answered from the cache with the same serializer``() =
        let ser = ExpressionDocumentSerializer<D7ManyFields>() :> IBsonDocumentSerializer
        let _, first = ser.TryGetMemberSerializationInfo("F01")
        let _, second = ser.TryGetMemberSerializationInfo("F01")
        Assert.That(Object.ReferenceEquals(first.Serializer, second.Serializer), Is.True)
        Assert.That(second.ElementName, Is.EqualTo "F01")

    // =====================================================================
    // D8. Reading a single optional member fails on an explicit null (DEC-000-35)
    //
    // - Today: the option and value-option wrappers return empty for a stored null but leave the null unread,
    //   so the next read on the reader fails. This affects a typed projection of that member, a shape built from
    //   several members, distinct values and an aggregation stage that selects it. Whole-document reads are not
    //   affected.
    // - Intended: US-004.
    // - Exposure: latent; no service reads single members this way.
    // - Where: `HelperSerializers.fs:45-48,55-58,71-74,81-84`.
    //
    // The store reads one member through the member serializer, then keeps reading the same reader.
    // =====================================================================

    let private readMemberThenNext (serializer: IBsonSerializer) (memberName: string) : obj * int =
        let doc = BsonDocument([ BsonElement(memberName, BsonNull.Value); BsonElement("Next", BsonInt32 7) ])
        use reader = new BsonDocumentReader(doc)
        reader.ReadStartDocument()
        reader.ReadName(memberName)
        let value = serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), BsonDeserializationArgs())
        reader.ReadName("Next")
        let next = reader.ReadInt32()
        value, next

    let private memberSerializer<'t> (memberName: string) =
        let ser = ExpressionDocumentSerializer<'t>() :> IBsonDocumentSerializer
        let ok, info = ser.TryGetMemberSerializationInfo(memberName)
        Assert.That(ok, Is.True, "member resolves")
        info.Serializer

    [<Test>]
    let ``D8 option member read of an explicit null returns empty and leaves the reader usable``() =
        let value, next = readMemberThenNext (memberSerializer<TestFlatClassWithOptionValue> "OptString") "OptString"
        Assert.That((value :?> string option), Is.EqualTo(None: string option))
        Assert.That(next, Is.EqualTo 7)

    [<Test>]
    let ``D8 value-option member read of an explicit null returns empty and leaves the reader usable``() =
        let value, next = readMemberThenNext (memberSerializer<TestFlatClassWithVOptionString> "OptString") "OptString"
        Assert.That((value :?> string voption), Is.EqualTo(ValueNone: string voption))
        Assert.That(next, Is.EqualTo 7)

    [<Test>]
    let ``D8 option wrapper typed read of an explicit null leaves the reader usable``() =
        let ser = OptionSerializer<string>(Serializers.StringSerializer()) :> IBsonSerializer<string option>
        let doc = BsonDocument([ BsonElement("a", BsonNull.Value); BsonElement("Next", BsonInt32 7) ])
        use reader = new BsonDocumentReader(doc)
        reader.ReadStartDocument()
        reader.ReadName("a")
        let value = ser.Deserialize(BsonDeserializationContext.CreateRoot(reader), BsonDeserializationArgs())
        reader.ReadName("Next")
        Assert.That(value, Is.EqualTo(None: string option))
        Assert.That(reader.ReadInt32(), Is.EqualTo 7)

    [<Test>]
    let ``D8 value-option wrapper typed read of an explicit null leaves the reader usable``() =
        let ser = ValueOptionSerializer<string>(Serializers.StringSerializer()) :> IBsonSerializer<string voption>
        let doc = BsonDocument([ BsonElement("a", BsonNull.Value); BsonElement("Next", BsonInt32 7) ])
        use reader = new BsonDocumentReader(doc)
        reader.ReadStartDocument()
        reader.ReadName("a")
        let value = ser.Deserialize(BsonDeserializationContext.CreateRoot(reader), BsonDeserializationArgs())
        reader.ReadName("Next")
        Assert.That(value, Is.EqualTo(ValueNone: string voption))
        Assert.That(reader.ReadInt32(), Is.EqualTo 7)

    // ---------------------------------------------------------------------
    // D8 (extension) — the paths the store reads single members through, and what must hold before and after the fix.
    // ---------------------------------------------------------------------

    /// Reads one stored value through a member serializer, then keeps reading the same reader (as the store does).
    let private readValueThenNext (serializer: IBsonSerializer) (stored: BsonValue) : obj * int =
        let doc = BsonDocument([ BsonElement("a", stored); BsonElement("Next", BsonInt32 7) ])
        use reader = new BsonDocumentReader(doc)
        reader.ReadStartDocument()
        reader.ReadName("a")
        let value = serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), BsonDeserializationArgs())
        reader.ReadName("Next")
        value, reader.ReadInt32()

    [<Test>]
    let ``D8 explicit null as the last element leaves the reader able to close the document``() =
        let ser = memberSerializer<TestFlatClassWithOptionValue> "OptString"
        use reader = new BsonDocumentReader(BsonDocument([ BsonElement("OptString", BsonNull.Value) ]))
        reader.ReadStartDocument()
        reader.ReadName("OptString")
        let value = ser.Deserialize(BsonDeserializationContext.CreateRoot(reader), BsonDeserializationArgs())
        reader.ReadEndDocument()
        Assert.That((value :?> string option), Is.EqualTo(None: string option))

    [<Test>]
    let ``D8 distinct-style read of option values with explicit nulls succeeds``() =
        // Distinct returns the values as an array, read element by element with the member serializer (US-004.AC-9).
        let item = memberSerializer<TestFlatClassWithOptionValue> "OptString" :?> IBsonSerializer<string option>
        let arraySer = Serializers.ArraySerializer<string option>(item) :> IBsonSerializer<string option[]>
        let doc = BsonDocument([ BsonElement("values", BsonArray([ BsonNull.Value :> BsonValue; BsonString "a"; BsonNull.Value ])) ])
        use reader = new BsonDocumentReader(doc)
        reader.ReadStartDocument()
        reader.ReadName("values")
        let values = arraySer.Deserialize(BsonDeserializationContext.CreateRoot(reader), BsonDeserializationArgs())
        reader.ReadEndDocument()
        Assert.That(values, Is.EqualTo(box [| None; Some "a"; None |]))

    [<Test>]
    let ``D8 projection of a single option member reads an explicit null as empty``() =
        // A query that reads back only that field (US-004.AC-8). The store returns a single projected value wrapped
        // as { _v: <value> } and reads it with the member serializer.
        let projectionSer = Render.Project<TestFlatClassWithOptionValue, string option>(fun x -> x.OptString)
        use reader = new BsonDocumentReader(BsonDocument([ BsonElement("_v", BsonNull.Value) ]))
        let value = projectionSer.Deserialize(BsonDeserializationContext.CreateRoot(reader), BsonDeserializationArgs())
        Assert.That(value, Is.EqualTo(None: string option))

    [<Test>]
    let ``D8 by-name enum option member read of an explicit null returns empty and leaves the reader usable``() =
        let value, next = readValueThenNext (memberSerializer<D5EnumStringInOption> "EnumOpt") BsonNull.Value
        Assert.That((value :?> TestEnum option), Is.EqualTo(None: TestEnum option))
        Assert.That(next, Is.EqualTo 7)

    [<Test>]
    let ``D8 exported nullable wrapper read of an explicit null leaves the reader usable``() =
        // Same defect in the library's own NullableSerializer (found while fixing D8); the member resolver uses the
        // driver's NullableSerializer instead (DEC-000-29), so only direct users of the exported one were exposed.
        let ser =
            Iezious.Libs.BSONExpressionSerializer.NullableSerializer<int32>(Serializers.Int32Serializer())
            :> IBsonSerializer<Nullable<int32>>
        let value, next = readValueThenNext ser BsonNull.Value
        Assert.That(value, Is.Null)
        Assert.That(next, Is.EqualTo 7)

    // Must hold before and after the fix.

    [<Test>]
    let ``D8 option member read of a value returns it and leaves the reader usable``() =
        let value, next = readValueThenNext (memberSerializer<TestFlatClassWithOptionValue> "OptString") (BsonString "x")
        Assert.That((value :?> string option), Is.EqualTo(Some "x"))
        Assert.That(next, Is.EqualTo 7)

    [<Test>]
    let ``D8 value-option member read of a value returns it and leaves the reader usable``() =
        let value, next = readValueThenNext (memberSerializer<TestFlatClassWithVOptionString> "OptString") (BsonString "x")
        Assert.That((value :?> string voption), Is.EqualTo(ValueSome "x"))
        Assert.That(next, Is.EqualTo 7)

    [<Test>]
    let ``D8 by-name enum option member read of a name returns the value``() =
        let value, next = readValueThenNext (memberSerializer<D5EnumStringInOption> "EnumOpt") (BsonString "OptionB")
        Assert.That((value :?> TestEnum option), Is.EqualTo(Some TestEnum.OptionB))
        Assert.That(next, Is.EqualTo 7)

    [<Test>]
    let ``D8 nullable member read of an explicit null returns empty and leaves the reader usable``() =
        let value, next = readValueThenNext (memberSerializer<TestClassWithNullable> "CountNullable") BsonNull.Value
        Assert.That(value, Is.Null)
        Assert.That(next, Is.EqualTo 7)

    [<Test>]
    let ``D8 option and value-option wrappers write an empty value as null``() =
        let write (ser: IBsonSerializer) (value: obj) =
            let doc = BsonDocument()
            use writer = new BsonDocumentWriter(doc)
            writer.WriteStartDocument()
            writer.WriteName("a")
            ser.Serialize(BsonSerializationContext.CreateRoot(writer), BsonSerializationArgs(), value)
            writer.WriteEndDocument()
            doc
        let optDoc = write (memberSerializer<TestFlatClassWithOptionValue> "OptString") (box (None: string option))
        let voptDoc = write (memberSerializer<TestFlatClassWithVOptionString> "OptString") (box (ValueNone: string voption))
        Assert.That(optDoc["a"].IsBsonNull, Is.True, $"written: {optDoc}")
        Assert.That(voptDoc["a"].IsBsonNull, Is.True, $"written: {voptDoc}")
