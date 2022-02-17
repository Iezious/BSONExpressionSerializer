namespace Iezious.Libs.BSONExpressionSerializer


open System
open System.Collections.Generic
open System.Linq.Expressions
open System.Reflection
open MongoDB.Bson
open MongoDB.Bson.Serialization
open MongoDB.Bson.Serialization.Attributes

module ExpressionReader =

    let private getNameInBson(m:MemberInfo) =
        match m.GetCustomAttribute<BsonElementAttribute>() with
        | null -> m.Name
        | attr when String.IsNullOrWhiteSpace(attr.ElementName) -> m.Name
        | attr -> attr.ElementName
        
    let rec buildReader(objType: Type, doc: ParameterExpression) : Expression =

        let propConverter (pr: PropertyInfo) (propBson: Expression) : Expression =
            
            let readEnum(t: Type) (bsonExpr: Expression) : Expression =
                match pr.GetCustomAttribute<BsonRepresentationAttribute>() with
                | null -> Expression.Convert(Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsInt32)), t)
                | attr when attr.Representation = BsonType.Int32
                       -> Expression.Convert(Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsInt32)), t)
                | attr when attr.Representation = BsonType.Int64
                       -> Expression.Convert(Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsInt64)), t)
                | attr when attr.Representation = BsonType.String
                       ->   let getter = Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsString))
                            let parseMethod = typeof<Enum>.GetMethod("Parse", 1, [| typeof<string> |]).MakeGenericMethod(t)
                            Expression.Call(parseMethod, getter)
                | _ -> Expression.Convert(Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsInt32)), t)

            
            let rec readOption(t: Type) (bsonExpr: Expression) : Expression =
                let argt = t.GenericTypeArguments[0]
                Expression.Condition( 
                        Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.IsBsonNull)),
                        Expression.Property(null, t, "None"),
                        Expression.Call(t.GetMethod("Some", BindingFlags.Static + BindingFlags.Public), readValue argt bsonExpr)        
                    )

            and readArray(t: Type) (bsonExpr: Expression) : Expression =
                let cnt = Expression.Variable(typeof<int32>)
                let arr = Expression.Variable(t)
                let i = Expression.Variable(typeof<int32>)
                let srcArray = Expression.Variable(typeof<BsonArray>)
                let getelem = Expression.Property(srcArray, typeof<BsonArray>.GetProperty("Item", [| typeof<int32> |]), i)
                let stepout = Expression.Label()
                          
                Expression.Block(
                    [cnt; i; arr; srcArray],
                    Expression.Assign(srcArray, Expression.Property(bsonExpr, "AsBsonArray")),
                    Expression.Assign(cnt, Expression.Property(srcArray,"Count")),
                    Expression.Assign(arr, Expression.NewArrayBounds(t.GetElementType(), cnt)),
                    Expression.Assign(i, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.Block(
                             Expression.IfThen(Expression.GreaterThanOrEqual(i, cnt), Expression.Break(stepout)),
                             Expression.Assign(Expression.ArrayAccess(arr, i), readValue <| t.GetElementType() <| getelem),
                             Expression.PostIncrementAssign(i)
                            ),
                        stepout
                    ),
                    arr
                )
                
                
            and readDictAsDocument(t: Type) (bsonExpr: Expression) : Expression =
                let tk = t.GenericTypeArguments[0]
                let tv = t.GenericTypeArguments[1]
                
                let cnt = Expression.Variable(typeof<int32>)
                let arr = Expression.Variable(t)
                let k = Expression.Variable(tk)
                let v = Expression.Variable(tv)
                let elem = Expression.Variable(typeof<BsonElement>)
                let i = Expression.Variable(typeof<int32>)
                let vDoc = Expression.Variable(typeof<BsonDocument>)
                
                let stepout = Expression.Label()
                
                let getElementMethod = typeof<BsonDocument>.GetMethod("GetElement", [| typeof<int32> |])
                let addMethod = t.GetMethod("Add")
                
                //(BsonDocument()).GetElement(0).Name.
                //Dictionary<string, int>().a
                
                Expression.Block(
                    [arr; cnt; vDoc; i],
                    Expression.Assign(vDoc, Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsBsonDocument))),
                    Expression.Assign(cnt, Expression.Property(vDoc, nameof(Unchecked.defaultof<BsonDocument>.ElementCount))),
                    Expression.Assign(arr, Expression.New(t)),
                    Expression.Assign(i, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.Block(
                             [elem; k; v],
                             Expression.IfThen(Expression.GreaterThanOrEqual(i, cnt), Expression.Break(stepout)),
                             Expression.Assign(elem, Expression.Call(vDoc, getElementMethod, i)),
                             Expression.Assign(k, Expression.Property(elem, "Name")),
                             Expression.Assign(v, readValue <| tv <| Expression.Property(elem, "Value")),
                             Expression.Call(arr, addMethod, k, v),
                             Expression.PostIncrementAssign(i)
                            ),
                        stepout
                    ),
                    arr
                )                

                            
            and readDict (t: Type) (bsonExpr: Expression) : Expression =
                let tk = t.GenericTypeArguments[0]
                let rpr = t.GetCustomAttribute<BsonRepresentationAttribute>()
                
                if((tk = typeof<string> && rpr = null) || (tk = typeof<string> && rpr <> null && rpr.Representation = BsonType.Document)) then
                    readDictAsDocument t bsonExpr
                else
                    Expression.Constant(null)
                    

            and readValue (ofType:Type) (bsonExpr: Expression) : Expression =
                match ofType with
                | t when t = typeof<string> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsString)) 
                | t when t = typeof<byte[]> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsByteArray)) 
                | t when t = typeof<bool> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsBoolean)) 
                | t when t = typeof<Int32> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsInt32)) 
                | t when t = typeof<Int64> ->
                           Expression.Condition(
                                Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.IsInt32)),
                                Expression.Convert(Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsInt32)), typeof<int64>),
                                Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsInt64))
                           )
                | t when t = typeof<Guid> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsGuid)) 
                | t when t = typeof<float> ->
                           Expression.Condition(
                                Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.IsInt32)),
                                Expression.Convert(Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsInt32)), typeof<float>),
                                Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsDouble))
                           )
                | t when t = typeof<Decimal> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsDecimal)) 
                | t when t = typeof<Decimal128> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsDecimal128)) 
                | t when t = typeof<Nullable<Int32>> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsNullableInt32)) 
                | t when t = typeof<Nullable<Int64>> ->Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsNullableInt64)) 
                | t when t = typeof<Nullable<decimal>> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsNullableDecimal)) 
                | t when t = typeof<Nullable<Guid>> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsNullableGuid)) 
                | t when t = typeof<Nullable<float>> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsNullableDouble)) 
                | t when t = typeof<DateTime> -> Expression.Call(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.ToUniversalTime), [||])
                | t when t = typeof<BsonObjectId> -> Expression.Convert(Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsObjectId)), typeof<BsonObjectId>) 
                | t when t = typeof<ObjectId> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsObjectId))
                | t when t.IsEnum
                      -> readEnum(t) bsonExpr
                | t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<Option<_>>.GetGenericTypeDefinition()
                      -> readOption(t) bsonExpr
                | t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<ValueOption<_>>.GetGenericTypeDefinition()
                      -> readOption(t) bsonExpr
                | t when t.IsArray
                      -> readArray(t) bsonExpr
                | t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<Dictionary<_,_>>.GetGenericTypeDefinition()
                      -> readDict(t) bsonExpr
                | t   ->
                         let paramDef = Expression.Parameter(typeof<BsonDocument>)
                         let param = Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsBsonDocument))
                         let subReader = buildReader(t, paramDef)
                         let lambda = Expression.Lambda(subReader, paramDef)
                         Expression.Condition(
                                Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.IsBsonNull)),
                                Expression.Constant(null, t),
                                Expression.Invoke(lambda, param)
                             )
                         
            
            readValue pr.PropertyType propBson
            
        let defaultAssignment (inst: Expression, pr: PropertyInfo) =
            match pr.GetCustomAttribute<BsonDefaultValueAttribute>(), pr.PropertyType with
            | attr, t when attr <> null
                -> Expression.Assign(Expression.Property(inst, pr), Expression.Convert(Expression.Constant(attr.DefaultValue), t))
            | _, t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<Option<_>>.GetGenericTypeDefinition()
                -> Expression.Assign(Expression.Property(inst, pr), Expression.Property(null, t, "None"))
            | _, t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<ValueOption<_>>.GetGenericTypeDefinition()
                -> Expression.Assign(Expression.Property(inst, pr), Expression.Property(null, t, "None"))
            | _, t -> Expression.Assign(Expression.Property(inst, pr), Expression.Default(t))
            

        let _v_doc = doc
        let _v_res = Expression.Variable(objType, "result")
        let steps = List<Expression>()
        steps.Add(Expression.Assign(_v_res, Expression.New(objType)))
        
        for pr in (objType.GetProperties() |> Seq.where(fun p -> p.GetCustomAttribute<BsonIgnoreAttribute>() = null)) do
            let name = getNameInBson pr |> Expression.Constant
            let propReader = propConverter <| pr <| Expression.Property(_v_doc, "Item", name)
            Expression.IfThenElse(
                    Expression.Call(_v_doc, typeof<BsonDocument>.GetMethod("Contains"), name),
                    Expression.Assign(Expression.Property(_v_res, pr), propReader),
                    defaultAssignment(_v_res, pr)
                )
            |> steps.Add
        
        steps.Add(_v_res)
        
        Expression.Block([_v_res], steps)
    
    let CreateReader<'t>() =
        let doc = Expression.Parameter(typeof<BsonDocument>)
        let expr = buildReader(typeof<'t>, doc)
        let l = Expression.Lambda<Func<BsonDocument, 't>>(expr, doc)
        l.Compile()
        