namespace Iezious.Libs.BSONExpressionSerializer

open System
open MongoDB.Bson
open MongoDB.Bson.Serialization

type NullableSerializer<'t when 't: struct and 't: (new : unit -> 't) and 't :> ValueType>(typeSerializer: IBsonSerializer<'t>) =
    
     interface IBsonSerializer<Nullable<'t>> with
    
         member this.ValueType = typeof<Nullable<'t>>
         
         member this.Serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: Nullable<'t>) =
             match value with
             | value when value.HasValue -> typeSerializer.Serialize(context, args, value.Value :> obj)
             | _ -> context.Writer.WriteNull()

         member this.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs) =
             match context.Reader.CurrentBsonType with
             | BsonType.Null -> Nullable<'t>()
             | _ -> Nullable<'t>(typeSerializer.Deserialize(context, args))

         member this.Serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: obj) =
             match value :?> Nullable<'t> with
             | value when value.HasValue -> typeSerializer.Serialize(context, args, value.Value :> obj)
             | _ -> context.Writer.WriteNull()

         member this.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs): obj =
             match context.Reader.CurrentBsonType with
             | BsonType.Null -> Nullable<'t>()
             | _ -> Nullable<'t>(typeSerializer.Deserialize(context, args))
             
         
type ValueOptionSerializer<'t>(typeSerializer: IBsonSerializer<'t>) =
    
     interface IBsonSerializer<ValueOption<'t>> with
    
         member this.ValueType = typeof<ValueOption<'t>>
         
         member this.Serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: 't voption) =
             match value with
             | ValueSome value -> typeSerializer.Serialize(context, args, value :> obj)
             | _ -> context.Writer.WriteNull()

         member this.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs) =
             match context.Reader.CurrentBsonType with
             | BsonType.Null -> ValueNone
             | _ -> typeSerializer.Deserialize(context, args) |> ValueSome

         member this.Serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: obj) =
             match value :?> 't voption with
             | ValueSome value -> typeSerializer.Serialize(context, args, value :> obj)
             | _ -> context.Writer.WriteNull()

         member this.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs): obj =
             match context.Reader.CurrentBsonType with
             | BsonType.Null -> ValueNone
             | _ -> ValueSome(typeSerializer.Deserialize(context, args))  
                      
type OptionSerializer<'t>(typeSerializer: IBsonSerializer<'t>) =
    
     interface IBsonSerializer<Option<'t>> with
    
         member this.ValueType = typeof<Option<'t>>
         
         member this.Serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: 't option) =
             match value with
             | Some value -> typeSerializer.Serialize(context, args, value :> obj)
             | _ -> context.Writer.WriteNull()

         member this.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs) =
             match context.Reader.CurrentBsonType with
             | BsonType.Null -> None
             | _ -> typeSerializer.Deserialize(context, args) |> Some

         member this.Serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: obj) =
             match value :?> 't option with
             | Some value -> typeSerializer.Serialize(context, args, value :> obj)
             | _ -> context.Writer.WriteNull()

         member this.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs): obj =
             match context.Reader.CurrentBsonType with
             | BsonType.Null -> None
             | _ -> Some(typeSerializer.Deserialize(context, args))  
             
         
