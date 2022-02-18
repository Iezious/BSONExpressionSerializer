namespace BSONExpressionSerializerTests

open System
open System.Linq.Expressions
open Iezious.Libs.BSONExpressionSerializer
open MongoDB.Bson
open NUnit.Framework

[<TestFixture>]
module WriterBuildTests =
        
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
    [<TestCase(typeof<TestFlatClassWithVOptionLong>)>]
    [<TestCase(typeof<TestClassWithEnumInt>)>]
    [<TestCase(typeof<TestClassWithEnumString>)>]
    [<TestCase(typeof<TestClassWithStringDictionary>)>]
    [<TestCase(typeof<TestClassWithIntDictionary>)>]
    [<TestCase(typeof<TestClassWithLongDictionary>)>]
    [<TestCase(typeof<TestClassWithSubClassDictionary>)>]
    [<TestCase(typeof<TestClassWithNullable>)>]
    let ``Test that we don't fail on writer lambda build``(t: Type) =
        let param = Expression.Parameter(t)
        let expr = ExpressionWriter.build(t, param)
//        let f = Expression.Lambda(expr, Expression.Parameter(typeof<BsonDocument>)).Compile()
        ()

    [<Test>]
    let ``Test that writer lambda compile works``() =
        ExpressionWriter.CreateWriter<TestFlatClass>() |> ignore
                
    [<Test>]
    let ``Test that writer lambda compile works for subobjects``() =
        ExpressionWriter.CreateWriter<TestClassWithSubObject>() |> ignore
                     
        
    [<Test>]
    let ``Test that writer lambda compile works for arrays``() =
        ExpressionWriter.CreateWriter<TestFlatClassWithArrayOfIntValues>() |> ignore