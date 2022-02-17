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
    [<TestCase(typeof<TestClassWithSubObjectOption>)>]
    [<TestCase(typeof<TestFlatClassWithVOptionInt>)>]
    [<TestCase(typeof<TestClassWithEnumInt>)>]
    [<TestCase(typeof<TestClassWithEnumString>)>]
    
    let ``Test that we don't fail on lambda build``(t: Type) =
        let expr = ExpressionReader.buildReader(t)
//        let f = Expression.Lambda(expr, Expression.Parameter(typeof<BsonDocument>)).Compile()
        ()    

    [<Test>]
    let ``Test that lambda compile works``() =
        ExpressionReader.CreateReader<TestFlatClass>() |> ignore
        
    [<Test>]
    let ``Test that lambda compile works for subobjects``() =
        ExpressionReader.CreateReader<TestClassWithSubObject>() |> ignore