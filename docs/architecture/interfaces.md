<!-- docs-section-writer:start -->
# BSONExpressionSerializer - Interfaces

## Purpose

The public contract consumers depend on: the document serializer, its registration, the stored form by type family, and the filter and update translation. The component view is in `overview.md`.

## Document serializer

`ExpressionDocumentSerializer<'T>`, defined at `src/Iezious.Libs.BSONExpressionSerializer/ExpressionDocumentSerializer.fs:12`.

- It has a parameterless constructor and no constraint on the type parameter.
- It implements the BSON library's serializer contracts `IBsonSerializer<'T>` and `IBsonDocumentSerializer`. These are the extension point consumers rely on.
- It serves whole-document serialize and deserialize for `'T`.
- It supplies member information for typed filters and updates. The interface member is defined at `src/Iezious.Libs.BSONExpressionSerializer/ExpressionDocumentSerializer.fs:110`.

## Registration contract

- Register one serializer per type with the BSON library's process-wide serializer registry (`BsonSerializer.RegisterSerializer`), once per process, at start-up, before the first store use. No other setup and no configuration are needed.
- Only registered types get this handling. Nested types inside a registered type are covered for whole-document reads and writes without a registration of their own.
- A type not registered in a process is handled there by the driver's default mapping, which does not apply these rules.
- The type must be constructible without arguments, and every public instance property it maps must be settable (`CLIMutable` records meet both). Otherwise creating the serializer fails.

## Stored form, by type family

- **Document layout:** a type becomes one document with one field per public instance property. Properties marked `BsonIgnore` are skipped. The field name is the property name, or the `BsonElement` name when one is given.
- **Scalars:** string, bool, 32- and 64-bit integers, double, decimal, Decimal128 and ObjectId each map to the matching native BSON value. Only the scalar types listed are supported.
- **Other single values:** DateTime maps to a BSON date. A byte array maps to binary. An embedded raw document maps to an embedded copy.
- **Enums:** stored as a 32-bit integer by default. `BsonRepresentation` selects a 64-bit integer or the member name instead.
- **Containers:** an array maps to a BSON array. A .NET dictionary with string keys maps to an embedded document. A nested record or class maps to an embedded document.
- **Option, value option, nullable:** null when empty, otherwise the inner value. As a field, an empty option or value option is left out entirely (see Write rules).
- **Not supported:** creating the serializer fails for a type with a member of any of these kinds, including members of nested types:
  - Guid.
  - Collection kinds other than arrays and string-keyed .NET dictionaries, such as .NET lists, F# lists, F# maps and sequences.
  - Dictionaries with non-string keys.

## Read rules

- A missing field never fails the read. The property takes its `BsonDefaultValue` if one is declared, is empty if it is an option or value option, and otherwise takes the type's default.
- Unknown fields in the document are ignored.
- 64-bit integer and double properties accept values stored as 32-bit integers.
- Dates are returned as UTC.

## Write rules

- Empty option and value-option fields are omitted.
- `BsonIgnoreIfNull` omits a null value.
- `BsonIgnoreIfDefault` omits a value equal to the default. The default is the `BsonDefaultValue` value if one is declared.
- The two attributes combine.
- Every other property is always written, and a null is written as BSON null. The exception is a null byte array: writing it fails unless `BsonIgnoreIfNull` or `BsonIgnoreIfDefault` omits it.

## Mapping attributes

The attribute names are public contract.

- **Honoured:** `BsonElement`, `BsonIgnore`, `BsonDefaultValue`, `BsonRepresentation` (enums), `BsonIgnoreIfNull`, `BsonIgnoreIfDefault`.
- **Ignored without error:** `BsonId`, `BsonRequired`, `BsonConstructor`, `BsonExtraElements`, `BsonIgnoreExtraElements`, `BsonSerializer`, `BsonGuidRepresentation`, `BsonDateTimeOptions`.

## Typed filters and updates

- Member information covers only the type's own top-level members.
- An enum member is translated in the same representation used to store it.
- An enum inside an option, value option, nullable or array is stored in its declared representation, but its filter value is translated as a 32-bit integer, so a member-name representation there does not match in filters.
- Option, value-option and nullable members translate empty to and from null. A present value uses the driver's serializer for the inner type.
- Any other member uses the serializer the driver has for that member's type. A nested type registered with this library therefore uses its stored form.
- An unknown member, or a member type the driver has no serializer for, is reported to the driver as unresolvable.

## Other exported surface

Public, used by no consumer, and no guarantee is stated for it. All public names live in the namespace `Iezious.Libs.BSONExpressionSerializer`.

- **Stand-alone reader and writer builders:** `ExpressionReader.CreateReader`, defined at `src/Iezious.Libs.BSONExpressionSerializer/ExpressionReader.fs:233`; `ExpressionWriter.CreateWriter`, defined at `src/Iezious.Libs.BSONExpressionSerializer/ExpressionWriter.fs:283`.
- **Optional-value member serializers:** `NullableSerializer`, `ValueOptionSerializer` and `OptionSerializer`, defined at `src/Iezious.Libs.BSONExpressionSerializer/HelperSerializers.fs:7`, `:34` and `:60`. Member translation uses them, and they are also public.
- **Reader override registry:** `ExpressionReaderRegistry.Add` and `Get`, defined at `src/Iezious.Libs.BSONExpressionSerializer/ExpressionReaderRegistry.fs:11` and `:14`. The document serializer does not read the registry, so an override does not change how a registered type is read.
- **Conversion operators:** the `Utils` module, defined at `src/Iezious.Libs.BSONExpressionSerializer/Utils.fs:6-8`. It duplicates operators of the shared Utils library.

## Model coupling

The library has no dependency on the shared Models. Models types are among the types consumers register.

## Consumers

Every in-repo consumer uses only the document serializer, through registration (`overview.md` section Consumers).

## Open questions / Known gaps

None at service level.
<!-- docs-section-writer:end -->
