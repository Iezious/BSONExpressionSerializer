namespace Iezious.Libs.BSONExpressionSerializer

open System
open System.Collections.Generic
open System.Reflection
open MongoDB.Bson
open MongoDB.Bson.Serialization
open MongoDB.Bson.Serialization.Attributes
open MongoDB.Bson.Serialization.Serializers


type ExpressionDocumentSerializer<'t>() =
    
//    inherit BsonDocumentSerializer()
    
    let reader = ExpressionReader.CreateReader<'t>().Invoke
    let writer = ExpressionWriter.CreateWriter<'t>().Invoke
    
    let propMap = Dictionary<string, IBsonSerializer>()
    
    let deserialize(context: BsonDeserializationContext, args: BsonDeserializationArgs) : 't =
        let doc = BsonDocumentSerializer.Instance.Deserialize<BsonDocument>(context)
        reader(doc)
        
    let serialize(context: BsonSerializationContext, args: BsonSerializationArgs, value: 't) =
        let doc = writer(value)
        BsonDocumentSerializer.Instance.Serialize<BsonDocument>(context, doc)

    
    // Declared enumeration form inside wrappers (US-009.AC-7, US-010.AC-5; known defect D5 / DEC-000-29).
    // The whole-document writer stores an enumeration in the member's BsonRepresentation form at any wrapper level
    // (option, value option, nullable, array element), or as a 32-bit number when none is declared. Typed filters and
    // updates used to take the inner serializer from the registry, which knows nothing about the member's attribute,
    // so a wrapped enumeration was always translated as a 32-bit number: a by-name filter never matched and an update
    // stored a number the reader then refused. innerSerializer now builds the enumeration serializer from the member's
    // declared form whenever the wrapped type is an enumeration, so translation uses the same form as storage.
    let enumRepresentation (_member: PropertyInfo) =
        match _member.GetCustomAttributes(typeof<BsonRepresentationAttribute>, false) with
        | attrs when attrs <> null && attrs.Length > 0 -> (attrs[0] :?> BsonRepresentationAttribute).Representation
        | _ -> BsonType.Int32

    let enumSerializer (enumType: Type) (representation: BsonType) =
        let serializerType = typedefof<EnumSerializer<BsonType>>.MakeGenericType(enumType)
        Activator.CreateInstance(serializerType, representation) :?> IBsonSerializer

    let innerSerializer (_member: PropertyInfo) (innerType: Type) : IBsonSerializer =
        if innerType.IsEnum then enumSerializer innerType (enumRepresentation _member)
        else BsonSerializer.LookupSerializer(innerType)

    let resolveTypeSerializer (_member:PropertyInfo) =
        match _member.PropertyType with
        | pt when pt.IsEnum ->

            let attr = _member.GetCustomAttributes(typeof<BsonRepresentationAttribute>, false)
            
            if attr <> null && attr.Length > 0 then
                let representation = (attr[0] :?> BsonRepresentationAttribute).Representation
                let serializerType = typedefof<EnumSerializer<BsonType>>.MakeGenericType(_member.PropertyType)
                let serializer = Activator.CreateInstance(serializerType, representation ) :?> IBsonSerializer
                propMap.Add(_member.Name, serializer)
                true, BsonSerializationInfo(_member.Name, serializer, _member.PropertyType)
            else
                let representation = BsonType.Int32
                let serializerType = typedefof<EnumSerializer<BsonType>>.MakeGenericType(_member.PropertyType)
                let serializer = Activator.CreateInstance(serializerType, representation) :?> IBsonSerializer
                propMap.Add(_member.Name, serializer)
                true, BsonSerializationInfo(_member.Name, serializer, _member.PropertyType)
        
        | _pt when _pt.IsGenericType && _pt.GetGenericTypeDefinition() = typeof<Nullable<_>>.GetGenericTypeDefinition() ->
            let underType = _pt.GetGenericArguments().[0]
            let valueSerializer = innerSerializer _member underType
            
            if (valueSerializer <> null) then
                let serializerType = typedefof<NullableSerializer<_>>.MakeGenericType(underType)
                let serializer = Activator.CreateInstance(serializerType, valueSerializer) :?> IBsonSerializer
                propMap.Add(_member.Name, serializer)
                true, BsonSerializationInfo(_member.Name, serializer, _member.PropertyType)
            else
                false, null
                
        | _pt when _pt.IsGenericType && _pt.GetGenericTypeDefinition() = typeof<ValueOption<_>>.GetGenericTypeDefinition() ->
            let underType = _pt.GetGenericArguments().[0]
            let valueSerializer = innerSerializer _member underType
            
            if (valueSerializer <> null) then
                let serializerType = typedefof<ValueOptionSerializer<_>>.MakeGenericType(underType)
                let serializer = Activator.CreateInstance(serializerType, valueSerializer) :?> IBsonSerializer
                propMap.Add(_member.Name, serializer)
                true, BsonSerializationInfo(_member.Name, serializer, _member.PropertyType)
            else
                false, null
                                
        | _pt when _pt.IsGenericType && _pt.GetGenericTypeDefinition() = typeof<Option<_>>.GetGenericTypeDefinition() ->
            let underType = _pt.GetGenericArguments().[0]
            let valueSerializer = innerSerializer _member underType
            
            if (valueSerializer <> null) then
                let serializerType = typedefof<OptionSerializer<_>>.MakeGenericType(underType)
                let serializer = Activator.CreateInstance(serializerType, valueSerializer) :?> IBsonSerializer
                propMap.Add(_member.Name, serializer)
                true, BsonSerializationInfo(_member.Name, serializer, _member.PropertyType)
            else
                false, null
                    
        // Array of enumerations (D5): the registry's array serializer would translate each element as a 32-bit number;
        // build it around an element serializer in the member's declared form instead.
        | _pt when _pt.IsArray && _pt.GetElementType().IsEnum ->
            let elementType = _pt.GetElementType()
            let serializerType = typedefof<ArraySerializer<_>>.MakeGenericType(elementType)
            let serializer = Activator.CreateInstance(serializerType, innerSerializer _member elementType) :?> IBsonSerializer
            propMap.Add(_member.Name, serializer)
            true, BsonSerializationInfo(_member.Name, serializer, _member.PropertyType)

        | _pt ->
            let serializer = BsonSerializer.LookupSerializer(_member.PropertyType)
            if (serializer <> null) then
                propMap.Add(_member.Name, serializer)
                true, BsonSerializationInfo(_member.Name, serializer, _member.PropertyType)
            else
                false, null
    
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
        
    interface IBsonDocumentSerializer with
        member this.TryGetMemberSerializationInfo(memberName, serializationInfo) =
            
            match propMap.TryGetValue(memberName), typeof<'t>.GetProperty(memberName) with
            | _, null -> false

            | (true, _serializer), _member ->
                serializationInfo <- BsonSerializationInfo(memberName, _serializer, _member.PropertyType)
                true
            
            | _, _member ->
                let resolved, sInfo = resolveTypeSerializer _member
                serializationInfo <- sInfo
                resolved
