<!-- docs-section-writer:start -->
# BSONExpressionSerializer - Interfaces

## Purpose

The public contract consumers depend on: the document serializer, its registration, the stored form by type family, the filter and update translation, and stand-alone conversion. The component view is in `overview.md`.

## Document serializer

`ExpressionDocumentSerializer<'T>`, defined at `src/Iezious.Libs.BSONExpressionSerializer/ExpressionDocumentSerializer.fs:13`.

- It has a parameterless constructor and no constraint on the type parameter.
- It implements the BSON library's serializer contracts `IBsonSerializer<'T>` and `IBsonDocumentSerializer`. These are the extension point consumers rely on.
- It does not implement the BSON library's identity-provider contract. The driver therefore never reads, assigns or generates an identity for a registered type (see `../decisions/000.legacy.md` DEC-000-26).
- One instance per type serves every concurrent request in the process (`overview.md` section Failure model).
- It serves whole-document serialize and deserialize for `'T`.
- It supplies member information for typed filters and updates. The interface member is defined at `src/Iezious.Libs.BSONExpressionSerializer/ExpressionDocumentSerializer.fs:148`.

## Registration contract

- Register one serializer per type with the BSON library's process-wide serializer registry (`BsonSerializer.RegisterSerializer`), once per process, at start-up, before the first store use. No other setup and no configuration are needed. Conventions registered with the driver do not apply to registered types (see `../decisions/000.legacy.md` DEC-000-2).
- A second registration for a type in one process is refused, and the first stays in force (DEC-000-4).
- Registration of a type is refused once the process has used that type through the driver in any way, because the driver then keeps its default handling for it (DEC-000-4, DEC-000-32). Such use includes:
  - opening a collection of the type;
  - storing or loading it, or a type that contains it, with default handling;
  - translating a typed filter or update that reaches it as a member, including a nested member of a registered type.
- So every type is registered before the process makes any store use of it. This includes every nested type that typed filters or updates reach.
- Registration applies wherever the process stores or loads a value of the type: as a whole document, as a member of a type that is not registered, or inside a hand-built document.
- Only registered types get this handling.
  - Inside a registered type, a nested type always takes this library's form in whole-document reads and writes, whatever is registered for it.
  - A nested value handled outside its container's whole-document path takes this library's form only if the nested type is itself registered. Examples: a typed update that sets the nested value, a hand-built change document that carries it, or a container that is not registered.
  - Otherwise the nested value takes the default mapping's form. The registered container cannot read that form back when an optional inside it holds a value (known limit; DEC-000-5).
- A type not registered in a process is handled there by the driver's default mapping, which does not apply these rules.
- The type must be constructible without arguments, and every public instance property it maps must be settable (`CLIMutable` records meet both). Otherwise creating the serializer fails. This applies to every nested type at any depth.

## Stored form, by type family

- **Document layout:** a type becomes one document with one field per public instance property. Properties marked `BsonIgnore` are skipped. The field name is the property name, or the `BsonElement` name when one is given. Two members with the same stored name refuse the type at creation (see `../decisions/001.known-defects.md` DEC-001-1).
  - The document identity is the field whose stored name is `_id`: a property named `_id`, or one whose `BsonElement` name is `_id`. `BsonId` does not select it (known limit; see `../decisions/000.legacy.md` DEC-000-26). The library never supplies or generates an identity: the writing service sets it.
  - No type marker is written. A registration or member whose declared type is a parent stores only the declared type's members and loads as the declared type, so a subtype's extra fields are lost (known limit; DEC-000-27).
- **Scalars:** string, bool, 32- and 64-bit integers, double, decimal, Decimal128 and ObjectId each map to the matching native BSON value. Any other scalar kind fails when the serializer is created, including inside optionals, arrays, maps and nested types (DEC-000-7).
- **Other single values:** DateTime maps to a BSON date. A byte array maps to binary. An embedded raw document maps to an embedded copy. A moment in time is stored as a universal-time instant. A value given in local time, or without zone information, is converted using the writing process's time zone (DEC-000-24).
- **Enums:** stored as a 32-bit integer by default. `BsonRepresentation` selects a 64-bit integer or the member name instead.
  - The field's form declaration governs every enumeration value the field holds, including the contents of its optional, nullable or array (DEC-000-19).
  - `BsonRepresentation` acts only on enum members. Any form other than 32-bit integer, 64-bit integer or name is stored and read as a 32-bit integer (known limit; DEC-000-20).
- **Containers:** an array maps to a BSON array. A .NET dictionary with string keys maps to an embedded document. A nested record or class maps to an embedded document.
- **Option, value option, nullable:** null when empty, otherwise the inner value. As a field, an empty option or value option is left out entirely (see Write rules).
  - An empty nullable field is written as null, unless an omission marker is set, in which case it is left out. Both forms read back as empty (DEC-000-16).
  - An update that empties an optional field stores null. Whole-record writes never produce that form for option or value option. Both forms read back as empty, and a filter for empty matches both (DEC-000-34).
- **Not supported:** creating the serializer fails for a type with a member of any of these kinds, including members of nested types and members inside optional, nullable, array and map members:
  - Guid (refused since the move to the current BSON generation; DEC-000-8).
  - Collection kinds other than arrays and string-keyed .NET dictionaries, such as .NET lists, F# lists, F# maps and sequences.
  - Dictionaries with non-string keys.
  - Single-precision float, 8-bit and 16-bit integers, unsigned integers, char.
  - `DateTimeOffset`, `TimeSpan`.
  - Reference and struct tuples, union types, interface-typed members.
  - Types without a parameterless constructor.
- One silent case: a member typed as the universal object type is accepted and stored as an empty embedded document, and its value is lost (known limit; DEC-000-5).

## Read rules

- A missing field never fails the read. The property takes its `BsonDefaultValue` if one is declared, is empty if it is an option or value option, and otherwise takes the type's default. The declared default applies only when the field is missing. A field stored as null does not take it. A field left out under either omission marker reads back as its default, not as null. So with both markers and a non-null declared default, a null value loads as the declared default.
- A field stored as null reads as empty for option, value-option and nullable properties. It reads as no value for text, arrays, dictionaries, nested types, byte arrays and embedded raw documents, at any depth: a nested type, an array element, a dictionary value (see `../decisions/000.legacy.md` DEC-000-14; text: `../decisions/001.known-defects.md` DEC-001-2).
  - For any other value-type property, a stored null fails the read.
- Unknown fields in the document are ignored. They are not kept, so writing back a value read this way drops them (DEC-000-13).
- No numeric conversion is made, with one exception: 64-bit integer and double properties accept values stored as 32-bit integers. A value stored in any other numeric kind than the property's fails the read (DEC-000-23).
  - Enumerations stored as numbers are read only in the number kind their form declares.
  - A date property accepts only a stored date.
  - The driver's default mapping, used for types not registered in a process, accepts any numeric conversion that loses nothing. A document can therefore load as an unregistered type and fail as a registered one.
- Dates are returned as UTC.

## Write rules

- An empty option or value-option field is left out. An omission marker replaces this rule. An option field with `BsonIgnoreIfDefault` and a declared default stores an empty value as null, and never leaves out the declared value (see `../decisions/000.legacy.md` DEC-000-17, DEC-000-18).
- `BsonIgnoreIfNull` omits a null value. On a nullable field it omits an empty value. Switched off, it has no effect, and the field follows the remaining rules (see `../decisions/001.known-defects.md` DEC-001-4).
  - It applies only to fields that can hold null: text, binary, raw document, nested type, map, array, option, nullable.
  - Switched on, on any other kind, value option included, creating the serializer fails. This holds for members of nested types and array element types too (known limit; DEC-000-3).
- `BsonIgnoreIfDefault` omits a value equal to the default. The default is the `BsonDefaultValue` value if one is declared.
  - Equal to the default means equal by value for numbers, flags, dates, enumerations, text and nullable numbers.
  - For nested types, maps, arrays and raw documents, the default is null unless an array default is declared, and only an absent value matches. An empty map, array or nested value is written, and a declared array default never matches (known limit; DEC-000-18).
  - Switched on, on a value-option field, creating the serializer fails (known limit; DEC-000-3).
  - Switched off, it has no effect, and the field follows the remaining rules (DEC-001-4).
  - A declared default must convert to the field type, or creating the serializer fails (DEC-000-12).
- The two attributes combine.
- Every other property is always written, and a null is written as BSON null, at any depth. This includes a null byte array, a null element of an array of byte arrays and a null dictionary value (DEC-001-2, DEC-001-3).

## Mapping attributes

The attribute names are public contract.

- **Honoured:** `BsonElement`, `BsonIgnore`, `BsonDefaultValue`, `BsonRepresentation` (enums), `BsonIgnoreIfNull`, `BsonIgnoreIfDefault`.
  - `BsonRepresentation` acts only on enum members, in the 32-bit integer, 64-bit integer and name forms.
  - Not read on the honoured attributes: the `Order` of `BsonElement`; `AllowOverflow` and `AllowTruncation` of `BsonRepresentation` (see `../decisions/000.legacy.md` DEC-000-25). The on/off argument of `BsonIgnoreIfDefault` and of `BsonIgnoreIfNull` is read (DEC-001-4).
  - `BsonDefaultValue` on a `BsonIgnore`d member has no effect.
- **Ignored without error:** every other mapping attribute, on a member or on the type (DEC-000-25). This includes `BsonId`, `BsonRequired`, `BsonConstructor`, `BsonFactoryMethod`, `BsonExtraElements`, `BsonIgnoreExtraElements`, `BsonSerializer`, `BsonGuidRepresentation`, `BsonDateTimeOptions`, `BsonTimeSpanOptions`, `BsonDictionaryOptions`, `BsonDiscriminator`, `BsonKnownTypes` and `BsonNoId`.

## Typed filters and updates

- Concurrent first use of members is safe (DEC-001-7).
- Member information covers only the type's own properties. A path into a nested type is translated one segment at a time, by that nested type's own handling in the driver (see `../decisions/000.legacy.md` DEC-000-31).
  - A registered nested type uses its stored form. A nested type that is not registered falls to the driver's default mapping. There, optional values differ from the stored form, and so do enumerations without a declared form when the service sets a by-name rule for all types. Filters then do not match, and updates leave records that cannot be loaded.
  - A field of a nested type held in an optional value cannot be reached; translation is refused (known limit; DEC-000-31).
- The driver asks for member information for sorts too, and whenever a filter, update or sort names a field as text.
- Translation targets the stored name: the `BsonElement` name when one is declared, otherwise the property name, as in whole-document storage. A `BsonIgnore`d member gets no member information, so a filter, update or sort on it is refused when it is built (see `../decisions/001.known-defects.md` DEC-001-6). For a nested type that is not registered, the default mapping applies the stored name.
- An enum member is translated in its declared form. For a form other than 32-bit integer, 64-bit integer or name, translation fails (DEC-000-20).
- An enum inside an option, value option, nullable or array is translated in the field's declared form, like a top-level enum, so filters match and updates store the form the reader expects (DEC-001-5). An enum held as a dictionary value takes whatever the driver's registry holds for the dictionary.
- Option and value-option members translate an empty value to null. A present value uses the driver's serializer for the inner type.
  - Nullable members use the BSON library's own nullable handling (DEC-000-29).
  - Reading a single option or value-option member back reads an explicit null as empty (`overview.md` section Failure model; DEC-001-8).
- Any other member uses whatever the driver's registry holds for the member's type: this library's serializer for a registered nested type, otherwise the driver's default handling. Parity with the stored form therefore holds only where that handling equals this library's form.
  - It holds on the current store line. On the older store line, decimal and 128-bit decimal members translate as text (known limit; DEC-000-29; `overview.md` section Compatibility).
- A field name the type does not have is passed to the driver unchanged, and its value takes the driver's default form.

## Stand-alone conversion

Two public functions convert between a type and an in-memory raw document without registration:

- `ExpressionReader.CreateReader<'T>()`, defined at `src/Iezious.Libs.BSONExpressionSerializer/ExpressionReader.fs:240`, returns a function from a raw document to `'T`.
- `ExpressionWriter.CreateWriter<'T>()`, defined at `src/Iezious.Libs.BSONExpressionSerializer/ExpressionWriter.fs:332`, returns a function from `'T` to a raw document.

Rules:

- For a type the registered path accepts, stand-alone conversion has the same stored form, read rules, write rules and mapping-attribute handling as registration (see `../decisions/000.legacy.md` DEC-000-1, DEC-000-9).
- Each call builds a new function (DEC-000-1).
- Each function is built on its own, whereas registration builds both, so a type that registration refuses is not always refused here:
  - the writer accepts some unsupported kinds and writes their public properties as an embedded document;
  - it writes an empty document for interface-typed members;
  - it builds for dictionaries with non-string keys, but every write then fails;
  - it terminates the process for a member that is an F# list;
  - the reader accepts Guid.
- Use only types that registration accepts.
- It does not use the driver's process-wide registry. It needs no registration and is unaffected by one for the same type.
- It supplies no member information. Typed filters and updates over a type need the type's registration.
- The caller moves raw documents to and from the store itself.

## Other exported surface

Public, but not supported: no guarantee is stated for it, and it is kept rather than removed. Of these, only the conversion operators are used by in-repo services. All public names live in the namespace `Iezious.Libs.BSONExpressionSerializer`.

- **Optional-value member serializers:** `NullableSerializer`, `ValueOptionSerializer` and `OptionSerializer`, defined at `src/Iezious.Libs.BSONExpressionSerializer/HelperSerializers.fs:16`, `:43` and `:71`. Member translation uses the value-option and option serializers. `NullableSerializer` is used by nothing, including the library (see `../decisions/000.legacy.md` DEC-000-37).
- **Reader override registry** (`ExpressionReaderRegistry.Add`, `Get`; `src/Iezious.Libs.BSONExpressionSerializer/ExpressionReaderRegistry.fs:11`, `:14`): a process-wide, add-only registry from a type to a document-to-value function. `Get` returns the added function, or else one that reads through the driver's own handling of the type. A second `Add` for a type is refused. No conversion in this library consults it, whether registered, nested or stand-alone. Abandoned (DEC-000-36).
- **Conversion operators:** the `Utils` module, defined at `src/Iezious.Libs.BSONExpressionSerializer/Utils.fs:5-8`.
  - It is not opened with the namespace: a caller opens `Utils` after opening the namespace, or qualifies the names.
  - It has three operators: convert a value through an implicit conversion declared on either type; convert a value to a document and then through the target type's conversion from a document; convert a value to a document.
  - The shared Utils library holds the master copy of these definitions (DEC-000-39).
  - In-repo users: the library's tests, and BackOffice and the Migrator, which reach this copy by opening `Utils` after this namespace.
- **Unused leftovers and test hooks**, public but not supported:
  - the builders `ExpressionReader.buildReader` (`src/Iezious.Libs.BSONExpressionSerializer/ExpressionReader.fs:20`) and `ExpressionWriter.build` (`src/Iezious.Libs.BSONExpressionSerializer/ExpressionWriter.fs:42`), called from outside the library only by its own tests (DEC-000-38);
  - two unused functions, `ExpressionReaderRegistry.proto` (`src/Iezious.Libs.BSONExpressionSerializer/ExpressionReaderRegistry.fs:21`) and `ExpressionWriter.privateIgnoreDefault` (`src/Iezious.Libs.BSONExpressionSerializer/ExpressionWriter.fs:32`) (DEC-000-37);
  - the empty module `ExpressionStreamReader` (`src/Iezious.Libs.BSONExpressionSerializer/ExpressionStreamReader.fs:5`) (DEC-000-10).

## Model coupling

The library has no dependency on the shared Models. Models types are among the types consumers register.

## Consumers

- Every in-repo consumer uses the document serializer through registration.
- The Migrator also uses stand-alone conversion in its import jobs.
- BackOffice declares a stand-alone reader that it never calls.
- BackOffice and the Migrator also use the conversion operators.
- Details are in `overview.md` section Consumers.

## Open questions / Known gaps

None at service level.
<!-- docs-section-writer:end -->
