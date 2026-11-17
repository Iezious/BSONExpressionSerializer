<!-- product-spec:start -->
<!-- auto-generated -->

# Use cases

Actors are system actors (`system/ACT-###`).

## FEAT-001 — Data types stored and loaded without mapping code

### UC-001 — Store and load a data type with nested data
- **Feature:** FEAT-001 · **Actor:** system/ACT-014
- **Preconditions:** the outer type is registered (UC-011).
- **Main flow:**
  1. The developer stores a value of the type.
  2. The document holds one field per public field of the value, under its stored name. A nested part becomes a nested document, a list becomes a list, and a text-keyed map becomes a nested document whose field names are the map's keys.
  3. The developer loads the document. Every field, nested part, list element and map entry equals what was stored.
  4. Storing the loaded value again produces an identical document.
- **Alternate flows:**
  - A1 — A computed field marked excluded is not stored. After loading, it is computed again from the stored fields.
  - A2 — A type-level value is never stored, and it does not stop registration.
  - A3 — A moment is stored as an instant and loads back in universal time.
- **Exception flows:**
  - E1 — The type, or a nested type at any depth, has a field of a kind the library does not support. Registration refuses the type when the service starts (UC-011 E3).
  - E2 — **Known limit.** A field that may hold a value of any kind is accepted. It is stored as an empty nested document, and its value is lost without an error.
  - E3 — A computed field that is not excluded makes registration refuse the type.
  - E4 — **Known limit.** A value of a subtype stored through its parent's registration, or in a field declared as the parent, keeps only the parent's fields and loads back as the parent. A subtype registered on its own keeps all its fields.
  - E5 — **Known limit.** Fields holding a globally unique identifier are not supported. A type with one is refused when the service starts.
- **Business rules:**
  - BR1 — The stored fields are exactly the public per-value fields, less the excluded ones.
  - BR2 — One registration covers the type's nested types, lists and maps at every depth.
  - BR3 — Every data type and nested type must be creatable empty, and every stored field must be settable.
  - BR4 — The supported field kinds are a closed set. The only collections are lists and text-keyed maps.
- **Source:** `[observed]` I-02 notes § Product (main, A1, A2, E3, BR1–BR3), I-13 notes § Product (A3); `[confirmed: user]` interview 2026-11-14 § Wave 1 (E1, E2, E4, E5, BR4)

### UC-002 — Load a document whose values do not fit the type
- **Feature:** FEAT-001 · **Actor:** system/ACT-014
- **Preconditions:** none.
- **Main flow:**
  1. The developer loads a document in which a stored value does not fit its field, such as text where a number is declared, or a fraction in a whole-number field.
  2. That load fails. The developer gets an error and no value.
  3. Other documents, and later loads, are unaffected.
- **Alternate flows:**
  - A1 — A smaller whole number stored in a wider whole-number field or a fractional field loads with the same value.
- **Exception flows:**
  - E1 — A value that does not fit, inside a nested part, fails the load of the whole document.
  - E2 — **Known limit.** The error names neither the field nor the document.
- **Business rules:**
  - BR1 — Only a smaller whole number widens. Every other mismatch fails the load.
  - BR2 — A load succeeds or fails for the whole document.
- **Source:** `[observed]` I-02 notes § Product (main, E1, BR2), I-13 notes § Product (A1); `[confirmed: user]` interview 2026-11-14 § Wave 1 (A1, E2, BR1)

## FEAT-002 — Optional values and tolerant loading

### UC-003 — Load a document that lacks a field
- **Feature:** FEAT-002 · **Actor:** system/ACT-014
- **Preconditions:** none.
- **Main flow:**
  1. The developer loads a document that does not have one of the type's fields, for example a document written before the field existed.
  2. The missing field takes its declared default.
  3. With no declared default, an optional value comes back empty, and any other field takes its type's zero value (zero, off or empty).
- **Alternate flows:**
  - A1 — The document holds fields the type does not know. They are ignored and the load succeeds.
- **Exception flows:**
  - E1 — A field stored empty is not missing, so the declared default does not apply (UC-004).
  - E2 — Fields the type does not know are not kept. Storing the loaded value writes the document without them.
- **Business rules:**
  - BR1 — A declared default fills only a missing field.
- **Source:** `[confirmed: user]` interview 2026-11-14 § Wave 1 (main, E1, BR1); `[observed]` I-03 notes § Product (A1, E2)

### UC-004 — Store and load a field that holds no value
- **Feature:** FEAT-002 · **Actor:** system/ACT-014
- **Preconditions:** none.
- **Main flow:**
  1. The developer stores a value whose optional field is empty.
  2. The document leaves that field out.
  3. On load, the field comes back empty.
- **Alternate flows:**
  - A1 — A text, binary-content, list, map or nested-part field with no value, and no omit choice, is stored empty. On load it comes back with no value, at any depth.
  - A2 — A field marked "omit when null" or "omit when default" is left out when it has no value, and comes back empty (UC-007).
- **Exception flows:**
  - E1 — A number, flag or moment field that is not optional fails the load when it is stored empty, even when a default is declared.
  - E2 — **Known limit.** A nullable number field with no value is stored empty instead of being left out.
- **Business rules:**
  - BR1 — A field stored empty is distinct from a missing field.
  - BR2 — An optional field stored empty reads back empty however it is read: as part of the whole value, on its own, within a shape built from several fields, or among the distinct values of that field.
- **Source:** `[observed]` I-03 notes § Product, I-12 notes § Product (main, A2); `[confirmed: user]` interview 2026-11-14 § Wave 1 (A1, E1, E2, BR1), § Wave 2 (BR2)

## FEAT-003 — Per-field storage choices

### UC-005 — Store a field under another name
- **Feature:** FEAT-003 · **Actor:** system/ACT-014
- **Preconditions:** none.
- **Main flow:**
  1. The developer declares a stored name on a field.
  2. The field is stored under that name and loaded from it. This also holds for the fields of nested types.
- **Alternate flows:**
  - A1 — A blank declared name keeps the field's own name.
  - A2 — A field stored under the store's identity name is the document's identity. A data type makes a field the identity by giving it that name.
- **Exception flows:**
  - E1 — A document that holds the field under its old name loads as if the field were missing (UC-003).
  - E2 — **Known limit.** A field marked as the identity but stored under another name is not the identity. Registration accepts the mark, and the field is stored and loaded under its own name.
- **Business rules:**
  - BR1 — Two fields may not share one stored name. A type in which they do is refused when the service starts.
  - BR2 — The document identity is the field stored under the store's identity name, and no other marking selects it. The library never generates an identity: the service that stores the value always sets it.
- **Source:** `[observed]` I-04 notes § Product, I-11 notes § Product (main, A1, E1), I-14 notes § Product P1 (A2); `[confirmed: user]` interview 2026-11-14 § Wave 1 (BR1), § Wave 2 Rules (E2, BR2)

### UC-006 — Keep a field out of storage
- **Feature:** FEAT-003 · **Actor:** system/ACT-014
- **Preconditions:** none.
- **Main flow:**
  1. The developer marks a field as excluded.
  2. The field is never stored.
  3. On load the field keeps its empty value. This holds even when the document has a field with that name, and even when a default is declared.
- **Alternate flows:**
  - A1 — Excluding a field of a kind the library does not support, or a computed field, lets the type register.
  - A2 — Type-level values need no marking (UC-001 A2).
- **Exception flows:** none.
- **Source:** `[observed]` I-04 notes § Product, I-11 notes § Product (main, A1, A2)

### UC-007 — Leave a field out when it is empty or at its default
- **Feature:** FEAT-003 · **Actor:** system/ACT-014
- **Preconditions:** none.
- **Main flow ("omit when null"):**
  1. The developer marks a field "omit when null".
  2. When the field has no value, it is left out of the document.
  3. On load it comes back empty.
- **Main flow ("omit when default"):**
  1. The developer marks a field "omit when default" and may declare a default.
  2. A value equal to the default is left out.
  3. On load, the missing field takes the default, so the value round-trips.
- **Alternate flows:**
  - A1 — With both choices on one field, it is left out when it has no value or when it is at its default.
  - A2 — When the developer switches "omit when default" off, the field is stored at its default.
  - A3 — A value that is empty but present, such as empty text or an empty list, is stored under "omit when null".
  - A4 — When the developer switches "omit when null" off, a field with no value is stored empty.
- **Exception flows:**
  - E1 — **Known limit.** These combinations, switched on, make registration refuse the type when the service starts:
    - "omit when null" on a field that can never be empty: a number, flag, moment, document identity, enumeration or inline optional value;
    - "omit when default" on an inline optional value.
  - E2 — **Known limit.** For nested parts, lists, maps and binary content, only an absent value counts as the default. An empty list, or a nested part whose fields are all empty, is stored.
  - E3 — A typed filter for a field's default value does not match documents in which that field was left out.
- **Source:** `[observed]` I-04 notes § Product, I-11 notes § Product (both main flows, A1, A3, E3); `[confirmed: user]` interview 2026-11-14 § Wave 1 (A2, E1, E2); `[confirmed: user]` request 2026-11-17 (A4)

### UC-008 — Store an enumeration as a number or by name
- **Feature:** FEAT-003 · **Actor:** system/ACT-014
- **Preconditions:** none.
- **Main flow:**
  1. An enumeration field is stored as a number, unless the developer declares the by-name form for it.
  2. With the by-name form, each value is stored as its name.
  3. The form declared on a field applies to every value that field holds, including values in a list or in an optional value.
- **Alternate flows:**
  - A1 — In the by-name form, a stored number written as text loads as that value. A value that has no name is stored as its number in text, and loads back.
- **Exception flows:**
  - E1 — In the by-name form, a stored name that matches no value fails the whole load. This includes a different letter case and empty text.
  - E2 — Once documents exist, changing a field's form makes those documents fail to load.
  - E3 — **Known limit.** In the number form, a stored number outside the defined values loads without error and keeps that number.
  - E4 — **Known limit.** Any other declared form is stored as a number, and typed filters on that field fail.
  - E5 — **Known limit.** The large-number form keeps only the ordinary number range. Larger values are cut.
  - E6 — **Known limit.** Name parsing is lenient. Surrounding spaces and comma-separated lists are accepted.
- **Source:** `[confirmed: user]` interview 2026-11-14 § Wave 1 (main, A1, E1, E3–E6); `[observed]` I-10 notes § Product, I-04 notes § Product (E2)

## FEAT-004 — Typed filters and updates

### UC-009 — Filter stored documents by field
- **Feature:** FEAT-004 · **Actor:** system/ACT-014
- **Preconditions:** the type, and every nested type a filter reaches, is registered before any store use (UC-011 BR1).
- **Main flow:**
  1. The developer writes a filter in code against the type's fields.
  2. The filter matches stored documents by their stored form. This covers top-level fields, enumerations in their declared form, optional values, nullable numbers and the fields of registered nested types.
- **Alternate flows:**
  - A1 — A filter for an empty optional value matches documents in which the field is absent, and documents in which it is stored empty.
  - A2 — A filter on a renamed field matches documents by the field's stored name.
  - A3 — A filter on a by-name enumeration held in an optional value or a list matches by name.
- **Exception flows:**
  - E1 — **Known limit.** A filter that reaches inside an optional value is refused when the filter is built.
  - E2 — A filter can reach a nested type the service has not registered. That type's optional fields are then not matched in their stored form. In such a type, when the service sets a store-wide rule to store enumerations by name, an enumeration field with no declared form is matched by name, although it is stored as a number.
  - E3 — **Known limit.** A filter on an enumeration field with an unsupported declared form fails (UC-008 E4).
- **Business rules:**
  - BR1 — Using a field for the first time since the service started never makes a filter fail, however many requests use that field at the same moment.
- **Source:** `[observed]` I-05 notes § Product (main, E2), I-15 notes § Product (E2 addition); `[confirmed: user]` interview 2026-11-14 § Wave 1 (precondition, A1–A3, E1, E3), § Wave 2 (BR1)

### UC-010 — Change stored documents by field
- **Feature:** FEAT-004 · **Actor:** system/ACT-014
- **Preconditions:** the type, and every nested type an update reaches, is registered before any store use (UC-011 BR1).
- **Main flow:**
  1. The developer writes an update in code against the type's fields.
  2. The changed fields are stored in the same form that storing the whole value would use.
  3. The changed document loads with the new values.
- **Alternate flows:**
  - A1 — Setting an optional field to empty stores it empty, and it loads back empty (UC-004).
  - A2 — An update on a renamed field changes the field under its stored name.
  - A3 — An update to a by-name enumeration held in an optional value or a list stores the name, so the document still loads.
- **Exception flows:**
  - E1 — An update can reach a nested type the service has not registered. A set optional field of that type is then stored in a form the containing document cannot load. When the service sets a store-wide rule to store enumerations by name, an enumeration field of that type with no declared form is also changed by name, and the document then fails to load.
- **Business rules:**
  - BR1 — An update never stores a value for an excluded field.
  - BR2 — Using a field for the first time since the service started never makes an update fail, however many requests use that field at the same moment.
- **Source:** `[observed]` I-05 notes § Product (main, A1), I-15 notes § Product (E1); `[confirmed: user]` interview 2026-11-14 § Wave 1 (A2, A3, BR1), § Wave 1 rule (a), which accepted the wording "register every nested type you filter or update into" (I-05 Q3) (precondition), § Wave 2 (BR2)

## FEAT-005 — One registration per type, no settings

### UC-011 — Register a type when the service starts
- **Feature:** FEAT-005 · **Actor:** system/ACT-014
- **Preconditions:** none.
- **Main flow:**
  1. At service start, before any store use, the developer registers each stored type once. This includes each nested type that a typed filter or update reaches.
  2. From then on, every store, load, filter and update of the type uses this handling. There are no settings.
- **Alternate flows:**
  - A1 — A type the service has not registered keeps the store's default handling in that service.
  - A2 — Conventions the service sets for the store's default handling do not reach registered types. Choices are made on each field (UC-005…UC-008).
  - A3 — A type that carries storage choices the library does not act on registers without error, and those choices have no effect. Examples:
    - a required field;
    - refusing unknown fields;
    - a number or text form on a field that is not an enumeration;
    - an identity mark on a field stored under another name;
    - choices declared for the whole type, such as subtype markers.
- **Exception flows:**
  - E1 — Registering the same type a second time fails.
  - E2 — Registering a type after the store has already handled it fails. That happens once a typed filter has reached the type, a collection of it has been opened, or a value of it has been stored.
  - E3 — Registration refuses a type the library cannot handle, so the service fails at start. Such types include:
    - a type with an unsupported field kind;
    - a computed field that is not excluded;
    - a nested type that cannot be created empty;
    - a combination of field choices the library does not support;
    - a declared default that does not fit its field.
  - E4 — **Known limit.** The refusal does not name the field.
  - E5 — **Known limit.** A type that contains itself crashes the service process at start instead of being refused. It may contain itself directly, or through a list, an optional value, a map or a nested part.
  - E6 — **Known limit.** A nested value stored on its own, outside its parent, has the registered form only if its own type is registered too. One way this happens is a hand-built change that embeds the value.
- **Business rules:**
  - BR1 — Every stored type is registered before any store use. This includes the nested types that typed filters or updates reach.
  - BR2 — Only six per-field storage choices act: the stored name, exclusion, the field default, the enumeration storage form, omit when default, and omit when null. Every other storage choice is accepted and has no effect, with no warning.
- **Source:** `[observed]` I-01 notes § Product, I-07 notes § Product, I-09 notes § Product (A1, E1, E2), I-14 notes § Product P2 (A3); `[confirmed: user]` interview 2026-11-14 § Wave 1 (main, A2, E3–E6, BR1), § Wave 1 rule (a) via I-05 Q3 (main 1, BR1 extension), § Wave 2 Rules (A3, BR2)

## FEAT-006 — Stand-alone conversion of whole documents

### UC-012 — Convert whole documents without registration
- **Feature:** FEAT-006 · **Actor:** system/ACT-014
- **Preconditions:** none.
- **Main flow:**
  1. The developer builds a loading converter, a storing converter, or both, for a data type.
  2. They convert raw documents to values, or values to raw documents, without registering the type.
  3. The documents have the same form as for a registered type.
- **Alternate flows:**
  - A1 — The developer builds each converter once and reuses it for every document in a run.
- **Exception flows:**
  - E1 — Building a converter for a type the library cannot handle is refused.
  - E2 — For a type with a computed field that is not excluded, the storing converter is built and stores the computed value, but building the loading converter is refused.
- **Business rules:**
  - BR1 — The stored form is the same on both paths.
- **Source:** `[confirmed: user]` interview 2026-11-14 § Wave 1 (main, BR1); `[observed]` I-06 notes § Product (E1), § Decisions (A1); I-02 notes § Cross-service touchpoints (E2)
<!-- product-spec:end -->
