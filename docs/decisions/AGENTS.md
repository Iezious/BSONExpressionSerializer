# Decisions layer contract — doc root `BSONExpressionSerializer` (service)

Doc root `BSONExpressionSerializer` is a **service** doc root inside the `_SIN` multi-service legacy root. The system doc root (key `system`) is `D:/GitRoot/_SIN/docs/`.

## Question this layer answers

WHICH way a specific technical question was settled, and why — at the altitude of the inside of one component or one feature.

## What it holds

Every technical decision that is **below architecture and above code**: how a contract is realized inside a component, numeric defaults and bounds, "flag or nullable field", library-specific mechanism choices, test tolerances, ordering tricks, rejected alternatives, and the user's mid-flight calls made during planning or coding. Also decisions whose status is `open` — the question is recorded before it is answered.

A decision is **feature-scoped and stage-linked**: each entry names the stage, the feature, the step (if any), and the code location that carries it. A later feature may supersede it; the old entry is never deleted.

A decision record **never introduces** a new contract, service, DTO or call structure. If writing one would require that, it is an architecture change: stop and route to `/architect`.

## Below decisions — who needs to know

| Who needs to know | Record | Written by |
|---|---|---|
| Only a reader of this block or file | **a comment in the code** — the `why`, citing the step/DoD, `DEC` or architecture § it serves | coder / fixer / reviser, while writing the code |
| Anyone working in this folder | **the folder's `AGENTS.md`, `## Conventions` section** (outside the `agent-ready` markers) | planner at planning time; after coding, the orchestrator's glossary writer from a `[decision]` observation marked `Scope: folder <path>` |
| Anyone working on the feature, or a reader of the architecture asking "why this default / this mechanism" | **`DEC` entry** in `docs/decisions/` | planner; architect agent at finalization |

- A block- or file-scoped choice lives **only** in the comment. It is never an `## Observations` bullet and never a `DEC` entry.
- A folder-scoped convention lives **only** in that folder's `AGENTS.md`.
- A `DEC` entry is for anything whose reader is outside the folder. When in doubt between folder and `DEC`, ask whether a second folder would have to agree with it — yes → `DEC`.
- Nothing is recorded twice. A comment cites the `DEC` id; it does not restate the decision.

## Layout

```
docs/decisions/
  AGENTS.md                 # layer contract (this file)
  index.md                  # registry: one line per DEC id → title · status · feature · stage; regenerated at every write
  <NNN>.<feature>.md        # multi-step feature's decisions
  fast-<NNN>.<name>.md      # fast feature's decisions
  arch-<slug>.md            # (rare) decisions made in an /architect session that are below architecture altitude
  000.legacy.md             # brownfield: choices the pre-existing system already embodies (DEC-000-<n>, Stage: legacy), written by /deep-docs
```

## Entry format

One per decision:

```
### DEC-<NNN>-<n> — <title>
- **Status:** open | accepted | superseded by DEC-… | rejected
- **Stage:** <stage id> · **Feature:** <NNN>.<feature> · **Step:** <SSS> or —
- **Date:** YYYY-MM-DD · **Decided by:** user | planner | coder (confirmed: user)
- **Question:** what had to be settled
- **Decision:** the choice
- **Reason:** why, in one or two sentences
- **Rejected:** alternatives and why not (omit if none were weighed)
- **Lands in:** code path(s) / config key(s) carrying it
- **Cites:** architecture doc § it realizes inside the component; the `US-###.AC-#` or catalog test id a tolerance serves (optional; cite, never restate)
```

Ids are permanent: `<NNN>` is the feature number, `<n>` counts within the feature. Superseding writes a new entry in the superseding feature's file and flips the old status; nothing is deleted or renumbered. `index.md` is regenerated from the entries at every write.

## Pre-existing system — `000.legacy.md`

- Decisions the legacy code already embodies are recorded in `000.legacy.md` with ids `DEC-000-<n>`.
- `**Stage:** legacy · **Feature:** 000.legacy · **Step:** —`.
- `**Decided by:** observed (confirmed: user)` or `observed [inferred]`.
- Feature number `000` is reserved for this — roadmap never allocates it to a real feature.
- A later feature that changes one writes a new entry in its own file and flips the old status to `superseded by DEC-…`; nothing is deleted or renumbered.

## Doc-root scope

- Decisions binding more than one service live at the system root (`D:/GitRoot/_SIN/docs/decisions/`), cited from here as `system/DEC-…`.
- Decisions binding only this service live here.

## Lifecycle

| When | Who decides | Who records | Where |
|---|---|---|---|
| Planning | user, via the planning orchestrator | `planner` / `fast-planner` | `docs/decisions/<feature>.md` directly (new file or appended entries) and a one-line pointer in the step's context |
| Coding, bug-fix, rewrite (escape valve, Fault SPEC, ambiguity) | user, via the orchestrator | the orchestrator | `outcome.md` `## Decisions` (append-only); the coder/fixer/reviser add their own `[decision]`-tagged observations |
| Finalization | user confirms categories | `architect` agent | merges `outcome.md` `## Decisions` and `[decision]` observations into `docs/decisions/<feature>.md`; marks superseded entries |
| Legacy (brownfield) | observed in code, confirmed by user | `deep-docs` via `deep-docs-writer` | `docs/decisions/000.legacy.md` |

A question that cannot be settled at the current altitude escalates: decision → `/architect` (new contract needed) → `/product-spec` (requirement missing). Planning does not proceed past an open escalation.

## Writers and reviewers

- **Writers:** `planner`, `fast-planner`, `architect` agent, `deep-docs-writer`.
- **Reviewer:** `deep-docs-reviewer` for `000.legacy.md`.

_Generated from ~/.claude/docs-layers.md by legacy-docs-bootstrapper, 2026-11-13._
