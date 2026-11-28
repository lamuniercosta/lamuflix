# DEV-553 grill conclusions

## Q1 — Download record

**Controlling write rule**. Q7 supersedes the original insert-only wording below. Use atomic same-key upsert after successful publication.

Where should the download record live? I recommend the ticket default: one ticket-owned database table keyed by movie identity and the actual relative published subtitle filename, across both adjacent and Subs paths as applicable. Record provider ID, release name, language, hash-match evidence, match percent and download UTC time after successful file publication. Discovery remains authoritative for file eligibility, so missing files are never listed. This adds storage and an integration fixture while preserving sidecar names. Do you confirm this ruling and the conclusions location?

[from Patron] DEV-553 Q1 ruling: CONFIRMED - one ticket-owned database table; conclusions location specs/DEV-553/CONCLUSIONS.md confirmed (you write it).

Basis:
- Ticket Open points: Default: a database table keyed by movie and relative file name, so the files on disk stay as they are; Where it lands row src/LamuFlix.Infrastructure/Persistence/ (Storage for the record, if the spec picks the database). Ticket-named, so this is the ticket's own table, not a 2.3(3) schema escalation.
- Wanted 1 fixes the field set (provider, release name, language, match percent, time, published file name); Wanted 4 sorts hash matches first, so hash-match evidence is required by the ticket, not an addition.
- Done when: a record whose file was deleted is not listed, and discovery tests pass unchanged - so discovery stays authoritative and the record is a join-only enrichment.

Implications / constraints:
- Key = movie identity + relative path from the movie folder (including the Subs\ segment), not bare file name, so adjacent Film-en.srt and Subs\Film-en.srt never collide. Unique on that pair.
- Write the row only after successful publication; a failed publish writes nothing. Re-publishing under the same key is not possible (store never overwrites), so no upsert semantics are needed beyond the unique constraint.
- Provider ID depends on DEV-551 (open, unbuilt): spec must state the dependency and build order; do not hand-roll a provider ID ahead of it.
- No file-name or recognition-policy change; hand-added files with no record keep today's label (Wanted 2).
- Requires an EF migration plus the integration fixture named in Done when; no new dependency, project or layer.


---

## Q2 — Metadata write failure

What failure behavior should apply after a subtitle is published but its metadata write fails? I recommend keeping the published file, returning the existing successful track response with filename-derived label and null metadata for a confirmed row failure, and logging the failed write through the existing observability seam. A cancelled or ambiguous write should not trigger file rollback or a duplicate publication retry. The next list reads the committed row if present and otherwise follows the ticket fallback. This preserves the download and exposes the degraded metadata as an operational error. Is that the correct boundary for Wanted 1?

[from Patron] DEV-553 Q2 ruling: CONFIRMED - keep the published file, return the existing success response with the filename-derived label and null provider/release, log the failed write; no rollback, no retry.

Basis:
- Wanted 2: a file with no record keeps today's label - a failed write lands in exactly that defined fallback, so no new state is invented.
- Out of scope: Deleting a downloaded track - rolling back the published file would be deletion and is outside the ticket; Renaming files on disk likewise excludes any re-publication.
- Product 2.3(4): keeping the existing success status and DTO shape (new fields nullable) avoids a new status code or error contract not in the ticket.

Constraints:
- Cancelled or ambiguous write: same path - no rollback, no duplicate publish; next list reads the committed row if present, else fallback.
- Log at Error through the existing ILogger/OpenTelemetry seam with movie id + relative path; no new metric or dependency required.
- Spec must name this as an edge case with a unit test (metadata store throws -> response succeeds with null provider/release, file remains discoverable).


---

## Q3 — Wire contract and ranking boundary

How should the existing list and download DTOs express provenance and support ranking? I recommend nullable `provider` and `releaseName` strings on both responses, with `provider` carrying the DEV-551 canonical provider ID and absent values explicitly null. Preserve existing `label`, `language`, `isDefault`, track identity, routes and statuses. Map both response paths in SubtitleEndpoints.cs as forced by Wanted 2 and the DTO change. Rank on the backend from persisted evidence so the UI needs no public hash, score or timestamp fields and can preserve the incoming order inside each language. Is this the right wire contract and file scope under §2.3?

[from Patron] DEV-553 Q3 ruling: CONFIRMED - add nullable provider and releaseName (explicit null) to the existing track DTO on both list and download responses; nothing else on the wire; backend ranks, web groups and preserves incoming order.

Basis:
- Wanted 2 names exactly two new fields (provider, release name, when known) and the Where it lands row SubtitleTrackDto.cs: Provider and release name - that is the ticket-approved API shape, so 2.3(4) is satisfied; adding hash/score/timestamp would be an unnamed public shape and is refused.
- SubtitleEndpoints.cs edits are forced by the DTO change (both response mappings) - in scope as a forced edit, no ruling needed; openapi.json regen (ticket row) and types.ts derive per the contract chain.
- Wanted 4 ordering from persisted evidence is a Core concern (Where it lands row src/LamuFlix.Core/Features/Subtitles/: join it into the track list); SubtitleSelector.tsx still owns language headings, labels and display order (ticket row), and its web tests assert the rendered order per Done when.

Constraints:
- provider = DEV-551 canonical provider ID, verbatim; no hand-rolled display-name map in web. Label is releaseName + ' · ' + provider when both present, otherwise today's label (logged [assumed] taste: ID shown as-is).
- Within a language: hash match first, then match percent desc, then download UTC desc; tracks with no record after all recorded tracks, in today's discovery order. Ties stable.
- Track id, label, language, isDefault, routes and status codes unchanged.


---

## Q4 — Ordering and label fallbacks

How should the selector handle ranking ties, manual tracks and incomplete provenance? I recommend backend order within each language: recorded tracks first, then hash match, then known score descending, then UTC download time descending, retaining discovery order for exact ties and among tracks without a record. A missing score follows known scores within the same hash rank. Render the ticket label `release name · provider` when both values are available, and use the existing filename-derived label for missing or incomplete provenance, including Film.en.srt. Show Off once outside the headed groups. Keep language group order by first appearance in list order, preserving discovery order across languages. Do you confirm these defaults and the [assumed] taste choices?

[from Patron] DEV-553 Q4 ruling: CONFIRMED as proposed; log the tie-break, Off placement and language-group order as [assumed] taste in ASSUMPTIONS.md.

Basis:
- Wanted 4 (hash first, then match percent, then newest) is the ticket order; recorded-first, missing-score-last and stable discovery order for ties/unrecorded only fill gaps the ticket leaves and match my Q3 constraint.
- Wanted 2 + Done when (hand-added Film.en.srt keeps today's label): any missing or partial provenance falls back to the filename label - no half label like release-only.
- Wanted 3 asks for a heading per language; group order by first appearance and a single Off outside the groups are taste, accepted per the Keel-recommendation rule.


---

## Q5 — Selection after download refresh

How should selection be restored after download refresh? I recommend keeping the currently selected existing track by the server track identity when it appears in the refreshed list, including a manual track, even if the refreshed order, label, or default differs. Keep explicit Off, retain existing handling if the selected track is absent, and ignore a stale refresh selection if the viewer makes a newer choice in flight. This stays inside the existing playback hook and covers manual choice, Off, and reordered same-language tracks in web tests. Do you confirm?

[from Patron] DEV-553 Q5 ruling: CONFIRMED with one correction - selection is kept by server track id, but on a download refresh the selected track is the just-downloaded one, as today; do not revert to the pre-download track.

Basis:
- useSubtitlePlayback.ts:203-226 (refreshAfterDownload) selects the downloaded track before the list reloads; :240-243 merges the reloaded list. Wanted 5 (the selected track stays selected when the list reloads after a download) therefore means that selection survives the reload with its new label/order/default - it does not ask to change auto-select, and changing it would alter shipped behaviour outside the ticket.
- Ticket row useSubtitlePlayback.ts: Keep the selection on reload (:147-196, :240-242) - scope stays inside the existing hook, no new state library (2.3(2) not triggered).
- Newer viewer choice in flight (another track or explicit Off) wins via the existing generation/attempt guards (:233-235, :252); absent-track handling stays as today (:240-242 merge).

Web tests: downloaded track stays selected after a refresh that reorders and relabels it; a manual track selected after starting a download is kept; explicit Off chosen in flight is kept.


---

## Q6 — Persistence port and governance

**Controlling write rule**. Q7 supersedes the original insert-only wording below. Use atomic same-key upsert after successful publication.

Which persistence boundary should the plan use? recon-DEV-553:74–82 rules out reusing the existing ports. I recommend one Core subtitle-download-record port for post-publication insert and per-movie batch reads, with one EF adapter and its binding in PersistenceServiceCollectionExtensions.cs. Keep handlers on the Core side, bulk-enrich discovered files by exact movie/relative path without a query per file, and keep the byte store focused on publication. This is inside the existing Core and Infrastructure architecture. The constitution requires an ADR for the new port, so the plan should carry the ADR as a required deliverable even though this is size M. I recommend the Conductor allocate its number before ADR drafting as a separate handoff. This grill records the decision and required ordering. Do you confirm this boundary and the ADR requirement under §2.3?

[from Patron] DEV-553 Q6 ruling: CONFIRMED - one new Core port for the subtitle download record (post-publication insert + per-movie batch read), one EF adapter in Infrastructure/Persistence, bound in PersistenceServiceCollectionExtensions.cs; ADR required, plus the constitution MINOR amendment and the architecture-test port list update, delivered atomically with the port.

Basis:
- recon-DEV-553:78–82: no existing port fits (ISubtitleSidecarStore is bytes/publication only; catalog, repository, features, playback ports are other aggregates). Ticket row Infrastructure/Persistence/ (Storage for the record) names the adapter location, so this stays inside the existing Core/Infrastructure layers - no new project or layer (2.3(2)).
- constitution.md:638-645: the port list is closed, Adding a port requires an ADR, and LamuFlix.ArchitectureTests enforces it. Following that stated route is compliance, not a departure, so no owner escalation; precedent ADR-0050 / DEV-572 (constitution.md:48-61) and ISubtitleSidecarStore (:233).
- Batch read by movie + exact relative path, no per-file query, keeps the list handler O(1) round trips; the byte store stays publication-only (single responsibility).

Ordering: Conductor allocates the ADR number before drafting (separate handoff); the plan lists ADR, constitution amendment (append the port to :643) and ArchitectureTests allowed-port update as required deliverables before the port code lands. Size stays M.


---

## Q7 — Publication when a path is reused

How should path-keyed records behave after an external file deletion? A file absent on disk is excluded by discovery as already ruled. If a later successful download is published under the exact same movie/relative-path key, never-overwrite alone does not guard the database against a stale row for a file that was previously removed. I recommend replacing that row atomically with the new publication metadata on successful persistence, keeping the unique key, and explicitly limiting this ticket to path-based matching for external edits or replacements. Cover same-key publication as a regression if the store can return that key. Does this refine Q1/Q6 correctly, or should the brief retain insert-only semantics with a different invariant?

[from Patron] DEV-553 Q7 ruling: CONFIRMED - replace the row atomically on same-key publication (upsert on the unique movie + relative-path key); this supersedes my Q1 line that no upsert semantics are needed.

Basis:
- Q1 was wrong on that point: never-overwrite guards the disk, not the table. SubtitleSidecarStore.cs:128 publishes the first free name (Subs\Film-en.srt), so after an external delete the next download reuses the same key - the store CAN return it, so the regression test is required, not conditional.
- Wanted 1 records metadata with the published file name; a stale row would attach the old provider/release to new bytes, violating Wanted 2/3 labels.
- Done when (deleted-file record not listed) is already met by discovery; no cleanup sweep is needed, and deleting tracks is out of scope.

Limit (record in brief): matching is path-based only; a file the owner replaces by hand under the same name keeps the old record until a download republishes that key. Noted, no ticket.


---

## Q8 — Dependency, build sequence and proof

What build dependency and proof sequence should Quill carry? I recommend allowing DEV-553 spec work to proceed while DEV-551 Spec PR #520 is open, and gating DEV-553 implementation on the DEV-551 delivery landing and the pickup check confirming provider ID plus the selected candidate evidence needed for the record. DEV-552 can proceed in parallel. After Gate 1 and that dependency, complete the port governance prerequisites and persistence foundation, then download/list projection and real-database/endpoint proofs, then regenerate OpenAPI and TypeScript, then implement and verify the selector and refresh. Require existing discovery tests unchanged, two-provider endpoint downloads with distinct provenance, hand-added and deleted-file cases, path reuse and failure, standalone ranking precedence and stable-tie proofs, and generated-type web tests for headings, labels, order, and refresh. Do you confirm this order and test envelope?

[from Patron] DEV-553 Q8 ruling: CONFIRMED - spec proceeds now; implementation gated on DEV-551 delivery merged and the pickup check; order and test envelope as proposed.

Basis:
- Ticket Needs: DEV-551: the provider id on each candidate. Can run alongside DEV-552 - gates the build, not the spec; DEV-552 parallel is ticket text.
- Order follows the contract chain (C# DTO -> openapi.json -> types.ts) and Q6 (ADR, constitution amendment, ArchitectureTests before port code); web work consumes generated types only.
- Test envelope maps one-to-one to Done when plus Q2 (metadata write failure keeps file, null provenance), Q4 (ranking precedence, stable ties, unrecorded last), Q5 (downloaded track kept, in-flight manual choice and Off kept) and Q7 (same-key upsert); discovery tests unchanged per Done when.

If the pickup check finds DEV-551 does not expose the provider ID or selected-candidate evidence as needed, stop and route needs recon - do not hand-roll it in DEV-553.


---

## Q9 — Same-key metadata failure

How should the brief reconcile Q2 with Q7 if a same-key re-download publishes new bytes but the metadata upsert fails or its outcome is ambiguous? I recommend the same successful download response with filename label and null provenance, with no delete, rollback or retry. The next list reads the last committed row by path, so it can show prior provenance if the replacement write did not commit. Record that cross-store failure limit alongside Q7 rather than assuming the row disappears or adding a fingerprint scheme. Do you confirm this precise failure behavior?

[from Patron] DEV-553 Q9 ruling: CONFIRMED - same success response with filename label and null provenance, Error log per Q2, no delete/rollback/retry; the last committed row stands, and the brief records this cross-store limit next to Q7.

Basis:
- Q2 already fixes the failure path (keep file, existing success shape, log); Q7 only changes the write from insert to upsert, so the failure semantics carry over unchanged.
- The stale-provenance case needs three rare events together (external delete, same-key reuse, failed DB write); a fingerprint or pre-publish delete adds new writes or a new failure status (2.3(4)) for little gain - sound practice is to accept and document it.
- Out of scope: Deleting a downloaded track - rollback of the new file is excluded.

Noted, no ticket.


---

## Q10 — Closing bar, scope, and round cap

Can we close the grill on this review bar and scope? I recommend blocking on every confirmed in-scope finding, including Low, and requiring explicit dispositions for all three M review axes. Scope is the Q1–Q9 download provenance, list contract, selector/refresh, glossary, governance required by the new port, and acceptance proofs. Ticket exclusions stand and anything else is a follow-up issue, not a finding in this round. Cap review remediation at two rounds. Require Phase 3 exact-diff C# analytics, full tests, property tests of ranking invariants, package scan, format, applicable mutation and web gates plus generated contract drift checks and repeat the applicable gates on the rebased delivery head. Report approved skips as the gate reports them. Do you confirm this closing bar, frozen scope and cap, and that the grill is ready for the brief?

[from Patron] DEV-553 Q10 ruling: CONFIRMED - closing bar, frozen scope (Q1-Q9 plus ticket Wanted/Done when, glossary, Q6 port governance), two-round remediation cap and the Phase 3 gate set as proposed; grill closed, ready for the brief.

Basis:
- Frozen scope = ticket Wanted 1-6 and Done when, Out of scope standing (delete track, rename files, remember last track); anything else is a follow-up, filed only if Critical/High or broken behaviour, otherwise noted, no ticket.
- Gates per CLAUDE.md static-analysis gates, the contract chain (openapi.json/types.ts drift) and Q4/Q8 (ranking property tests); skips reported as the gate reports them, never as passes.
- Every confirmed in-scope finding blocks, Low included; after round 2, remaining items go to the owner on the PR rather than a third round.

Standing ruling for this chain; not re-ruled per ticket.

## Phase 2 step 6 plan challenge

All three axes reviewed `feature/553-spec` at `6bd8be67`. Dispositions below follow Q1–Q10 and the ticket's Wanted, Open points, Done when and Where it lands (`recon-DEV-553:40–72`). The implementation refinements and proof decisions are recorded in [brief.md](brief.md#phase-2-step-6-refinements).

| Finding | Disposition and basis |
|---|---|
| Sentry F1 · High | **Accepted.** One representation at publication, persistence, discovery join and token input preserves provenance and selection by ID. Preserve discovery's existing filename semantics and prove the Subs round trip in T002/T012 (Wanted 1, 2, 5 and recognition-policy acceptance, Q1/Q3/Q5/Q7). |
| Sentry F2 · Medium | **Accepted.** T002 requires concurrent same-key writes from independent contexts against PostgreSQL, both successful with one complete committed row. The adapter mechanism is a Build choice that must satisfy that proof (Q7). |
| Sentry F3 · Low | **Accepted.** Q7 supersedes the historical insert-only language in Q1. See the marker at Q1. |
| Sentry F4 · Low | **Accepted.** Persist a row with a null score when score is unknown. It keeps recorded-track precedence, with the missing-score order from Q4 (Wanted 1 and 4). T005 proves the write. |
| Sentry F5 · Low | **Accepted.** Use discovery-derived language for partitioning and wire language. Candidate language remains stored provenance (Wanted 1 and 3–4, Q3/Q4). T009 covers conflicting values; T016 proves grouping on the wire field. |
| Ledger 1 · Medium | **Accepted.** ADR 0054 locks the Core port name only. Build names the table, entity, configuration and adapter under existing Persistence conventions (Q6, ticket Where it lands). |
| Ledger 2 · Medium | **Accepted.** ADR 0054 is claimed and proposed. The Spec PR stays ticket-specs-only per task-pipeline Phase 2 step 7. The proposed ADR is carried to Build, where acceptance ships atomically with the port, Principle I amendment and whitelist update (Q6). |
| Ledger 3 · Medium | **Accepted.** T001 and the delivery rebase verify the ADR claim, current constitution Version and Last Amended, and current whitelist. A collision goes to Bernstein. Use the next MINOR from the live base and preserve later entries (Q6, Principle I, `recon-DEV-553:8–17`). |
| Ledger 4 · Low | **Accepted.** Annotate Q1 and Q6 to make Q7 controlling for all writes. Keep the original Q&A for provenance. |
| Ledger 5 · Low | **Accepted.** T015 checks OpenAPI nullability and generated TypeScript `string \| null` for both fields in addition to drift. T012/T014 remain the JSON serialization proof (Q3/Q8, constitution API contract rules). |
| Compass 1 · Medium | **Resolved by restored evidence.** Independently compared Wants 1–6, the open points, all five acceptance bullets and the file envelope in `recon-DEV-553:40–72` against the plan and specification. Cases for two providers, hand-added and deleted files, selector groups/labels/order/refresh, and unchanged discovery remain in the task proofs. Persistence recon is present at lines 74–82. |
| Compass 2 · Low | **Accepted with Ledger 2.** T000 explicitly excludes the ADR file. The accepted ADR is part of T003 in the Build PR. |

The Sentry residuals carry forward under Q2/Q7/Q9/Q10. Discovery excludes deleted files, orphan rows remain, and same-path external replacement or a failed replacement write can retain prior provenance. Provider/release values use ordinary escaped text in the selector design. No new follow-up or structural owner checkbox arose.

## PLAN FROZEN

Keel closed the DEV-553 Phase 2 step 6 plan challenge on `feature/553-spec` from review baseline `6bd8be67`. All 12 findings are dispositioned, with 11 repaired and Compass 1 closed by restored recon. The read-only `/speckit-analyze` pass on the repaired artifacts found no open issues. All 18 functional requirements and 6 success criteria map to T000–T019. The task dependency order and parallel markers are consistent. The private plan, tasks and proposed ADR copies match their worktree counterparts.

Ready for Patron to set `gate1: provisional`. Rigger's Spec Kit PR contains only `specs/DEV-553/`. ADR 0054 stays proposed for Build pickup under Q6. Ordinary Gate 1 is pending the user's Spec Kit PR merge. Build also requires the DEV-551 delivery merge and T001 pickup evidence.

## Gate 1 provisional (Patron)

`gate1: provisional` at `6bd8be67`. No owner-only structural checkbox on the Spec PR.
- `/speckit-analyze` clean and all 12 plan-challenge findings dispositioned (PLAN FROZEN above); `checklists/requirements.md` User-Visible Outcome items ticked; DRAFTING_RECEIPT `needs decision: none`.
- No §2.3(a) trigger: scope is ticket Wanted 1–6 / Done when (Q10); no §2.3(b) trigger: the port follows constitution Principle I's ADR route (Q6), which is compliance, not departure.
- Spec PR carries `specs/DEV-553/**` only; ADR 0054 stays proposed for Build (Ledger 2). Owner Gate 1 = user merge; Build also waits on DEV-551 delivery and T001 pickup (Q8).
