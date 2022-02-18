namespace BSONExpressionSerializerTests

open System
open Iezious.Libs.BSONExpressionSerializer
open MongoDB.Bson
open NUnit.Framework
open FluentAssertions
open NUnit.Framework.Internal.Commands

[<TestFixture>]
module WriterExecutionTests =
    
    [<Test>]
    let ``Test write flat class``() =
        let data = {
            TestFlatClass.Name = "Tssa" 
            TestFlatClass.Count = -1 
            TestFlatClass.Date = DateTime.UtcNow
        }
        let convert = ExpressionWriter.CreateWriter<TestFlatClass>()
        let test =  convert.Invoke(data) 
        
        test["Count"].AsInt32.Should().Be(data.Count, "") |> ignore  
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Date"].ToUniversalTime().Should().BeCloseTo(data.Date, TimeSpan.FromMilliseconds(100), "") |> ignore

        
    [<Test>]
    let ``Test write types``() =
        let data = {
            TestFlatDoublesClass.Name = "Tssa" 
            TestFlatDoublesClass.Count = -1 
            TestFlatDoublesClass.Date = DateTime.UtcNow
            TestFlatDoublesClass.Value = 0.22
            TestFlatDoublesClass.CountLong = 15L
            TestFlatDoublesClass.Is = true
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatDoublesClass>()
        let test =  convert.Invoke(data)
        
        test["Count"].AsInt32.Should().Be(data.Count, "") |> ignore  
        test["CountLong"].AsInt64.Should().Be(data.CountLong, "") |> ignore  
        test["Value"].AsDouble.Should().BeApproximately(data.Value, 0.0001, "") |> ignore  
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Is"].AsBoolean.Should().Be(data.Is, "") |> ignore
        test["Date"].ToUniversalTime().Should().BeCloseTo(data.Date, TimeSpan.FromMilliseconds(100), "") |> ignore        
    
    [<Test>]
    let ``Test ignore default values``() =
        let data = {
            TestFlatWithDefaultValuesClass.Name = "Tssa" 
            TestFlatWithDefaultValuesClass.Count = 0 
            TestFlatWithDefaultValuesClass.Is = false
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatWithDefaultValuesClass>()
        let test =  convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test.Contains("Count").Should().Be(false, "") |> ignore  
        test.Contains("Is").Should().Be(false, "") |> ignore
    
    [<Test>]
    let ``Test not ignore default values if values are not default``() =
        let data = {
            TestFlatWithDefaultValuesClass.Name = "Tssa" 
            TestFlatWithDefaultValuesClass.Count = 1 
            TestFlatWithDefaultValuesClass.Is = true
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatWithDefaultValuesClass>()
        let test =  convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test.Contains("Count").Should().Be(true, "") |> ignore  
        test.Contains("Is").Should().Be(true, "") |> ignore
        test["Is"].AsBoolean.Should().Be(data.Is, "") |> ignore
        test["Count"].AsInt32.Should().Be(data.Count, "") |> ignore
        
    [<Test>]
    let ``Test not ignore default string values``() =
        let data = {
            TestFlatWithDefaultValuesClass.Name = null
            TestFlatWithDefaultValuesClass.Count = 1 
            TestFlatWithDefaultValuesClass.Is = false
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatWithDefaultValuesClass>()
        let test =  convert.Invoke(data)
        
        test.Contains("Name").Should().Be(false, "") |> ignore
        test.Contains("Count").Should().Be(true, "") |> ignore
        test["Count"].Should().Be(data.Count, "") |> ignore
        test.Contains("Is").Should().Be(false, "") |> ignore
    
    [<Test>]
    let ``Test not ignore string values in no ignore default attribute``() =
        let data = {
            TestFlatClass.Name = null
            TestFlatClass.Count = 1 
            TestFlatClass.Date = DateTime.MinValue
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClass>()
        let test =  convert.Invoke(data)
        
        test.Contains("Name").Should().Be(true, "") |> ignore
    
    [<Test>]
    let ``Test write ObjectID``() =
        let data = {
            TestFlatClassWithObjectID.Name = null
            TestFlatClassWithObjectID.Count = 1 
            TestFlatClassWithObjectID._id = ObjectId.GenerateNewId() 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithObjectID>()
        let test =  convert.Invoke(data)
        
        test.Contains("Name").Should().Be(true, "") |> ignore
        test["_id"].AsObjectId.Should().Be(data._id, "") |> ignore
    
    
    [<Test>]
    let ``Test write BsonId``() =
        let data = {
            TestFlatClassWithBsonId.Name = null
            TestFlatClassWithBsonId.Count = 1 
            TestFlatClassWithBsonId._id = ObjectId.GenerateNewId() 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithBsonId>()
        let test =  convert.Invoke(data)
        
        test.Contains("Name").Should().Be(true, "") |> ignore
        test["_id"].AsObjectId.Should().Be(data._id.Value, "") |> ignore
    
    [<Test>]
    let ``Test write string array``() =
        let data = {
            TestFlatClassWithArrayOfStringValues.Name = "wqdqwdqw"
            TestFlatClassWithArrayOfStringValues.Count = 1 
            TestFlatClassWithArrayOfStringValues.SubArray = [| "a"; "b"; "c"; null |]
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithArrayOfStringValues>()
        let test =  convert.Invoke(data)
        
        test["SubArray"].IsBsonArray.Should().Be(true, "") |> ignore
        test["SubArray"].AsBsonArray.Count.Should().Be(data.SubArray.Length, "") |> ignore
        for i in 0..data.SubArray.Length-1 do
            if (data.SubArray[i] <> null) then
                test["SubArray"].AsBsonArray[i].AsString.Should().Be(data.SubArray[i], "") |> ignore
            else
                test["SubArray"].AsBsonArray[i].IsBsonNull.Should().Be(true, "") |> ignore
    
    [<Test>]
    let ``Test write int array``() =
        let data = {
            TestFlatClassWithArrayOfIntValues.Name = "wqdqwdqw"
            TestFlatClassWithArrayOfIntValues.Count = 33 
            TestFlatClassWithArrayOfIntValues.SubArray = [| 2 ; 5; 7; 8; 12|]
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithArrayOfIntValues>()
        let test =  convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Count"].AsInt64.Should().Be(data.Count, "") |> ignore
        test["SubArray"].IsBsonArray.Should().Be(true, "") |> ignore
        test["SubArray"].AsBsonArray.Count.Should().Be(data.SubArray.Length, "") |> ignore
        for i in 0..data.SubArray.Length-1 do
                test["SubArray"].AsBsonArray[i].AsInt32.Should().Be(data.SubArray[i], "") |> ignore

    [<Test>]
    let ``Test write of string option filled``() =
        let data = {
            TestFlatClassWithOptionValue._id =  ObjectId.GenerateNewId() 
            TestFlatClassWithOptionValue.Name = "qwqdqsqddq"
            TestFlatClassWithOptionValue.Count = 33 
            TestFlatClassWithOptionValue.OptString = Some "jiqwsdjpdqwopj" 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithOptionValue>()
        let test =  convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Count"].AsInt64.Should().Be(data.Count, "") |> ignore
        test["OptString"].AsString.Should().Be(data.OptString.Value, "") |> ignore

    [<Test>]
    let ``Test write of string option None``() =
        let data = {
            TestFlatClassWithOptionValue._id =  ObjectId.GenerateNewId() 
            TestFlatClassWithOptionValue.Name = "qwqdqsqddq"
            TestFlatClassWithOptionValue.Count = 33 
            TestFlatClassWithOptionValue.OptString = None
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithOptionValue>()
        let test =  convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Count"].AsInt64.Should().Be(data.Count, "") |> ignore
        test.Contains("OptString").Should().Be(false, "") |> ignore

    [<Test>]
    let ``Test write of string value option filled``() =
        let data = {
            TestFlatClassWithVOptionString.Name = "qwqdqsqddq"
            TestFlatClassWithVOptionString.Count = 33 
            TestFlatClassWithVOptionString.OptString = ValueSome "jiqwsdjpdqwopj" 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithVOptionString>()
        let test =  convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Count"].AsInt64.Should().Be(data.Count, "") |> ignore
        test["OptString"].AsString.Should().Be(data.OptString.Value, "") |> ignore

    [<Test>]
    let ``Test write of string value option None``() =
        let data = {
            TestFlatClassWithVOptionString.Name = "qwqdqsqddq"
            TestFlatClassWithVOptionString.Count = 33 
            TestFlatClassWithVOptionString.OptString = ValueNone
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithVOptionString>()
        let test =  convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Count"].AsInt64.Should().Be(data.Count, "") |> ignore
        test.Contains("OptString").Should().Be(false, "") |> ignore


    [<Test>]
    let ``Test write of long value option filled``() =
        let data = {
            TestFlatClassWithVOptionLong.Name = "qwqdqsqddq"
            TestFlatClassWithVOptionLong.CountOpt = ValueSome 33 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithVOptionLong>()
        let test =  convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["CountOpt"].AsInt64.Should().Be(data.CountOpt.Value, "") |> ignore

    [<Test>]
    let ``Test write of long value option None``() =
        let data = {
            TestFlatClassWithVOptionLong.Name = "qwqdqsqddq"
            TestFlatClassWithVOptionLong.CountOpt = ValueNone 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithVOptionLong>()
        let test =  convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test.Contains("CountOpt").Should().Be(false, "") |> ignore

    [<Test>]
    let ``Test write of datetime value option filled``() =
        let data = {
            TestFlatClassWithVOptionDate.Name = "qwqdqsqddq"
            TestFlatClassWithVOptionDate.Count = 23123
            TestFlatClassWithVOptionDate.OptDate = ValueSome DateTime.UtcNow 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithVOptionDate>()
        let test =  convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["OptDate"].ToUniversalTime().Should().BeCloseTo(data.OptDate.Value, TimeSpan.FromMilliseconds(1), "") |> ignore

    [<Test>]
    let ``Test write of datetime value option None``() =
        let data = {
            TestFlatClassWithVOptionDate.Name = "qwqdqsqddq"
            TestFlatClassWithVOptionDate.Count = 23123
            TestFlatClassWithVOptionDate.OptDate = ValueNone 

        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithVOptionDate>()
        let test =  convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test.Contains("OptDate").Should().Be(false, "") |> ignore


    [<Test>]
    let ``Test write of nested class``() =
        let data = {
            TestClassWithSubObject._id = ObjectId.GenerateNewId()
            TestClassWithSubObject.Name = "qwqdqsqddq"
            TestClassWithSubObject.Count = 23123
            TestClassWithSubObject.SubObject = {
                TestFlatClass.Name = "oiwdqwjdoipqwd"
                TestFlatClass.Count = 2131
                TestFlatClass.Date = DateTime.UtcNow
            } 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithSubObject>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Count"].AsInt64.Should().Be(data.Count, "") |> ignore
        test["SubObject"].AsBsonDocument["Name"].AsString.Should().Be(data.SubObject.Name, "") |> ignore
        test["SubObject"].AsBsonDocument["Count"].AsInt32.Should().Be(data.SubObject.Count, "") |> ignore


    [<Test>]
    let ``Test write of nested class option filled``() =
        let data = {
            TestClassWithSubObjectOption._id = ObjectId.GenerateNewId()
            TestClassWithSubObjectOption.Name = "qwqdqsqddq"
            TestClassWithSubObjectOption.Count = 23123
            TestClassWithSubObjectOption.SubObjectOption = Some {
                TestFlatClass.Name = "oiwdqwjdoipqwd"
                TestFlatClass.Count = 2131
                TestFlatClass.Date = DateTime.UtcNow
            } 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithSubObjectOption>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Count"].AsInt64.Should().Be(data.Count, "") |> ignore
        test["SubObjectOption"].AsBsonDocument["Name"].AsString.Should().Be(data.SubObjectOption.Value.Name, "") |> ignore
        test["SubObjectOption"].AsBsonDocument["Count"].AsInt32.Should().Be(data.SubObjectOption.Value.Count, "") |> ignore


    [<Test>]
    let ``Test write of nested class option none``() =
        let data = {
            TestClassWithSubObjectOption._id = ObjectId.GenerateNewId()
            TestClassWithSubObjectOption.Name = "qwqdqsqddq"
            TestClassWithSubObjectOption.Count = 23123
            TestClassWithSubObjectOption.SubObjectOption = None
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithSubObjectOption>()
        let test = convert.Invoke(data)
        
        test.Contains("SubObject").Should().Be(false, "") |> ignore

