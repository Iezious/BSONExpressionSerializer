namespace Iezious.Libs.BSONExpressionSerializer

open System
open System.Collections.Generic
open MongoDB.Bson
open MongoDB.Bson.Serialization

module ExpressionReaderRegistry =
    let private registered = Dictionary<Type, obj>()
    
    let Add<'t>(f: Func<BsonDocument,'t>) =
        lock registered <| fun () -> registered.Add(typeof<'t>, f)
        
    let Get<'t>() =
        let def(doc:BsonDocument) = BsonSerializer.Deserialize<'t>(doc)
        lock registered <| fun () ->
            match registered.TryGetValue(typeof<'t>) with
            | true, ff -> ff :?> Func<BsonDocument, 't>
            | _ -> def

    let proto(d: BsonDocument) =
        
//        d.GetElement(0).
        d["dwq"].AsString
//        d.ElementCount
//        let v = d["dwq"].AsInt32
//        let v = d["dwq"].AsString
//        let v = d["dwq"].AsBsonArray.Count
//        Expression.Property()