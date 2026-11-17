# BSONExpressionSerializerTests

<!-- agent-ready:start -->

F# NUnit test project for `Iezious.Libs.BSONExpressionSerializer`. Test framework confirmed as NUnit via `NUnit3.TestAdapter` in build output.

## Contents

**Key files:**
- `BSONExpressionSerializerTests.fsproj` — test project file; defines PackageReferences and compile order
- `TestData.fs` — test data definitions (types and document fixtures shared across test files)
- `Program.fs` — test program entry point
- `ReaderBuildTests.fs` — tests for expression-tree building in `ExpressionReader`
- `ReaderExecuteTests.fs` — tests for compiled reader delegate execution
- `WriterBuildTests.fs` — tests for expression-tree building in `ExpressionWriter`
- `WriterExecutionTests.fs` — tests for compiled writer delegate execution
- `MongoExpressionBuilderTests.fs` — tests for `ExpressionDocumentSerializer` used with MongoDB LINQ/filter expressions
- `PostReviewTests.fs` — regression guards for the reviewed defects D1–D9 (null text and binary at any depth, absent fields, omission on/off, wrapped enumerations, stored names and exclusion in filters/updates/sorts, concurrent first use, single-member null reads); see `docs/decisions/001.known-defects.md`

<!-- agent-ready:end -->
