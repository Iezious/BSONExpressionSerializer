<!-- docs-section-writer:start -->
# BSONExpressionSerializer - Overview

## Purpose

BSONExpressionSerializer is a published F# class library. It lets SIN services store their F# data types (records, option values, enums, arrays, string-keyed dictionaries, nested records) as MongoDB documents and load them back, and use typed filters and updates over those types. It plugs into the MongoDB BSON library as a per-type document serializer.

It has no executable, no network surface, no storage of its own and no configuration. Behaviour is driven only by the BSON mapping attributes on the stored types. Conventions a consumer registers with the driver do not apply to types registered with this library (see `../decisions/000.legacy.md` DEC-000-2). The public contract is in `interfaces.md`.

## Components

- **Document serializer** - the one entry point consumers use. There is one instance per stored type, registered with the driver. It serves whole-document writes and reads for that type, and supplies member information when the driver translates typed filters and updates.
- **Reader and writer** - per type, document to value and value to document. Both are built once, when the document serializer is created, and reused on every call. They are also exposed for stand-alone conversion, which builds them each time a caller creates one (see `../decisions/000.legacy.md` DEC-000-1). Nested types inside a stored type are handled by the same reader and writer.
- **Stand-alone conversion** - the reader and writer exposed directly, so a caller converts between a type and a raw document without registering the type. The stored form is the document serializer's. The Migrator uses it. Detail is in `interfaces.md` section Stand-alone conversion.
- **Optional-value member serializers** - handle option and value-option members when a filter or update value is translated. Nullable members use the BSON library's own nullable handling.
- **Other exported surface** - conversion operators that BackOffice and the Migrator use, an abandoned reader override registry, and unused leftovers and test hooks. None is supported. Detail is in `interfaces.md`.
- **Tests** - cover reader and writer behaviour and filter and update translation.

Stack: an F# class library built on the MongoDB BSON library.

## Call structure

1. At service start, the consumer creates one document serializer per type and registers it with the driver. Creating the serializer builds that type's reader and writer. Registration must come before any store use of the type, including use as a nested type in a typed filter or update (`interfaces.md` section Registration contract).
2. On a store write, the driver calls the serializer, which runs the writer and hands the document to the driver.
3. On a store read, the driver hands over the document and the reader builds the value.
4. When the driver translates a typed filter, update or sort, including one that names a field as text, it asks the serializer for that member's serializer. The answer depends on the member's type (`interfaces.md` section Typed filters and updates). Member information is resolved on the first request for each member. Nested and inner types are resolved through the driver's process-wide registry. Each answer is kept for the life of the serializer instance, which for a registered type is the life of the process (see `../decisions/000.legacy.md` DEC-000-32).
5. Stand-alone: the caller creates the reader or writer for a type, fetches or stores raw documents itself, and calls the function. The driver's registry is not involved.

## Failure model

- A type the library cannot build fails when its serializer is created. Registration happens at start-up, so on the registered path this is at service start, not on first use (see `../decisions/000.legacy.md` DEC-000-3). For stand-alone conversion, it fails when the caller creates the function. The type must be constructible without arguments, and every public instance property it maps must be settable (`CLIMutable` records meet both). Otherwise creating the serializer fails.
  - The requirement applies to every nested type at any depth.
  - Some combinations of honoured mapping attributes also fail at creation (`interfaces.md` sections Write rules and Mapping attributes).
  - Apart from a stored-name clash (below), a creation failure does not name the member, and in most cases not the type (known limit; DEC-000-3).
  - Exceptions (known limits; DEC-000-5):
    - A type that contains itself, directly or through an array, optional, map or nested type, does not fail with an error. Creating its serializer terminates the process with a stack overflow, which cannot be handled.
    - A member declared as the universal object type is accepted. It is stored as an empty embedded document, and its value is lost.
  - Two members mapped to the same stored name are refused at creation, at any depth. The error names the type, the stored name and the members (see `../decisions/001.known-defects.md` DEC-001-1).
- Registering a type twice, or after the process has already used it through the driver, is refused (`interfaces.md` section Registration contract). Both refusals surface at start-up when registration happens there.
- A read never fails because a field is missing or unknown.
- A stored value that does not fit the field's type fails the read of the whole document, including a misfit inside a nested part. The error identifies neither the field nor the document. Nothing in the library catches it, so it fails the calling operation.
  - Examples:
    - an enum stored by a name that matches no member;
    - a null stored in a non-optional value field.
  - Exception: an enum stored as a number outside its member set reads without error (known limit; DEC-000-22).
- Mapping attributes the library does not honour are ignored without error (DEC-000-25). The exception is an unsupported enumeration form: it is ignored when storing, but a typed filter or update on that field fails when it is translated (DEC-000-20).
- A typed filter or update the library cannot translate fails that query when it is built, not at service start. Examples: an unsupported enumeration form, or a path that reaches inside an optional value (known limits; DEC-000-20, DEC-000-31).
- Concurrency:
  - One serializer instance per type serves every concurrent request in the process. Whole-document reads and writes keep no shared state.
  - Member resolution is safe for simultaneous first use. Concurrent translations that first use members of one type all succeed and get the same answer for a member (DEC-001-7).
- Reading one member back through member information reads an option or value-option value stored as an explicit null as empty, as it does a value that was left out. This covers a typed projection of that member, a shape built from several members, the distinct values of a member, and an aggregation stage that selects it (DEC-001-8).
- Errors reach the calling service as exceptions. The library emits no logs, metrics or health signals.

## Consumers

- **APIV2** - its domain library.
- **BackOffice** - its logic library; it also uses the conversion operators.
- **Dogma** - its domain library and the admin CLI.
- **Karma** - the Runner. The Karma Domain project references the package but does not use it.
- **Migrator** - the data migrator: registration, stand-alone conversion in its import jobs, and the conversion operators.

Each consumer uses the document serializer through registration. The Migrator also converts stand-alone in its import jobs. It reads legacy documents into shared types, and writes some kinds back as raw documents without registering them. BackOffice declares a stand-alone reader that it never calls. BackOffice and the Migrator also use the conversion operators. Each registers shared Models types plus its own projection types, and each registers its own set. A type that a consumer does not register is handled by the driver's default mapping in that consumer, unless that consumer converts it stand-alone.

Pairing contract: the shared Models records (option fields, enums stored as strings) are written to be stored through this library. Models does not reference the library, so each consumer's registration makes the pairing.

## Compatibility

- The library targets a single .NET runtime generation. A consumer on an older runtime cannot take newer releases without moving up. A release also requires at least the generation of the BSON object-model library, and the F# core-library version, it was built against. A consumer whose store client uses an older object-model generation, or that holds the core library lower, cannot adopt the release until it moves up, whatever its runtime (see `../decisions/000.legacy.md` DEC-000-40, DEC-000-41, DEC-000-42).
- Two consumer cohorts exist today. Karma and the Migrator are on an older release line, an older runtime and an older store-client generation. APIV2, BackOffice and Dogma are on the current ones.
- The cohorts differ beyond the target:
  - On the older line with the older store client, typed filters and updates translate decimal and 128-bit decimal members as text. Filters on them do not match, and an update stores a form the reader refuses (known limit; DEC-000-29).
  - The older line stores unique-identifier members in the legacy binary form. The current line refuses them and cannot read that form (DEC-000-8). This is latent, because no shared type has such a member.
- The defect fixes (see `../decisions/001.known-defects.md`) change behaviour that a consumer moving up from an earlier release can observe:
  - a type with two members mapped to one stored name is refused at service start;
  - a text field stored as null loads as no value, and a binary field with no value is stored as null instead of failing the write;
  - an omission attribute switched off has no effect;
  - typed filters, updates and sorts address renamed fields by their stored name, and enumerations inside optionals and arrays in their declared form;
  - a typed filter, update or sort on an excluded field is refused when it is built; before, a filter matched nothing;
  - documents in which an earlier release's typed update wrote a renamed field under its property name keep that stray field, which the reader ignores; cleaning them is a data task for the owning service.
- The stored form is a data contract shared by every service that reads or writes the same collections. Changing how a type family is stored changes the documents that every consumer sees, so it is a cross-service change.
- The library depends on no other SIN package.

## Distribution

- One NuGet package, `Iezious.Libs.BSONExpressionSerializer`. The package id equals the assembly name.
- It is released on a version tag, after build and test. The version comes from the tag; none is stored in the repo.
- A build-and-test workflow runs on pushes to `master` (see `../decisions/001.known-defects.md` DEC-001-9). No workflow runs on pull requests.
- Every workflow installs the SDK of the runtime generation the library targets before it builds, rather than relying on the build machine's preinstalled SDK (DEC-001-10).
- A pre-release workflow exists but is unused, and no pre-release has been published (see `../decisions/000.legacy.md` DEC-000-46).
- It is published to the package feed of the maintainer's personal account (`iezious`), not the SIN organisation feed. Consumers must therefore list both feeds to restore (`.github/workflows/release.yml:22`).
- It is not deployed, and nothing about it lives under `SinSystems.Deployments/`.

## Open questions / Known gaps

None at service level.
<!-- docs-section-writer:end -->
