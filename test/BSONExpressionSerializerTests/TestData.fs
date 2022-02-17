namespace BSONExpressionSerializerTests

open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes

[<CLIMutable>]
type TestFlatClass = {
    Name: string
    Count: int32
    Date: System.DateTime
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
    _id: BsonObjectId
    Name: string
    Count: int32
    SubArray: string[]
}

[<CLIMutable>]
type TestFlatClassWithArrayOfIntValues = {
    _id: BsonObjectId
    Name: string
    Count: int64
    SubArray: int32[]
}

[<CLIMutable>]
type TestFlatClassWithOptionValue = {
    _id: BsonObjectId
    Name: string
    Count: int64
    OptString: string option
}

[<CLIMutable>]
type TestFlatClassWithVOptionString = {
    _id: BsonObjectId
    Name: string
    Count: int64
    OptString: string voption
}

[<CLIMutable>]
type TestFlatClassWithVOptionDate = {
    _id: BsonObjectId
    Name: string
    Count: int64
    OptDate: System.DateTime voption
}
[<CLIMutable>]
type TestFlatClassWithVOptionInt = {
    _id: BsonObjectId
    Name: string
    CountOpt: int64 voption
    Date: System.DateTime 
}

[<CLIMutable>]
type TestClassWithSubObject = {
    _id: BsonObjectId
    Name: string
    Count: int64
    SubObject: TestFlatClass
}

[<CLIMutable>]
type TestClassWithSubObjectOption = {
    _id: BsonObjectId
    Name: string
    Count: int64
    SubObjectOption: TestFlatClass option
}

[<CLIMutable>]
type TestFlatClassWithArrayOfObjects = {
    _id: BsonObjectId
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
    