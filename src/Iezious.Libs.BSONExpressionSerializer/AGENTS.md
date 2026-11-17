# Iezious.Libs.BSONExpressionSerializer

<!-- agent-ready:start -->

The F# library project that produces the `Iezious.Libs.BSONExpressionSerializer` NuGet package.

## Contents

**Key files (in fsproj compile order):**
- `Utils.fs` — three inline SRTP operators (`!>`, `!!>`, `!->`) for `BsonValue`/`BsonDocument` convenience conversion
- `ExpressionReaderRegistry.fs` — thread-safe registry allowing callers to substitute custom `BsonDocument -> 'T` readers per type; exported API but unused across the entire SIN codebase (see `docs/architecture/overview.md` Anomaly 6)
- `ExpressionReader.fs` — builds and compiles `BsonDocument -> 'T` expression trees; entry point `CreateReader<'t>`
- `ExpressionWriter.fs` — builds and compiles `'T -> BsonDocument` expression trees; entry point `CreateWriter<'t>`
- `ExpressionStreamReader.fs` — empty stub; body is `module ExpressionStreamReader = ()` with an unused `open`; planned stream-based reader never implemented (see `docs/architecture/overview.md` Anomaly 1)
- `HelperSerializers.fs` — `NullableSerializer<'t>`, `ValueOptionSerializer<'t>`, `OptionSerializer<'t>` operating over the streaming `IBsonReader`/`IBsonWriter` path
- `ExpressionDocumentSerializer.fs` — primary exported type `ExpressionDocumentSerializer<'T>`; implements `IBsonSerializer<'T>`, `IBsonSerializer`, and `IBsonDocumentSerializer`
- `Iezious.Libs.BSONExpressionSerializer.fsproj` — library project file; defines compile order, NuGet package metadata, and `MongoDB.Bson` + `FSharp.Core` dependencies

See also: `D:/GitRoot/_SIN/BSONExpressionSerializer/docs/architecture/interfaces.md`.

<!-- agent-ready:end -->
