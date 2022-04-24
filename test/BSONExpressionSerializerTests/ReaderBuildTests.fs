namespace BSONExpressionSerializerTests

open System
open System.Linq.Expressions
open BSONExpressionSerializerTests
open Iezious.Libs.BSONExpressionSerializer
open MongoDB.Bson
open NUnit.Framework


[<TestFixture>]
module ReaderBuildTests =
    
    [<TestCase(typeof<TestFlatClass>)>]
    [<TestCase(typeof<TestFlatClassWithObjectID>)>]
    [<TestCase(typeof<TestFlatClassWithBsonId>)>]
    [<TestCase(typeof<TestFlatClassWithArrayOfStringValues>)>]
    [<TestCase(typeof<TestFlatClassWithArrayOfIntValues>)>]
    [<TestCase(typeof<TestClassWithSubObject>)>]
    [<TestCase(typeof<TestFlatClassWithArrayOfObjects>)>]
    [<TestCase(typeof<TestFlatClassWithOptionValue>)>]
    [<TestCase(typeof<TestFlatClassWithVOptionString>)>]
    [<TestCase(typeof<TestFlatClassWithVOptionDate>)>]
    [<TestCase(typeof<TestClassWithLongDictionary>)>]    
    [<TestCase(typeof<TestClassWithSubObjectOption>)>]
    [<TestCase(typeof<TestFlatWithDefaultValuesClass>)>]
    [<TestCase(typeof<TestFlatClassWithVOptionLong>)>]
    [<TestCase(typeof<TestClassWithEnumInt>)>]
    [<TestCase(typeof<TestClassWithEnumString>)>]
    [<TestCase(typeof<TestClassWithStringDictionary>)>]
    [<TestCase(typeof<TestClassWithIntDictionary>)>]
    [<TestCase(typeof<TestClassWithSubClassDictionary>)>]
    [<TestCase(typeof<TestClassWithNullable>)>]
    [<TestCase(typeof<TestClassWithBsonDocument>)>]
    [<TestCase(typeof<TestClassWithBsonDocumentWithDefault>)>]
    [<TestCase(typeof<TestClassBinaryData>)>]
    [<TestCase(typeof<TestClassWithEnumStringOption>)>]
    [<TestCase(typeof<TestClassWithStaticProperty>)>]
    
    let ``Test that we don't fail on lambda build``(t: Type) =
        let param = Expression.Parameter(typeof<BsonDocument>)
        let expr = ExpressionReader.buildReader(t, param)
//        let f = Expression.Lambda(expr, Expression.Parameter(typeof<BsonDocument>)).Compile()
        ()    

    [<Test>]
    let ``Test that lambda compile works``() =
        ExpressionReader.CreateReader<TestFlatClass>() |> ignore
        
    [<Test>]
    let ``Test that lambda compile works for subobjects``() =
        ExpressionReader.CreateReader<TestClassWithSubObject>() |> ignore
        
    [<Test>]
    let ``Test that lambda compile works for arrays``() =
        ExpressionReader.CreateReader<TestFlatClassWithArrayOfIntValues>() |> ignore        