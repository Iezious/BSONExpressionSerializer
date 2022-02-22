namespace BSONExpressionSerializerTests

open System
open System.Collections.Generic
open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes

[<CLIMutable>]
type TestFlatClass = {
    Name: string
    Count: int32
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
    
    