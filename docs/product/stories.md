<!-- product-spec:start -->
<!-- auto-generated -->

# Stories

Actors are system actors (`system/ACT-###`).

## FEAT-001 — Data types stored and loaded without mapping code

### US-001 — Store and load data types with nested parts
- **Feature:** FEAT-001 · **Actor:** system/ACT-014 · **Realizes:** UC-001
- **Status:** delivered
- **Story:** As a service developer, I want to store and load my data types, nested parts included, without mapping code, so that my service keeps its data in the shared document store.
- **Acceptance criteria:**
  - **US-001.AC-1** — Given a stored value, when it is loaded, then every field, nested part, list element and map entry equals the stored one.
  - **US-001.AC-2** — Given a loaded value, when it is stored again, then the document is identical to the first.
  - **US-001.AC-3** — Given a nested type with no registration of its own, when whole values of the outer type are stored and loaded, then both succeed.
  - **US-001.AC-4** — Given a computed field marked excluded, when a value of the type is stored, then the field is absent from the stored document.
  - **US-001.AC-5** — Given a computed field marked excluded, when the document is loaded, then the field holds the value computed from the stored fields.
  - **US-001.AC-6** — Given a type-level value, when a value of the type is stored, then the type-level value is absent from the stored document.
  - **US-001.AC-7** — Given a stored moment, when it is loaded, then it is in universal time.
- **Source:** `[observed]` I-02 notes § Product, I-13 notes § Product (AC-1–AC-7)

### US-002 — A document that does not fit fails only its own load
- **Feature:** FEAT-001 · **Actor:** system/ACT-014 · **Realizes:** UC-002
- **Status:** delivered
- **Story:** As a service developer, I want a document that does not fit my type to fail only its own load, so that one bad document does not stop my service.
- **Acceptance criteria:**
  - **US-002.AC-1** — Given a document with a value that does not fit its field, when it is loaded, then the load fails.
  - **US-002.AC-2** — Given a failed load, when a document that fits is loaded next, then the load succeeds.
  - **US-002.AC-3** — Given a smaller whole number stored in a wider whole-number or fractional field, when it is loaded, then it loads with the same value.
  - **US-002.AC-4** — Given a value that does not fit inside a nested part, when the document is loaded, then the load of the whole document fails.
- **Source:** `[observed]` I-02 notes § Product (AC-1, AC-2, AC-4), I-13 notes § Product (AC-3); `[confirmed: user]` interview 2026-11-14 § Wave 1 (AC-3)

## FEAT-002 — Optional values and tolerant loading

### US-003 — Documents written before a field existed still load
- **Feature:** FEAT-002 · **Actor:** system/ACT-014 · **Realizes:** UC-003
- **Status:** delivered
- **Story:** As a service developer, I want documents written before a field existed to still load, so that adding a field needs no data migration.
- **Acceptance criteria:**
  - **US-003.AC-1** — Given a missing field with a declared default, when the document is loaded, then the field holds that default.
  - **US-003.AC-2** — Given a missing optional field without a declared default, when the document is loaded, then the field is empty.
  - **US-003.AC-3** — Given a missing field of any other kind without a declared default, when the document is loaded, then the field holds its type's zero value.
  - **US-003.AC-4** — Given a document with fields the type does not know, when it is loaded, then the load succeeds.
- **Source:** `[confirmed: user]` interview 2026-11-14 § Wave 1 (AC-1–AC-3); `[observed]` I-03 notes § Product (AC-4)

### US-004 — A field with no value stores and loads back with no value
- **Feature:** FEAT-002 · **Actor:** system/ACT-014 · **Realizes:** UC-004
- **Status:** delivered
- **Story:** As a service developer, I want fields with no value to store and load back with no value, so that an empty value never breaks a store or a load.
- **Acceptance criteria:**
  - **US-004.AC-1** — Given an empty optional value, when it is stored, then the field is absent from the stored document.
  - **US-004.AC-2** — Given an empty optional value that was stored, when the document is loaded, then the field is empty.
  - **US-004.AC-3** — Given a text field at any depth stored with no value, when the document is loaded, then the field has no value.
  - **US-004.AC-4** — Given a binary-content field with no value and no omit choice, when the value is stored, then the document holds the field stored empty.
  - **US-004.AC-5** — Given a binary-content field stored with no value, when it is loaded, then it has no value.
  - **US-004.AC-6** — Given a number, flag or moment field that is not optional and is stored with no value, when the document is loaded, then the load fails.
  - **US-004.AC-7** — Given a field stored empty and a declared default, when the document is loaded, then the field does not hold the default.
  - **US-004.AC-8** — Given a stored document whose optional field a typed update emptied, when a query reads back only that field, then the query returns that document with the field empty.
  - **US-004.AC-9** — Given stored documents in which some optional field values were emptied by a typed update, when a query asks for the distinct values of that field, then the query succeeds.
- **Source:** `[observed]` I-03 notes § Product, I-12 notes § Product (AC-1, AC-2, AC-5); `[confirmed: user]` interview 2026-11-14 § Wave 1 (AC-3, AC-4, AC-6, AC-7), § Wave 2 (AC-8, AC-9)

## FEAT-003 — Per-field storage choices

### US-005 — Store a field under a name of my choice
- **Feature:** FEAT-003 · **Actor:** system/ACT-014 · **Realizes:** UC-005
- **Status:** delivered
- **Story:** As a service developer, I want to store a field under a name of my choice, so that stored documents keep their field names when my type's names change.
- **Acceptance criteria:**
  - **US-005.AC-1** — Given a field with a declared stored name, when a value is stored, then the field appears in the document under that name.
  - **US-005.AC-2** — Given a field with a declared stored name, when a document is loaded, then the field is read from that name.
  - **US-005.AC-3** — Given a type with two fields mapped to one stored name, when the service starts, then the type is refused.
  - **US-005.AC-4** — Given a field stored under the store's identity name, when a value of the type is stored, then that field is the stored document's identity.
  - **US-005.AC-5** — Withdrawn.
- **Source:** `[observed]` I-04 notes § Product, I-11 notes § Product (AC-1, AC-2), I-14 notes § Product P1 (AC-4); `[confirmed: user]` interview 2026-11-14 § Wave 1 (AC-3)

### US-006 — Keep a field out of storage
- **Feature:** FEAT-003 · **Actor:** system/ACT-014 · **Realizes:** UC-006
- **Status:** delivered
- **Story:** As a service developer, I want to keep a field out of storage, so that working values and computed values are never written.
- **Acceptance criteria:**
  - **US-006.AC-1** — Given an excluded field, when a value is stored, then the field is absent from the document.
  - **US-006.AC-2** — Given an excluded field and a document with a field of the same name, when it is loaded, then the excluded field keeps its empty value.
  - **US-006.AC-3** — Given a type whose unsupported field is excluded, when it is registered, then registration succeeds.
- **Source:** `[observed]` I-04 notes § Product, I-11 notes § Product (AC-1–AC-3)

### US-007 — Leave a field out when it is empty or at its default
- **Feature:** FEAT-003 · **Actor:** system/ACT-014 · **Realizes:** UC-007
- **Status:** delivered
- **Story:** As a service developer, I want a field left out when it is empty or at its default, so that stored documents stay small and older documents stay compatible.
- **Acceptance criteria:**
  - **US-007.AC-1** — Given a field marked "omit when null" with no value, when it is stored, then the field is absent from the document.
  - **US-007.AC-2** — Given a field marked "omit when default" equal to its default, when it is stored, then the field is absent from the document.
  - **US-007.AC-3** — Given a field left out at its default, when the document is loaded, then the field holds that default.
  - **US-007.AC-4** — Given a field with "omit when default" switched off, when it is stored at its default, then the field is present in the document.
  - **US-007.AC-5** — Given a field marked with both choices and no value, when it is stored, then the field is absent from the document.
- **Source:** `[observed]` I-04 notes § Product, I-11 notes § Product (AC-1–AC-3, AC-5); `[confirmed: user]` interview 2026-11-14 § Wave 1 (AC-4)

### US-008 — Store an enumeration as a number or by name
- **Feature:** FEAT-003 · **Actor:** system/ACT-014 · **Realizes:** UC-008
- **Status:** delivered
- **Story:** As a service developer, I want to choose whether an enumeration is stored as a number or as its name, so that the stored documents use the form my data needs.
- **Acceptance criteria:**
  - **US-008.AC-1** — Given an enumeration field with no declared form, when a value is stored, then it is stored as a number.
  - **US-008.AC-2** — Given an enumeration field with the by-name form, when a value is stored, then it is stored as the value's name.
  - **US-008.AC-3** — Given an enumeration list on a field with a declared form, when values are stored, then each is stored in that form.
  - **US-008.AC-4** — Given an enumeration held in an optional value on a field with a declared form, when it is stored, then it is stored in that form.
  - **US-008.AC-5** — Given a by-name field whose stored name matches no value, when the document is loaded, then the load fails.
  - **US-008.AC-6** — Given a by-name field whose stored text is a number, when the document is loaded, then it loads that value.
- **Source:** `[confirmed: user]` interview 2026-11-14 § Wave 1 (AC-1, AC-5, AC-6); `[observed]` I-10 notes § Product, I-04 notes § Product (AC-2–AC-4)

## FEAT-004 — Typed filters and updates

### US-009 — Filter stored documents by my type's fields
- **Feature:** FEAT-004 · **Actor:** system/ACT-014 · **Realizes:** UC-009
- **Status:** delivered
- **Story:** As a service developer, I want to filter stored documents by my type's fields, so that my queries match what was stored.
- **Acceptance criteria:**
  - **US-009.AC-1** — Given a filter on a top-level field, when it is applied, then it matches the documents whose stored value satisfies it.
  - **US-009.AC-2** — Given a filter on an enumeration, when it is applied, then it matches by the field's declared form.
  - **US-009.AC-3** — Given a filter for an empty optional value, when it is applied, then it matches documents in which the field is absent.
  - **US-009.AC-4** — Given a filter for an empty optional value, when it is applied, then it matches documents in which the field is stored empty.
  - **US-009.AC-5** — Given a filter on a field of a registered nested type, when it is applied, then it matches the stored nested field.
  - **US-009.AC-6** — Given a filter on a renamed field, when it is applied, then it matches by the field's stored name.
  - **US-009.AC-7** — Given a filter on a by-name enumeration held in an optional value or a list, when it is applied, then it matches by name.
  - **US-009.AC-8** — Given a service that has just started and a field no filter has used since, when several filters on that field run at the same moment, then every filter succeeds.
- **Source:** `[observed]` I-05 notes § Product (AC-1, AC-2, AC-5); `[confirmed: user]` interview 2026-11-14 § Wave 1 (AC-3, AC-4, AC-6, AC-7), § Wave 2 (AC-8)

### US-010 — Typed updates store fields in the stored form
- **Feature:** FEAT-004 · **Actor:** system/ACT-014 · **Realizes:** UC-010
- **Status:** delivered
- **Story:** As a service developer, I want typed updates to store fields in the same form as storing the whole value, so that changed documents still load.
- **Acceptance criteria:**
  - **US-010.AC-1** — Given a field changed by a typed update, when the document is loaded, then the field holds the new value.
  - **US-010.AC-2** — Given a field set to empty by a typed update, when the document is loaded, then the field is empty.
  - **US-010.AC-3** — Given a typed update on a renamed field, when it is applied, then the field is changed under its stored name.
  - **US-010.AC-4** — Given an excluded field, when a typed update is applied, then no value is stored for it.
  - **US-010.AC-5** — Given a typed update to a by-name enumeration held in an optional value or a list, when the document is loaded, then the load succeeds.
  - **US-010.AC-6** — Given a service that has just started and a field no update has used since, when several updates on that field run at the same moment, then every update succeeds.
  - **US-010.AC-7** — Given a registered nested type that an update reaches, when the update sets an optional field of that nested type, then the changed document loads.
- **Source:** `[observed]` I-05 notes § Product (AC-1, AC-2); `[confirmed: user]` interview 2026-11-14 § Wave 1 (AC-3–AC-5), § Wave 1 rule (a) via I-05 Q3 (AC-7), § Wave 2 (AC-6)

## FEAT-005 — One registration per type, no settings

### US-011 — Register each type once at start
- **Feature:** FEAT-005 · **Actor:** system/ACT-014 · **Realizes:** UC-011
- **Status:** delivered
- **Story:** As a service developer, I want to turn the handling on with one registration per type at start, so that a type the library cannot handle stops my service before it stores anything.
- **Acceptance criteria:**
  - **US-011.AC-1** — Given a registered type, when it is stored or loaded anywhere in the service, then this handling is used.
  - **US-011.AC-2** — Given a type the service has not registered, when it is stored or loaded, then the store's default handling is used.
  - **US-011.AC-3** — Given a registered type, when it is registered a second time, then registration fails.
  - **US-011.AC-4** — Given a type the store has already handled, when it is registered, then registration fails.
  - **US-011.AC-5** — Given a type the library cannot handle, when it is registered, then registration refuses it.
  - **US-011.AC-6** — Given conventions set for the store's default handling, when a registered type is stored, then its stored form is unchanged by them.
  - **US-011.AC-7** — Given a type that carries a storage choice the library does not act on, when the service starts, then the type registers without error.
  - **US-011.AC-8** — Given a registered type that carries a storage choice the library does not act on, when a value of the type is stored, then the stored document is the same as for the type without that choice.
- **Source:** `[observed]` I-01 notes § Product, I-07 notes § Product, I-09 notes § Product (AC-1–AC-4), I-14 notes § Product P2 (AC-7, AC-8); `[confirmed: user]` interview 2026-11-14 § Wave 1 (AC-5, AC-6), § Wave 2 Rules (AC-7, AC-8)

## FEAT-006 — Stand-alone conversion of whole documents

### US-012 — Convert whole documents without registering the type
- **Feature:** FEAT-006 · **Actor:** system/ACT-014 · **Realizes:** UC-012
- **Status:** delivered
- **Story:** As a service developer, I want to convert whole documents to and from my data types without registering them, so that a one-off data move can read and write the shared form.
- **Acceptance criteria:**
  - **US-012.AC-1** — Given a document written by a storing converter, when it is loaded through a registration of the same type, then every field equals the value that was converted.
  - **US-012.AC-2** — Given a document written through a registration, when it is loaded through a loading converter, then every field equals the stored value.
  - **US-012.AC-3** — Given a data type the service has not registered, when a converter is built for it and converts a document, then the conversion returns a result.
  - **US-012.AC-4** — Given a type the library cannot handle, when a converter is built for it, then building is refused.
- **Source:** `[confirmed: user]` interview 2026-11-14 § Wave 1 (AC-1, AC-2); `[observed]` I-06 notes § Product (AC-3, AC-4)
<!-- product-spec:end -->
