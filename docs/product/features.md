<!-- product-spec:start -->
<!-- auto-generated -->

# Features

Actors are system actors (`system/ACT-###`). All features are what a service developer can rely on.

### FEAT-001 — Data types stored and loaded without mapping code
- **Purpose:** A service developer can store a service's data types as documents in the shared document store and load them back without writing mapping code. Each public field of the type becomes a document field. Nested types become nested documents. Arrays and text-keyed maps are carried across.
- **Actors:** system/ACT-014 · **Priority:** must
- **Status:** delivered
- **Realized by:** _TBD: stories — run /deep-docs or /product-spec_
- **Delivered:** legacy
- **Source:** `[confirmed: user]` interview 2026-11-13 § Phase 3 batch confirmation (plan approved); `[inferred]` behaviour from scout.inventory.md:25 candidate feature "Store and load F# types as MongoDB documents", :34-37 use case "Register a type and persist it", rules :53, :56, :61; harvest § Data model

### FEAT-002 — Optional values and tolerant loading
- **Purpose:** An empty optional value is left out of the stored document and comes back empty when loaded. A document that lacks a field still loads. The field takes its declared default if it has one, stays empty if it is optional, and otherwise takes its type's zero value. Fields the type does not know are ignored.
- **Actors:** system/ACT-014 · **Priority:** must
- **Status:** delivered
- **Realized by:** _TBD: stories — run /deep-docs or /product-spec_
- **Delivered:** legacy
- **Source:** `[confirmed: user]` interview 2026-11-13 § Phase 3 batch confirmation (plan approved); `[inferred]` behaviour from scout.inventory.md:26 candidate feature "F# optional values in documents", :38-41 use case "Read a document with missing or null fields", rules :54, :55; harvest § Data model (missing element, unknown elements ignored)

### FEAT-003 — Per-field storage choices
- **Purpose:** For each field, a service developer can choose the name it is stored under, whether it is stored at all, whether it is left out when null or when equal to its default, and the default it loads with when missing. For an enumeration field, they can choose whether it is stored as a number or as its name.
- **Actors:** system/ACT-014 · **Priority:** must
- **Status:** delivered
- **Realized by:** _TBD: stories — run /deep-docs or /product-spec_
- **Delivered:** legacy
- **Source:** `[confirmed: user]` interview 2026-11-13 § Phase 3 batch confirmation (plan approved); `[inferred]` behaviour from scout.inventory.md:27 candidate feature "Attribute-driven mapping control", :42-45 use case "Write a record while omitting empty values", rules :52, :53, :57, :58; harvest § Data model (attributes honoured, write-omission rules)

### FEAT-004 — Typed filters and updates
- **Purpose:** A service developer can write filters and updates in code against a stored type's fields, and they apply to the stored documents. Optional values and enumerations in a filter or update are translated the same way as when the type is stored.
- **Actors:** system/ACT-014 · **Priority:** must
- **Status:** delivered
- **Realized by:** _TBD: stories — run /deep-docs or /product-spec_
- **Delivered:** legacy
- **Source:** `[confirmed: user]` interview 2026-11-13 § Phase 3 batch confirmation (plan approved); `[inferred]` behaviour from scout.inventory.md:28 candidate feature "Typed LINQ filters and updates over F# types", :46-49 use case "Filter or update by member in LINQ"; harvest § Interfaces exposed

### FEAT-005 — One registration per type, no settings
- **Purpose:** A service developer turns this handling on for a data type by registering it once when the service starts. There are no settings to manage. A type the service has not registered keeps the default handling in that service.
- **Actors:** system/ACT-014 · **Priority:** must
- **Status:** delivered
- **Realized by:** _TBD: stories — run /deep-docs or /product-spec_
- **Delivered:** legacy
- **Source:** `[confirmed: user]` interview 2026-11-13 § Phase 3 batch confirmation (plan approved); `[inferred]` behaviour from scout.inventory.md:35 use case "Register a type and persist it" main flow, :21 candidate actor "Consuming service developer"; harvest § Entry points, § Config surface, § Consumers

## Present in code, not specified

Public surface no consumer relies on. Not a promise; _TBD: supported or abandoned — ask in deep-docs_

- **Stand-alone reader and writer builders** — usable without the store; consumers mention them only in disabled code.
  - **Source:** `[inferred]` scout.inventory.md:29 candidate feature "Stand-alone compiled reader and writer", :21 candidate actor evidence
- **Custom reader override registry** — a way to supply a custom loader for one type; loading a registered type never consults it, and no consumer uses it.
  - **Source:** `[inferred]` scout.inventory.md:30 candidate feature "Custom reader override registry", :80
- **Conversion operators** — shorthand conversions to and from stored values; a second copy exists in the shared utilities library and no consumer uses this one.
  - **Source:** `[inferred]` scout.inventory.md:31 candidate feature "Implicit-conversion operators"; harvest § Interfaces exposed
- **Optional-value member handlers exported for direct use** — serve typed filters and updates internally; no consumer calls them directly.
  - **Source:** `[inferred]` harvest § Consumers; scout.inventory.md:13 surface map
- **Dead entry points** — an empty placeholder, a dead diagnostic function and an unused write helper; kept, not supported.
  - **Source:** `[inferred]` scout.inventory.md:80 "Dead/unreachable entry points"
<!-- product-spec:end -->
