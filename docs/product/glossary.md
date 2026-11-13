<!-- product-spec:start -->
<!-- auto-generated -->

# Glossary

Service terms only. No system glossary term applies.
- **Source:** `[confirmed: user]` interview 2026-11-13 § Phase 3 batch confirmation (term list); meanings `[inferred]` from scout.inventory.md:67-75 vocabulary and the spec plan.

- **Registration** — turning this handling on for one data type in one service, once, at service start. A type handled this way is a *registered type*.
- **Stored form** — the document shape a registered type is written as and read from.
- **Stored name** — the name a field has in the document; the field's own name unless another is declared.
- **Excluded field** — a field marked as not stored; neither written nor read.
- **Optional value** — a value that may be empty; when empty, it is left out when stored and comes back empty when loaded.
- **Field default** — the value a field loads with when the document lacks it; under "omit when default" also the value that causes the field to be left out.
- **Omit when null / Omit when default** — per-field choices to leave a field out of the stored document when it is null, or when it equals its default; they can be combined.
- **Typed filter / typed update** — a query or change written in code against a type's fields, not against raw document field names.
<!-- product-spec:end -->
