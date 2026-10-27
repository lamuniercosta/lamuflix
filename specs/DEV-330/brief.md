# DEV-330 — Enrichment status SignalR push

Status: Grill complete; ready for Quill drafting. Patron confirmed shared understanding in Q9. This is not plan freeze, Gate 1 approval, Phase B authorization, or review clearance.

## Identity and evidence

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-330-spec`; branch: `feature/330-spec`.
- Recon baseline: `52e443666b5e767edc93befc20eb55de2e77787b`.
- Ticket facts: note `DEV-330:27-30`; factual evidence: notes `recon-DEV-330` sections 0–9 and `recon-DEV-330-2` Q1–Q3. These are baseline facts, subject to Phase B drift checks.
- Complete questions, recommendations, answers, and cited bases are append-only in `CONCLUSIONS.md`, Q1–Q9. All operative decisions are restated below.
- Size L. Dependencies named by ticket: DEV-421, DEV-310, DEV-314, DEV-320. This brief does not claim live prerequisite completion or authorize upstream work.

## Owner question — Gate 1 stays closed

- [ ] **blocked: structural — Approve Constitution v1.5.0 MINOR amendment (add IEnrichmentStatusNotifier to the Principle I closed port list, update polling-only enrichment status wording per ADR-0022, and update the architecture port whitelist) plus ADR-0029 for the notifier port and post-commit semantics?**

Q1 basis: ticket authorizes the notifier; accepted ADR-0022 accepts SignalR but defers constitution alignment; the constitution closes the port list and requires an ADR for additions. Recon locates the port rules at `constitution.md:148-152`, polling text at `:507` and `:534`, amendment procedure at `:550-567`, and whitelist at `ArchitectureTests.cs:30-32,155-177`. Patron rules the amendment necessary under charter §2.3(b). Only the user can answer this checkbox; Patron approval of the design does not answer it.

Quill must carry this checkbox onto the spec PR. Prepare the proposed amendment in the Phase A documents, preserving unrelated constitution rules and its existing port list. The proposal adds the notifier, aligns only enrichment polling wording with ADR-0022, updates the amendment/version record to v1.5.0, and names the matching whitelist change. Actual delivery must accompany the approved amendment; no noncompliant implementation is authorized in advance.

## Frozen scope and grill answers

**Q1 — Constitution and ADR.** The ticket-required port and push channel require the above owner question. Draft `docs/adr/0029-enrichment-status-post-commit-notifier.md`; number 0029 is verified free in recon addendum Q1. ADR remains Proposed until the owner gate is satisfied.

**Q2 — Committed change.** Emit exactly one event per movie's persisted actual enrichment-status transition: insertion into Pending counts; existing persisted-before status differing from committed-after status counts. Pending-to-Pending, unchanged status, unrelated movie updates, and failed saves emit nothing. Multiple mutations before a save are judged by persisted-before and final committed-after, not intermediate in-memory states. Cover all repository callers without registering the currently unused handlers. For duplicate IMDb recovery, the failed first save emits nothing; only a successful recovery transition to Failed emits one event. Do not retain a failed attempt's pending event.

**Q3 — Delivery, exceptions, and cancellation.** One awaited broadcast invocation after successful commit is the AC1 delivery boundary. No guaranteed client receipt, replay, durable notification state, notification outbox, retry, or crash recovery is added. Log and isolate broadcast failures so they cannot fail an already committed save or trigger enrichment retry. Cancellation that aborts persistence emits nothing. After commit, caller cancellation must not silently skip the send attempt. Reconnect/refetch belongs to DEV-422 under ADR-0022.

**Q4 — Public contract.** Route `/hubs/enrichment`; event `EnrichmentStatusChanged`; one positional `EnrichmentStatusChangedDto(int Id, EnrichmentStatus EnrichmentStatus)` argument. Api unwraps `MovieId.Value`. JSON is camelCase with exactly `id` and `enrichmentStatus`; status is one of `Pending`, `Enriched`, `NotFound`, `Failed`, never numeric or a SmartEnum object. Use the existing Api immutable record conventions. Configure SignalR JSON name-converter semantics independently of HTTP JSON. Broadcast to all connected clients; add no client-to-server hub methods, groups, or subscriptions. Include no timestamps, metadata, or error details.

Expose this C# DTO through a document transformer in `components.schemas`, preserving required fields and the four-name string enum. It must derive from the C# contract and use the existing serializer/schema conventions. Add no REST operation. Regenerate `web/src/api/openapi.json` and its derived Verify snapshot via the existing `LAMUFLIX_REGENERATE_OPENAPI=true` workflow; run `npm run generate:api` for `types.ts` and generated mocks. The existing generator emits components-only types and paths-only mocks; a hub schema needs no new mock handler. Never hand-edit generated artifacts. DEV-422 consumes the generated type.

**Q5 — Architecture.** Add Core port `Task NotifyAsync(MovieId movieId, EnrichmentStatus status, CancellationToken cancellationToken)`. Core contains no SignalR/Api DTO dependency. At `EfMovieRepository`, detect changes using persisted-before and final committed-after data, retain assigned insert IDs, and invoke the port only after the actual enclosing persistence transaction commits. An intermediate `SaveChangesAsync` inside an uncommitted transaction is insufficient. Clear failed-attempt transitions before recovery. Api `SignalREnrichmentStatusNotifier` maps the DTO and uses `IHubContext<EnrichmentHub>` / `Clients.All`; send logging and isolation live at this adapter. The Api host must resolve the real notifier. An optional constructor fallback used for isolated fixtures cannot silently disable live notifications. No handler-specific notification calls, event bus, EF interceptor, new project/layer, or database schema changes.

**Q6 — Dependency.** Patron approves `Microsoft.AspNetCore.SignalR.Client` only in the existing integration test project, through `Directory.Packages.props`. Selected version is 10.0.14 to align with the Microsoft package train cited by Patron; verify alignment for drift at Build pickup. No server NuGet or npm dependency is added. Use the official client against the test host rather than implementing raw SignalR framing.

**Q7 — CORS/security.** Retain existing `DevSpa`: exactly `http://localhost:5173`, any method/header, exposed `Location`, no credentials. Apply to the hub and negotiation endpoint. Do not add origins, authentication, authorization, or transport restrictions. DEV-422 must use `withCredentials: false`. CORS is a browser policy; rejected-origin assertions concern missing allow-origin permission, not a newly invented authorization response.

**Q8 — Plan and validation.** Use the file scope, task order, test matrix, and gate expectations below. Production/test/config edits occur only in the Build workspace after approval and pickup drift checks.

**Q9 — Closing agreement.** Patron confirmed Q1–Q9 and the loop terms below. No new domain vocabulary or taste rulings arose; no `CONTEXT.md` or `ASSUMPTIONS.md` edits are needed. Existing vocabulary and ADR-0022, plus proposed ADR-0029, are ready to inform Quill's spec.

Frozen delivery scope is the ticket hub, Core port and Api adapter; Q2–Q3 commit/failure semantics; Q4 payload and generated contract chain; bounded persistence/DI/CPM/test adaptations in Q5–Q8; localhost CORS; ADR-0029 and the owner-gated constitution alignment. React hook/client work, scale-out/backplane, replay, notification outbox, schema migrations, auth, playback execution, secrets, `Process.Start`, new projects/layers, and unrelated rewrites are excluded; anything else is a follow-up issue, not a finding in this round.

## Planned files and bounded adaptations

Phase A writes are documentation only:

- Keel: this brief, append-only `CONCLUSIONS.md`, and `docs/adr/0029-enrichment-status-post-commit-notifier.md`.
- Quill: `specs/DEV-330/spec.md`, `plan.md`, `tasks.md`, and the proposed constitution amendment/supporting design documents required by its workflow. Quill drafts from this brief; Keel does not draft those artifacts.

Phase B proposed new production files:

- `src/LamuFlix.Core/Ports/IEnrichmentStatusNotifier.cs`.
- `src/LamuFlix.Api/EnrichmentHub.cs`.
- `src/LamuFlix.Api/EnrichmentStatusChangedDto.cs`.
- `src/LamuFlix.Api/SignalREnrichmentStatusNotifier.cs`.
- `src/LamuFlix.Api/EnrichmentHubSchemaTransformer.cs`.

Phase B modifications are confined to `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs`, `src/LamuFlix.Api/Program.cs`, the existing JSON configuration if necessary to share converters, `Directory.Packages.props`, the integration test project, `.specify/memory/constitution.md`, `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`, `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`, `tests/LamuFlix.IntegrationTests/ApiCorsTests.cs`, generated contract artifacts/snapshot, and strictly necessary existing repository composition/constructor fixtures. The latter adaptations may change only wiring required by this notifier, not unrelated behavior. Existing-handler notification edits and new unused-handler registrations are excluded.

Proposed new tests:

- `tests/LamuFlix.UnitTests/Enrichment/SignalREnrichmentStatusNotifierTests.cs`.
- `tests/LamuFlix.IntegrationTests/EnrichmentHubTests.cs`.
- `tests/LamuFlix.IntegrationTests/EnrichmentStatusNotificationPersistenceTests.cs`.

These are design paths, not assertions that the new files already exist. Architecture whitelist, CORS, contract tests, and direct fixture updates are forced by ticket deliverables/gates under §2.3 item 6. No unrelated tracked file deletion/rewrite is approved. If implementation requires facts absent from recon, request bounded recon rather than inventing contracts.

## Task ordering and test strategy

After user Gate 1 and Phase B pickup drift, preserve this order:

1. Apply approved constitution/port-whitelist alignment; add Core port and test-only CPM client dependency.
2. Implement repository transition detection and post-commit invocation with focused persistence tests using real database/Testcontainers behavior.
3. Implement Api hub, adapter, serializer and DI; add adapter unit tests and real official-client/test-host integration tests.
4. Expose the C# DTO components schema; regenerate OpenAPI baseline, Verify snapshot, TypeScript and other generated artifacts through existing commands.
5. Run applicable verification and review loops at the resulting head.

Test the ordinary inserted-Pending case with its assigned ID; actual transitions through re-request, manual match and consumer completion; unchanged Pending and unrelated saves; failed persistence and transaction commit; duplicate-IMDb recovery; repeated saves without duplicate notifications; and all changed movies in one save exactly once each. Test send failure without changing committed-save outcome, cancellation before commit, and caller cancellation after commit. Use real persistence for transaction/query behavior; never mock the data-access driver for these semantics.

Unit tests verify adapter mapping/broadcast and send-exception isolation. Real HubConnection/test-host integration verifies negotiation, actual broadcast framing, camelCase/int fields and all four status names. Test allowed OPTIONS preflight on `/hubs/enrichment/negotiate`, disallowed origin, successful negotiation and live delivery. OpenAPI tests verify required fields, enum names and unchanged exact REST operations. Generated contract drift checks must pass. Match existing test/assertion/mock conventions; no opt-in acceptance-test framework is introduced.

## Gate expectations and loop discipline

Phase A: docs drafting, read-only `speckit-analyze`, reconciliation against this brief, then plan challenge/freeze through Conductor. No production tests, analyzers, formatters, mutation or delivery gates run in this grill. Later docs checks follow their actual scope; no unrun gate is claimed passed.

Phase B: recheck main/landed prerequisites and contract/DI drift; run applicable analyzer, complexity, inspection, architecture, unit/integration, property, vulnerable-package, format, mutation and web/generated-contract gates under current harness configuration. Never edit generated thresholds or lower them. Never use Stryker `since` in a worktree. Report scope-empty SKIPPED, opt-out SKIP and mutation N/A distinctly; an applicable gate that could not run has not passed.

Closing bar: verified in-scope Critical, High and Medium findings block. Low findings are recorded non-blocking. Unnecessary abstraction and out-of-scope improvements follow charter follow-up rules, not additional fix commits. Review may not silently demote a blocker or close an owner checkbox.

Round cap: two rounds for a challenge/review loop; no extra review to repair publication/receipt defects. At the cap, unresolved blocking findings return blocked to Conductor. Later M/L adjudication requires all Sentry, Ledger and Compass axes. Keep analysis, plan freeze, owner Gate 1, implementation, review clearance, local/hosted verification and merge bar as separate claims. Never merge; the user owns merge.

Next handoff: Conductor schedules Quill drafting from this brief. Keel then runs read-only cross-artifact analysis, checks plan/tasks against the brief, and returns one numbered fix list per round. No code delivery is authorized by this handoff.

## Plan challenge Round 1 — PC1 clarification (operative)

Patron approved PC1 in CONCLUSIONS.md; this supersedes Q5 optional-fallback permission and clarifies Q3/Q5 without changing ticket delivery:

- Pass `CancellationToken.None` at every post-commit `NotifyAsync` call, independently of the caller token.
- Require `IEnrichmentStatusNotifier` in the repository constructor. Fixtures supply explicit doubles; no optional/no-op production fallback. Name a live-host integration test proving real `SignalREnrichmentStatusNotifier` resolution.
- Catch/log notifier exceptions per transitioned movie at the repository dispatch boundary as well as isolating adapter sends. A throwing notifier never fails the committed save and never prevents attempts for remaining transitioned movies. Add that multi-movie throwing-double persistence proof.
- Retain sequential awaited ephemeral attempts, with no application-level timeout or fan-out bound. Single-instance scope accepts potential unbounded post-commit latency; there is no new performance or delivery guarantee.
- Adapter logs attempt start and failure with movie id and committed status. Process death between commit and send can lose the event without a log; logging is not durable crash-window detection. T036 names this limit and DEV-422 reconnect/refetch convergence.

Task clarifications must preserve phase order, stable task IDs, generated-contract chain, and the unchecked owner constitution/ADR question. Gate 1 remains closed.

### PC2 — Scope and task clarification (operative)

Patron approved PC2 in CONCLUSIONS.md. Ledger M2 is non-blocking Follow-up (noted, no ticket): speculative transition-tracker abstraction is not added. Preserve frozen file set; T016/T031 enforce actual complexity, and measured need requires a bounded file-list ruling. Retain Api root paths; no taste change is adopted.

Keep the new document transformer: recon-DEV-330-2 Q3 distinguishes document-level insertion for an unreferenced hub DTO from the existing transformer for discovered types. Record this rationale in research/plan; preserve serializer/schema conventions and exact operation tests.

Clarify T018 retained DevSpa facets (exposed Location on the appropriate actual response), T011 caller-independent repository proof, T001 gating T006/T007, and incremental gate exact touched-file `-Files` receipts. Keep stable T012 and phase dependencies; no renumbering needed. No scope, dependency, or owner checkbox change.

## Plan challenge closure — 2026-10-26

All three axis reports received; all 18 findings adjudicated in PLAN_CHALLENGE_ADJUDICATION.md. Patron PC1/PC2 exchanges recorded above; Quill applied the single consolidated Round 1 list and DRAFTING_RECEIPT.md:38-71 records completion. Keel independently read back revised artifacts and reconciled 14 FR + 6 SC across 36 tasks, with no uncovered requirement or new blocking inconsistency. All accepted document fixes resolved; Ledger M2 remains non-blocking Follow-up (noted, no ticket), with no speculative helper authorized.

Plan challenge COMPLETE; plan frozen for owner Gate 1 review with operative PC1/PC2. Ready for Phase A spec PR preparation/publication by the responsible seats. This is not owner approval: Q1 remains unchecked, Gate 1 stays CLOSED, and Phase B implementation is unauthorized until user approval and pickup drift clearance. No delivery gates or merge-bar claim.

### Phase 6 T033 mutation gate scope ruling (operative, 2026-10-27)

Patron approved the T033 mutation-gate execution decision in `CONCLUSIONS.md`: the initial diff-filtered run at `698da306` remains recorded as applicable `FAILED` (exit 1, blocking; never `PASS`, `N/A`, or `SKIPPED`), and one whole-Core recovery run is authorized from `F:/Dev/LamuFlix.worktrees/DEV-330` using the pinned supported harness invocation `pwsh -NoProfile -File ./scripts/run-mutation.ps1 -WholeProject` (`gates.mutation.wholeProject: LamuFlix.Core`). Preserve Constitution IX, threshold 80, `LamuFlix.Api`/`LamuFlix.Infrastructure` exclusions, `maxKillShare: 50`, `maxKillFileSpan: 15`, and `since` disabled, and retain the full console transcript, emitted `stryker-config.json` + SHA256, `mutation-report.json`, and per-file mutant counts and `statusReason` values. No production code, threshold, or config change is authorized to manufacture green; if the retained report proves a shared harness defect, route an evidenced follow-up separately through Rigger.

## Remediation Round 2 — post-commit cache failure and inspection gates (2026-10-27)

Patron approved this PC1/FR-008 clarification at delivery HEAD `579a1f99a8ed1244f52304dd61df26d7196f9592` on `feature/DEV-330`, worktree `F:/Dev/LamuFlix.worktrees/DEV-330`; full cited ruling is appended to CONCLUSIONS.md. Basis: note DEV-330:136,155-168; existing main FeedbackTasteProfileInvalidationTests.cs:143-178; spec FR-005/FR-008; PC1. No ticket scope or owner-checkbox change.

1. In `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs`, restore original taste-profile invalidation caller-token and exception-propagation behavior. After successful actual persistence/commit, capture/drain the committed transition batch, execute post-commit cache invalidation inside try, and await notification dispatch inside finally. The try/finally must not surround persistence/commit. Retain `CancellationToken.None` and exception isolation per movie at dispatch; a notifier failure must not replace the invalidation exception or prevent remaining attempts. Preserve Round 1 external-transaction commit handling, no emission on failure/rollback, final committed states, and no replay on repeated saves. Do not introduce a speculative helper, dependency, schema or API change.
2. In `tests/LamuFlix.IntegrationTests/EnrichmentStatusNotificationPersistenceTests.cs`, update post-commit cache-cancellation and non-cancellation failure tests to assert both the original propagated exception and exactly one notification attempt per committed transition with the caller-independent token. Add combined cache failure plus throwing notifier coverage across multiple committed movies; prove database writes remain and repeated saves do not replay the consumed batch. Keep main `FeedbackTasteProfileInvalidationTests.SaveChangesAsync_FailedInvalidation_PropagatesAndKeepsTheDatabaseWrite` unchanged and passing. Use existing real persistence/Testcontainers conventions.
3. In that same test file, match FailingHybridCache override optional-parameter defaults exactly to the inherited HybridCache declarations. Remove only the four reported redundant using directives in `tests/LamuFlix.IntegrationTests/ApiEndToEndFactory.cs`, `ApiHostFactory.cs`, and `HealthEndpointTests.cs`. No warning suppression or unrelated rewrite.
4. Cog implements this Round 2 prescription and supplies changed-head/diff and focused proof. Gauge verifies the resulting exact HEAD with the applicable non-waived gates, including InspectCode and the full test suite. Failure B remains the recorded transient RabbitMQ failure with isolated retry evidence; any recurrence must be reported honestly. Preserve T033 owner WAIVER and its original FAILED receipt; do not rerun mutation. No threshold/config relaxation, new review round, review-clearance claim, or merge-bar claim is authorized. Unresolved blockers at the two-round cap return blocked to Conductor.
