# DEV-456 — Frozen grill brief

Status: BRIEF FROZEN by Patron Q9, 2026-11-07. This is Phase A input for Quill, not PLAN FROZEN, Gate 1 approval, implementation authorization, runtime acceptance, or merge clearance.

## Identity and authority

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-456-spec`.
- Branch: `feature/456-spec`; recon base: `659a2e9b739c092dc8565ee31ed6bc5bcc4b68e3`.
- Ticket: DEV-456, Size M. Dependencies: DEV-438 and DEV-459.
- Facts: `recon-DEV-456` and `recon-DEV-456-2` in LamuFlix notes. No independent code searches or runtime measurements were used in this grill.
- Ticket scope already decides `GET /api/movies/{id}/stream`, existing resolver reuse, library-root containment, full/partial/invalid-range/seeking verification through Compose nginx (recon-DEV-456 §2).
- Decision authority: Patron Q1-Q9, with full exchanges, cited bases, costs and implications in [CONCLUSIONS.md](CONCLUSIONS.md). This brief carries every answer as operative requirements below.

## Vocabulary

Primary video means the file selected by the existing `IMediaLibraryScanner.ResolvePrimaryVideo` rule, not a new stream-specific selection rule. Movie folder means the stored library folder, normally canonical relative after DEV-459. Library root means configured `LibraryOptions.RootPath`. Stream means the bytes of the authorized primary file over HTTP, not a local-player launch or a playback-history event. Seeking proof includes both byte-range sequences and browser playback after a seek.

## Q1 — Route, access, and side effects

Patron approved the ticket-named GET route as read-only binary delivery under the current library caller access policy, following the thumbnail access-parity precedent. No history/catalog writes, launcher calls, `Process.Start`, secrets, or `Features:LocalPlay` dependency. With `Features:LocalPlay=false`, streaming succeeds. Existing local launch behavior remains governed by its current flag. Do not call the launcher-bearing play handler to reuse resolution.

Basis: recon-DEV-456 §§2, 4, 7, 10; explicit §2.3.4/5 ruling in Q1.

## Q2-Q3 — Range, media type, and generated contract

Patron approved framework file-result processing, then explicitly superseded Q2's pre-freeze documentation prerequisite for Learn-unspecified edges in Q3. The following requirements are frozen:

| Request | Required response |
| --- | --- |
| No Range | 200, complete byte-exact file, exact file length, `Accept-Ranges: bytes` |
| Satisfiable single bytes range | 206, exact source slice, exact `Content-Range` and `Content-Length`, `Accept-Ranges: bytes` |
| Unsatisfiable valid byte range | 416, `Content-Range: bytes */length` |

- Use `Results.Stream`/`TypedResults.Stream` or equivalent stream file-result family with `enableRangeProcessing: true` on one open readable seekable file stream with known length. No custom Range parser, whole-file buffering, or multipart implementation.
- Seekability/known-length framework prerequisites are not doc-proven; the implementation supplies these properties and characterizes the machinery in tests rather than asserting undocumented framework facts.
- Exact frozen headers are acceptance requirements, even where automatic framework emission is not documented. Assert them through real endpoint and Compose tests; set a frozen header where necessary without replacing framework range parsing. Any contradiction of a frozen item blocks delivery and returns to Patron.
- Malformed Range syntax, unsupported range units and multiple ranges retain framework behavior. Characterize and record observed results at implementation time; no guaranteed status or multipart promise belongs in the spec.
- Content type: read-only `FileExtensionContentTypeProvider` lookup using `.` plus resolved `MediaFormat.Extension`; fallback `application/octet-stream`. No copied MIME table or request-time mutation of provider mappings. Do not promise a browser codec for every supported extension.
- Pass no `entityTag`, `lastModified` or download filename. No `Content-Disposition` attachment. Do not promise conditional/304/If-Range behavior; characterize If-Range without validators, without turning observations into a public contract.
- Declare explicit C# OpenAPI metadata: 200 and 206 binary Stream (`string`, `binary`), explicit 416, plus the frozen error responses below. File results do not automatically supply the required metadata.
- Preserve C# endpoint metadata/DTO → `web/src/api/openapi.json` → generated TypeScript chain. Regenerate using existing tools, including generated mocks; never hand-edit generated artifacts or invent duplicate frontend DTOs.

Basis: recon-DEV-456 §§3, 6, 8, 10 and recon-DEV-456-2 §§2-9; Q2/Q3 explicit §2.3.4 rulings. Documentation provenance in the supplement includes Microsoft Learn [.NET 10 Results.Stream](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.http.results.stream?view=aspnetcore-10.0), [minimal API responses](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0), and [MIME provider](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.staticfiles.fileextensioncontenttypeprovider?view=aspnetcore-10.0). The supplement distinguishes documented facts from unresolved framework behavior; do not erase that distinction.

## Q4 — Containment and threat boundary

Patron approved both checks, in this order:

1. Authorize the stored movie folder before invoking `ResolvePrimaryVideo`.
2. Authorize the returned primary file before opening it.

Canonicalize the configured root and candidates to full paths; compare using platform path rules and separator boundaries. Reject outside-root and sibling-prefix escapes, symlink/reparse-point chains and non-regular files. `LibraryPath` validation and plain string prefixes are insufficient. Relative movie folders are normal; legacy rooted folders may work only if contained, never via a rooted-path bypass. Preserve the resolver's existing top-level eligibility, largest-file and tie-break rules.

Follow existing containment precedents locally in the stream path. Do not extract or rewrite thumbnail/cache/discovery subsystems. Open the authorized file once; serve that response from that handle.

The trusted local library mount is an explicit ruled boundary. A malicious same-machine writer racing authorization against open is outside this ticket; no atomic race-proof filesystem sandbox is promised. Missing/replaced content at open fails safely without serving unauthorized bytes. Linked folders/files fail closed even when legitimate; this cost is accepted.

Basis: recon-DEV-456 §§4-5, 10 and supplement §2; Q4 explicit §2.3.3/4/5/6 rulings.

## Q5 — Errors, ownership, and cancellation

| Condition before response starts | Required status |
| --- | --- |
| Malformed movie id | 400 |
| Missing movie, folder or primary file, including disappearance before open | 404 |
| Existing folder with no eligible primary video | 409 |
| Containment/link denial | 403 |
| Unexpected filesystem access denial or I/O fault | 500 |
| Actual existing ValidationException, where applicable | 422 |

Every pre-header error uses sanitized ProblemDetails without filesystem paths or exception text, except framework-owned 416, whose body is not forced into ProblemDetails. Do not manufacture 422 for the explicitly mapped conditions. Distinguish safety denial (403) from operational unreadability (500). Preserve current global mappings; stream-local shaping handles cases not already supplied.

Transfer stream ownership to the file result. Do not close it in endpoint `using` before result execution. Dispose any opened stream on pre-result failure; framework completion/cancellation owns it after transfer. Once headers/body start, a cancellation or read failure aborts/cleans up without a second JSON response or a success claim for truncated delivery. No side effects occur on error paths.

Basis: recon-DEV-456 §§4, 7, 9 and supplement §2; Q5 explicit §2.3.4 ruling.

## Q6 — Required live Compose nginx and browser acceptance

Extend the existing `web/e2e/run.mjs` harness through `http://localhost:3000`, the web nginx `/api/` proxy. Direct API calls cannot substitute. Use the existing sample-library import and source MP4 corpus.

Required on the same delivery SHA:

- Full response: byte-exact source comparison and exact length.
- Prefix, suffix and open-ended single ranges: 206, exact source slices, lengths, Content-Range and Accept-Ranges assertions.
- Valid beyond-EOF range: 416 and `bytes */length`.
- Disjoint sequential ranges and backwards seek: exact requested source slices.
- Malformed/unit/multiple Range characterization, without promised statuses.
- LocalPlay=false streaming success; assert no history/process/launcher calls through the integration test seams as well as live stream success.
- Required real-browser seek: play a browser-decodable sample MP4 through the stream URL in a harness context, seek, then verify playback advances. No product player UI is added, and no all-format codec guarantee is made.
- Record source identity/length, requested ranges, observed statuses/headers, byte comparison and browser seek result, pinned to the delivery SHA.

No Docker or unavailable browser codec means required acceptance could not run and closure remains blocked. Preserve an underlying script exit-2 SKIPPED as SKIPPED, never PASS. Unit/API tests cannot replace live proxy/browser evidence. No speculative nginx tuning: a concrete failed proxy assertion requires a further Patron ruling before config changes.

Basis: recon-DEV-456 §§6-8, 11 and supplement §9; Q6 explicit §2.3.6 ruling. Browser seek is mandatory, not optional smoke.

## Q7 — Approach and approved file responsibilities

Patron approved one endpoint plus one narrow port/implementation in existing layers:

| File/responsibility | Decision |
| --- | --- |
| New `src/LamuFlix.Api/Endpoints/StreamEndpoints.cs` | Orchestrate existing movie-details query, folder authorization, existing resolver, authorized open, MIME lookup, framework result and route-local error shaping |
| New `src/LamuFlix.Core/Ports/ILibraryFileReader.cs` | Narrow folder-authorization and authorized-read-open contract; no HTTP/framework result types |
| New `src/LamuFlix.Infrastructure/FileSystem/LibraryFileReader.cs` | Real path/link containment, regular-file validation and single read-open |
| New focused endpoint and file-reader tests in existing test projects | Exercise real HTTP results and real filesystem containment separately |
| `src/LamuFlix.Api/Endpoints/ApiEndpoints.cs` | Register the new endpoint alongside existing map methods |
| Existing Infrastructure DI composition seam | Register narrow port/implementation; exact file must first be identified by Wisp in Phase B |
| `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` | Operation/response contract inventory |
| `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` | Endpoint-name inventory |
| `web/e2e/run.mjs` and existing smoke harness | Required Q6 acceptance surface |
| `web/src/api/openapi.json`, `web/src/api/types.ts`, `web/src/mocks/handlers.ts` | Regeneration tools only |

No new Core handler, general streaming service, repository/mediator layer, project, top-level folder, dependency, schema or migration. No product UI, launcher/playback-handler changes, global exception remapping, selector rewrite, thumbnail/discovery extraction, nginx/Compose config rewrite, file deletion or unrelated rewrite.

The existing DI file is the sole unresolved factual identification, deliberately delegated by Patron to Phase B Wisp. Implementation must not start before Wisp names it; never guess or invent a registration location. This is a pre-implementation recon task, not an open product decision or Gate 1 checkbox.

Basis: recon-DEV-456 §§3-5, 7-8, 10 and supplement §§2, 6-7; Q7 explicit §2.3.1/2/3/5/6 rulings. The narrow port follows existing Core Ports/Infrastructure FileSystem structure and buys independent filesystem and HTTP-lifetime testability.

## Q8 — Test strategy, gates, and ordering

Quill must express this dependency order in plan/tasks:

1. Phase B Wisp checks drift/dependencies at the implementation base and identifies the exact DI file. Do not infer DEV-459 implementation from its spec-only presence at this grill base.
2. Real-disk file-reader tests and implementation: relative/contained legacy rooted paths, outside/sibling-prefix escapes, links/reparse chains, missing files and regular-file enforcement.
3. Endpoint tests and implementation: frozen statuses, bytes and headers; error sanitization; access parity; LocalPlay=false and zero history/process side effects; cancellation/disposal and framework-edge characterization.
4. C# response metadata, generated contract chain, and operation/route inventory tests.
5. Live Compose nginx and required browser-seek acceptance on delivery SHA.
6. Full normal build-workspace gates, then the pipeline's separate reviews and delivery steps.

Use existing xUnit, assertion and mocking conventions. Real filesystem authorization is not mocked; endpoint tests may double the narrow ports. EF query/serialization behavior, if needed, uses existing Testcontainers patterns, never driver mocks. Retain existing selector property tests; add containment boundary properties if useful. No property opt-out without explicit task-note justification.

Expected build-workspace gates: Roslyn, cyclomatic complexity, diff-scoped InspectCode, mutation on eligible changed production C#, property tests, vulnerable packages, format verification, `dotnet test`, and applicable web gates. Never lower thresholds, substitute build for an unrunnable gate, or waive surviving mutants. Preserve PASS/FAIL, `SKIPPED (scope-empty)`, harness `SKIP`, mutation `N/A`, and `Could not run` distinctions. Scope-empty and explicitly configured non-blocking exits remain non-blocking and never PASS; required live acceptance is separately mandatory. No runtime gates run in this Phase A spec workspace.

Basis: recon-DEV-456 §§5-8, 10, current harness rules, and Q8 ruling. This brief sets expectations, not test receipts.

## Q9 — Loop discipline and freeze

Closing bar: Critical and High findings block code-review/ship-review closure. Medium and Low each require explicit evidence-based disposition. No severity disposition waives failed gates or frozen acceptance.

Frozen scope: primary-file HTTP streaming on the ticket-named route; existing resolver reuse; folder/file containment and authorized open; frozen range, MIME and error contracts; generated contract chain; focused regression tests; required Compose nginx and browser seeking evidence in Q1-Q8; anything else is a follow-up issue, not a finding in this round.

Round cap: two remediation/review rounds. Remaining Critical/High findings after the cap block and report to Bernstein. Do not waive them or continue an unbounded loop. Size M requires Sentry, Ledger and Compass reports; a round closes only after its outcome is published on the PR.

Patron confirmed shared understanding and authorized this brief freeze. No taste decisions were made; [ASSUMPTIONS.md](ASSUMPTIONS.md) records none. Trusted mount is the explicit Q4 boundary, not an assumption. No ticket contradiction or constitution departure was identified; no owner checkbox arises from this grill. The user retains merge authority.

## Handoff to Quill

Draft spec, plan and tasks from this brief and the two recon notes, preserving the canonical terms and every frozen requirement. Full ruling exchanges are in CONCLUSIONS.md. Q10 supersedes the earlier no-ADR restriction: the new port requires an ADR and the scoped constitution/architecture enforcement updates below. No glossary rewrite is authorized. Carry the exact-DI-file Phase B prerequisite explicitly. Missing decisions return as `needs decision:`; missing facts return through Bernstein as `needs recon:`. Keel next performs read-only speckit-analyze and brief-to-plan/tasks comparison; plan challenge and Gate 1 follow separately.

## Q10 — Constitution-compliant new-port correction (2026-11-07)

Patron approved retaining Q7 narrow `ILibraryFileReader` and superseding the no-ADR constraint. This section takes precedence over earlier Q1-Q9 statements that describe the DI file as the sole remaining prerequisite or imply no ADR is required.

- Constitution Principle I (`.specify/memory/constitution.md:299-305`) requires an ADR for each new port, includes a closed named-port list, and requires architecture-test enforcement. The permitted enumeration-extension procedure (`:65-82`) and Governance (`:712-734`) apply without waiving any principle.
- Add a scoped constitution named-port enumeration extension with its required governance record. Preserve existing entries, project reference rules, and the ADR requirement. Record the required version/sync-impact/template review and migration or follow-up implications under Governance; do not reinterpret the extension as a waiver.
- Add the port ADR before port implementation, using only the number Bernstein assigns. No number is currently assigned; do not guess it or author an unnumbered substitute.
- Extend the existing architecture whitelist to include only the exact new port, preserving all existing entries. Exact whitelist facts are absent from recon-DEV-456 and recon-DEV-456-2: Wisp must supply them before the edit; nobody infers the pattern or edits it speculatively.
- These bounded ADR, constitution/governance, and architecture-enforcement surfaces supplement Q7 file scope. They are compliance work for the approved port, not a new layer or a change in ticket deliverables. No structural owner checkbox: no principle is waived and no ticket deliverable changes. Ordinary Gate 1 remains pending.
- Quill must propagate Q10 to spec, plan, tasks, checklist, and drafting receipt, removing the stale no-ADR prohibition and open decision language. ADR, enumeration/governance record, and evidenced architecture enforcement must be prerequisites to implementing/completing the port; Phase B drift recon must recheck the landed rules before code.

Full ask, ruling, cited basis, and accepted cost are recorded in CONCLUSIONS.md Q10. Plan challenge and plan freeze remain pending.

### Q10 prerequisite evidence update

Bernstein assigned ADR number 0037 on resume of Phase 2 step 3.3. Keel authored [0037-library-file-reader-port.md](../../docs/adr/0037-library-file-reader-port.md). This supersedes the earlier unassigned-number status, without changing Q10.

Wisp `recon-DEV-456-3` section 3 supplies the exact enforcement edit: append only `|ILibraryFileReader` immediately before `)$` in `PortWhitelistPattern` at `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs:30-31`, preserving all 16 existing entries and their order, selector, count floor, and method body. Deliver port and whitelist insertion in one buildable slice; reconfirm the snapshot at Phase B pickup. The scoped constitution enumeration/governance prerequisite remains required. Existing omissions of other ports from constitution prose are reported in recon, not authorization to broaden this ticket. No missing ADR-number or whitelist fact remains at this snapshot; exact DI-file identification is still Phase B Wisp work.

Patron approved the bounded disposition of recon section 3.4: preserve both existing surfaces apart from the single `ILibraryFileReader` addition; the pre-existing constitution omissions of `IEnrichmentOutbox` and `IRecommendationProfileSource` are out-of-scope follow-up via Bernstein to Rigger. This follows Q9 frozen scope and Q10, relaxes nothing for the new port, and grants no blanket constitution-compliance claim. Full exchange follows in CONCLUSIONS.md.

## Q11 — Plan challenge decisions (2026-11-07)

Patron ruled all seven choices below after all three plan challenge reports (17 findings). This append supersedes earlier contradictory sequencing/status wording only; Q1-Q10 scope, acceptance, cap and authority remain binding.

1. Core contract: `void AuthorizeFolder(LibraryPath folder)` and `Stream OpenRead(LibraryPath file)`. Add `src/LamuFlix.Core/Ports/LibraryFileAccessDeniedException.cs` as the explicit policy/safety-denial type (including non-regular paths); route-local 403 catches only that type. Missing/disappeared folder/file uses existing NotFoundException ->404. OS UnauthorizedAccessException and genuine permission/I/O/read failure remain sanitized 500, never blanket-translated into policy denial. No HTTP/framework types in Core; Stream is BCL.
2. Same canonicalization/link-denial rules for both operations; reject link/reparse components throughout the candidate chain, use authorized canonical full path only, authorize selected file then open once. Async FileStream, FileShare.Read, Asynchronous|SequentialScan. Add focused concurrent-reader proof; no concurrency subsystem.
3. Add route-local `X-Content-Type-Options: nosniff` on successful binary 200/206 only; document and test; no global configuration/header change.
4. Constitution VI structured error logging carries requested movie id without filesystem paths, including local 403 and post-header failure. Add explicit task and tests; do not claim existing pipeline compliance without evidence. No new telemetry names.
5. Build PR owns scoped constitution extension. Keel pickup verification precedes explicit Keel-owned constitution edit: ILibraryFileReader only, appropriate MINOR version (1.11.0 only if pickup remains 1.10.0), Sync Impact Report, Last Amended, template review and migration/follow-up record per Governance. No prior omitted-port backfill (DEV-467). ADR 0037 stays Proposed until extension/governance exists; any later acceptance needs applicable authority evidence, never auto-accept.
6. Buildable type + atomic whitelist slice before real-disk tests compile. Behavior-red tests only after referenced surfaces exist; no uncompiled-red commits. Remove provisional parallel markers and T002/T001c overlap. Link-proof setup fails loudly if no actual link fixture can be created; unavailable proof is never PASS. Endpoint host tests can fail behaviorally on missing route without referencing a nonexistent endpoint class.
7. 422 remains inherited ValidationException mapping, not a manufactured stream-path scenario. Tests assert frozen mapped statuses (and thus exclude 422 for those cases).

Basis: Patron Q11 recorded verbatim in CONCLUSIONS.md; Q3-Q10; recon-DEV-456 sections 5,7,9,10; recon-DEV-456-3 section 3; constitution I, VI and Governance. These are bounded refinements/compliance, no ticket contradiction or principle departure and no structural owner checkbox. This decision record precedes Quill repairs. Plan freeze pending independent repair verification.

## Plan challenge freeze receipt — 2026-11-07

**PLAN FROZEN — ready for ordinary owner Gate 1 review.** This append supersedes earlier pending-plan-challenge/freeze statuses only. Authoritative plan content is the Q1-Q11 brief plus the independently verified Quill repair-round-1 artifacts; drafting banners describe authorship history, not an outstanding Keel decision.

Keel adjudicated all 17 findings from Sentry (4), Ledger (8), Compass (5): 0 Critical/High, 7 Medium, 10 Low. Fifteen document corrections are independently verified, Sentry F4 is an explicitly accepted personal-scale residual, Ledger L4 is resolved by recon-DEV-456-3 section 2. Patron Q11 settled all required decisions with cited basis. One bounded plan-challenge repair round used; no second round required. ADR 0037 status/format/contract repaired and independently read back.

Read-only cross-artifact reanalysis and brief-to-plan/tasks comparison: 0 unresolved planning findings, 13 FR + 5 SC = 18/18 requirements with task coverage; 28 unique unchecked tasks; no unmapped feature tasks. No provisional parallel markers on T002 or endpoint test triad/new logging tests. Ordering is T000 Gate 1 receipt -> T001 Wisp facts -> T001b Keel analyze -> T001c Keel verify-only -> T001d Keel constitution/governance -> T003 atomic buildable type/whitelist -> T002 behavioral tests -> T004 reader -> T005 DI -> endpoint -> contracts -> live Compose/browser -> gates/review/delivery. Existing brief Q8 wording is refined by Q11, not permission for noncompiling-red commits.

Receipt: Maestri note adjudication-DEV-456-Keel in LamuFlix notes, with individual dispositions and file:line verification. Worktree F:/Dev/LamuFlix.worktrees/feature-456-spec; branch feature/456-spec; current Git base/HEAD 659a2e9b739c092dc8565ee31ed6bc5bcc4b68e3. The reviewed documents are working-tree artifacts, not committed content attributed to that base SHA.

Ordinary Gate 1 remains pending; no structural owner checkbox arises. No implementation authorization, constitution amendment completion, Principle I/VI implementation PASS, runtime/build gate result, live acceptance, PR publication, or merge clearance is claimed. ADR 0037 remains Proposed; T001d in the Build PR still requires the scoped extension/governance, and Phase B Wisp must supply exact DI-file identification and dependency/drift facts before Keel pickup analysis. Required nginx/browser acceptance remains mandatory; Could not run/SKIPPED never PASS. Bernstein owns next routing/publication; the user owns merge.
