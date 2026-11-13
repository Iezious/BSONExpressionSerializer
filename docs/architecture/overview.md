<!-- docs-section-writer:start -->
# BSONExpressionSerializer - Overview

## Purpose

BSONExpressionSerializer is a published F# class library. It lets SIN services store their F# data types (records, option values, enums, arrays, string-keyed dictionaries, nested records) as MongoDB documents and load them back, and use typed filters and updates over those types. It plugs into the MongoDB BSON library as a per-type document serializer.

It has no executable, no network surface, no storage of its own and no configuration. Behaviour is driven only by the BSON mapping attributes on the stored types. The public contract is in `interfaces.md`.

## Components

- **Document serializer** - the one entry point consumers use. There is one instance per stored type, registered with the driver. It serves whole-document writes and reads for that type, and supplies member information when the driver translates typed filters and updates.
- **Reader and writer** - per type, document to value and value to document. Both are built once, when the document serializer is created, and reused on every call. Nested types inside a stored type are handled by the same reader and writer.
- **Optional-value member serializers** - handle option, value-option and nullable members when a filter or update value is translated.
- **Other exported surface, used by no consumer** - stand-alone reader and writer builders, a reader override registry and conversion operators. Detail is in `interfaces.md`.
- **Tests** - cover reader and writer behaviour and filter and update translation.

Stack: an F# class library built on the MongoDB BSON library.

## Call structure

1. At service start, the consumer creates one document serializer per type and registers it with the driver. Creating the serializer builds that type's reader and writer.
2. On a store write, the driver calls the serializer, which runs the writer and hands the document to the driver.
3. On a store read, the driver hands over the document and the reader builds the value.
4. When the driver translates a typed filter or update, it asks the serializer for the member's serializer. The answer depends on the member's type (`interfaces.md` section Typed filters and updates).

## Failure model

- A type the library cannot build fails when its serializer is created, which is at service start, not on first use. The type must be constructible without arguments, and every public instance property it maps must be settable (`CLIMutable` records meet both). Otherwise creating the serializer fails.
- A read never fails because a field is missing or unknown.
- A stored value that does not fit the field's type fails that read. Example: an enum stored by a name that matches no member.
- Mapping attributes the library does not honour are ignored without error.
- Errors reach the calling service as exceptions. The library emits no logs, metrics or health signals.

## Consumers

- **APIV2** - its domain library.
- **BackOffice** - its logic library.
- **Dogma** - its domain library and the admin CLI.
- **Karma** - the Runner. The Karma Domain project references the package but does not use it.
- **Migrator** - the data migrator.

Each consumer uses only the document serializer, through registration. Each registers shared Models types plus its own projection types, and each registers its own set. A type that a consumer does not register is handled by the driver's default mapping in that consumer.

Pairing contract: the shared Models records (option fields, enums stored as strings) are written to be stored through this library. Models does not reference the library, so each consumer's registration makes the pairing.

## Compatibility

- The library targets a single .NET runtime generation. A consumer on an older runtime cannot take newer releases without moving up.
- Two consumer cohorts exist today. Karma and Migrator are on an older release line and an older runtime. APIV2, BackOffice and Dogma are on the current ones.
- The stored form is a data contract shared by every service that reads or writes the same collections. Changing how a type family is stored changes the documents that every consumer sees, so it is a cross-service change.
- The library depends on no other SIN package.

## Distribution

- One NuGet package, `Iezious.Libs.BSONExpressionSerializer`. The package id equals the assembly name.
- It is released on a version tag, after build and test. The version comes from the tag; none is stored in the repo.
- It is published to the package feed of the maintainer's personal account (`iezious`), not the SIN organisation feed. Consumers must therefore list both feeds to restore (`.github/workflows/release.yml:22`).
- It is not deployed, and nothing about it lives under `SinSystems.Deployments/`.

## Open questions / Known gaps

None at service level.
<!-- docs-section-writer:end -->
