# docs/architecture

Architecture layer contract — doc root `BSONExpressionSerializer` (service). Doc root `BSONExpressionSerializer` is a **service** doc root inside the `_SIN` multi-service legacy root; the system doc root (key `system`, network layer) is `D:/GitRoot/_SIN/docs/`.

## Question this layer answers

HOW the system is shaped — at the altitude of services, components, and the contracts between them.

## What it holds

The system's parts and their responsibilities; boundaries between them; **contracts between parts** — public APIs, DTOs and messages, configuration document shape (keys and meaning, not defaults), protocols, call structure and ordering between components; cross-cutting models (security, failure, identity, observability — which signals exist, not their ids); stack choices at the level of "this library is built on Kestrel"; deployment topology; parity and invariants; what is deliberately out of scope. Every choice with its reasoning.

**Architecture is what changes when a new service, contract, DTO, call structure or cross-component rule is introduced.** If a feature needs one of those and the architecture does not have it, the architecture is incomplete — planning stops until `/architect` adds it.

## Never in architecture

- A type, class, method or field internal to one component.
- A framework mechanism used to realize a contract (`SocketsHttpHandler`, `IOptionsMonitor`, `undici` dispatcher internals).
- Numeric defaults, limits, timeouts, retry formulas.
- Log event ids and metric attribute values.
- Test ids.
- Step, DoD or review-item ids.
- The history of how a feature's plan evolved.

These go to `docs/decisions/` (or below: folder `AGENTS.md` `## Conventions`, code comments).

## No product ids

Architecture is about HOW things are done; it is not a map of who realizes which requirement. Architecture docs carry **no product ids**: no `Realizes:` headers, no `FEAT-` / `UC-` / `US-` citations, no requirement restated in prose. The what → where mapping lives in the plans layer (`roadmap.md` / `brief.md` `Delivers:` + `Architecture:`), never here. A text change in one layer never forces an edit in the other.

## Classification test

Apply to any sentence about to be written into documentation, in order:

1. **Would an actor or the business notice if this changed?** → product. (If the only reason to notice is that a test would fail, it is not product.)
2. **Does it add, remove or change a service, a component, a contract between components (API, DTO, message, config key, protocol, call order), or a rule that spans components?** → architecture.
3. **Would it still be true if the component were re-implemented with a different library or framework?** Yes → architecture (if it passed 2) · No → decision.
4. **Does it name a type, method, field, default value, timeout, event id, test id or step id?** → decision, unless the name is itself a public contract (then architecture names the contract, a decision names the mechanism).
5. Otherwise it is below the decisions layer: a folder convention or a code comment.

Steps 2–4 decide architecture vs decision.

## `quick-reference.md` and decision history

`quick-reference.md` is an index of architecture — contracts, keys, ids, open architecture questions — never a second place for mechanisms or a running log. `## Decision history` sections record only revisions of **architecture-level** decisions, in one line each, pointing at the `DEC` entry that holds the detail.

## Files at this doc root (service)

- `overview.md` — always.
- `subsystems.md` — components and boundaries.
- `interfaces.md` — contracts the service exposes and consumes.
- `data-model.md` — persisted / exchanged data **contracts** only.
- `quick-reference.md` — index (see above).

Sized to complexity; not every file is mandatory. Network-level concerns (services catalog, cross-service contracts, topology) live at the system root: `D:/GitRoot/_SIN/docs/architecture/` (`overview.md`, `services.md`, `cross-service.md`, `quick-reference.md`).

## Writing rules

- State facts with evidence (`path:line`, code_index result, or user statement), kept to contract level.
- `_TBD: <reason>_` for unknowns.
- No marketing prose.
- No code recommendations.

## Writers and reviewers

- **Writers:** `docs-section-writer` (legacy-docs, deep-docs gap patches); `architect` agent (`/architect`, `/finalize`).
- **Reviewer:** `docs-reviewer`.

_Generated from ~/.claude/docs-layers.md by legacy-docs-bootstrapper, 2026-11-13._

<!-- agent-ready:start -->

Service-level architecture documentation for `BSONExpressionSerializer`.

## Contents

**Key files:**
- `overview.md` — what the library does, expression-tree compilation approach, BSON attribute support (honoured and not honoured), consumer registration pattern, consumers table, publication mechanics, and 10 known anomalies
- `interfaces.md` — all exported public types and modules with full signatures; reader type-handling matrix (`ExpressionReader.readValue` dispatch); writer type-handling matrix (`ExpressionWriter.writeValue` dispatch); omission-guard logic (`getChecker`)

<!-- agent-ready:end -->
