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

- **Source (terms below):** `[confirmed: user]` interview 2026-11-14 § Wave 1 (Stored empty, Stand-alone conversion, Known limit), § Wave 2 (Document identity, Nested type refinement); the other meanings `[inferred]` from I-01, I-02, I-04, I-06 and I-10 notes § Product and the spec plan § Deep wave 1.

- **Nested type** — a data type used as a field of another stored type; stored as a nested document and covered by the outer type's registration when whole values are stored and loaded; a typed filter or update that reaches into it needs the nested type registered too.
- **Computed field** — a field whose value is derived from other fields and cannot be set; it must be excluded from storage and is computed again after loading.
- **Type-level value** — a value declared once for a data type rather than per value, such as a projection description; never stored.
- **Text-keyed map** — a map whose keys are text; stored as a nested document whose field names are the keys.
- **Stored empty** — a field present in a stored document that holds no value; distinct from a missing field.
- **Inline optional value** — the second kind of optional value, held directly in its field; always left out when empty and cannot carry the omit choices.
- **Enumeration storage form** — whether an enumeration field is stored as a number or as the value's name; declared on each field.
- **Default handling** — the store's own handling, used for every type a service has not registered.
- **Stand-alone conversion** — converting whole documents to and from a data type without registering the type.
- **Raw document** — a document handled as fields and values, without a data type.
- **Hand-built change** — a change written against document field names directly instead of through a typed update.
- **Known limit** — behaviour that is documented but not promised, and that has no fix planned.
- **Document identity** — the field that identifies a stored document: the field stored under the store's identity name.
<!-- product-spec:end -->
