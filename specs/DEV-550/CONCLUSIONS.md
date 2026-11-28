# DEV-550 - Conclusions

## Q1 - Guard installation path (Phase 2 grill)
Ruling: yes - one shared guard via `ConfigureHttpClientDefaults`, set on the effective `SocketsHttpHandler.ConnectCallback`; ticket text, no ADR.
- Basis: ticket Wanted 1-2 name `SocketsHttpHandler.ConnectCallback` + `ConfigureHttpClientDefaults` on all named/typed clients (decided ticket text); recon-DEV-550 lines 10-12, 32-37 (5 typed registrations in 3 Infrastructure files, 1 `HttpClientHandler`, 0 constructed clients); PRODUCT.md 2.3 - no new dependency, project, layer, schema or API.
- Install point: one idempotent internal Infrastructure extension, invoked from each of the three existing client registration extensions, so every host that registers a client gets the guard without editing host `Program.cs` (stays inside the ticket directories).
- Handler rule: defaults configure the primary handler with the `(handler, sp)` configure overload, never by replacing it; the subtitle client switches to `SocketsHttpHandler` with `AllowAutoRedirect = false` via the same configure-not-replace path, so no per-client handler can drop the callback. OMDb quota, resilience and timeout registrations stay unchanged.
- Coverage: DI tests resolve each typed client's effective primary handler and assert `SocketsHttpHandler` with non-null `ConnectCallback` (plus `AllowAutoRedirect == false` for subtitles), in `tests/LamuFlix.IntegrationTests/`.

- Question (Keel): Should the shared guard use `ConfigureHttpClientDefaults` in the existing Infrastructure registration path for each participating host, configure the effective handler, retain quota/resilience/timeout and the subtitle redirect setting, and stay in the ticket directories using built-in APIs with no ADR? Each host needs the shared registration, and the DI test needs the effective handler.
- Answer (Patron, delivered after commit `4fefc580`): Yes. Use an idempotent internal Infrastructure extension called from the three existing registration extensions, configure the effective handler in place, retain OMDb and redirect behavior, and assert the callback on each effective handler.


## Q2 - Dial-time address policy (Phase 2 grill)
Ruling: yes, Keel's policy, with these concrete sets; no constitution impact (hardening inside the ticket's own scope, no departure).
- Mandatory deny set, every client: 169.254.0.0/16 (link-local, includes 169.254.169.254 metadata), fe80::/10, 100.100.100.200 and fd00:ec2::254 (documented cloud-metadata endpoints), 0.0.0.0/8, ::/128, 224.0.0.0/4, ff00::/8, 255.255.255.255. IPv4-mapped IPv6 addresses are normalized with `MapToIPv4` before matching. OMDb/TMDb/Ollama keep configured loopback/private reachability.
- Stricter set, the whole OpenSubtitles client: mandatory set plus 127.0.0.0/8, ::1, 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 100.64.0.0/10, fc00::/7, 198.18.0.0/15. Test override is an explicit options flag, default false, that relaxes only this stricter set, never the mandatory one.
- Documentation: both sets live in one internal static range table with an XML doc citing the IANA IPv4/IPv6 Special-Purpose Address Registries (RFC 6890); the spec lists the same ranges. Basis: ticket Wanted 1 and Done when; recon-DEV-550 lines 10-12, 32-37; PRODUCT.md 2.3 (no dependency, schema or API change).
- Mechanics: resolve once in `ConnectCallback`, validate each candidate, connect to the exact validated `IPEndPoint`; no second resolution. Deny is a connect failure with a logged reason carrying no secrets or query strings.

- Question (Keel): What dial-time policy should each client enforce? Recommend mandatory link-local/metadata denial including mapped IPv4, whole-client subtitle local/private denial, default-off test override limited to the strict set, preserving configured provider endpoints, and dialing the validated address without another lookup.
- Answer (Patron): Accepted with the explicit address sets, option boundary, documentation, and connection mechanics above. No constitution impact.


## Q3 - Enforced transport paths and proof (Phase 2 grill)
Ruling: yes - guarded clients use only paths where the effective `ConnectCallback` vets the destination before connecting; one enforcement-and-proof decision.
- Enforcement: guarded handlers set `UseProxy = false`, because a proxy resolves the target outside the callback. Requests are capped at HTTP/2 or lower, because HTTP/3 (QUIC) bypasses `ConnectCallback`. Recon-DEV-550 lines 32-37 show no proxy or HTTP/3 configuration in use, so direct provider traffic is preserved. Basis: ticket Wanted 1-2 and Done when; Q1-Q2.
- Proof: DI tests enumerate every named/typed registration from production composition, assert the effective primary handler carries the callback, and include a negative control that fails on an unguarded handler. Request tests drive controlled resolver/connect seams per client: mandatory denial, strict private rejection (subtitles), public allowance, and configured-private reach (OMDb/TMDb/Ollama), with no internet traffic.
- Q2 amendment: range citations (IANA Special-Purpose Address Registries, RFC 6890) live in the spec and the PR body, not XML prose, because AGENTS allows only three comment exemptions. The range table stays one internal static list.

- Question (Keel): Should every guarded connection use a transport where the callback can vet the destination, with production DI coverage, a negative control, controlled resolver/connector behavior tests, and range references in the spec and PR body?
- Answer (Patron): Yes. Disable proxy use, cap HTTP versions at HTTP/2, retain direct provider operation, add the DI and behavior proofs above, and use the spec and PR body for range citations. Q3 supersedes Q2's XML documentation wording. No structural blocker.


## Q4 - NetArchTest rule boundary and exception granularity (Phase 2 grill)
Ruling: yes, Keel's boundary, with exceptions matched at type level by exact full name.
- Namespace rule: the seven ticket-named names match only as whole namespace segments across production application assemblies, so legitimate partial names stay legal. Basis: ticket Wanted 3 and Done when; ADR 0015 as the ticket cites it.
- EF rule: any EF Core dependency, method-body calls included, outside `LamuFlix.Infrastructure.Persistence` and its subnamespaces fails. Exactly two exceptions, each one exact full type name with no namespace wildcard: `OutboxRelayHostedService` and the Api `Program` type (the migration call). NetArchTest works per type, so finer exceptions are not available and no new dependency is justified (PRODUCT.md 2.3). Moving either one is out of scope (ticket Out of scope; recon-DEV-550 lines 13-14).
- Proof: use the existing `ArchitectureTests.cs` and the `Fixtures/Valid` / `Fixtures/Violating` conventions (recon-DEV-550 lines 15-16, 28). Production passes, each violating fixture fails, and one violating fixture uses EF only inside a method body.

- Question (Keel): Should the architecture rules match whole namespace segments and EF dependencies including method-body calls, with exactly the two ticket-named exceptions and fixture tests that show both rules are effective?
- Answer (Patron): Yes. The EF exceptions are the exact full type names of the relay and Api Program because NetArchTest evaluates per type. No wildcards or dependencies. Require an EF fixture with EF only inside a method body.


## Q5 - Test override location and file boundary (Phase 2 grill)
Ruling: yes - the override lives only in internal Infrastructure guard options, default false, set only by test service composition. It is never bound from configuration and has no app setting or deployment key.
- Basis: ticket Wanted 1 and Where it lands; recon-DEV-550 lines 11, 16, 25, 28; Q1-Q4. Production configuration and constitution Principle VII subtitle acquisition are unchanged.
- File boundary (PRODUCT.md 5 item 6): the three named registration files; new guard/option code in `src/LamuFlix.Infrastructure/Adapters/`; `ArchitectureTests.cs` and its fixtures; new guard/DI tests in `tests/LamuFlix.IntegrationTests/`. A focused override opt-in is permitted in the existing `OpenSubtitlesSubtitleProviderTests.cs`, `SubtitleSearchDownloadEndpointTests.cs` and `SubtitleProviderHealthRegistrationTests.cs`, only where they hit guarded local endpoints. Those tests must still prove the mandatory set applies.
- Forced edit: if the integration tests cannot reach the internal options, adding `InternalsVisibleTo` for `LamuFlix.IntegrationTests` to the Infrastructure project is in scope, forced by the Done-when guard tests. Any other existing file needs a ruling. Host `Program.cs` stays unedited (Q1).

- Question (Keel): Can the test override and its implementation stay in internal Infrastructure guard options and ticket-scoped fixtures/tests, with existing subtitle tests changed only where they exercise guarded local endpoints?
- Answer (Patron): Yes. The default-false option is set only by test service composition, with zero configuration binding. The three known subtitle integration tests can opt in locally, and the existing Infrastructure project file can add test friendship if needed. Any other existing-file expansion requires a ruling.


## Q6 - Closure package (Phase 2 grill, final)
Ruling: approved as proposed; this closes the grill on Patron's side.
- Frozen scope: ticket Wanted 1-3 and Done when, inside the Q1-Q5 boundaries. Anything else becomes a follow-up issue. Close every verified in-scope finding through Low and record all three Size-M dispositions. Two review rounds, at most two fix commits per round. No ADR and no Gherkin opt-in. Basis: ticket Done when/Needs; PRODUCT.md 2-5; task-pipeline 0, 2.2 and Phases 3-5.
- Proof bar: green C# gates at the exact head, including property checks of the address-policy invariants and mutation testing under existing harness policy. Separate redacted reachability receipts for OMDb, TMDb, Ollama and OpenSubtitles, from both Compose and the home cluster.
- Order: factory/transport and address-policy work first, then behavior and regression proof. The architecture rules run as an independent stream. Verification runs after both streams. DEV-550 and DEV-551 run one after the other, per the ticket.
- Inventory: the plan works from the four providers recon-DEV-550 lists individually (lines 11, 34) plus dynamic DI discovery, not the headline count of five. A fixed count is not an acceptance criterion.

- Question (Keel): Do the frozen ticket scope, through-Low three-axis review, two-round/two-fix cap, exact-head gates, separate two-environment receipts, and stated ordering close the grill?
- Answer (Patron): Approved as proposed, closing the grill. Use four individually listed providers as starting coverage and dynamic DI discovery for completeness. No ADR or Gherkin.
- Inventory clarification: recon-DEV-550 lines 34–37 list four typed providers. The raw tally includes the subtitle primary-handler construction and does not identify a fifth client registration. This clarification supersedes the Q1 headline registration count for planning.

- Question (Keel, analyze round 1 item 1): Constitution IX assigns FsCheck property tests to LamuFlix.UnitTests, but Q5 and the brief placed the two address-policy properties in IntegrationTests. Where do they go?
- Answer (Patron): Approved, scope-preserving. All ticket-required guard, request-behaviour and DI tests stay in `tests/LamuFlix.IntegrationTests/`. The two pure properties (mandatory-deny invariance under the subtitle override, IPv4-mapping equivalence) move to the existing `tests/LamuFlix.UnitTests/`. This supersedes the Q5/brief property placement.
  - Basis: constitution IX (property tests live in LamuFlix.UnitTests); ticket Where it lands names IntegrationTests only for guard and DI tests (recon-DEV-550 lines 10, 16, 28), so the properties' home is not ticket text and no constitution departure is needed.
  - UnitTests already references Infrastructure, FsCheck and FsCheck.Xunit.v3 (verified in its csproj), so there is no new dependency, project, layer or public API.
  - Forced edit: add `InternalsVisibleTo` for `LamuFlix.UnitTests` to the Infrastructure csproj only if the properties cannot reach the internal policy. It is forced by constitution IX plus FR-009.

## Phase 2 step 6 plan-challenge adjudication — 2026-11-28

Reviewed `feature/550-spec` at `ed5cf8e1da13b2fc683cd2f2c56bbf0168015bab` together with its working-tree draft artifacts. Sources are `findings-DEV-550-Sentry`, `findings-DEV-550-Ledger`, `findings-DEV-550-Compass`, `recon-DEV-550`, `DEV-550`, and this directory's brief, spec, plan and tasks. Source facts are restricted to recon. Where a proposed fix depends on an unproven source detail, the ruling either defers it or makes it a read-only T001 pickup check. The brief records the accepted refinements before this adjudication.

### Sentry — Risk

| Finding | Disposition | Rationale and closure |
|---|---|---|
| M1 | Accept | Q3 and FR-006 apply to any followed connection. T008 needs a controlled followed-redirect regression for a client that already follows redirects. Preserve the OpenSubtitles redirect setting and cover any existing explicit follow path established at T001. Recon lines 11–12 and 35 establish the setting and URI checks. They leave the explicit follow path open. Fixes 1 and 4. |
| M2 | Defer | Q1 and FR-002 preserve existing quota and resilience. Recon line 11 locates setup and leaves actual denial handling and quota timing unverified. A new non-retry exception contract and a no-quota guarantee need evidence and a cited ruling. Existing per-connection denial proofs and retention checks remain the closing evidence for this round. |
| M3 | Defer | Q2 fixes the address sets and normalization. Recon lines 10–12 and 32–37 contain no deployment translation-path evidence to justify expanding both policies. A proposed change needs evidence and a policy ruling before any new range or interpretation enters the artifacts. |
| M4 | Accept | Q2/Q3 and FR-003/FR-005/FR-008 support controlled dependencies and fallback within the same validated candidate result. Require production DNS and socket delegates, test-only substitution, and regression proof of fallback and zero denied dials. Candidate order remains an implementation choice subject to existing timeout and cancellation behavior. Fixes 2 and 4. |
| L1 | Accept | FR-003 requires failure when no endpoint qualifies. An exception or empty result must yield zero connector calls. Cover null if the selected seam can represent it. Fixes 2 and 4. |
| L2 | Reject | FR-011, SC-006 and Q6 define T017 as two candidate-specific provider reachability receipts. T007–T008 and the exact-head gates already carry enforcement evidence. A mandatory deployment negative probe would add a new runtime acceptance condition. |
| L3 | Accept | FR-007 and SC-003 require discovery of an unguarded addition. The negative control must pass through the independent discovery and effective-handler inspection path. Start with the Api composition from recon line 13 and inventory all other participating compositions at T001. Fixes 1 and 3. |
| L4 | Accept | FR-008 already requires CIDR edges. Independent expected examples for every approved range boundary and singleton catch classification errors that invariant properties can miss. Include mapped equivalents where applicable and account for neighboring ranges in expected outcomes. Fix 4. |
| L5 | Accept in part | Q2 and FR-003 require a redacted reason. Use a reason category and assert captured logs omit URL, query and secret header values. Leave severity and throttling to existing conventions unless evidence calls for a distinct decision. Fix 4. |

### Ledger — Standards

| Finding | Disposition | Rationale and closure |
|---|---|---|
| L1 | Accept | AGENTS mutation guidance applies to T002 and T015. Use stable literals and keep mutable or mutation-sensitive computation out of static initialization. Q2 still calls for one policy table. Fix 2. |
| L2 | Accept | Q3 requires deterministic request tests. State the resolver/connector delegate substitution boundary in T003/T004 and T008, using the existing Infrastructure envelope and production defaults. Keep the relaxation option limited to the address policy. Fixes 2 and 4. |
| L3 | Defer | Recon lines 16 and 28 establish test locations. They leave Infrastructure project friendship unverified. T001 and T006 already resolve that exact condition at pickup, with a recorded skip when access exists or is unnecessary. Retain conditional authorization. |
| L4 | Accept | Q4 and FR-010 require a production pass with two exceptions. Recon lines 13–15 identify the two known cases and existing checks, while the full assembly and EF dependency inventory remains open. Make the inventory explicit in read-only T001 and gate rule work on its disposition. An extra source dependency needs a cited Patron ruling before a file or exception expansion. Fixes 1 and 5. |
| L5 | Accept in part | Q4 limits exceptions to two source full type names. Verify how the installed rule reports compiled artifacts at pickup. Any required attribution must use evidence of compiler-generated origin and exact source identity. Keep an ordinary nested or near-match violating case if attribution logic is needed. Fixes 1 and 5. |
| L6 | Accept | Q4 and FR-010 require independent fixture proof. Reuse the same predicates on production and fixture inputs, supplying the fixture scope, Persistence root and exact exception names as needed. Follow the existing directories confirmed by recon lines 16 and 28. Fix 5. |
| L7 | Accept | Q1 already requires idempotence and configuration of the effective handler. Specify a per-service-collection registration guard, verify applicable configuration ordering at pickup, and exercise repeat registration and the retained subtitle callback in T007. Leave the marker representation to Build. Fixes 1–3. |
| L8 | Accept in part | Use project conventions and cite constitution IX by section. Confirm property placement and harness discovery at T001 and identify the T017 operator evidence owner. Keep per-task gates and T013/T015 because the constitution and current task conventions require both. Fixes 1 and 6. |

### Compass — Spec

CLEAN. Zero findings. Scope, acceptance mapping and delivery evidence agree with Q1–Q6.

### Quill fix list

1. **Plan Phase B and T001** — Add read-only pickup receipts for the participating host compositions (Api first, others if found), existing redirect handling, effective factory setup ordering, property file and runner conventions, and production architecture assembly/EF inventory including generated-type reporting. Verify API ordering against Microsoft Learn at pickup. Retain the existing friendship check and DEV-551 serialization. Stop on missing required evidence or an additional EF source dependency before dependent work.
2. **Plan Approach and T002–T005/T015** — State mutation-safe policy initialization, internal resolver/connector delegates with production DNS/socket defaults and test-only substitution, one resolution with fallback among validated exact endpoints under existing timeout/cancellation, and fail-closed resolver errors. State per-service-collection idempotence and configure-in-place subtitle setup. Include static-initialization discipline in mutation verification.
3. **Spec FR-007/US1, plan DI and T007** — Discover real factory registrations independently of guard wiring and require nonempty coverage of each confirmed composition. Run the same discovery and inspection against an added unguarded registration, and prove repeated installation applies the shared defaults once while the effective subtitle handler retains its callback and redirect setting.
4. **Spec FR-003/FR-008 and edge cases, plan request proofs, T008** — Add controlled followed-redirect coverage (plus any confirmed explicit subtitle follow flow), allowed-candidate fallback in one lookup, resolver exception and empty-result cases (null only if representable), independent examples for all approved range boundaries and singletons with mapped equivalents where applicable, and captured denial reason without URL, query or secret-header values. Preserve current request-path and retention assertions.
5. **Plan architecture verification and T011–T013** — Use the T001 assembly/EF inventory and the same predicates for production and independent positive/negative fixture assertions. Preserve the two exact source exceptions. If generated-artifact attribution is needed, require exact origin evidence and a nested or near-match violating case for the attribution logic, alongside the independent method-body fixture.
6. **Plan conventions and T009/T017** — Match existing assertion and mocking conventions, cite constitution IX by section, apply the pickup-verified property path and harness discovery convention, and identify the pipeline-assigned operator evidence owner. Retain current per-task gates, T013, and T015.
7. **Private mirrors** — Sync the changed canonical plan and tasks to private `IMPL_PLAN` and `TASKS`. Verify SHA-256 equality after all edits and return the changed sections and mirror evidence for Keel's independent reread.

**Disposition** — 17 findings adjudicated. 13 accepted including partial acceptances, 3 deferred, and 1 rejected. The independent reread closed the required repairs. See the freeze receipt below. Ordinary Gate 1 follows plan freeze and the Spec Kit PR merge.

## PLAN FROZEN (2026-11-28)

Keel independently reread the Phase 2 step 6 artifacts on `feature/550-spec` at `ed5cf8e1da13b2fc683cd2f2c56bbf0168015bab` with the working-tree drafts and checked them against the brief, constitution, and `DEV-550` and `recon-DEV-550` notes. The 17 plan-challenge dispositions above stand.

- Fix 6 is closed. `plan.md:23` and `tasks.md:15` require Shouldly wherever it has an equivalent and allow xUnit `Assert` only where it does not, per constitution IX. `plan.md:27` includes both IntegrationTests and UnitTests.
- Canonical and private plan SHA-256 match `819D0E0B6E4547EE6C08C58372061A9592DAA3C427F516203440ACE57987E20C`. Canonical and private tasks SHA-256 match `E758F31F7246FB513ECDDB0219ABF4464D02E1072A2BE2C5E095EA76085612A2`.
- Read-only `/speckit-analyze` passed with `PYTHONUTF8=1`. Prerequisites resolved the expected artifacts and both mirrors. All 12 functional requirements and seven success criteria have task coverage across T000–T018. The reread found no remaining consistency, task ordering, coverage, or constitution issues.

Owner Gate 1 is pending the user merge of the Spec Kit PR. Phase B pickup follows Gate 1. Compose and home-cluster reachability receipts are pending delivery verification.

## Gate 1 provisional (2026-11-28)

gate1: provisional - `/speckit-analyze` clean and plan challenge adjudicated (PLAN FROZEN above); requirements checklist User-Visible Outcome ticked (Nothing visible, spec.md:23). Owner Gate 1 pends the user merge of the Spec Kit PR.
