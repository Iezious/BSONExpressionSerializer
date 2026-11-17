namespace Iezious.Libs.BSONExpressionSerializer

open System
open MongoDB.Bson
open MongoDB.Bson.Serialization

// Explicit null is consumed (US-004.AC-8, US-004.AC-9; known defect D8 / DEC-000-35).
// When the stored value was an explicit null, Deserialize used to return the empty value without reading the null, so
// the reader stayed on the value: the next ReadName / ReadEndDocument / array element failed with "... can only be
// called when State is ..., not when State is Value". That broke reads of a single optional member (a projection to
// one field, Distinct over it, any driver path that reads the member and then goes on). Each wrapper now calls
// ReadNull before returning the empty value, so the reader moves past the null like every other serializer.
// NullableSerializer had the same defect (found while fixing D8); the member resolver uses the driver's own
// NullableSerializer (DEC-000-29), but this exported one is fixed too so it behaves the same if a caller uses it.

type NullableSerializer<'t when 't: struct and 't: (new : unit -> 't) and 't :> ValueType>(typeSerializer: IBsonSerializer<'t>) =
    
     interface IBsonSerializer<Nullable<'t>> with
    
         member this.ValueType = typeof<Nullable<'t>>
         
         member this.Serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: Nullable<'t>) =
             match value with
             | value when value.HasValue -> typeSerializer.Serialize(context, args, value.Value :> obj)
             | _ -> context.Writer.WriteNull()

         member this.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs) =
             match context.Reader.CurrentBsonType with
             | BsonType.Null -> context.Reader.ReadNull(); Nullable<'t>()
             | _ -> Nullable<'t>(typeSerializer.Deserialize(context, args))

         member this.Serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: obj) =
             match value :?> Nullable<'t> with
             | value when value.HasValue -> typeSerializer.Serialize(context, args, value.Value :> obj)
             | _ -> context.Writer.WriteNull()

         member this.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs): obj =
             match context.Reader.CurrentBsonType with
             | BsonType.Null -> context.Reader.ReadNull(); Nullable<'t>()
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
             | BsonType.Null -> context.Reader.ReadNull(); ValueNone
             | _ -> typeSerializer.Deserialize(context, args) |> ValueSome

         member this.Serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: obj) =
             match value :?> 't voption with
             | ValueSome value -> typeSerializer.Serialize(context, args, value :> obj)
             | _ -> context.Writer.WriteNull()

         member this.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs): obj =
             match context.Reader.CurrentBsonType with
             // Typed empty value (found while fixing D8): with obj as the expected type, a bare ValueNone was inferred
             // as ValueOption<obj>, so a caller casting the untyped read to 't voption failed on an explicit null.
             | BsonType.Null -> context.Reader.ReadNull(); (ValueNone: 't voption)
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
             | BsonType.Null -> context.Reader.ReadNull(); None
             | _ -> typeSerializer.Deserialize(context, args) |> Some

         member this.Serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: obj) =
             match value :?> 't option with
             | Some value -> typeSerializer.Serialize(context, args, value :> obj)
             | _ -> context.Writer.WriteNull()

         member this.Deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs): obj =
             match context.Reader.CurrentBsonType with
             // Typed for the same reason as ValueOptionSerializer; None is stored as null, so this one was harmless.
             | BsonType.Null -> context.Reader.ReadNull(); (None: 't option)
             | _ -> Some(typeSerializer.Deserialize(context, args))  
             
         
