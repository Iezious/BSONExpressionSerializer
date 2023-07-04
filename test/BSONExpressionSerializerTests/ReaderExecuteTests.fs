namespace BSONExpressionSerializerTests

open System
open BSONExpressionSerializerTests
open Iezious.Libs.BSONExpressionSerializer
open NUnit.Framework
open NUnit.Framework.Internal.Commands
open Utils
open MongoDB.Bson
open FluentAssertions

[<TestFixture>]
module ReaderExecuteTests =
    
    [<Test>]
    let ``Test read flat class``() =
        let data = {| Name = "Tssa"; Count = -1; Date = DateTime.UtcNow  |}
        let convert = ExpressionReader.CreateReader<TestFlatClass>()
        let test =  convert.Invoke(!-> data) 
        
        test.Count.Should().Be(data.Count, "") |> ignore  
        test.Name.Should().Be(data.Name, "") |> ignore
        test.Date.Should().BeCloseTo(data.Date, TimeSpan.FromMilliseconds(100), "") |> ignore
        
    [<Test>]
    let ``Test read types``() =
        let data = {| Name = "Tssa"; Date = DateTime.UtcNow; Is=true; Count = 33; CountLong=142L; Value=222.23  |}
        let convert = ExpressionReader.CreateReader<TestFlatDoublesClass>()
        let test =  convert.Invoke(!-> data) 
        
        test.Count.Should().Be(data.Count, "") |> ignore  
        test.CountLong.Should().Be(data.CountLong, "") |> ignore  
        test.Value.Should().BeApproximately(data.Value, 0.0001, "") |> ignore  
        test.Name.Should().Be(data.Name, "") |> ignore
        test.Is.Should().Be(data.Is, "") |> ignore
        test.Date.Should().BeCloseTo(data.Date, TimeSpan.FromMilliseconds(100), "") |> ignore        
    
    [<Test>]
    let ``Test read types with conversion``() =
        let data = {| Name = "Tssa"; Date = DateTime.UtcNow; Count = 33; CountLong=142; Value=222  |}
        let convert = ExpressionReader.CreateReader<TestFlatDoublesClass>()
        let test =  convert.Invoke(!-> data) 
        
        test.Count.Should().Be(data.Count, "") |> ignore  
        test.CountLong.Should().Be(data.CountLong, "") |> ignore  
        test.Value.Should().BeApproximately(data.Value, 0.0001, "") |> ignore  
        test.Name.Should().Be(data.Name, "") |> ignore
        test.Date.Should().BeCloseTo(data.Date, TimeSpan.FromMilliseconds(100), "") |> ignore
    
    [<Test>]
    let ``Test read flat class with defaults``() =
        let data = {| Name = "Tssa"; Date = DateTime.UtcNow  |}
        let convert = ExpressionReader.CreateReader<TestFlatClass>()
        let test =  convert.Invoke(!-> data) 
        
        test.Count.Should().Be(0, "") |> ignore  
        test.Name.Should().Be(data.Name, "") |> ignore
        test.Date.Should().BeCloseTo(data.Date, TimeSpan.FromMilliseconds(100), "") |> ignore
        
    [<Test>]
    let ``Test read flat class with voption``() =
        let data = {| Name = "Tssa"; Date = DateTime.UtcNow; CountOpt = 12L  |}
        let convert = ExpressionReader.CreateReader<TestFlatClassWithVOptionLong>()
        let test =  convert.Invoke(!-> data) 
        
        test.CountOpt.Should().Be(ValueSome data.CountOpt, "") |> ignore  
        test.Name.Should().Be(data.Name, "") |> ignore
        
    [<Test>]
    let ``Test read flat class with voption and int64 conversion``() =
        let data = {| Name = "Tssa"; Date = DateTime.UtcNow; CountOpt = 12  |}
        let convert = ExpressionReader.CreateReader<TestFlatClassWithVOptionLong>()
        let test =  convert.Invoke(!-> data) 
        
        test.CountOpt.Should().Be(ValueSome data.CountOpt, "") |> ignore  
        test.Name.Should().Be(data.Name, "") |> ignore
        
    [<Test>]
    let ``Test read flat class with voption none``() =
        let data = {| Name = "Tssa"  |}
        let convert = ExpressionReader.CreateReader<TestFlatClassWithVOptionLong>()
        let test =  convert.Invoke(!-> data) 
        
        test.CountOpt.Should().Be(ValueNone, "") |> ignore  
        test.Name.Should().Be(data.Name, "") |> ignore
            
    [<Test>]
    let ``Test read flat class with objectid``() =
        let data = {| Name = "Tssa" ; _id = ObjectId.GenerateNewId() |}
        let convert = ExpressionReader.CreateReader<TestFlatClassWithObjectID>()
        let test =  convert.Invoke(!-> data) 
        
        test.Count.Should().Be(0, "") |> ignore  
        test.Name.Should().Be(data.Name, "") |> ignore
        test._id.Should().Be(data._id, "") |> ignore
                    
    [<Test>]
    let ``Test read flat class with bsonid``() =
        let data = {| Name = "Tssa" ; _id = ObjectId.GenerateNewId() |}
        let convert = ExpressionReader.CreateReader<TestFlatClassWithBsonId>()
        let test =  convert.Invoke(!-> data) 
        
        test.Count.Should().Be(0, "") |> ignore  
        test.Name.Should().Be(data.Name, "") |> ignore
        test._id.Value.Should().Be(data._id, "") |> ignore
                            
    [<Test>]
    let ``Test read flat array``() =
        let data = {| Name = "Tssa" ; SubArray=[| "a"; "b"; "c" |] |}
        let convert = ExpressionReader.CreateReader<TestFlatClassWithArrayOfStringValues>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.SubArray.Should().BeEquivalentTo(data.SubArray, "") |> ignore
            
    [<Test>]
    let ``Test read flat array of ints``() =
        let data = {| Name = "Tssa" ; SubArray= [| 1 ;2; 3;4 |] |}
        let convert = ExpressionReader.CreateReader<TestFlatClassWithArrayOfIntValues>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.SubArray.Should().BeEquivalentTo(data.SubArray, "") |> ignore

    [<Test>]
    let ``Test read of TestClassWithSubObject``() =
        let data = {| Name = "Tssa"; SubObject = {| Name ="dqwdqw"; Count = 22  |}  |}
        let convert = ExpressionReader.CreateReader<TestClassWithSubObject>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.SubObject.Should().NotBeNull("") |> ignore
        test.SubObject.Name.Should().Be(data.SubObject.Name, "") |> ignore
        test.SubObject.Count.Should().Be(data.SubObject.Count, "") |> ignore
        
    [<Test>]
    let ``Test read of TestClassWithSubObject not set``() =
        let data = {| Name = "Tssa" |}
        let convert = ExpressionReader.CreateReader<TestClassWithSubObject>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.SubObject.Should().BeNull("") |> ignore
    
    [<Test>]
    let ``Test read of TestClassWithSubObject set to null``() =
        let data = {| Name = "Tssa"; SubObject = null |}
        let convert = ExpressionReader.CreateReader<TestClassWithSubObject>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.SubObject.Should().BeNull("") |> ignore
    
    [<Test>]
    let ``Test read of TestClassWithSubObject option full``() =
        let data = {| Name = "Tssa"; SubObjectOption = {| Name ="dqwdqw"; Count = 22  |}  |}
        let convert = ExpressionReader.CreateReader<TestClassWithSubObjectOption>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.SubObjectOption.IsSome.Should().Be(true, "") |> ignore
        test.SubObjectOption.Value.Name.Should().Be(data.SubObjectOption.Name, "") |> ignore
        test.SubObjectOption.Value.Count.Should().Be(data.SubObjectOption.Count, "") |> ignore

                    
    [<Test>]
    let ``Test read of TestClassWithSubObject option none``() =
        let data = {| Name = "Tssa" |}
        let convert = ExpressionReader.CreateReader<TestClassWithSubObjectOption>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.SubObjectOption.Should().Be(None, "") |> ignore
        
    [<Test>]
    let ``Test read of enum as int``() =
        let data = {| Name = "Tssa"; EnumData = 2 |}
        let convert = ExpressionReader.CreateReader<TestClassWithEnumInt>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        int(test.EnumData).Should().Be(data.EnumData, "") |> ignore
        
    [<Test>]
    let ``Test read of enum as string``() =
        let data = {| Name = "Tssa"; EnumData = "OptionB" |}
        let convert = ExpressionReader.CreateReader<TestClassWithEnumString>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.EnumData.ToString().Should().Be(data.EnumData, "") |> ignore
                
    [<Test>]
    let ``Test read of enum as string option set``() =
        let data = {| Name = "Tssa"; EnumData = "OptionB" |}
        let convert = ExpressionReader.CreateReader<TestClassWithEnumStringOption>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.EnumData.IsSome.Should().Be(true, "") |> ignore
        test.EnumData.ToString().Should().Be(data.EnumData, "") |> ignore
        
    [<Test>]
    let ``Test read of enum as string option none``() =
        let data = {| Name = "Tssa";|}
        let convert = ExpressionReader.CreateReader<TestClassWithEnumStringOption>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.EnumData.IsSome.Should().Be(false, "") |> ignore
        
                
    [<Test>]
    let ``Test read of array of objects``() =
        let data = {| Name = "Tssa"
                      SubArray = [|
                          {| Name = "wdqwqdqdw"; Count = -1 |}
                          {| Name = "qwdqdwq"; Count = 33 |}
                      |]
                   |}
        let convert = ExpressionReader.CreateReader<TestFlatClassWithArrayOfObjects>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.SubArray.Length.Should().Be(data.SubArray.Length, "") |> ignore
        test.SubArray[0].Name.Should().Be(data.SubArray[0].Name, "")  |> ignore
        test.SubArray[0].Count.Should().Be(data.SubArray[0].Count, "")  |> ignore
        test.SubArray[1].Name.Should().Be(data.SubArray[1].Name, "")  |> ignore
        test.SubArray[1].Count.Should().Be(data.SubArray[1].Count, "")  |> ignore                
    [<Test>]
    let ``Test read of array set to null``() =
        let data = {| Name = "Tssa"
                      SubArray = null
                   |}
        let convert = ExpressionReader.CreateReader<TestFlatClassWithArrayOfObjects>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.SubArray.Should().BeNull("") |> ignore
        
    [<Test>]
    let ``Test read of string dictionary``() =
        let data = {| Name = "Tssa"
                      Dict = {| KeyA = "wdqwqdqdw"; KeyB = "vaxacax"; KeyC = ";joioljoij" |}
                   |}
        let convert = ExpressionReader.CreateReader<TestClassWithStringDictionary>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.Dict["KeyA"].Should().Be(data.Dict.KeyA, "") |> ignore        
        test.Dict["KeyB"].Should().Be(data.Dict.KeyB, "") |> ignore        
        test.Dict["KeyC"].Should().Be(data.Dict.KeyC, "") |> ignore           
    
    [<Test>]
    let ``Test read of dictionary set to null``() =
        let data = {| Name = "Tssa"
                      Dict = null
                   |}
        let convert = ExpressionReader.CreateReader<TestClassWithStringDictionary>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        (test.Dict :> obj).Should().BeNull("") |> ignore
                
    [<Test>]
    let ``Test read of int dictionary``() =
        let data = {| Name = "Tssa"
                      Dict = {| KeyA = 22; KeyB = 33; KeyC = 44 |}
                   |}
        let convert = ExpressionReader.CreateReader<TestClassWithIntDictionary>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.Dict["KeyA"].Should().Be(data.Dict.KeyA, "") |> ignore        
        test.Dict["KeyB"].Should().Be(data.Dict.KeyB, "") |> ignore        
        test.Dict["KeyC"].Should().Be(data.Dict.KeyC, "") |> ignore        
                        
    
    [<Test>]
    let ``Test read of object dictionary``() =
        let data = {| Name = "Tssa"
                      Dict = {| KeyA = {| Name = "wdq" |}; KeyB = {| Count = 12313 |} |}
                   |}
        let convert = ExpressionReader.CreateReader<TestClassWithSubClassDictionary>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.Dict.Count.Should().Be(2, "") |> ignore
        test.Dict["KeyA"].Name.Should().Be(data.Dict.KeyA.Name, "") |> ignore        
        test.Dict["KeyB"].Count.Should().Be(data.Dict.KeyB.Count, "") |> ignore        
            
    [<Test>]
    let ``Test read of object with nullable null and empty``() =
        let data = {| Name = "Tssa"
                      CountNullable = null
                   |}
        let convert = ExpressionReader.CreateReader<TestClassWithNullable>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.CountNullable.Should().BeNull("") |> ignore
        test.DateNullable.Should().BeNull("") |> ignore
                    
    [<Test>]
    let ``Test read of object with nullable filled with values``() =
        let data = {| Name = "Tssa"
                      CountNullable = 22
                      DateNullable = DateTime.UtcNow
                   |}
        let convert = ExpressionReader.CreateReader<TestClassWithNullable>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.Name.Should().Be(data.Name, "") |> ignore
        test.CountNullable.Should().NotBeNull("") |> ignore
        test.CountNullable.Value.Should().Be(data.CountNullable, "") |> ignore
        test.DateNullable.Should().NotBeNull("") |> ignore
        test.DateNullable.Value.Should().BeCloseTo(data.DateNullable, TimeSpan.FromMilliseconds(1), "") |> ignore
        
            
    [<Test>]
    let ``Test read of object with default strange value``() =
        let data = {| 
                      Count = 11
                   |}
        let convert = ExpressionReader.CreateReader<TestFlatWithDefaultAndNull>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be("zz", "") |> ignore
        test.Count.Should().Be(data.Count, "") |> ignore
                
            
    [<Test>]
    let ``Test read of object with bson document``() =
        let data = {| 
                      Name = "wqdqwqwdqdw"
                      Payload = !-> {| SomeKey = 11; OtherKey = "ddqwqd" |}
                   |}
        let convert = ExpressionReader.CreateReader<TestClassWithBsonDocument>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.Payload["SomeKey"].AsInt32.Should().Be(11, "") |> ignore
        test.Payload["OtherKey"].AsString.Should().Be("ddqwqd", "") |> ignore
                    
    [<Test>]
    let ``Test read of object with null bson document``() =
        let data = {| 
                      Name = "wqdqwqwdqdw"
                      Payload = null
                   |}
        let convert = ExpressionReader.CreateReader<TestClassWithBsonDocument>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        (test.Payload :> obj).Should().BeNull("") |> ignore
                            
    [<Test>]
    let ``Test read of object with null bson document and default value``() =
        let data = {| 
                      Name = "wqdqwqwdqdw"
                      Payload = null
                   |}
        let convert = ExpressionReader.CreateReader<TestClassWithBsonDocumentWithDefault>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        (test.Payload :> obj).Should().BeNull("") |> ignore
                            
    [<Test>]
    let ``Test read of binary data without payload``() =
        let data = {| 
                      Name = "wqdqwqwdqdw"
                   |}
        let convert = ExpressionReader.CreateReader<TestClassBinaryData>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        (test.Payload :> obj).Should().BeNull("") |> ignore
                                    
    [<Test>]
    let ``Test read of binary data with null payload``() =
        let data = {| 
                      Name = "wqdqwqwdqdw"
                      Payload = null
                   |}
        let convert = ExpressionReader.CreateReader<TestClassBinaryData>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        (test.Payload :> obj).Should().BeNull("") |> ignore
        
    [<Test>]
    let ``Test read of binary data with payload``() =
        let data = {| 
                      Name = "wqdqwqwdqdw"
                      Payload = System.Security.Cryptography.RandomNumberGenerator.GetBytes(100)
                   |}
        let convert = ExpressionReader.CreateReader<TestClassBinaryData>()
        let test =  convert.Invoke(!-> data) 
        
        test.Name.Should().Be(data.Name, "") |> ignore
        test.Payload.Should().BeEquivalentTo(data.Payload, "") |> ignore
        
    [<Test>]        
    let ``Test that empty string in enum value set it to None``() =
        let data = {| 
                        _id = ObjectId()
                        Name = "wqqewqqwe"
                        Count = 12
//                        EnumData = ""
                   |}

        let convert = ExpressionReader.CreateReader<TestClassWithEnumStringOption>()
        let test =  convert.Invoke(!-> data)
        test.EnumData.IsNone.Should().BeTrue("") |> ignore
        
        