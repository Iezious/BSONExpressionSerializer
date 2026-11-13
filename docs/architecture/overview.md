# BSONExpressionSerializer — Overview

## Purpose

`Iezious.Libs.BSONExpressionSerializer` is a published F# class library (NuGet package ID `Iezious.Libs.BSONExpressionSerializer`, v1.0.11, `net6.0`) that provides compile-once, reflection-free MongoDB BSON (de)serialization for F# domain types.

This document covers: what the library does, its expression-tree compilation approach, which BSON attributes are and are not honoured, the consumer registration pattern, known consumers, publication mechanics, and anomalies.

It does not cover: internal type dispatch details (see `interfaces.md`) or cross-service integration (see network-layer docs).

## Approach — expression-tree compilation

The stock MongoDB driver does not understand F#-native `Option<_>` / `ValueOption<_>` and pays a per-call reflection cost on every document. This library solves both problems by compiling `System.Linq.Expressions` trees **once at construction time** into typed delegates:

- `Func<BsonDocument, 'T>` — deserialization (reader)
- `Func<'T, BsonDocument>` — serialization (writer)

Every subsequent read/write invokes the compiled delegate directly. Construction happens when `ExpressionDocumentSerializer<'T>()` is instantiated — typically once per type at application startup during DI initialisation.

The primary type implements both `IBsonSerializer<'T>` and `IBsonDocumentSerializer`, making it fully compatible with `ExpressionFilterDefinition` and the MongoDB LINQ layer (`UpdateDefinitionBuilder`, etc.).

Sources: `D:/GitRoot/_SIN/BSONExpressionSerializer/src/Iezious.Libs.BSONExpressionSerializer/ExpressionDocumentSerializer.fs:12-17`, deep harvest §"What this library is".

## BSON attribute support

### Honoured

| Attribute | Site(s) in source | Effect |
|---|---|---|
| `[<BsonElement(name)>]` | `ExpressionReader.fs:14`; `ExpressionWriter.fs:14` | Overrides field name used in BSON document; falls back to property name if value is null or whitespace |
| `[<BsonIgnore>]` | `ExpressionReader.fs:219`; `ExpressionWriter.fs:263` | Property excluded in both read and write directions |
| `[<BsonDefaultValue(v)>]` | `ExpressionReader.fs:203`; `ExpressionWriter.fs:20` | Read: substituted when field is absent from document. Write: used as the comparison value by `writeIfNotDefCheck` |
| `[<BsonRepresentation(BsonType)>]` | `ExpressionReader.fs:25`, `:134`; `ExpressionWriter.fs:44`, `:153` | Controls enum wire format; controls dictionary key representation |
| `[<BsonIgnoreIfDefault>]` | `ExpressionWriter.fs:242` | Property omitted when its value equals the default |
| `[<BsonIgnoreIfNull>]` | `ExpressionWriter.fs:243` | Property omitted when null |

### NOT honoured (silently ignored)

These attributes are recognised by the standard MongoDB driver but are not inspected by this library. Using them has no effect and produces no error.

| Attribute | Consequence |
|---|---|
| `[<BsonId>]` | `_id` field is handled identically to any other field. Use `[<BsonElement("_id")>]` if a name override is needed. |
| `[<BsonRequired>]` | Not checked. A missing field is handled by `defaultAssignment` without raising an error. |
| `[<BsonConstructor>]` | Not checked. Construction always uses the parameterless constructor via `Expression.New(objType)`. |
| `[<BsonExtraElements>]` | Not checked. Extra fields in the BSON document are silently ignored. |
| `[<BsonSerializer>]` | Not checked. Per-property serializer overrides are not supported. |
| `[<BsonGuidRepresentation>]` | Not checked. `Guid` always uses the driver's built-in `BsonValue` implicit conversion. |
| `[<BsonDateTimeOptions>]` | Not checked. `DateTime` is always written as `BsonDateTime` and read via `.ToUniversalTime()`. |

Source: deep harvest §"BSON attribute support".

## Consumer registration pattern

Register once per type at application startup, before any MongoDB queries are issued:

```fsharp
BsonSerializer.RegisterSerializer(ExpressionDocumentSerializer<MyRecord>())
```

After registration the type is queryable via `ExpressionFilterDefinition` and all LINQ-layer helpers. No further configuration is required.

Example from `SinSystems.API.Domain/Library.fs:23-37` (`module DomainInit`):

```fsharp
BsonSerializer.RegisterSerializer(ExpressionDocumentSerializer<Profile>())
BsonSerializer.RegisterSerializer(ExpressionDocumentSerializer<Article>())
// ... one call per domain type
```

## Consumers

All active consumers pin v1.0.11 and target `net8.0`.

| Service | Project | PackageReference line | Registration site | Serializer count |
|---|---|---|---|---|
| SinSystems.APIV2 | `SinSystems.API.Domain` | `SinSystems.API.Domain.fsproj:54` | `Library.fs:23-37` (`module DomainInit`) | 15 |
| SinSystem.BackOffice | `SinSystems.BackOffice.Logic` | `SinSystems.BackOffice.Logic.fsproj:76` | `DomainInit.fs:29-43` (`configureSerializers`) | 14 |
| SinSystems.Dogma | `SinSystems.Dogma.Domain` | `SinSystems.Dogma.Domain.fsproj:78` | `Library.fs:24-43` | 17 (1 commented out) |
| SinSystems.Dogma | `SinSystems.Dogma.CLI` | `SinSystems.Dogma.CLI.fsproj:21` | `Program.fs:24-35` | 12 |
| SinSystems.Karma | `SinSystems.Karma.Domain` | `SinSystems.Karma.Domain.fsproj:16` | None — package listed for transitive resolution only (see Anomaly 9) | — |
| SinSystems.Karma | `SinSystems.Karma.Runner` | (transitive via Domain) | `Program.fs:28-33` | 6 |
| SINSystem.Migrator | `SinSystems.Collared.Tools.DataMigrator` | `SinSystems.Collared.Tools.DataMigrator.fsproj:34` | `Program.fs:47-58` | 12 |
| Backup/SinSystems.API | `SinSystems.API.Domain` | `SinSystems.API.Domain.fsproj:50` | (historical snapshot) | v1.0.9 (older pin) |

Source: deep harvest §"Consumers".

## Publication

- **NuGet package ID:** `Iezious.Libs.BSONExpressionSerializer`
- **Published to:** `https://nuget.pkg.github.com/iezious/index.json` — the **`iezious` personal GitHub org**, not the `sinsystems` org.
- **Trigger:** GitHub Actions workflow `.github/workflows/release.yml`, tag-driven (`v*.*.*` pattern).
- **Version injection:** `/p:Version=${VERSION}` at pack time. No `<Version>` element in `.fsproj`, no `.nuspec`, no `Directory.Build.props`.

**Consumer note:** any project consuming this package must add the `iezious` personal org as a NuGet source. This is distinct from `SinSystem.Utils`, which publishes to the `sinsystems` org.

Source: deep harvest §Anomaly 7.

## Anomalies and known issues

1. **`ExpressionStreamReader.fs` is an empty stub.** The file is in the compile order (`fsproj:21`) but its body is `module ExpressionStreamReader = ()` with an unused `open System.Linq.Expressions`. A stream-based reader (reading directly from `IBsonReader` instead of materialising a `BsonDocument` intermediary) was apparently planned but never implemented.

2. **`ExpressionReaderRegistry.proto` is dead code.** The `proto` function at `ExpressionReaderRegistry.fs:21-28` is non-private (public surface) and contains one live statement (`d["dwq"].AsString`) plus five commented-out alternatives. It is called nowhere. On any real document that lacks a `"dwq"` field it would throw `KeyNotFoundException` at runtime.

3. **`ValueOption<_>` Reader reuses `readOption`, not a dedicated function.** `ExpressionReader.fs:183-184` routes `ValueOption<_>` through the same `readOption` helper used for `Option<_>`. This works because `ValueSome`/`None` names align via F# SRTP, but it is structurally misleading. The Writer correctly has separate `writeOption` and `writeVOption` paths.

4. **`net6.0` library consumed by `net8.0` projects.** The library `.fsproj` targets `net6.0` and pins `FSharp.Core 6.0.3`. All active consumers target `net8.0` with their own `FSharp.Core` pin (8.x). Forward-compatible in practice; the library's pin is overridden by consumers at restore time.

5. **`Backup/SinSystems.API` pins v1.0.9.** All active consumers are on v1.0.11. The `Backup/` folder is a historical snapshot only.

6. **`ExpressionReaderRegistry` is exported but unused across the entire codebase.** `Add<'t>` and `Get<'t>` are public API but no consumer calls either. The `Get<'t>` fallback path (which wraps `BsonSerializer.Deserialize<'t>`) is also never triggered. This is effectively dead infrastructure.

7. **Publish target is the `iezious` personal org, not `sinsystems`.** See Publication section. Consumers who copy NuGet source config from other SIN libraries will target the wrong feed.

8. **`propMap` in `ExpressionDocumentSerializer` has no lock.** `TryGetMemberSerializationInfo` reads and writes the `propMap` dictionary (`ExpressionDocumentSerializer.fs:19`) without synchronisation. Safe in practice because all consumers initialise from single-threaded DI startup, but structurally inconsistent with `ExpressionReaderRegistry`, which does use `lock`.

9. **`SinSystems.Karma.Domain` references the package but does not use it.** The `PackageReference` at `SinSystems.Karma.Domain.fsproj:16` exists only so that `SinSystems.Karma.Runner` can resolve it transitively. The Domain project's own `.fs` files contain no `open Iezious` or `ExpressionDocumentSerializer` calls.

10. **`Writer.byte[]` branch has no null guard.** `ExpressionWriter.fs` constructs `new BsonBinaryData(v)` directly for `byte[]` properties. Passing `null` here throws. All other reference-type branches in the writer use `nullSafe` wrapping; `byte[]` does not.

Source: deep harvest §"Anomalies".

## Open TBDs

- _TBD: `ExpressionStreamReader.fs` intended purpose is not documented anywhere in the repository. Whether it was abandoned or is still planned is unknown._
- _TBD: External consumer authentication / NuGet source configuration for the `iezious` personal org is not visible in this repository._
- _TBD: Test coverage for edge cases (null `byte[]`, absent `BsonDefaultValue`, `ValueOption<_>` round-trips) not verified — `test/` directory not read._
- _TBD: Whether `ExpressionReaderRegistry.Get<'t>()` creating a fresh closure per call for unregistered types is intentional or an oversight is not determinable from the source._
