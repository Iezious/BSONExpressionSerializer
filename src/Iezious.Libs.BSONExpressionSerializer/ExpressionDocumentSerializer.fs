namespace Iezious.Libs.BSONExpressionSerializer

open MongoDB.Bson
open MongoDB.Bson.Serialization
open MongoDB.Bson.Serialization.Serializers


type ExpressionDocumentSerializer<'t>() =
    
    let reader = ExpressionReader.CreateReader<'t>().Invoke
    let writer = ExpressionWriter.CreateWriter<'t>().Invoke
    
    let deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs) : 't =
        let doc = BsonDocumentSerializer.Instance.Deserialize<BsonDocument>(context)
        reader(doc)
        
    let serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: 't) =
        let doc = writer(value)
        BsonDocumentSerializer.Instance.Serialize<BsonDocument>(context, doc)

    
    interface IBsonSerializer<'t> with
        
        member _.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs) : 't =
            deserialize(context, args)


        member _.Serialize(context:BsonSerializationContext, args:BsonSerializationArgs, value:'t) =
            serialize(context, args, value)
            
    interface IBsonSerializer with
    
        member this.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs): obj =
            deserialize(context, args)
        
        member this.Serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: obj): unit =
            serialize(context, args, value :?> 't)
        member this.ValueType = typeof<'t>