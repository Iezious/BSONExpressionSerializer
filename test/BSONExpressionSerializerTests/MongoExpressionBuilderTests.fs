namespace BSONExpressionSerializerTests

open System
open Iezious.Libs.BSONExpressionSerializer
open MongoDB.Bson.Serialization
open MongoDB.Driver
open NUnit.Framework

[<TestFixture>]
module MongoExpressionBuilderTests =
    
    [<Test>]
    let ``Flat class find expression should not fail``() =
        let expression = ExpressionFilterDefinition<TestFlatClass>(fun f -> f.Name <> "a")
        let b = expression.Render(ExpressionDocumentSerializer<TestFlatClass>() ,BsonSerializer.SerializerRegistry)
        Assert.That(b, Is.Not.Null)
        Assert.That(b["Name"], Is.Not.Empty)
        Assert.That(b["Name"].["$ne"].AsString, Is.EqualTo("a"))

    [<Test>]
    let ``find expression for int enum should not fail``() =
        let expression = ExpressionFilterDefinition<TestClassWithEnumInt>(fun f -> f.EnumData = TestEnum.OptionB)
        let b = expression.Render(ExpressionDocumentSerializer<TestClassWithEnumInt>() ,BsonSerializer.SerializerRegistry)
        Assert.That(b, Is.Not.Null)
        Assert.That(b["EnumData"].AsInt32, Is.EqualTo(int TestEnum.OptionB))

    [<Test>]
    let ``find expression for string enum should not fail``() =
        let expression = ExpressionFilterDefinition<TestClassWithEnumString>(fun f -> f.EnumData = TestEnum.OptionB)
        let b = expression.Render(ExpressionDocumentSerializer<TestClassWithEnumString>() ,BsonSerializer.SerializerRegistry)
        Assert.That(b, Is.Not.Null)
        Assert.That(b["EnumData"].AsString, Is.EqualTo(nameof TestEnum.OptionB))
        
    [<Test>]
    let ``find expression for nested class string prop should not fail``() =
        let expression = ExpressionFilterDefinition<TestClassWithSubObject>(fun f -> f.SubObject.Name = "wqeqweqwe")
        let b = expression.Render(ExpressionDocumentSerializer<TestClassWithSubObject>() ,BsonSerializer.SerializerRegistry)
        Assert.That(b, Is.Not.Null)
        Assert.That(b["SubObject.Name"].AsString, Is.EqualTo("wqeqweqwe"))
                
    [<Test>]
    let ``find expression for nested class int prop should not fail``() =
        let expression = ExpressionFilterDefinition<TestClassWithSubObject>(fun f -> f.SubObject.CountInt = 22)
        let b = expression.Render(ExpressionDocumentSerializer<TestClassWithSubObject>() ,BsonSerializer.SerializerRegistry)
        Assert.That(b, Is.Not.Null)
        Assert.That(b["SubObject.CountInt"].AsInt32, Is.EqualTo(22))
        
    [<Test>]
    let ``find expression for nested class should not fail for datetime values``() =
        let now = DateTime.Now
        let expression = ExpressionFilterDefinition<TestClassWithSubObject>(fun f -> f.SubObject.Date < now)
        let b = expression.Render(ExpressionDocumentSerializer<TestClassWithSubObject>() ,BsonSerializer.SerializerRegistry)
        Assert.That(b, Is.Not.Null)
        Assert.That(b["SubObject.Date"], Is.Not.Null)
        

    [<Test>]
    let ``find expression with flat string option should not fail``() =
        let expression = ExpressionFilterDefinition<TestFlatClassWithOptionValue>(fun f -> f.OptString = Some "a")
        let b = expression.Render(ExpressionDocumentSerializer<TestFlatClassWithOptionValue>() ,BsonSerializer.SerializerRegistry)
        Assert.That(b, Is.Not.Null)
        Assert.That(b["OptString"].AsString, Is.EqualTo("a"))

    // [<Test>]
    // let ``find expression with flat date value option should not fail``() =
    //     let expression = ExpressionFilterDefinition<TestFlatClassWithVOptionDate>(fun f -> f.OptDate = ValueSome DateTime.Now)
    //     let b = expression.Render(ExpressionDocumentSerializer<TestFlatClassWithVOptionDate>() ,BsonSerializer.SerializerRegistry)
    //     Assert.That(b, Is.Not.Null)
    //     Assert.That(b["OptDate"], Is.Not.Null)
    //
    // [<Test>]
    // let ``find expression with flat int value option should not fail``() =
    //     let expression = ExpressionFilterDefinition<TestFlatClassWithVOptionLong>(fun f -> f.CountOpt > ValueSome 33L)
    //     let b = expression.Render(ExpressionDocumentSerializer<TestFlatClassWithVOptionLong>() ,BsonSerializer.SerializerRegistry)
    //     Assert.That(b, Is.Not.Null)
    //     Assert.That(b["CountOpt"]["$gt"], Is.Not.Null)
        
    [<Test>]
    let ``Set for flat class should not fail``() =
        let update = UpdateDefinitionBuilder<TestFlatClass>().Set((fun f -> f.Name), "a")
        let b = update.Render(ExpressionDocumentSerializer<TestFlatClass>() ,BsonSerializer.SerializerRegistry)
        Assert.That(b, Is.Not.Null)
        Assert.That(b["$set"].["Name"].AsString, Is.EqualTo("a"))
        
    [<Test>]
    let ``update expression with flat string option should not fail``() =
        let expression = UpdateDefinitionBuilder<TestFlatClassWithOptionValue>().Set((fun f -> f.OptString), Some "a")
        let b = expression.Render(ExpressionDocumentSerializer<TestFlatClassWithOptionValue>() ,BsonSerializer.SerializerRegistry)
        Assert.That(b, Is.Not.Null)
        Assert.That(b["$set"].["OptString"].AsString, Is.EqualTo("a"))        