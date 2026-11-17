<!-- product-spec:start -->
<!-- auto-generated -->

# Features

Actors are system actors (`system/ACT-###`). All features are what a service developer can rely on.

### FEAT-001 — Data types stored and loaded without mapping code
- **Purpose:** A service developer can store a service's data types as documents in the shared document store and load them back without writing mapping code. Each public field of the type becomes a document field. Nested types become nested documents. Arrays and text-keyed maps are carried across.
- **Actors:** system/ACT-014 · **Priority:** must
- **Status:** delivered
- **Realized by:** UC-001, UC-002, US-001, US-002
- **Delivered:** legacy
- **Source:** `[confirmed: user]` interview 2026-11-13 § Phase 3 batch confirmation (plan approved); `[inferred]` behaviour from scout.inventory.md:25 candidate feature "Store and load F# types as MongoDB documents", :34-37 use case "Register a type and persist it", rules :53, :56, :61; harvest § Data model

### FEAT-002 — Optional values and tolerant loading
- **Purpose:** An empty optional value is left out of the stored document and comes back empty when loaded. A document that lacks a field still loads. The field takes its declared default if it has one, stays empty if it is optional, and otherwise takes its type's zero value. Fields the type does not know are ignored.
- **Actors:** system/ACT-014 · **Priority:** must
- **Status:** delivered
- **Realized by:** UC-003, UC-004, US-003, US-004
- **Delivered:** legacy
- **Source:** `[confirmed: user]` interview 2026-11-13 § Phase 3 batch confirmation (plan approved); `[inferred]` behaviour from scout.inventory.md:26 candidate feature "F# optional values in documents", :38-41 use case "Read a document with missing or null fields", rules :54, :55; harvest § Data model (missing element, unknown elements ignored)

### FEAT-003 — Per-field storage choices
- **Purpose:** For each field, a service developer can choose the name it is stored under, whether it is stored at all, whether it is left out when null or when equal to its default, and the default it loads with when missing. For an enumeration field, they can choose whether it is stored as a number or as its name.
- **Actors:** system/ACT-014 · **Priority:** must
- **Status:** delivered
- **Realized by:** UC-005, UC-006, UC-007, UC-008, US-005, US-006, US-007, US-008
- **Delivered:** legacy
- **Source:** `[confirmed: user]` interview 2026-11-13 § Phase 3 batch confirmation (plan approved); `[inferred]` behaviour from scout.inventory.md:27 candidate feature "Attribute-driven mapping control", :42-45 use case "Write a record while omitting empty values", rules :52, :53, :57, :58; harvest § Data model (attributes honoured, write-omission rules)

### FEAT-004 — Typed filters and updates
- **Purpose:** A service developer can write filters and updates in code against a stored type's fields, and they apply to the stored documents. Optional values and enumerations in a filter or update are translated the same way as when the type is stored.
- **Actors:** system/ACT-014 · **Priority:** must
- **Status:** delivered
- **Realized by:** UC-009, UC-010, US-009, US-010
- **Delivered:** legacy
- **Source:** `[confirmed: user]` interview 2026-11-13 § Phase 3 batch confirmation (plan approved); `[inferred]` behaviour from scout.inventory.md:28 candidate feature "Typed LINQ filters and updates over F# types", :46-49 use case "Filter or update by member in LINQ"; harvest § Interfaces exposed

### FEAT-005 — One registration per type, no settings
- **Purpose:** A service developer turns this handling on for a data type by registering it once when the service starts. There are no settings to manage. A type the service has not registered keeps the default handling in that service.
- **Actors:** system/ACT-014 · **Priority:** must
- **Status:** delivered
- **Realized by:** UC-011, US-011
- **Delivered:** legacy
- **Source:** `[confirmed: user]` interview 2026-11-13 § Phase 3 batch confirmation (plan approved); `[inferred]` behaviour from scout.inventory.md:35 use case "Register a type and persist it" main flow, :21 candidate actor "Consuming service developer"; harvest § Entry points, § Config surface, § Consumers

### FEAT-006 — Stand-alone conversion of whole documents
- **Purpose:** A service developer can convert whole documents to and from a data type without registering it, for example to move data between stores. The documents have the same form as when the type is registered. Filters and updates are not part of this feature.
- **Actors:** system/ACT-014 · **Priority:** must
- **Status:** delivered
- **Realized by:** UC-012, US-012
- **Delivered:** legacy
- **Source:** `[confirmed: user]` interview 2026-11-14 § Wave 1 rules (f) and (g); `[observed]` I-06 notes § Product (the one-off data move uses it on every run)

## Present in code, not specified

Public surface that no feature specifies. Kept as unsupported public surface: not a promise, and no clean-up release is planned.
- **Source:** `[confirmed: user]` interview 2026-11-14 § Wave 2 Public surface

- **Custom reader override registry** — a way to supply a custom loader for one type. It was abandoned: loading never consults it, and no service has ever used it.
  - **Source:** `[confirmed: user]` why-interview 2026-11-14 § Wave 2 Why (I-18 Q1); `[observed]` I-18 notes D1
- **Conversion operators** — shorthand conversions to and from stored values. The master copy is in the shared utilities library, and services still build against this copy.
  - **Source:** `[confirmed: user]` interview 2026-11-14 § Wave 2 Public surface (I-19 Q1, Q2); `[observed]` I-19 notes § Cross-service touchpoints
- **Optional-value member handlers exported for direct use** — those for optional values and inline optional values serve typed filters and updates internally, and the one for nullable values is used by nothing. No service calls them directly.
  - **Source:** `[observed]` I-17 notes D2, I-15 notes D1; `[confirmed: user]` why-interview 2026-11-14 § Wave 2 Why (I-17 Q2)
- **Dead entry points** — an empty placeholder, a dead diagnostic function and an unused write helper, plus the internal building steps behind every conversion, which are public only for the library's own tests. Kept, not supported.
  - **Source:** `[inferred]` scout.inventory.md:80 "Dead/unreachable entry points"; `[confirmed: user]` why-interview 2026-11-14 § Wave 2 Why (I-18 Q3)
<!-- product-spec:end -->
