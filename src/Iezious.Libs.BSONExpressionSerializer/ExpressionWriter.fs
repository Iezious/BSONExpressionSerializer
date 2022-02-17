namespace Iezious.Libs.BSONExpressionSerializer

open System
open System.Collections.Generic
open System.Linq.Expressions
open System.Reflection
open System.Reflection.Metadata
open System.Xml.Schema
open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes

module ExpressionWriter =
    
    let private getNameInBson(m:MemberInfo) =
        match m.GetCustomAttribute<BsonElementAttribute>() with
        | null -> m.Name
        | attr when String.IsNullOrWhiteSpace(attr.ElementName) -> m.Name
        | attr -> attr.ElementName
        
    let private getDefaultValue(pr: PropertyInfo) : Expression =
        match pr.GetCustomAttribute<BsonDefaultValueAttribute>(), pr.PropertyType with
        | attr, t when attr <> null
             -> Expression.Convert(Expression.Constant(attr.TypeId), t)
        | _, t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<Option<_>>.GetGenericTypeDefinition()
            -> Expression.Property(null, t, "None")
        | _, t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<ValueOption<_>>.GetGenericTypeDefinition()
            -> Expression.Property(null, t, "None")
        | _, t -> Expression.Default(t)

    let privateIgnoreDefault(pr: PropertyInfo) : bool =
        match pr.GetCustomAttribute<BsonIgnoreIfDefaultAttribute>(), pr.PropertyType with
        | _, t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<Option<_>>.GetGenericTypeDefinition()
            -> true
        | _, t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<ValueOption<_>>.GetGenericTypeDefinition()
            -> true
        | attr, t when attr <> null
            -> true
        | _ -> false
    
    let rec build(objType: Type, inst: Expression)  =
        
        let rec buildValue(pr: PropertyInfo) (propValue: Expression) : Expression =
            
            let writeEnum(t: Type) (valueExpr: Expression) : Expression =
                match pr.GetCustomAttribute<BsonRepresentationAttribute>() with
                | null
                     -> Expression.Convert(valueExpr, typeof<int32>)
                | attr when attr.Representation = BsonType.Int32
                     -> Expression.Convert(valueExpr, typeof<int32>)
                | attr when attr.Representation = BsonType.Int64
                     -> Expression.Convert(Expression.Convert(valueExpr, typeof<int32>), typeof<int64>)
                | attr when attr.Representation = BsonType.String
                     -> 
                        let toStringMethod = pr.PropertyType.GetMethod("ToString")
                        Expression.Call(valueExpr, toStringMethod)
                | _ -> Expression.Convert(valueExpr, typeof<int32>)
                
            let rec writeOption(t: Type) (valueExpr: Expression) : Expression =
                let argt = t.GenericTypeArguments[0]
                Expression.Condition( 
                        Expression.Property(valueExpr, nameof(Unchecked.defaultof<Option<_>>.IsNone)),
                        Expression.Constant(BsonNull.Value, typeof<BsonValue>),
                        writeValue <| argt <| Expression.Property(valueExpr, nameof(Unchecked.defaultof<Option<_>>.Value))
                    )
                
            and writeArray(t: Type) (valueExpr: Expression) : Expression =
                let cnt = Expression.Variable(typeof<int32>)
                let arr = Expression.Variable(typeof<BsonArray>)
                let i = Expression.Variable(typeof<int32>)
                let srcArray = Expression.Variable(t)
                let getelem = Expression.ArrayAccess(srcArray, i)
                let stepout = Expression.Label()
                let addMethod = typeof<BsonArray>.GetMethod("Add", [| typeof<BsonArray> |])
                          
                Expression.Block(
                    [cnt; i; arr; srcArray],
                    Expression.Assign(srcArray, valueExpr),
                    Expression.Assign(cnt, Expression.Property(srcArray,"Count")),
                    Expression.Assign(arr, Expression.New(typeof<BsonArray>.GetConstructor([|typeof<int>|]), cnt)),
                    Expression.Assign(i, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.Block(
                             Expression.IfThen(Expression.GreaterThanOrEqual(i, cnt), Expression.Break(stepout)),
                             Expression.Call(arr, addMethod, writeValue <| t.GetElementType() <| getelem),
                             Expression.PostIncrementAssign(i)
                            ),
                        stepout
                    ),
                    Expression.Convert(arr, typeof<BsonValue>)
                )
                
            and writeDictAsDocument(t: Type) (valueExpr: Expression) : Expression =
                let tk = t.GenericTypeArguments[0]
                let tv = t.GenericTypeArguments[1]
                let tpair = typeof<KeyValuePair<_,_>>.MakeGenericType([|tk; tv|])
                let tenum = typeof<IEnumerator<_>>.MakeGenericType([| tpair |])
                
                let kv = Expression.Variable(tpair)
                let k = Expression.Variable(tk)
                let v = Expression.Variable(tv)
                let enum = Expression.Variable(tenum)
                let vDoc = Expression.Variable(typeof<BsonDocument>)
                
                let stepout = Expression.Label()
                
                let getValue = typeof<BsonDocument>.GetMethod("GetElement", [| typeof<int32> |])
                let addMethod = t.GetMethod("Add")
                let nextMethod = tenum.GetMethod("MoveNext")

                
                Expression.Block(
                    [enum; vDoc],
                    Expression.Assign(enum, Expression.Call(valueExpr, "GetEnumerator", [||])),
                    Expression.Assign(vDoc, Expression.New(typeof<BsonDocument>)),
                    Expression.Loop(
                        Expression.Block(
                             [kv; k; v],
                             Expression.IfThen(Expression.Not(Expression.Call(enum, nextMethod)), Expression.Break(stepout)),
                             Expression.Assign(kv, Expression.Property(enum, "Current")),
                             Expression.Assign(k, Expression.Property(kv, "Key")),
                             Expression.Assign(v, Expression.Property(kv, "Value")),
                             Expression.Call(vDoc, addMethod, k, writeValue <| tv <| v )
                            ),
                        stepout
                    ),
                    vDoc
                )                
                            
            and writeDict (t: Type) (valueExpr: Expression) : Expression =
                let tk = t.GenericTypeArguments[0]
                let rpr = t.GetCustomAttribute<BsonRepresentationAttribute>()
                
                if((tk = typeof<string> && rpr = null) || (tk = typeof<string> && rpr <> null && rpr.Representation = BsonType.Document)) then
                    writeDictAsDocument t valueExpr
                else
                    Expression.Constant(null)
                    
            and writeNullable(nt: Type) (valueExpr: Expression) : Expression =
                let t = nt.GetGenericArguments()[0]
                Expression.Condition(
                        Expression.Property(valueExpr, "HasValue"),
                        writeValue <| t  <| Expression.Property(valueExpr, "Value"),
                        Expression.Constant(BsonNull.Value, typeof<BsonValue>)
                    )
                
            and writeValue(ofType: Type) (valueExpr: Expression) : Expression =
                match ofType with
                | t when t = typeof<string> -> Expression.Convert(valueExpr, typeof<BsonString>) 
                | t when t = typeof<byte[]> -> Expression.New(typeof<BsonBinaryData>.GetConstructor([| typeof<byte[]> |]), valueExpr) 
                | t when t = typeof<Int32> -> Expression.Convert(valueExpr, typeof<BsonInt32>) 
                | t when t = typeof<Int64> -> Expression.Convert(valueExpr, typeof<BsonInt64>)
                | t when t = typeof<Guid> -> Expression.Convert(valueExpr, typeof<BsonValue>) 
                | t when t = typeof<float> -> Expression.Convert(valueExpr, typeof<BsonValue>)
                | t when t = typeof<Decimal> -> Expression.Convert(valueExpr, typeof<BsonDecimal128>) 
                | t when t = typeof<Decimal128> -> Expression.Convert(valueExpr, typeof<BsonDecimal128>) 
                | t when t = typeof<Nullable<Int32>> -> writeNullable t valueExpr 
                | t when t = typeof<Nullable<Int64>> -> writeNullable t valueExpr 
                | t when t = typeof<Nullable<decimal>> -> writeNullable t valueExpr 
                | t when t = typeof<Nullable<Guid>> -> writeNullable t valueExpr 
                | t when t = typeof<Nullable<float>> -> writeNullable t valueExpr 
                | t when t = typeof<DateTime> -> Expression.Convert(valueExpr, typeof<BsonDateTime>)
                | t when t = typeof<BsonObjectId> -> Expression.Convert(valueExpr, typeof<BsonObjectId>) 
                | t when t = typeof<ObjectId> -> Expression.Convert(valueExpr, typeof<BsonObjectId>)
                | t when t.IsEnum -> writeEnum(t) valueExpr
                | t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<Option<_>>.GetGenericTypeDefinition()
                      -> writeOption(t) valueExpr
                | t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<ValueOption<_>>.GetGenericTypeDefinition()
                      -> writeOption(t) valueExpr
                | t when t.IsArray
                      -> writeArray(t) valueExpr
                | t when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<Dictionary<_,_>>.GetGenericTypeDefinition()
                      -> writeDict(t) valueExpr
                | t   ->
                         let vobj = Expression.Variable(t)
                         Expression.Block(
                             [vobj],
                             Expression.Assign(vobj, valueExpr),
                             Expression.Condition(
                                    Expression.Equal(vobj, Expression.Constant(null)),
                                    Expression.Constant(BsonNull.Value, typeof<BsonValue>),
                                    build(t, vobj)
                                 )
                         )
                         
            writeValue pr.PropertyType propValue
                
        let writeIfNotDefCheck (pr: PropertyInfo) (valueExr: Expression) : Expression =
            Expression.Not(Expression.Equal(valueExr, getDefaultValue(pr)))
            
        let writeIfNotNoneCheck (pr: PropertyInfo) (valueExr: Expression) : Expression =
            Expression.Property(valueExr, "IsSome")
            
        let getChecker(pr: PropertyInfo) =                 
            
            let attr = pr.GetCustomAttribute<BsonIgnoreIfDefaultAttribute>()
            let t = pr.PropertyType
            
            if(attr <> null) then Some writeIfNotDefCheck
            else if 
                    t.IsGenericType && t.GetGenericTypeDefinition() = typeof<Option<_>>.GetGenericTypeDefinition() ||
                    t.IsGenericType && t.GetGenericTypeDefinition() = typeof<ValueOption<_>>.GetGenericTypeDefinition()
                then Some writeIfNotNoneCheck
            else None
        
        let addMethod = typeof<BsonDocument>.GetMethod("Add", [| typeof<string>; typeof<BsonValue> |])
        let _v_res = Expression.Variable(typeof<BsonDocument>, "result")
        let steps = List<Expression>()
        steps.Add(Expression.Assign(_v_res, Expression.New(typeof<BsonDocument>)))

        for pr in (objType.GetProperties() |> Seq.where(fun p -> p.GetCustomAttribute<BsonIgnoreAttribute>() = null)) do
            let name = getNameInBson pr |> Expression.Constant
            let value = Expression.Property(inst, pr)
            
            match getChecker pr with
            | Some writeCheck ->
                Expression.IfThen(
                    writeCheck <| pr <| value,
                    Expression.Call(_v_res, addMethod, name, buildValue pr value)    
                ) :> Expression
            | None ->
                Expression.Empty() :> Expression
            |> steps.Add
            
        steps.Add(_v_res)
        Expression.Block([_v_res], steps)
