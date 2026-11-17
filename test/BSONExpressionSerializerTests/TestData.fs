namespace BSONExpressionSerializerTests

open System
open System.Collections.Generic
open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes

[<CLIMutable>]
type TestFlatClass = {
    Name: string
    CountInt: int32
    Date: System.DateTime
}

[<CLIMutable>]
type TestFlatDoublesClass = {
    Name: string
    Count: int32
    CountLong: int64
    Value: float
    Is : bool
    Date: System.DateTime
}

[<CLIMutable>]
type TestFlatWithDefaultValuesClass = {
    [<BsonIgnoreIfDefault>]
    Name: string
    [<BsonIgnoreIfDefault; BsonDefaultValue(0)>]
    Count: int32
    [<BsonIgnoreIfDefault; BsonDefaultValue(false)>]
    Is : bool
}

[<CLIMutable>]
type TestFlatWithDefaultNull = {
    [<BsonIgnoreIfNull>]
    Name: string
    Count: int32
}

[<CLIMutable>]
type TestFlatWithDefaultAndNull = {
    [<BsonIgnoreIfNull; BsonIgnoreIfDefault; BsonDefaultValue("zz")>]
    Name: string
    Count: int32
}

[<CLIMutable>]
type TestFlatClassWithObjectID = {
    _id: ObjectId
    Name: string
    Count: int32
}

[<CLIMutable>]
type TestFlatClassWithBsonId = {
    _id: BsonObjectId
    Name: string
    Count: int32
}

[<CLIMutable>]
type TestFlatClassWithArrayOfStringValues = {
    Name: string
    Count: int32
    SubArray: string[]
}

[<CLIMutable>]
type TestFlatClassWithArrayOfIntValues = {
    Name: string
    Count: int64
    SubArray: int32[]
}

[<CLIMutable>]
type TestFlatClassWithOptionValue = {
    _id: ObjectId
    Name: string
    Count: int64
    OptString: string option
}

[<CLIMutable>]
type TestFlatClassWithVOptionString = {
    Name: string
    Count: int64
    OptString: string voption
}

[<CLIMutable>]
type TestFlatClassWithVOptionDate = {
    Name: string
    Count: int64
    OptDate: System.DateTime voption
}

[<CLIMutable>]
type TestFlatClassWithVOptionLong = {
    Name: string
    CountOpt: int64 voption
}

[<CLIMutable>]
type TestClassWithSubObject = {
    _id: ObjectId
    Name: string
    Count: int64
    SubObject: TestFlatClass
}

[<CLIMutable>]
type TestClassWithSubObjectOption = {
    _id: ObjectId
    Name: string
    Count: int64
    SubObjectOption: TestFlatClass option
}

[<CLIMutable>]
type TestFlatClassWithArrayOfObjects = {
    _id: ObjectId
    Name: string
    Count: int64
    SubArray: TestFlatClass[]
}

type TestEnum =
    | OptionA = 1
    | OptionB = 2
    | OptionC = 3
    
[<CLIMutable>]
type TestClassWithEnumInt = {
    _id: BsonObjectId
    Name: string
    Count: int64
    EnumData: TestEnum
}

[<CLIMutable>]
type TestClassWithEnumString = {
    _id: BsonObjectId
    Name: string
    Count: int64
    [<BsonRepresentation(BsonType.String)>]
    EnumData: TestEnum
}
    
[<CLIMutable>]
type TestClassWithEnumStringOption = {
    _id: BsonObjectId
    Name: string
    Count: int64
    [<BsonRepresentation(BsonType.String)>]
    EnumData: TestEnum voption
}

[<CLIMutable>]
type TestClassWithStringDictionary = {
    Name: string
    Dict: Dictionary<string, string> 
}    

[<CLIMutable>]
type TestClassWithStringDictionaryAndDefaultValue = {
    Name: string
    [<BsonIgnoreIfDefault>]
    Dict: Dictionary<string, string> 
}
       
[<CLIMutable>]
type TestClassWithNullable = {
    Name: string
    CountNullable: Nullable<int32> 
    DateNullable: Nullable<DateTime> 
}
        

[<CLIMutable>]
type TestClassWithIntDictionary = {
    Name: string
    Dict: Dictionary<string, int32> 
}
            

[<CLIMutable>]
type TestClassWithLongDictionary = {
    Name: string
    Dict: Dictionary<string, int64> 
}

[<CLIMutable>]
type TestClassWithSubClassDictionary = {
    Name: string
    Dict: Dictionary<string, TestFlatClass> 
}
    
[<CLIMutable>]
type TestClassWithBsonDocument = {
    Name: string
    Payload: BsonDocument 
}
        
[<CLIMutable>]
type TestClassWithBsonDocumentWithDefault = {
    Name: string
    [<BsonIgnoreIfNull>]
    Payload: BsonDocument 
}
    
[<CLIMutable>]
type TestClassBinaryData = {
    Name: string
    [<BsonIgnoreIfNull>]
    Payload: byte[] 
}
    
[<CLIMutable>]
type TestClassWithStaticProperty = {
    Name: string
    Count: int32
} with static member Projection = {| One=true; Second = true |}    

// ===== Fixture types for KnownDefectTests.fs — one group per defect (D1–D8) =====

// ---- D1: two members mapped to one stored name ----

[<CLIMutable>]
type D1TwoMembersOneStoredName = {
    [<BsonElement("x")>]
    A: string
    [<BsonElement("x")>]
    B: string
}

[<CLIMutable>]
type D1StoredNameClashesWithPropertyName = {
    Name: string
    [<BsonElement("Name")>]
    Other: string
}

// ---- D2: null text at any depth (reuses TestFlatClass, TestClassWithSubObject,
//      TestFlatClassWithArrayOfStringValues, TestClassWithStringDictionary) ----

// D2 (omission): a reference member with BsonIgnoreIfDefault and no value is left out of the document
// (smaller documents; newly added fields stay absent in old and new data alike). One type per reference kind,
// so a kind that fails at creation does not hide the others.

[<CLIMutable>]
type D2OmitNullText = {
    Name: string
    [<BsonIgnoreIfDefault>]
    Value: string
}

[<CLIMutable>]
type D2OmitNullSubObject = {
    Name: string
    [<BsonIgnoreIfDefault>]
    Value: TestFlatClass
}

[<CLIMutable>]
type D2OmitNullTextArray = {
    Name: string
    [<BsonIgnoreIfDefault>]
    Value: string[]
}

[<CLIMutable>]
type D2OmitNullSubObjectArray = {
    Name: string
    [<BsonIgnoreIfDefault>]
    Value: TestFlatClass[]
}

[<CLIMutable>]
type D2OmitNullDictionary = {
    Name: string
    [<BsonIgnoreIfDefault>]
    Value: Dictionary<string, string>
}

[<CLIMutable>]
type D2OmitNullBsonDocument = {
    Name: string
    [<BsonIgnoreIfDefault>]
    Value: BsonDocument
}

[<CLIMutable>]
type D2OmitNullBinary = {
    Name: string
    [<BsonIgnoreIfDefault>]
    Value: byte[]
}

// The same omission one level down and inside array elements.
[<CLIMutable>]
type D2OmitNullTextNested = {
    Name: string
    Sub: D2OmitNullText
    Items: D2OmitNullText[]
}

// D2 (absence): members with no attributes, read from documents written before the members existed.
// Every member that can hold "no value" must load as null / empty when its field is absent.

[<CLIMutable>]
type D2AbsentFields = {
    Name: string
    Text: string
    Sub: TestFlatClass
    Texts: string[]
    Subs: TestFlatClass[]
    Dict: Dictionary<string, string>
    Doc: BsonDocument
    Payload: byte[]
    CountNullable: Nullable<int32>
    TextOpt: string option
    TextVOpt: string voption
}

[<CLIMutable>]
type D2AbsentFieldsNested = {
    Name: string
    Sub: D2AbsentFields
    Items: D2AbsentFields[]
}

// ---- D3: null binary with no omit choice ----

[<CLIMutable>]
type D3BinaryNoOmit = {
    Name: string
    Payload: byte[]
}

[<CLIMutable>]
type D3ArrayOfBinary = {
    Name: string
    Payloads: byte[][]
}

// D3 at depth: binary with no attribute in a nested type, in array elements and as dictionary values.
[<CLIMutable>]
type D3BinaryNested = {
    Name: string
    Sub: D3BinaryNoOmit
    Items: D3BinaryNoOmit[]
}

[<CLIMutable>]
type D3BinaryDictionary = {
    Name: string
    Dict: Dictionary<string, byte[]>
}

// D3 omission at depth: binary with BsonIgnoreIfDefault (D2OmitNullBinary) in a nested type and in array elements.
// Omission with BsonIgnoreIfNull at the top level reuses TestClassBinaryData.
[<CLIMutable>]
type D3OmitNullBinaryNested = {
    Name: string
    Sub: D2OmitNullBinary
    Items: D2OmitNullBinary[]
}

// ---- D4: "omit when default" switched off ----

[<CLIMutable>]
type D4IgnoreDefaultOffInt = {
    Name: string
    [<BsonIgnoreIfDefault(false); BsonDefaultValue(0)>]
    Count: int32
}

[<CLIMutable>]
type D4IgnoreDefaultOffString = {
    [<BsonIgnoreIfDefault(false); BsonDefaultValue("zz")>]
    Name: string
    Count: int32
}

[<CLIMutable>]
type D4IgnoreNullAndDefaultOff = {
    [<BsonIgnoreIfNull; BsonIgnoreIfDefault(false); BsonDefaultValue("zz")>]
    Name: string
    Count: int32
}

// ---- D5: by-name enumeration inside option / nullable / array ----

[<CLIMutable>]
type D5EnumStringInOption = {
    Name: string
    [<BsonRepresentation(BsonType.String)>]
    EnumOpt: TestEnum option
}

[<CLIMutable>]
type D5EnumStringInNullable = {
    Name: string
    [<BsonRepresentation(BsonType.String)>]
    EnumNullable: Nullable<TestEnum>
}

[<CLIMutable>]
type D5EnumStringInArray = {
    Name: string
    [<BsonRepresentation(BsonType.String)>]
    Enums: TestEnum[]
}

// ---- D6: stored names and exclusion in typed filters / updates ----

[<CLIMutable>]
type D6RenamedAndExcluded = {
    [<BsonElement("n")>]
    Name: string
    Count: int32
    [<BsonIgnore>]
    Excluded: string
}

// ---- D7: concurrent first use of members ----

[<CLIMutable>]
type D7ManyFields = {
    F01: string; F02: string; F03: string; F04: string
    F05: int32;  F06: int32;  F07: int32;  F08: int32
    F09: int64;  F10: int64;  F11: int64;  F12: int64
    F13: bool;   F14: bool;   F15: float;  F16: float
}

// ---- D8: option / value-option member read of an explicit null
//      (reuses TestFlatClassWithOptionValue, TestFlatClassWithVOptionString) ----
