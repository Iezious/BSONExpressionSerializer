namespace Iezious.Libs.BSONExpressionSerializer

open MongoDB.Bson

module Utils =
    let inline (!>) (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit : ^a -> ^b) x)
    let inline (!!>) (x:^a) : ^b = ((^b) : (static member op_Implicit : BsonDocument -> ^b) x.ToBsonDocument())        
    let inline (!->) (x:^a) = x.ToBsonDocument()
    