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
        
    let rec buildReader(objType: Type) : Expression =

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
                            Expression.Call(t.GetMethod("Parse", BindingFlags.Static, [| typeof<Type>; typeof<string> |]), getter)
                | _ -> Expression.Convert(Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsInt32)), t)

            let rec readOption(t: Type) (bsonExpr: Expression) : Expression =
                let argt = t.GenericTypeArguments[0]
                Expression.IfThenElse( 
                        Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.IsBsonNull)),
                        Expression.Property(null, t, "None"),
                        Expression.Call(t.GetMethod("Some", BindingFlags.Static), readValue argt bsonExpr)        
                    )

            and readVOption(t: Type) (bsonExpr: Expression) : Expression =
                let argt = t.GenericTypeArguments[0]
                Expression.IfThenElse( 
                        Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.IsBsonNull)),
                        Expression.Property(null, t, "None"),
                        Expression.Call(t.GetMethod("Some", BindingFlags.Static), readValue argt bsonExpr)        
                    )

            and readArray(t: Type) (bsonExpr: Expression) : Expression =
                let cnt = Expression.Variable(typeof<int32>)
                let arr = Expression.Variable(t)
                let i = Expression.Variable(typeof<int32>)
                let asArray = Expression.Property(bsonExpr, "AsBsonArray")
                let getelem = Expression.Property(asArray, typeof<BsonArray>.GetProperty("Item"), i)
                let stepout = Expression.Label()
                          
                Expression.Block(
                    [cnt; i], 
                    Expression.Assign(cnt, Expression.Property(asArray,"Count")),
                    Expression.Assign(arr, Expression.New(t)),
                    Expression.Assign(i, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.Block(
                             Expression.IfThen(Expression.GreaterThanOrEqual(i, cnt), Expression.Break(stepout)),
                             Expression.Assign(Expression.ArrayIndex(arr, i), readValue <| t.GetElementType() <| getelem),
                             Expression.Increment(i)
                            )
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
                
                let stepout = Expression.Label()
                
                Expression.Block(
                    [cnt; i],
                    Expression.Assign(cnt, Expression.Property(bsonExpr,"ElementCount")),
                    Expression.Assign(arr, Expression.New(t)),
                    Expression.Assign(i, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.Block(
                             [elem; k; v],
                             Expression.IfThen(Expression.GreaterThanOrEqual(i, cnt), Expression.Break(stepout)),
                             Expression.Assign(elem, Expression.Call(bsonExpr, "GetElement", [| typeof<Int32> |], i)),
                             Expression.Assign(k, Expression.Property(Expression.Property(elem, "Key"), "AsString")),
                             Expression.Assign(v, readValue <| tv <| Expression.Property(elem, "Value")),
                             Expression.Call(arr, "Add", [| tk; tv |], k, v),
                             Expression.Increment(i)
                            )
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
                | t when t = typeof<Int32> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsInt32)) 
                | t when t = typeof<Int64> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsInt64)) 
                | t when t = typeof<Guid> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsGuid)) 
                | t when t = typeof<Decimal> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsDecimal)) 
                | t when t = typeof<Decimal128> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsDecimal128)) 
                | t when t = typeof<Nullable<Int32>> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsNullableInt32)) 
                | t when t = typeof<Nullable<Int64>> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsNullableInt64)) 
                | t when t = typeof<Nullable<decimal>> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsNullableDecimal)) 
                | t when t = typeof<Nullable<Guid>> -> Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsNullableGuid)) 
                | t when t = typeof<DateTime> -> Expression.Call(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.ToUniversalTime), [||]) 
                | t when t = typeof<string voption>
                      -> Expression.Call(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.ToUniversalTime), [||])
                | t when t.IsEnum
                      -> readEnum(t) bsonExpr
                | t when t.GetGenericTypeDefinition() = typeof<Option<_>>.GetGenericTypeDefinition()
                      -> readOption(t) bsonExpr
                | t when t.GetGenericTypeDefinition() = typeof<ValueOption<_>>.GetGenericTypeDefinition()
                      -> readOption(t) bsonExpr
                | t when t.IsArray
                      -> readArray(t) bsonExpr
                | t when t.GetGenericTypeDefinition() = typeof<Dictionary<_,_>>.GetGenericTypeDefinition()
                      -> readDict(t) bsonExpr
                | t   -> Expression.Invoke(buildReader(t), Expression.Property(bsonExpr, nameof(Unchecked.defaultof<BsonValue>.AsBsonDocument)))
            
            readValue pr.PropertyType propBson

        let _v_doc = Expression.Parameter(typeof<BsonDocument>)
        let _v_res = Expression.Variable(objType, "result")
        let steps = List<Expression>()
        steps.Add(Expression.Assign(_v_res, Expression.New(objType)))
        
        for pr in (objType.GetProperties() |> Seq.where(fun p -> p.GetCustomAttribute<BsonIgnoreAttribute>() = null)) do
            let name = getNameInBson pr |> Expression.Constant
            Expression.IfThenElse(
                    Expression.Call(_v_doc, typeof<BsonDocument>.GetMethod("Contains"), name),
                    Expression.Assign(Expression.Property(_v_res, pr), Expression.Invoke(propConverter <| pr <| Expression.Property(_v_doc, "Item", name))),
                    Expression.Default(pr.PropertyType)
                )
            |> steps.Add
        
        steps.Add(_v_res)
        
        Expression.Block([_v_res], steps)
    
    let CreateReader<'t>() =
        let expr = buildReader(typeof<'t>)
        let l = Expression.Lambda<Func<BsonDocument, 't>>(expr)
        l.Compile()
        