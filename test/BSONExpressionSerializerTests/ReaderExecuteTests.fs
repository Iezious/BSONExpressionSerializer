namespace BSONExpressionSerializerTests

open System
open Iezious.Libs.BSONExpressionSerializer
open NUnit.Framework
open Newtonsoft.Json.Bson
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
        let convert = ExpressionReader.CreateReader<TestFlatClassWithVOptionInt>()
        let test =  convert.Invoke(!-> data) 
        
        test.CountOpt.Should().Be(ValueSome data.CountOpt, "") |> ignore  
        test.Name.Should().Be(data.Name, "") |> ignore
        
    [<Test>]
    let ``Test read flat class with voption none``() =
        let data = {| Name = "Tssa"  |}
        let convert = ExpressionReader.CreateReader<TestFlatClassWithVOptionInt>()
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