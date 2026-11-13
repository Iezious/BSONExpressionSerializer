# BSONExpressionSerializer — Public Interfaces

## Purpose

This document lists every type and module exported by `Iezious.Libs.BSONExpressionSerializer` (NuGet v1.0.11). It covers signatures, type-handling matrices, and known behavioural constraints. It does not repeat the consumer registration pattern or BSON attribute table — see `overview.md` for those.

Compile order matches fsproj order: `Utils` → `ExpressionReaderRegistry` → `ExpressionReader` → `ExpressionWriter` → `ExpressionStreamReader` → `HelperSerializers` → `ExpressionDocumentSerializer`.

---

## `Utils` module

**File:** `Utils.fs:6-8`
**Namespace:** `Iezious.Libs.BSONExpressionSerializer`

Three inline SRTP operators for BSON value conversion. Intended as convenience helpers inside consumer code that directly manipulates `BsonValue`.

```fsharp
val inline (!>)  : ^a -> ^b   // BsonValue implicit conversion via op_Implicit
val inline (!!>) : ^a -> ^b   // x.ToBsonDocument() then ^b.op_Implicit from BsonDocument
val inline (!->) : ^a -> ^a   // x.ToBsonDocument(); return type inferred
```

---

## `ExpressionReaderRegistry` module

**File:** `ExpressionReaderRegistry.fs`
**Namespace:** `Iezious.Libs.BSONExpressionSerializer`

A thread-safe registry that lets callers substitute a custom `BsonDocument -> 'T` delegate for a given type.

**Internal state** (`ExpressionReaderRegistry.fs:9`):
```fsharp
private registered : Dictionary<Type, obj>   // guarded by lock registered
```

### `Add<'t>`

```fsharp
val Add<'t> : f: Func<BsonDocument, 't> -> unit
// ExpressionReaderRegistry.fs:11-12
```

Registers `f` under `typeof<'t>`. Throws `ArgumentException` if a reader for `'t` is already registered.

### `Get<'t>`

```fsharp
val Get<'t> : unit -> Func<BsonDocument, 't>
// ExpressionReaderRegistry.fs:14-19
```

Returns the registered override for `'t` if one exists. For unregistered types returns a fresh closure that wraps `BsonSerializer.Deserialize<'t>(doc)` — a new closure object is allocated per call.

**Anomaly:** `Add` and `Get` are public API but are called nowhere in the SIN codebase. This module is effectively unused infrastructure. See `overview.md` Anomaly 6.

**Anomaly:** `ExpressionReaderRegistry.fs:21-28` contains a non-private `proto` function with one live statement (`d["dwq"].AsString`) and five commented-out alternatives. It is never called. Any invocation on a real document would throw `KeyNotFoundException`.

---

## `ExpressionReader` module

**File:** `ExpressionReader.fs`
**Namespace:** `Iezious.Libs.BSONExpressionSerializer`

Builds and compiles a `BsonDocument -> 'T` expression tree at call time.

### `CreateReader<'t>`

```fsharp
val CreateReader<'t> : unit -> Func<BsonDocument, 't>
// ExpressionReader.fs:233-237
```

Entry point. Calls `buildReader` recursively, wraps in a `LambdaExpression`, calls `.Compile()`, returns the resulting `Func`. Delegates compiled here are stored by `ExpressionDocumentSerializer` and invoked on every deserialization.

### `buildReader` (public recursive helper)

```fsharp
val buildReader : objType: Type * doc: ParameterExpression -> Expression
// ExpressionReader.fs:20
```

Constructs a `Block` expression that: instantiates `objType` via parameterless `Expression.New`; iterates public properties not marked `[<BsonIgnore>]`; maps each property using `propConverter`/`readValue`; applies `defaultAssignment` for absent fields.

### Reader type-handling matrix

Dispatch is in `readValue` (`ExpressionReader.fs:151-198`). The table shows how each .NET/F# type is extracted from the `BsonValue` for a given field.

| .NET / F# type | BSON extraction | Notes |
|---|---|---|
| `string` | `.AsString` | No null guard at this level |
| `bool` | `.AsBoolean` | |
| `Int32` | `.AsInt32` | |
| `Int64` | `.IsInt32` → convert `.AsInt32`; else `.AsInt64` | Tolerates Int32-stored Int64 fields |
| `Guid` | `.AsGuid` | |
| `float` (`double`) | `.IsInt32` → convert; else `.AsDouble` | Tolerates Int32-stored float fields |
| `Decimal` | `.AsDecimal` | |
| `Decimal128` | `.AsDecimal128` | |
| `DateTime` | `.ToUniversalTime()` | Always UTC |
| `BsonObjectId` | `.AsObjectId` → `Convert` | |
| `ObjectId` | `.AsObjectId` | |
| `BsonDocument` | `readBsonDocument` — deep clone, null-guarded | null BSON → `null` |
| `byte[]` | `nullSafe readBinary` → `.AsByteArray` | Null-guarded on read |
| enum | `readEnum` (`ExpressionReader.fs:24-35`): `Int32` (default) / `Int64` / `String` via `Enum.Parse<T>` | Controlled by `[<BsonRepresentation>]` |
| `Nullable<_>` | `readNullable` (`ExpressionReader.fs:141`): `IsBsonNull` → `Nullable<T>()`; else wrap delegate result | |
| `Option<_>` | `readOption` (`ExpressionReader.fs:49`): `IsBsonNull` → `None`; else `Some` | |
| `ValueOption<_>` | **same `readOption` function** (`ExpressionReader.fs:183-184`) | Works because `ValueSome`/`None` names align via SRTP; structurally misleading — see overview Anomaly 3 |
| `T[]` | `nullSafe readArray` (`ExpressionReader.fs:64`): allocate fixed array, loop, assign | |
| `Dictionary<string,V>` | `nullSafe readDictAsDocument` (`ExpressionReader.fs:90`) when key is `string` and representation is absent or `Document`; otherwise `null` | |
| nested record / other | `buildReader` recursive call, `IsBsonNull`-guarded (`ExpressionReader.fs:190-198`) | |

Source: deep harvest §"Reader dispatch".

---

## `ExpressionWriter` module

**File:** `ExpressionWriter.fs`
**Namespace:** `Iezious.Libs.BSONExpressionSerializer`

Builds and compiles a `'T -> BsonDocument` expression tree at call time.

### `CreateWriter<'t>`

```fsharp
val CreateWriter<'t> : unit -> Func<'t, BsonDocument>
// ExpressionWriter.fs:283-287
```

Entry point. Calls `build` recursively, wraps in a `LambdaExpression`, calls `.Compile()`, returns the `Func`. Delegates compiled here are stored by `ExpressionDocumentSerializer` and invoked on every serialization.

### `build` (public recursive helper)

```fsharp
val build : objType: Type * inst: Expression -> Expression
// ExpressionWriter.fs:40
```

Constructs a `Block` that: creates a `BsonDocument`; iterates non-`[<BsonIgnore>]` properties; converts each value with `buildValue`/`writeValue`; conditionally adds it via the omission checker returned by `getChecker`.

### Writer type-handling matrix

Dispatch is in `writeValue` (`ExpressionWriter.fs:175-220`).

| .NET / F# type | Conversion | Notes |
|---|---|---|
| `string` | `Convert` to `BsonString`; null → `BsonNull.Value` | Null-guarded |
| `byte[]` | `new BsonBinaryData(v)` | **No null guard — null input throws** (see overview Anomaly 10) |
| `Int32` / `Int64` / `Guid` / `bool` / `float` / `Decimal` / `Decimal128` | `Convert` to `BsonValue` | |
| `DateTime` | `Convert` to `BsonDateTime` | |
| `BsonObjectId` / `ObjectId` | `Convert` | |
| `BsonDocument` | `nullSafe writeBsonDocument` — deep clone or `BsonNull.Value` | |
| `Option<_>` | `writeOption` (`ExpressionWriter.fs:70`): `Option.get_IsNone` static call; none → `BsonNull.Value` | |
| `ValueOption<_>` | `writeVOption` (`ExpressionWriter.fs:80`): `.IsNone` property | Dedicated path — unlike the Reader |
| `Nullable<_>` | `writeNullable` (`ExpressionWriter.fs:160`): `.HasValue` → wrap value; else `BsonNull.Value` | |
| enum | `writeEnum` (`ExpressionWriter.fs:44`): `Int32` / `Int64` / `String` (`.ToString()`) | Controlled by `[<BsonRepresentation>]` |
| `T[]` | `nullSafe writeArray` (`ExpressionWriter.fs:89`): iterate, `BsonArray.Add(writeValue elem)` | |
| `Dictionary<string,V>` | `nullSafe writeDict` → `writeDictAsDocument` (`ExpressionWriter.fs:115`): enumerate, `BsonDocument.Add(key, writeValue val)` | |
| nested record / other | `nullSafe` + `build` recursive call; null → `BsonNull.Value` (`ExpressionWriter.fs:210`) | |

Source: deep harvest §"Writer dispatch".

### Omission-guard logic

`getChecker` (`ExpressionWriter.fs:241-256`) selects a predicate per property that controls whether the property is added to the `BsonDocument`. Evaluated at serialization time via the compiled expression.

| Condition | Predicate applied |
|---|---|
| `[<BsonIgnoreIfNull>]` + `[<BsonIgnoreIfDefault>]` both present | `value != null && value != default` |
| `[<BsonIgnoreIfNull>]` only | `value != null` |
| `[<BsonIgnoreIfDefault>]` only | `value != default` |
| Property type is `Option<_>` (no attribute needed) | `Option<T>.get_IsSome(value)` |
| Property type is `ValueOption<_>` (no attribute needed) | `.IsSome` property |
| None of the above | Always written |

Source: deep harvest §"Omission-guard logic".

---

## `ExpressionStreamReader` module

**File:** `ExpressionStreamReader.fs`
**Namespace:** `Iezious.Libs.BSONExpressionSerializer`

```fsharp
module ExpressionStreamReader = ()
```

Empty stub. The file is included in the fsproj compile order but contributes only an empty module to the assembly. No public surface. See overview Anomaly 1.

---

## `HelperSerializers` — standalone serializers

**File:** `HelperSerializers.fs`
**Namespace:** `Iezious.Libs.BSONExpressionSerializer` (no module wrapper)

Three serializers that operate over the MongoDB streaming `IBsonReader`/`IBsonWriter` path (not the `BsonDocument` intermediary used by `ExpressionDocumentSerializer`). They are used internally by `TryGetMemberSerializationInfo` and are also exported for direct registration.

### `NullableSerializer<'t>`

```fsharp
type NullableSerializer<'t
    when 't : struct
    and  't : (new : unit -> 't)
    and  't :> ValueType>
    (typeSerializer: IBsonSerializer<'t>)
// HelperSerializers.fs:7
// Implements: IBsonSerializer<Nullable<'t>>, IBsonSerializer
```

- **Serialize:** if `HasValue` → delegate to `typeSerializer`; else `WriteNull()`.
- **Deserialize:** `BsonType.Null` → `Nullable<'t>()`; else `Nullable<'t>(typeSerializer.Deserialize(...))`.

### `ValueOptionSerializer<'t>`

```fsharp
type ValueOptionSerializer<'t>(typeSerializer: IBsonSerializer<'t>)
// HelperSerializers.fs:34
// Implements: IBsonSerializer<ValueOption<'t>>
```

- **Serialize:** `ValueSome v` → delegate to `typeSerializer`; `ValueNone` → `WriteNull()`.
- **Deserialize:** `BsonType.Null` → `ValueNone`; else `ValueSome(typeSerializer.Deserialize(...))`.

### `OptionSerializer<'t>`

```fsharp
type OptionSerializer<'t>(typeSerializer: IBsonSerializer<'t>)
// HelperSerializers.fs:60
// Implements: IBsonSerializer<Option<'t>>
```

- **Serialize:** `Some v` → delegate to `typeSerializer`; `None` → `WriteNull()`.
- **Deserialize:** `BsonType.Null` → `None`; else `Some(typeSerializer.Deserialize(...))`.

---

## `ExpressionDocumentSerializer<'T>` — primary type

**File:** `ExpressionDocumentSerializer.fs:12`
**Namespace:** `Iezious.Libs.BSONExpressionSerializer`

```fsharp
type ExpressionDocumentSerializer<'t>()
// Implements: IBsonSerializer<'t>, IBsonSerializer, IBsonDocumentSerializer
```

Parameterless constructor; no type constraints on `'t`.

### Construction-time bindings

```fsharp
let reader  : BsonDocument -> 't = ExpressionReader.CreateReader<'t>().Invoke
let writer  : 't -> BsonDocument = ExpressionWriter.CreateWriter<'t>().Invoke
let propMap : Dictionary<string, IBsonSerializer> = Dictionary()
// ExpressionDocumentSerializer.fs:16-19
```

`propMap` is lazily populated by `TryGetMemberSerializationInfo`. It has no lock — see overview Anomaly 8.

### `IBsonSerializer<'t>` members

```fsharp
member Deserialize(ctx: BsonDeserializationContext, _: BsonDeserializationArgs) : 't
// Reads a BsonDocument from ctx.Reader; calls reader(doc)
// ExpressionDocumentSerializer.fs:93

member Serialize(ctx: BsonSerializationContext, _: BsonSerializationArgs, value: 't) : unit
// Calls writer(value); writes resulting BsonDocument to ctx.Writer
// ExpressionDocumentSerializer.fs:~97
```

### `IBsonDocumentSerializer` member

```fsharp
member TryGetMemberSerializationInfo(memberName: string, [<out>] serializationInfo: byref<BsonSerializationInfo>) : bool
// ExpressionDocumentSerializer.fs:111-123
```

Three branches:

1. `memberName` is not a property on `'T` → returns `false`.
2. Member is cached in `propMap` → reconstructs `BsonSerializationInfo`; returns `true`.
3. Member is uncached → calls `resolveTypeSerializer`, stores in `propMap`, returns `BsonSerializationInfo` and `true`.

`TryGetMemberSerializationInfo` is the mechanism that makes `ExpressionFilterDefinition` and LINQ-layer filter/update builders work with this serializer.

### `resolveTypeSerializer` (internal)

```fsharp
let resolveTypeSerializer (pr: PropertyInfo) : IBsonSerializer
// ExpressionDocumentSerializer.fs:30-91
```

Handles:
- enum: constructs an `EnumSerializer<T>` with `[<BsonRepresentation>]` default `Int32`.
- `Nullable<_>`: constructs `NullableSerializer<T>(innerSerializer)`.
- `ValueOption<_>`: constructs `ValueOptionSerializer<T>(innerSerializer)`.
- `Option<_>`: constructs `OptionSerializer<T>(innerSerializer)`.
- All other types: delegates to `BsonSerializer.LookupSerializer`.

---

## External dependencies

| Package | Version | Used for |
|---|---|---|
| `MongoDB.Bson` | 2.19.2 | `BsonDocument`, `BsonValue`, `BsonArray`, `ObjectId`, `IBsonSerializer`, `IBsonDocumentSerializer`, all BSON attributes |
| `FSharp.Core` | 6.0.3 (pinned in library; overridden by consumer at restore) | F# runtime, `Option<_>`, `ValueOption<_>` |

No in-network dependencies. No executable entry point.

---

## Open TBDs

- _TBD: `ExpressionStreamReader` public surface — what stream-based API was intended is not documented anywhere in the repository._
- _TBD: Whether `resolveTypeSerializer` handles all types a consumer might register (e.g., custom record nested inside `Option<_>`) is not verified from source — test coverage unknown._
