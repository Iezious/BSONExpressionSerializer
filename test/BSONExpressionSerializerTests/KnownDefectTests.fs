namespace BSONExpressionSerializerTests

// Reproduction tests for docs/plans/tmp.defects.md (D1–D8).
// Every test asserts the INTENDED behaviour (the cited story / acceptance criterion),
// so each one is expected to FAIL against the current library code.
// The defect descriptions are copied verbatim from docs/plans/tmp.defects.md.
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

[<TestFixture>]
module KnownDefectTests =

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
        let u = Render.Set<D5EnumStringInOption, TestEnum option>((fun f -> f.EnumOpt), Some TestEnum.OptionC)
        let stored = u["$set"]["EnumOpt"]
        Assert.That(stored.BsonType, Is.EqualTo BsonType.String)
        let doc = BsonDocument([ BsonElement("Name", BsonString "a"); BsonElement("EnumOpt", stored) ])
        let back = ExpressionReader.CreateReader<D5EnumStringInOption>().Invoke(doc)
        Assert.That(back.EnumOpt, Is.EqualTo(Some TestEnum.OptionC))

    [<Test>]
    let ``D5 update of by-name enum in a nullable stores the name and the document loads``() =
        let u = Render.Set<D5EnumStringInNullable, Nullable<TestEnum>>((fun f -> f.EnumNullable), Nullable TestEnum.OptionC)
        let stored = u["$set"]["EnumNullable"]
        Assert.That(stored.BsonType, Is.EqualTo BsonType.String)
        let doc = BsonDocument([ BsonElement("Name", BsonString "a"); BsonElement("EnumNullable", stored) ])
        let back = ExpressionReader.CreateReader<D5EnumStringInNullable>().Invoke(doc)
        Assert.That(back.EnumNullable, Is.EqualTo(Nullable TestEnum.OptionC))

    [<Test>]
    let ``D5 update of by-name enum array stores names and the document loads``() =
        let u = Render.Set<D5EnumStringInArray, TestEnum[]>((fun f -> f.Enums), [| TestEnum.OptionA; TestEnum.OptionC |])
        let stored = u["$set"]["Enums"]
        Assert.That(stored.AsBsonArray[0].BsonType, Is.EqualTo BsonType.String)
        let doc = BsonDocument([ BsonElement("Name", BsonString "a"); BsonElement("Enums", stored) ])
        let back = ExpressionReader.CreateReader<D5EnumStringInArray>().Invoke(doc)
        Assert.That(back.Enums, Is.EqualTo(box [| TestEnum.OptionA; TestEnum.OptionC |]))

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
