# Implementation Plan: Distinct downloaded subtitle track labels

**Branch**: `feature/553-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-553-spec`) | **Date**: 2026-11-28 | **Spec**: `specs/DEV-553/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q10); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-553`, `recon-DEV-553`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft — Gate 1 pending. Spec Kit PR remains `specs/**` only and excludes the ADR file. ADR 0054 is claimed and proposed (`ISubtitleDownloadRecordStore` only; Build names EF/table types by Persistence conventions); the proposed draft is parked in `PRIVATE_TICKET_DIR` as `0054-subtitle-download-record-persistence-port.md` for Build pickup, with brief/conclusions as durable fallback. ADR 0054 Accepted + Principle I MINOR (next MINOR from the then-current base) + ArchitectureTests whitelist ship atomically with the Core port in the Build PR. Zero structural owner checkboxes. Ordinary Gate 1, DEV-551 delivery merge, pickup evidence check, Build, Size M review, and rebased-head gates are pending.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/subtitle-tracks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. Private Spec Kit mirrors: `plan.md` / `tasks.md` under `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-553\`.

## Summary

Record provider/release provenance for successfully published subtitle sidecars in one ticket-owned PostgreSQL table, join it into the existing track list and download responses as independently projected nullable `provider` / `releaseName` while preserving discovery-derived wire `label`, rank same-language tracks in Core, and let the player selector compose `release name · provider` only when both fields are known — without changing recognition policy or file names.

## Technical Context

**Language/Version**: .NET 10 (C#) API + React 19 / TypeScript / Vite web per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing subtitle feature (`DownloadSubtitleCommandHandler`, `ListSubtitleTracksQueryHandler`, `SubtitleSidecarDiscovery`, `ISubtitleSidecarStore`); existing EF/PostgreSQL persistence (`PersistenceServiceCollectionExtensions`, migrations); DEV-551 canonical provider ID on candidates (implementation gate); existing playback web hooks/components. No new NuGet/npm dependency (`brief.md` Q6; PRODUCT §5 item 1).

**Storage**: One ticket-owned PostgreSQL table (fields/rules in `data-model.md`); sidecar files unchanged on disk.

**Testing**: Extend `tests/LamuFlix.UnitTests/Features/Subtitles/` (download, list, ranking, discovery regression untouched); extend `tests/LamuFlix.IntegrationTests/SubtitleSearchDownloadEndpointTests.cs` (and related) in the wire phase for two-provider provenance, hand-added label, deleted-file exclusion, same-key upsert, and explicit JSON nulls on both responses; property tests for ranking invariants; web Vitest coverage in `subtitleSelector.test.tsx` / `subtitlesTab.test.tsx` (and hook behaviour via those surfaces). `LamuFlix.ArchitectureTests` green after atomic port whitelist. No new test framework.

**Target Platform**: Compose / home API + browser player subtitle panel.

**Project Type**: Existing vertical-slice subtitle feature + Infrastructure persistence adapter + existing playback web components.

**Performance Goals**: List uses one per-movie batch provenance read (no per-file query) (`brief.md` Q6).

**Constraints**: Charter §2.3 / PRODUCT §5 items ruled in Q1–Q6 (ticket-owned table; ticket-approved DTO fields; no new project/layer/dependency; no LocalPlay/secrets/`Process.Start`). Recognition policy frozen. Size M requires Sentry, Ledger, Compass with explicit dispositions. Every confirmed in-scope finding blocks (Low included). Two review remediation rounds; residuals to owner on the PR (Q10). Honest gate labels; never lower thresholds. Exit 1 and `Could not run` are blocking on Phase 3 and rebased-head gate sets (`brief.md` Task order item 5).

**Scale/Scope**: Size M; file envelope in brief Approach / Structure below.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No structural owner checkbox (`PRODUCT.md` §5; brief Q6).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests | **PASS (design)** — Core port `ISubtitleDownloadRecordStore` (post-publication write + per-movie batch read); EF adapter under Infrastructure/Persistence (table/EF types named by Persistence conventions); handlers stay in Core; byte store stays publication-only (`brief.md` Q6; `recon-DEV-553:74–82`, key evidence `:78–82`). **Build PR (atomic)**: introduce the port under the ADR-locked name; mark ADR 0054 Accepted; amend Principle I enumeration with the next MINOR from the then-current constitution base, Sync Impact Report and Last Amended; add matching `PortWhitelistPattern` entry while preserving intervening whitelist entries; keep ArchitectureTests green. Spec Kit PR stays specs-only and excludes the ADR file — conforming amendment, no structural owner checkbox. Pickup and delivery rebase revalidate the ADR claim, constitution Version/Last Amended, and whitelist; route a numbering collision to Bernstein. |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — extend existing download command and list query handlers; no new mediator/layer. |
| III (Typed query model) | MovieQuery | **N/A** — no browse/filter change. |
| IV (Domain transitions) | Movie mutators | **N/A** — no Movie lifecycle mutation; provenance is a join-only record. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — metadata write failure keeps existing success status/shape with null provenance (Q2); no new error contract. |
| VI (Observability) | Safe diagnostics | **PASS** — Error log on failed upsert through existing ILogger/OTel path with movie id + relative path; no new metric/dependency (Q2). |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no LocalPlay, secrets, or `Process.Start`; `TimeProvider` for download UTC. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — update Local Subtitle Track per Wanted 6; recognition-policy wording elsewhere unchanged. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — unit + integration + property + web proofs per Q8 / Done when; ArchitectureTests required. |

### Stack / workflow checks

| Check | Plan response |
|---|---|
| Tech stack / API docs (OpenAPI chain) | **PASS** — extend `SubtitleTrackDto`; map both endpoints; regenerate `openapi.json` → `types.ts` only after endpoint projection proofs pass; drift green (`contracts/subtitle-tracks.md`). |
| Static-analysis gates (C#) | Gauge Phase 3 step 5 / rebased head on changed `.cs` exact-diff: Roslyn, cyclomatic (incl. refactor threshold), InspectCode. Scope-empty → `SKIPPED (scope-empty)` honestly. Exit 1 / `Could not run` block. |
| PR quality / ArchitectureTests | Build PR MUST keep ArchitectureTests green after atomic port/whitelist/ADR Accepted delivery. |
| Web gates | Required for `SubtitleSelector.tsx` / `useSubtitlePlayback.ts` and generated contract consumers. |
| Other brief gates | Property tests, vulnerable packages, format, full `dotnet test`, OpenAPI/types drift. |
| Mutation | Applicable mutation on production C# under `src/`. Exit 2 / N/A reported honestly — never as PASS. |

### Complexity Tracking

No constitution violations requiring justification. New port follows the stated ADR + amendment route (Q6).

## Approach

- **Phase A (this ask)**: Quill drafts Spec Kit artifacts from the closed grill. Keel owns analyze / plan challenge / ADR 0054 (proposed draft parked in `PRIVATE_TICKET_DIR`). Patron provisional Gate 1; Bernstein/Rigger Spec Kit PR (`specs/**` only, ADR file excluded); user merge completes Gate 1.
- **Phase B pickup**: After Gate 1 and DEV-551 delivery merge, **Wisp** confirms provider ID and selected-candidate evidence (hash-match, match percent, release name, language) on the landed candidate/binding contract (**read-only** pickup). Missing evidence → `needs recon` (Q8). Revalidate ADR 0054 claim/namespace, constitution Version and Last Amended, and current `PortWhitelistPattern` (collision → Bernstein). Confirm Persistence DI site, migration convention, and test seams against pickup HEAD. **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**.
- **Build order** (`brief.md` Task order):
  1. ADR 0054 Accepted + Principle I MINOR (next MINOR from then-current base; preserve intervening whitelist entries) + ArchitectureTests whitelist **with** Core port `ISubtitleDownloadRecordStore` and EF adapter/registration/migration (canonical discovery relative-path key; adjacent vs `Subs\` separation; atomic same-key upsert including concurrent separate-context writes leaving one complete row; real DB round trips). Build names table/EF types by Persistence conventions.
  2. Download handler: persist after successful publication (including null score when match percent is unknown); project provider/release onto the returned Core `SubtitleTrack` (same shape list/wire later consume) while retaining discovery-derived label, discovery-derived language, and identity from the canonical key; metadata failure/cancellation/ambiguous path (file kept, immediate fallback response, Error log, no retry); same-key upsert. Unit/handler proofs green when each immediate behaviour lands.
  3. List handler: discovery-first; batch enrich by canonical discovery key; rank by discovery-derived language partitions; preserve discovery-derived label and language on the wire (stored candidate language is provenance only); project nullable `provider` / `releaseName` independently on the shared Core track shape. Handler/list unit proofs include conflicting stored-vs-discovery language, Q9 last-committed-row list behaviour (old row if replacement did not commit, new row if it committed), and unedited discovery regression — not endpoint assertions.
  4. Wire phase: author endpoint assertions first; then DTO + both endpoint mappings; prove successful download and list projections, known-provenance responses preserve discovery-derived wire `label`, unknown values serialize as JSON `null` independently on both responses (including fallback), two-provider listing, `Film.en.srt`, deleted-file row retained/not listed, same-key successful replacement, and a Subs publish-then-list round trip returning the same track ID and provenance. Passing endpoint proof is required before OpenAPI → TypeScript regeneration (`brief.md` Task order items 3–4). After endpoint proof, confirm both generated OpenAPI properties accept string or null and both generated TypeScript properties admit `string | null`, plus drift checks.
  5. Web selector: first-appearance language groups keyed by wire `language`; compose `release name · provider` only when both fields known else wire label (including partial provenance); selectable manual track; selection-through-reload with absent-track regression preserved; generated DTOs only.
  6. `CONTEXT.md` Local Subtitle Track. Gate-only verification is Gauge Phase 3 step 5 (exact-diff analytics + full suite + web + mutation/property/vuln/format); repeat applicable set on rebased delivery head, including revalidation of ADR claim / constitution Version / Last Amended / whitelist. Compose SMOKE-001–003 is a separate downstream checkpoint after the applicable gate set on the rebased delivery head (not a local Build task).
- **Do not**: change recognition policy or file names; hand-roll provider IDs; add packages/layers; expose ranking fields; rewrite wire `label` to the composed display form; override discovery-derived language with stored candidate language; delete/rollback published files on metadata failure; invent ADR numbers or include the ADR file in the Spec Kit PR; claim Gate 1 / merge readiness from drafting; lower gate thresholds; claim Gauge Phase 3 step 5 is hosted.

## Authorized project files

| Area | Files and scope |
|---|---|
| ADR / constitution / ArchitectureTests | Proposed ADR 0054 parked in `PRIVATE_TICKET_DIR` (Keel-owned; Spec Kit PR excludes `docs/adr/`); Build accepts under `docs/adr/0054-subtitle-download-record-persistence-port.md` atomically with Principle I MINOR (`.specify/memory/constitution.md`) and `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` whitelist |
| Core port + feature | New port under `src/LamuFlix.Core/Ports/`; `DownloadSubtitleCommandHandler.cs`; `ListSubtitleTracksQueryHandler.cs`; `SubtitleTrack` projection / ranking helpers under `src/LamuFlix.Core/Features/Subtitles/` |
| Persistence | EF entity/configuration, migration + snapshot, adapter, `PersistenceServiceCollectionExtensions.cs` registration under `src/LamuFlix.Infrastructure/Persistence/` |
| API | `SubtitleTrackDto.cs`; `SubtitleEndpoints.cs` list + download mappings |
| Generated contract | `web/src/api/openapi.json` then `web/src/api/types.ts` |
| Web | `web/src/features/playback/SubtitleSelector.tsx`; `useSubtitlePlayback.ts`; existing subtitle web tests |
| Glossary | `CONTEXT.md` Local Subtitle Track |
| Tests | Existing subtitle unit/integration/web surfaces named in `recon-DEV-553`; property tests for ranking |

## Project Structure

### Documentation (this feature)

```text
specs/DEV-553/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/subtitle-tracks.md
├── tasks.md
├── checklists/requirements.md
└── DRAFTING_RECEIPT.md
```

### Source Code (repository root)

```text
src/LamuFlix.Core/Ports/                          # download-record port
src/LamuFlix.Core/Features/Subtitles/             # handlers, track projection, ranking
src/LamuFlix.Infrastructure/Persistence/          # EF entity, adapter, migration, DI
src/LamuFlix.Api/Endpoints/SubtitleTrackDto.cs
src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs
web/src/api/openapi.json
web/src/api/types.ts
web/src/features/playback/SubtitleSelector.tsx
web/src/features/playback/useSubtitlePlayback.ts
CONTEXT.md
docs/adr/                                         # ADR 0054 Accepted in Build PR only (proposed draft in PRIVATE_TICKET_DIR)
.specify/memory/constitution.md                   # Principle I MINOR (Build)
tests/LamuFlix.ArchitectureTests/
tests/LamuFlix.UnitTests/Features/Subtitles/
tests/LamuFlix.IntegrationTests/                  # subtitle download/list endpoint proofs
web/src/features/playback/*.test.tsx
```

**Structure Decision**: Stay inside existing Core / Infrastructure / Api / web playback folders. No new project or top-level layer (Q6).

## Verification Approach

1. Persistence foundation: canonical discovery key; adjacent vs `Subs\` separation; atomic same-key upsert; concurrent same-key writes from separate PostgreSQL contexts both complete with one complete committed row; real DB round trips.
2. Unit (download success path): publication-without-write; successful upsert recording including null-score row when match percent is unknown; returned Core track carries provider/release with discovery-derived label/language and existing identity from the canonical key. Green when the success path lands.
3. Unit (download failure path): upsert throw/ambiguous/cancellation → immediate fallback on the returned track; no delete/rollback/retry; Error log. Green when the immediate failure path lands (Q9 committed-row list is not part of this checkpoint).
4. Unit/property (list/ranking): ranking precedence / missing scores within hash rank before UTC / ties / unrecorded-last; batch join by canonical key; discovery-derived language partitions (conflicting stored candidate language does not override); deleted-file exclusion; hand-added fallback; Q9 last-committed-row list (old if replacement did not commit, new if it committed); discovery tests unedited and byte-stable.
5. Endpoint (wire phase, before codegen): successful download and list projections; known-provenance responses preserve discovery-derived wire `label`; unknown values serialize as JSON `null` independently on both responses including fallback; two-provider listing; `Film.en.srt`; deleted-file row retained/not listed; same-key successful replacement; Subs publish-then-list returns the same track ID and provenance. Authored before DTO mappings; passing after both mappings; required before OpenAPI → TypeScript and web. After endpoint proof: OpenAPI string-or-null schema for both fields, TypeScript `string | null` for both fields, plus drift.
6. Web: first-appearance language-group order keyed by wire `language`; composed vs partial-provenance fallback labels; selectable manual track; Off; selection through reload; in-flight newer choice; absent-track handling preserved as regression; generated types.
7. Glossary: `CONTEXT.md` Local Subtitle Track.
8. Downstream (not local Build): Cog → Gauge Phase 3 step 5 exact-diff gate set → Size M review → rebase + applicable gate set on the rebased delivery head (including ADR claim / constitution Version / Last Amended / whitelist revalidation); exit 1 / `Could not run` block; exit-2 reported as the gate reports them — never as PASS. Compose SMOKE-001–003 after the applicable gate set on the rebased delivery head (explicit downstream checkpoint; no local Build task). Read-only pickup/verification stay on Wisp/Keel/Gauge owning seats.

## Phase 0 / Phase 1 note

Grill Q1–Q10 freeze inventable unknowns. `research.md`, `data-model.md`, `contracts/subtitle-tracks.md`, and `quickstart.md` record those rulings for Build — they do not reopen decisions.
