# Product layer contract — doc root `BSONExpressionSerializer` (service)

Doc root `BSONExpressionSerializer` is a **service** doc root inside the `_SIN` multi-service legacy root. The system doc root (key `system`) is `D:/GitRoot/_SIN/docs/`.

## Question this layer answers

WHAT the product does, for WHOM, and WHY — at the altitude of observable behaviour and business rules.

## What it holds

Vision, actors, features, use cases, stories with acceptance criteria, glossary, product-level constraints (legal, platform, business), and — when the user specified it as a requirement during the spec discussion — an acceptance-test catalog (test ids, stage matrix, required fixtures). The catalog is a requirement the user stated, not test planning; it is written and amended only through `/product-spec`.

At this service root: the features, use cases, stories and glossary this service owns. Vision and actors live at the system root.

## Acceptance criteria

An acceptance criterion or catalog entry states an **observable outcome** an actor can see. Numbers appear only when the number itself is a business requirement the user stated as such ("a session expires after 30 minutes of inactivity" because that is the policy). A number that exists to make a behaviour testable or to bound an implementation is a decision, not a requirement — the catalog entry says "the old connection closes after its last in-flight call completes"; the `DEC` entry says "within 5 s, measured by T51c".

## Never in product

- Library or framework names.
- Protocol internals.
- Configuration keys.
- Type or field names.
- Tolerances chosen for testability.
- Delivery history ("amended by delta 16", "step 013").

**Finalization feeds product only product items**: a requirement delivered with modification, a new behaviour, a withdrawal — by id, in requirement language. Mechanism, numbers and test tolerances learned during delivery go to `docs/decisions/`, never into the catalog or the stories.

## No links to architecture

Product and architecture answer different questions and share no sentences — and **no links**. Product docs carry nothing about architecture:
- no "resolved, see `docs/architecture/x.md`";
- no mirrored summary of a design;
- no status that changes when a design changes;
- no open questions "for the architect" with resolution pointers into architecture.

Open requirement questions stay `_TBD:` in product until the user answers them; open design questions live in `docs/decisions/` with `Status: open` or in `quick-reference.md` `## Open`, never in product. Product never records delivery history, deltas, dates or "amended to match architecture". The what → where mapping lives in the plans layer, never here.

## Classification test

Apply to any sentence about to be written into documentation, in order:

1. **Would an actor or the business notice if this changed?** → product. (If the only reason to notice is that a test would fail, it is not product.) — **this layer's test.**
2. **Does it add, remove or change a service, a component, a contract between components (API, DTO, message, config key, protocol, call order), or a rule that spans components?** → architecture.
3. **Would it still be true if the component were re-implemented with a different library or framework?** Yes → architecture (if it passed 2) · No → decision.
4. **Does it name a type, method, field, default value, timeout, event id, test id or step id?** → decision, unless the name is itself a public contract (then architecture names the contract, a decision names the mechanism).
5. Otherwise it is below the decisions layer: a folder convention or a code comment.

A sentence that fails step 1 does not belong in this layer.

## Ids

- `ACT-###` (actor), `FEAT-###` (feature), `UC-###` (use case), `US-###` (story), `US-###.AC-#` (acceptance criterion).
- Zero-padded, permanent, never reused. Withdrawal = tombstone (the id stays, marked withdrawn).

## Doc-root id space

- Ids are unique **within this doc root only**.
- A citation into another doc root is qualified with its key: `system/ACT-002`, `billing/FEAT-004`, `billing/US-014.AC-2`. Ids of this root (`BSONExpressionSerializer`) are cited unqualified here.
- **Actors live only at the system root.** This service's product docs cite them as `system/ACT-###` and this folder has no `actors.md`.

## Files at this doc root

- `features.md`
- `use-cases.md` or `use-cases/`
- `stories.md` or `stories/`
- `glossary.md`

No `vision.md`, no `actors.md` (system root only).

Generated content sits between the merge markers `<!-- product-spec:start -->` / `<!-- product-spec:end -->`.

## Writers and reviewers

- **Writer:** `product-spec-writer` — dispatched by `/product-spec`, `legacy-docs`, `deep-docs`, `/finalize`.
- **Reviewer:** `product-spec-reviewer`.
- Orchestrators never write here directly.

_Generated from ~/.claude/docs-layers.md by legacy-docs-bootstrapper, 2026-11-13._
