# docs/architecture

<!-- agent-ready:start -->

Service-level architecture documentation for `BSONExpressionSerializer`.

## Contents

**Key files:**
- `overview.md` — what the library does, expression-tree compilation approach, BSON attribute support (honoured and not honoured), consumer registration pattern, consumers table, publication mechanics, and 10 known anomalies
- `interfaces.md` — all exported public types and modules with full signatures; reader type-handling matrix (`ExpressionReader.readValue` dispatch); writer type-handling matrix (`ExpressionWriter.writeValue` dispatch); omission-guard logic (`getChecker`)

<!-- agent-ready:end -->
