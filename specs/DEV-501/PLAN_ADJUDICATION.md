# DEV-501 — Phase 2 plan challenge adjudication

Status (2026-11-12): **CLOSED — PLAN FROZEN**. Challenge round 1 is closed. Gate 1 is not approved; implementation and runtime acceptance are not cleared.

## Authority and evidence limits

Worktree `F:/Dev/LamuFlix.worktrees/feature-501-spec`, branch `feature/501-spec`. Reports: Maestri `findings-DEV-501-Sentry`, `findings-DEV-501-Ledger`, `findings-DEV-501-Compass`, all read. Decision sources: ticket in `recon-DEV-501:9–80`, Patron Q1–Q9, catalog ruling, D1–D5 and final Principle V alignment in `CONCLUSIONS.md`, carried into `brief.md` before drafting repairs.

Recon: intake HEAD `9c1898ac2b5a0c2f05056ce738c3ae7e1bd33865`; current R1–R5 pickup `ecc641dcb186c52083b611e32f12a88134be5272` (`recon-DEV-501:113`). These SHAs identify source-inspection baselines, not a commit containing the current uncommitted planning repairs. Current recon:113–130 establishes only the cited inspected facts and explicitly leaves inventories, adapter details and installer/snapshot interfaces unverified. No reviewer list, older baseline or Patron description is promoted to current source verification.

Patron D4 deliberately assigns those unresolved factual inventories to implementation pickup and the pre-deletion T026 sweep. This resolves the *plan's handling of unknown facts*; it does not prove the files absent, establish inventory completeness, or waive the eventual checks. Missing current facts during execution remain `needs recon` before affected changes. Historical recon references are candidate locations and must be revalidated. No product/scope decision remains delegated to the implementer.

Round accounting: this is the closure continuation of challenge round 1, not a third challenge round. Sentry has nine numbered findings plus one language-normalization note; Ledger ten findings despite its eight-finding header; Compass eight. All **28 reported items** receive a disposition below. Overlapping reports are not counted as separate implementation requirements. The two-round cap and all-severity closing bar remain unchanged.

## Risk dispositions — Sentry

| Finding | Final plan disposition | Decision, repair and future proof |
|---|---|---|
| H1 High — quota snapshot/reset | Accepted; repaired in plan | D1: provider observations only, nullable unknowns, TimeProvider conversion for provider-relative reset, free local checks before paid call, unknown/positive observation allows explicit call, known exhaustion refuses. No unproved charge/refund or reset replenishment. Contract/handler/API/UI tests cover freshness and precheck ordering. |
| H2 High — capacity/token/replicas | Accepted; repaired in plan | D2: concrete Core singleton binder, named typed TTL/capacity options, injected seeded Random/TimeProvider under lock, opaque 32-hex token, purge expiry then earliest-expiry/FIFO eviction, stale-400 before paid calls. No crypto-security claim. Process-local bindings are not shared; missing issuer state requires free re-search, without silently introducing distributed storage or deployment affinity. |
| H3 High — Subs race/collision scan | Accepted; repaired in plan | D3 and brief enumerate pre-create, post-create/pre-stage, each-publication checks; positive typed 10,000-attempt default, one staged payload reused, next-free sequence preserved, exhaustion 409 read_only_library, owned-only cleanup/cancellation. Real-filesystem tests cover the checkpoints and no-overwrite. No claim that path rechecks eliminate every OS race; no janitor added. |
| H4 High — duplicate paid requests | Rejected as an existing acceptance requirement | Ticket:27–30,47,51,54 and Q5/Q7 allow explicit requests and concurrent next-free publication; one UI's serialization does not promise API idempotency/content deduplication. No singleflight, AlreadyPresent public result, dedup persistence or automatic retry is approved. Retain concurrency/non-overwrite proofs. No follow-up ticket is claimed filed. |
| M1 Medium — unbounded payload | Accepted; repaired in plan | D3: typed positive MaxSubtitleDownloadBytes default 2 MiB, enforced during provider-body read before unbounded buffering/staging; invalid/oversized SRT 502 provider_unavailable; no guessed billing. Adapter implementation remains pickup work, not a verified current body limit. |
| M2 Medium — error/quota shape and search 429 | Accepted; repaired in plan | D1 defines RFC 7807 reason and optional quota extensions; search_rate_limited differs from download_quota_exhausted, with approved distinct copy and truthful nullable metadata. Final Principle V ruling preserves central handler and traceId. |
| M3 Medium — config/policy precedence | Accepted; repaired in plan | D1: GET requires provider config, read-only valid-config GET still 200; POST checks read-only before provider config, both before paid call; machine-readable reasons distinguish failures. Catalog remains key-free. |
| M4 Medium — request cancellation | Accepted; repaired in plan | D3: propagate caller cancellation through search/read/stage/publish, cleanup owned staging, never translate caller cancellation to 504. Only upstream timeout while caller active is provider_timeout. Test explicitly. |
| L1 Low — discovery premise | Existing protection retained; source premise not accepted as verified | Q9 and store/discovery tests explicitly enforce leading-dot staging exclusion. Recon limits are recorded; no assertion of pre-existing filtering or `.tmp` implementation substitutes for tests. |
| Unnumbered Medium — code normalization | Accepted; repaired in plan | Catalog/D2: trim and OrdinalIgnoreCase lookup against SupportedSubtitleLanguages, emit canonical code in DTO/binding/provider/filename. Empty preferences valid, empty/unknown query rejected under the approved 400/422 distinction. |

## Standards dispositions — Ledger

| Finding | Final plan disposition | Decision, repair and future proof |
|---|---|---|
| H1 High — removal inventory | Accepted plan gap; repaired by D4's explicit verification boundary | Brief/plan delete/update/retain table plus T025/T026 pre-deletion current-source classification and exhaustive code/settings/mocks/generated-contract sweep. Additional reviewer paths remain unverified candidates, never automatic deletions. Shared client/hash/local serving/options are retained; historical documentation/negative 404 assertions are distinguished from live dependencies. |
| H2 High — harness/rendering/dirty edit | Accepted; repaired in plan | Older recon confirms harness candidates; D4 requires current sweep, minimal prepare-only edit coordinated with owner, preservation of unrelated dirty edits, actual installer discovery and rendered-output regeneration. No guessed installer path, stashing/resetting, hand-edited generated file or threshold-loosening exemption. |
| H3 High — glossary conflict | Claimed source conflict unverified; planned glossary gap repaired | Current recon does not substantiate every quoted historical definition. D4 authorizes additive qualified acquisition terms in CONTEXT.md, preserving Movie/Library/exact-basename sidecar entries. Verify current definitions before edit; do not silently rewrite them or treat absent evidence as proof of conflict. |
| M1 Medium — constitution/ADR scope | Accepted precision gap; repaired in plan | D4: post-Gate-1 Principle VII 1.15.0 → 1.16.0, Sync Impact/Last Amended/template review and ADR 0041 acceptance together; 0041 supersedes only Proposed 0038's prepare-route wording. Existing separation/status survives. Concrete binder introduces no port, so no invented Principle I port amendment. Preserve Principle VI conventions when removing prepare-only telemetry. |
| M2 Medium — binder interface/lifetime/options | Accepted; repaired in plan | D2 fixes concrete SubtitleCandidateBinder/SubtitleMatchScorer in Core Features/Subtitles, singleton binder with thread safety, existing typed SubtitlesOptions defaults/validation and no mutable static state. Remove optional interface/placement discretion. |
| M3 Medium — test organization/properties | Accepted planning precision; repaired in plan | Meaningful scorer/ranking properties and applicable property gate are explicit; real-filesystem and WireMock remain. Planned new filenames are concrete, existing test paths are verified at pickup. Reject inference that a named existing store test is absent and reject an unproved mandatory SurvivorTests suffix; no duplicate or ceremonial test file. |
| M4 Medium — shared-file order/snapshots | Accepted sequencing/snapshot gap; repaired in plan | Sequential endpoint/registration edits, prepare removal before final contract regeneration, generated snapshot review/approval via discovered workflow. T005 remains Gate-1-gated foundational maintenance; later contract regeneration alone was not a constitution-first violation. |
| L1 Low — optional names | Accepted; repaired in plan | Fixed SubtitleCandidateBinder, SubtitleMatchScorer, SupportedSubtitleLanguages and feature/task file surface; no "or equivalent helper" architecture discretion. |
| L2 Low — ADR status | Accepted lifecycle precision; repaired in plan | ADR 0041 remains Proposed during planning; only post-Gate-1 amendment execution accepts it, together with constitution change and citation. Gate 1/plan freeze are separate. |
| L3 Low — predicted complexity | Rejected mandatory abstractions | No implemented changed method/measured violation supports adding guard/pipeline types now. Existing seams and actual complexity gates govern extraction; no suppression/threshold change. |

## Spec dispositions — Compass

| Finding | Final plan disposition | Decision, repair and future proof |
|---|---|---|
| F1 High — catalog source | Accepted; repaired in plan | Patron catalog ruling: key-free GET /api/subtitles/languages, response/default/bilingual order/dedup/no-store, one Core validation catalog and generated web DTOs. Foundational T033 and language/search/UI tests map this work. |
| F2 High — empty preferences | Accepted; repaired in plan | Empty configured list is valid/null default/no initial search; explicit supported-language choice searches with otherwise valid provider config. Typed validator/acquisition settings work is explicit; source locations verified at pickup, never asserted already fixed. |
| F3 Medium — active-check contradiction | Accepted; repaired in design | D5 reconciles ticket flow: only downloaded active local track is active. Palette removed candidate checkbox prefixes/active-check responsibility; no heuristic identity matching/persistence added. |
| F4 Medium — prepare verification task | Accepted; repaired in plan | T026 explicitly sweeps code/settings/mocks/generated contracts, classifies residual historical/negative assertions, proves removed route 404 and retains shared behavior. |
| F5 Medium — eviction/refusal | Accepted; repaired in plan | D2 picks purge then earliest-expiry/FIFO eviction and successful new issuance, with cap/expiry/evicted-token proofs. Same issue as Sentry H2, not a second store. |
| F6 Low — GET error no-store | Accepted; repaired in plan | GET success and every mapped error explicitly assert Cache-Control private, no-store, including 422 validation; POST has independent matrix coverage. |
| F7 Low — GET config 409 amendment | Accepted; repaired in plan | Both search/download runtime acquisition-config validation appear in scoped Principle VII task/plan/ADR; parameterless catalog is key-free, empty preference list valid. POST read-only precedence preserved. |
| F8 Low — ADR documentation tree | Accepted; repaired in plan | Plan locates docs/adr/0041-explicit-subtitle-acquisition.md at repository root, not inside specs/DEV-501. |

## Additional cross-artifact defect caught during closure

Principle V (`.specify/memory/constitution.md:551–569`, recon:122) requires ValidationException → 422, centralized AddProblemDetails/IExceptionHandler and traceId. Q3/D1 shorthand omitted 422 while planning validators. Patron explicitly approved the final clarification in CONCLUSIONS: malformed binding/domain/token prechecks outside ValidationException retain 400; actual decorator ValidationException returns 422 invalid_request. Brief was updated first; spec/plan/tasks/design/contract/ADR and API proof matrix carry the split. No global validation remap or constitutional departure is authorized; no owner structural checkbox arose.

## Recon and decisions receipt

R1–R5 were reread at recon:111–130, not reported as fully verified inventories. D1–D5 and final Principle V ruling were read from CONCLUSIONS. D4 deliberately assigns source-completeness checks to T003/T004/T006/T025/T026 before affected execution, with missing evidence stopping as needs recon. This is an explicit plan decision, not a finding waived on an unsupported claim. All public shape, resource policy, state lifetime, glossary/governance and active-indicator questions now have cited rulings. Two rejected scope/abstraction demands above do not conceal a verified acceptance defect.

## Verification and freeze receipt

Keel independently reread repaired brief.md, spec.md, plan.md, tasks.md, Palette design.md and Proposed ADR 0041, plus CONCLUSIONS/ASSUMPTIONS, data-model, contracts, research, quickstart, checklists and Quill drafting receipt. Quill/Palette self-reports were not substituted for this reread. Rechecked T018 precheck order, actual shared-file serialization, explicit T025→T026→T027 then T022→T023→T024 despite phase numbering, exact catalog filenames, sweep-candidate versus confirmed-deletion labels, and removed nonexistent D6/D7 provenance citations.

Read-only SpecKit analysis completed: prerequisites command `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` succeeded, selecting the intended DEV-501 feature directory; no before/after-analyze hooks are configured in the inspected extensions file. Analyzed requirement/story inventories, task coverage, consistency, ambiguity/duplication, dependency order and constitution alignment. Analysis made no edits; the separately authorized document repairs and freeze receipt were applied outside that analysis.

| Analysis measure | Result |
|---|---|
| Functional requirements | 23 (FR-001–FR-023) |
| Success criteria | 8 (SC-001–SC-008); runtime outcomes remain future receipts |
| Unique planned tasks | 33 (T001–T033), no duplicate task IDs or undefined task references |
| Requirement/criterion coverage | 31/31 have task mappings (100%); complete mapping in tasks.md |
| Orphan implementation work | None; setup/governance/glossary tasks are explicit authority/constitution prerequisites |
| Outstanding actionable ambiguity/duplication/inconsistency | 0 after the cited rulings and reread repairs |
| Outstanding Critical / High / Medium / Low plan findings | 0 / 0 / 0 / 0 |
| Additional Principle V issue | Closed by cited Patron clarification and explicit 400/422/traceId/central-handler contract proofs |
| Structural owner checkboxes introduced | 0; ordinary owner Gate 1 remains required |

All 28 reported items are disposed: 25 accepted planning gaps repaired, 2 unsupported mandatory requirements rejected (Sentry H4, Ledger L3), 1 existing protection retained with unverified source premise rejected (Sentry L1). The repaired-plan dispositions are CLOSED at Phase 2; they do not assert implemented behavior or runtime acceptance. D4's inventory/pickup checks remain executable obligations and have not been silently waived.

**Analysis verdict: CLEAN. Plan status: PLAN FROZEN.** The Conductor may proceed to spec-PR preparation for ordinary owner Gate 1. Implementation still starts only after that Gate 1 and the named dependency/drift receipts; no merge is authorized by this receipt.

Final read-back confirmed consistent freeze/Gate-1/runtime labels, ADR 0041 still `status: proposed`, 23 FRs / 8 SCs / 33 unique tasks with no missing mapping, duplicate task ID or undefined task reference, and 28 disposition rows. Fifteen planning documents were checked for conflict markers and accidental trailing whitespace: none; eleven intentional Markdown hard line breaks were preserved. Scoped `git diff --check` was clean for tracked planning changes; untracked artifacts were covered by the explicit document checks rather than inferred from Git's tracked diff.

No implementation tests, analyzer gates, mutation or home-cluster/BS Player runtime checks were run by this adjudication. Baseline skips remain skips, not PASS. No production/tests/generated contracts/constitution/harness changes were made by Keel. Runtime receipts remain OPEN; Gate 1 is not approved and no merge is authorized.
