namespace BSONExpressionSerializerTests

open System
open System.Collections.Generic
open BSONExpressionSerializerTests
open Iezious.Libs.BSONExpressionSerializer
open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes
open NUnit.Framework
open FluentAssertions
open Utils

[<TestFixture>]
module WriterExecutionTests =
    
    [<Test>]
    let ``Test write flat class``() =
        let data = {
            TestFlatClass.Name = "Tssa" 
            TestFlatClass.CountInt = -1 
            TestFlatClass.Date = DateTime.UtcNow
        }
        let convert = ExpressionWriter.CreateWriter<TestFlatClass>()
        let test =  convert.Invoke(data) 
        
        test["CountInt"].AsInt32.Should().Be(data.CountInt, "") |> ignore  
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
    let ``Test ignore null values by attribute``() =
        let data = {
            TestFlatWithDefaultNull.Name = null 
            TestFlatWithDefaultNull.Count = 2 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatWithDefaultNull>()
        let test =  convert.Invoke(data)
        
        test["Count"].AsInt32.Should().Be(data.Count, "") |> ignore
        test.Contains("Name").Should().Be(false, "") |> ignore          
    
    [<Test>]
    let ``Test ignore null values by both attributes``() =
        let data = {
            TestFlatWithDefaultAndNull.Name = null 
            TestFlatWithDefaultAndNull.Count = 2 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatWithDefaultAndNull>()
        let test =  convert.Invoke(data)
        
        test["Count"].AsInt32.Should().Be(data.Count, "") |> ignore
        test.Contains("Name").Should().Be(false, "") |> ignore  
        
    [<Test>]
    let ``Test ignore null values by both attributes and set to default``() =
        let data = {
            TestFlatWithDefaultAndNull.Name = "zz" 
            TestFlatWithDefaultAndNull.Count = 2 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatWithDefaultAndNull>()
        let test =  convert.Invoke(data)
        
        test["Count"].AsInt32.Should().Be(data.Count, "") |> ignore
        test.Contains("Name").Should().Be(false, "") |> ignore  
    
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
            TestFlatClass.CountInt = 1 
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
            TestFlatClassWithBsonId._id = ObjectId.GenerateNewId() |> BsonObjectId 
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
                TestFlatClass.CountInt = 2131
                TestFlatClass.Date = DateTime.UtcNow
            } 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithSubObject>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Count"].AsInt64.Should().Be(data.Count, "") |> ignore
        test["SubObject"].AsBsonDocument["Name"].AsString.Should().Be(data.SubObject.Name, "") |> ignore
        test["SubObject"].AsBsonDocument["CountInt"].AsInt32.Should().Be(data.SubObject.CountInt, "") |> ignore


    [<Test>]
    let ``Test write of nested class option filled``() =
        let data = {
            TestClassWithSubObjectOption._id = ObjectId.GenerateNewId()
            TestClassWithSubObjectOption.Name = "qwqdqsqddq"
            TestClassWithSubObjectOption.Count = 23123
            TestClassWithSubObjectOption.SubObjectOption = Some {
                TestFlatClass.Name = "oiwdqwjdoipqwd"
                TestFlatClass.CountInt = 2131
                TestFlatClass.Date = DateTime.UtcNow
            } 
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithSubObjectOption>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Count"].AsInt64.Should().Be(data.Count, "") |> ignore
        test["SubObjectOption"].AsBsonDocument["Name"].AsString.Should().Be(data.SubObjectOption.Value.Name, "") |> ignore
        test["SubObjectOption"].AsBsonDocument["CountInt"].AsInt32.Should().Be(data.SubObjectOption.Value.CountInt, "") |> ignore


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


    [<Test>]
    let ``Test write of object array``() =
        let data = {
            TestFlatClassWithArrayOfObjects._id = ObjectId.GenerateNewId()
            TestFlatClassWithArrayOfObjects.Name = "qwqdqsqddq"
            TestFlatClassWithArrayOfObjects.Count = 23123
            TestFlatClassWithArrayOfObjects.SubArray = [|
                { Name = "oiwdqwjdoipqwd";  CountInt = 2131; Date = DateTime.UtcNow } 
                { Name = "qwdkliqjpdowj";  CountInt = 2131221; Date = DateTime.UtcNow.AddHours(2) } 
                { Name = "poqwpoqwdpod";  CountInt = 22; Date = DateTime.UtcNow.AddHours(32) } 
            |]
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithArrayOfObjects>()
        let test = convert.Invoke(data)
        
        test["Count"].AsInt64.Should().Be(data.Count, "") |> ignore
        test["SubArray"].AsBsonArray.Count.Should().Be(data.SubArray.Length, "") |> ignore
        for i in 0..data.SubArray.Length-1 do
            test["SubArray"].AsBsonArray[i].AsBsonDocument["Name"].AsString.Should().Be(data.SubArray[i].Name, "") |> ignore
            test["SubArray"].AsBsonArray[i].AsBsonDocument["CountInt"].AsInt32.Should().Be(data.SubArray[i].CountInt, "") |> ignore
    
    [<Test>]
    let ``Test write of object array set to null``() =
        let data = {
            TestFlatClassWithArrayOfObjects._id = ObjectId.GenerateNewId()
            TestFlatClassWithArrayOfObjects.Name = "qwqdqsqddq"
            TestFlatClassWithArrayOfObjects.Count = 23123
            TestFlatClassWithArrayOfObjects.SubArray = null
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestFlatClassWithArrayOfObjects>()
        let test = convert.Invoke(data)
        
        test["Count"].AsInt64.Should().Be(data.Count, "") |> ignore
        test["SubArray"].IsBsonNull.Should().Be(true, "") |> ignore

    [<Test>]
    let ``Test write of dictionary set to null``() =
        let data = {
            TestClassWithStringDictionary.Name = "qwqdqsqddq"
            TestClassWithStringDictionary.Dict = null
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithStringDictionary>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Dict"].IsBsonNull.Should().Be(true, "") |> ignore

    [<Test>]
    let ``Test write of dictionary set to default null``() =
        let data = {
            TestClassWithStringDictionaryAndDefaultValue.Name = "qwqdqsqddq"
            TestClassWithStringDictionaryAndDefaultValue.Dict = null
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithStringDictionaryAndDefaultValue>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test.Contains("Dict").Should().Be(false, "") |> ignore


    [<Test>]
    let ``Test write of string dictionary``() =
        let data = {
            TestClassWithStringDictionary.Name = "qwqdqsqddq"
            TestClassWithStringDictionary.Dict = Dictionary(["KeyA", "wqdqwdwq"; "KeyB", "wqdqwdqwqwddqw"] |> dict)
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithStringDictionary>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Dict"].AsBsonDocument["KeyA"].Should().Be(data.Dict["KeyA"], "") |> ignore
        test["Dict"].AsBsonDocument["KeyB"].Should().Be(data.Dict["KeyB"], "") |> ignore

    [<Test>]
    let ``Test write of int dictionary``() =
        let data = {
            TestClassWithIntDictionary.Name = "qwqdqsqddq"
            TestClassWithIntDictionary.Dict = Dictionary(["KeyA", 22; "KeyB", 33] |> dict)
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithIntDictionary>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Dict"].AsBsonDocument["KeyA"].Should().Be(data.Dict["KeyA"], "") |> ignore
        test["Dict"].AsBsonDocument["KeyB"].Should().Be(data.Dict["KeyB"], "") |> ignore
        
    [<Test>]
    let ``Test write of subobject dictionary``() =
        let data = {
            TestClassWithSubClassDictionary.Name = "qwqdqsqddq"
            TestClassWithSubClassDictionary.Dict = [
                 "KeyA", { Name = "oiwdqwjdoipqwd";  CountInt = 2131; Date = DateTime.UtcNow } 
                 "KeyB", { Name = "wqdqwqdwdwq";  CountInt = 1111; Date = DateTime.UtcNow.AddHours(3) } 
            ] |> dict |> Dictionary
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithSubClassDictionary>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Dict"].AsBsonDocument["KeyA"].AsBsonDocument["Name"].AsString.Should().Be(data.Dict["KeyA"].Name, "") |> ignore
        test["Dict"].AsBsonDocument["KeyA"].AsBsonDocument["CountInt"].AsInt32.Should().Be(data.Dict["KeyA"].CountInt, "") |> ignore
        test["Dict"].AsBsonDocument["KeyA"].AsBsonDocument["Date"].ToUniversalTime().Should().BeCloseTo(data.Dict["KeyA"].Date, TimeSpan.FromMilliseconds(1), "") |> ignore
        test["Dict"].AsBsonDocument["KeyB"].AsBsonDocument["Name"].AsString.Should().Be(data.Dict["KeyB"].Name, "") |> ignore
        test["Dict"].AsBsonDocument["KeyB"].AsBsonDocument["CountInt"].AsInt32.Should().Be(data.Dict["KeyB"].CountInt, "") |> ignore
        test["Dict"].AsBsonDocument["KeyB"].AsBsonDocument["Date"].ToUniversalTime().Should().BeCloseTo(data.Dict["KeyB"].Date, TimeSpan.FromMilliseconds(1), "") |> ignore
        
        
    [<Test>]
    let ``Test write of int enum``() =
        let data = {
            TestClassWithEnumInt._id = ObjectId.GenerateNewId() |> BsonObjectId
            TestClassWithEnumInt.Name = "wqdijqwoijdqodwq"
            TestClassWithEnumInt.Count = 2323131231L
            TestClassWithEnumInt.EnumData = TestEnum.OptionB
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithEnumInt>()
        let test = convert.Invoke(data)
        
        test["EnumData"].AsInt32.Should().Be(data.EnumData |> int32, "") |> ignore

    [<Test>]
    let ``Test write of string enum``() =
        let data = {
            TestClassWithEnumString._id = ObjectId.GenerateNewId() |> BsonObjectId
            TestClassWithEnumString.Name = "wqdijqwoijdqodwq"
            TestClassWithEnumString.Count = 2323131231L
            TestClassWithEnumString.EnumData = TestEnum.OptionB
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithEnumString>()
        let test = convert.Invoke(data)
        
        test["EnumData"].AsString.Should().Be(data.EnumData |> string, "") |> ignore


    [<Test>]
    let ``Test write of string enum voption set``() =
        let data = {
            TestClassWithEnumStringOption._id = ObjectId.GenerateNewId() |> BsonObjectId
            TestClassWithEnumStringOption.Name = "wqdijqwoijdqodwq"
            TestClassWithEnumStringOption.Count = 2323131231L
            TestClassWithEnumStringOption.EnumData = ValueSome TestEnum.OptionB
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithEnumStringOption>()
        let test = convert.Invoke(data)
        
        test["EnumData"].AsString.Should().Be(data.EnumData.Value |> string, "") |> ignore

    [<Test>]
    let ``Test write of string enum voption none``() =
        let data = {
            TestClassWithEnumStringOption._id = ObjectId.GenerateNewId() |> BsonObjectId
            TestClassWithEnumStringOption.Name = "wqdijqwoijdqodwq"
            TestClassWithEnumStringOption.Count = 2323131231L
            TestClassWithEnumStringOption.EnumData = ValueNone
        }        
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithEnumStringOption>()
        let test = convert.Invoke(data)
        
        test.Contains("EnumData").Should().Be(false, "") |> ignore


    [<Test>]
    let ``Test write of object with bson document``() =
        let data = { 
                      TestClassWithBsonDocument.Name = "11"
                      TestClassWithBsonDocument.Payload = !-> {| SomeKey = 11; OtherKey = "ddqwqd" |}
                   }
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithBsonDocument>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Payload"].AsBsonDocument["SomeKey"].AsInt32.Should().Be(11, "") |> ignore
        test["Payload"].AsBsonDocument["OtherKey"].AsString.Should().Be("ddqwqd", "") |> ignore
        
    [<Test>]
    let ``Test write of object with bson document and ignored null``() =
        let data = { 
                      TestClassWithBsonDocumentWithDefault.Name = "11"
                      TestClassWithBsonDocumentWithDefault.Payload = null
                   }
        
        let convert = ExpressionWriter.CreateWriter<TestClassWithBsonDocumentWithDefault>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test.Contains("Payload").Should().Be(false, "") |> ignore
        
    [<Test>]
    let ``Test write of object binary data set to null``() =
        let data = { 
                      TestClassBinaryData.Name = "11"
                      TestClassBinaryData.Payload = null
                   }
        
        let convert = ExpressionWriter.CreateWriter<TestClassBinaryData>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test.Contains("Payload").Should().Be(false, "") |> ignore
        
                
    [<Test>]
    let ``Test write of object binary data``() =
        let data = { 
                      TestClassBinaryData.Name = "11"
                      TestClassBinaryData.Payload = Security.Cryptography.RandomNumberGenerator.GetBytes(100)
                   }
        
        let convert = ExpressionWriter.CreateWriter<TestClassBinaryData>()
        let test = convert.Invoke(data)
        
        test["Name"].AsString.Should().Be(data.Name, "") |> ignore
        test["Payload"].AsByteArray.Should().BeEquivalentTo(data.Payload, "") |> ignore
        
        