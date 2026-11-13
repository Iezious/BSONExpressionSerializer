# Claude session guide — BSONExpressionSerializer

<!-- agent-ready:start -->

`Iezious.Libs.BSONExpressionSerializer` is a published F# class library (NuGet package ID `Iezious.Libs.BSONExpressionSerializer`, v1.0.11, `net6.0`) that provides compile-once, reflection-free MongoDB BSON (de)serialization for F# domain types. At construction time it uses `System.Linq.Expressions` to build and `.Compile()` a typed `Func<BsonDocument,'T>` (reader) and `Func<'T,BsonDocument>` (writer) for any record type `'T`. Every subsequent read/write invokes the compiled delegate directly — no per-call reflection. The primary export `ExpressionDocumentSerializer<'T>` implements MongoDB's `IBsonSerializer<'T>` and `IBsonDocumentSerializer`, making registered types fully compatible with `ExpressionFilterDefinition` and the LINQ layer. Three standalone helper serializers for `Nullable<_>`, `ValueOption<_>`, and `Option<_>` are also exported. This library is the deepest leaf in the SIN .NET dependency tree — it consumes nothing inside the monorepo and is consumed by 5 in-network services.

## Stack

- F# / `net6.0` class library
- `FSharp.Core` 6.0.3 (pinned in library; overridden by consumers at restore time — all active consumers target `net8.0`)
- `MongoDB.Bson` 2.19.2

## Build & test

Run from the service root (`D:/GitRoot/_SIN/BSONExpressionSerializer/`):

- Restore: `dotnet restore BSONExpressionSerializer.sln`
- Build: `dotnet build BSONExpressionSerializer.sln`
- Tests: `dotnet test BSONExpressionSerializer.sln`
- Pack (local): `dotnet pack src/Iezious.Libs.BSONExpressionSerializer/Iezious.Libs.BSONExpressionSerializer.fsproj -c Release /p:Version=<version>`

Publication is tag-driven via GitHub Actions (`.github/workflows/release.yml`, trigger pattern `v*.*.*`). The NuGet target feed is `https://nuget.pkg.github.com/iezious/index.json` — the `iezious` personal org, **not** the `sinsystems` org. Any project consuming this package must add the `iezious` personal org as a NuGet source.

## Layout

- `src/Iezious.Libs.BSONExpressionSerializer/` — 7 F# source files comprising the library (compile order documented in `interfaces.md`)
- `test/BSONExpressionSerializerTests/` — F# NUnit test project
- `docs/architecture/` — service-level architecture documentation (`overview.md`, `interfaces.md`)
- `BSONExpressionSerializer.sln` — solution file

## Path conventions

- Forward slashes in all paths, including on Windows.
- Uppercase Windows drive letters: `D:/...` not `d:/...`.
- Wrap paths containing spaces in double quotes.

## Architecture documentation

- Service-level: `docs/architecture/overview.md` — purpose, expression-tree approach, BSON attribute support, consumer registration pattern, consumers table, publication mechanics, and 10 known anomalies
- Service-level: `docs/architecture/interfaces.md` — all exported public types and modules with signatures, and reader/writer type-handling matrices
- Network-level (cross-service): `D:/GitRoot/_SIN/docs/architecture/overview.md`, `services.md`, `cross-service.md`

## Code index

This service has its own `code_index` at `docs/.helpers/` (config committed; `index.sqlite*` gitignored). **Standard flows — the harvester (`docs-harvester` / `context-harvester`), `coder`, `planner`, and `architect` — must use THIS index when working on this service.** Query via `code_index search` / `symbols` / `graph`; run `code_index usage` for syntax. After pulling changes, refresh with `code_index index sync`.

The legacy-root aggregated index at `D:/GitRoot/_SIN/docs/.helpers/` remains available for cross-service queries.

## Agent guidance

- `ExpressionDocumentSerializer<'T>` is the primary contract surface (see `docs/architecture/interfaces.md`). This package is consumed by 5 in-network services (`SinSystems.APIV2`, `SinSystem.BackOffice`, `SinSystems.Dogma` x2, `SINSystem.Migrator`). Any breaking API change is a cross-service change — coordinate with consumers before merging.
- `docs/architecture/overview.md` documents 10 known anomalies (dead code, missing null guard, empty stub, etc.). These are known and intentional — do not "clean up" any of them without a deliberate PR reviewed against all 5 consumers.
- Tests exist in `test/BSONExpressionSerializerTests/` (NUnit, 6 test files) but coverage gaps are unknown — the harvester did not read the test files.
- The NuGet publish target is the `iezious` personal GitHub org (`nuget.pkg.github.com/iezious`), distinct from the `sinsystems` org used by other SIN libraries. A `GITHUB_TOKEN` / `NUGET_TOKEN` scoped to the `iezious` org is required at restore time.

<!-- agent-ready:end -->
