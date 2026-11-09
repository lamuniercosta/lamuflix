# DEV-458 — Patron Conclusions

## Q1 — Proxy / request boundary for POST /api/movies/{id}/play (§2.3(4), §2.3(5))

Verdict: Adopt Keel's recommended request boundary, plus a mandatory deployment exclusion for shipped proxies; no new trust mechanism (no token, mTLS, or named-pipe channel).

- Request boundary (§2.3(4): no new route, DTO, or status): launch is allowed only when the raw transport peer `HttpContext.Connection.RemoteIpAddress` is loopback (`IPAddress.IsLoopback`, covering 127.0.0.0/8 and ::1) AND none of `Forwarded`, `X-Forwarded-For`, `X-Forwarded-Host`, `X-Forwarded-Proto`, `X-Real-IP` is present (header presence, any value). Otherwise reject before catalog lookup, disk/file probing, process start, and history recording, reusing the already-declared 403 ProblemDetails (`PlaybackEndpoints.cs:21-26` ProducesProblem 403; `GlobalExceptionHandler` FeatureDisabledException → 403). Do not enable `UseForwardedHeaders` or otherwise rewrite the peer address for this route. Basis: ticket scope at recon-DEV-458:9 (verify a proxy cannot expose the launch action remotely); `PRODUCT.md` §3 (ticket Scope & Technical Design is authoritative) and §5 care items 4-5.
- Indistinguishable-proxy clause: a loopback proxy that strips all provenance headers is indistinguishable from a local client at the HTTP layer, so the request check alone cannot support an absolute any-proxy claim. Ruled: deployment must exclude the play route — any shipped proxy in front of the API must not forward `POST /api/movies/{id}/play` (deny it at the proxy, e.g. nginx in `docker/nginx.conf`, which today proxies all `/api/` at lines 7-14 and sets X-Real-IP/X-Forwarded-For/X-Forwarded-Proto), or `Features:LocalPlay` must be false in that deployment (already the case for the docker profile, recon-DEV-458:33). A deployment that loopback-proxies the play route with provenance stripped is unsupported for LocalPlay. No different trust boundary is required; inventing one would be a new API/config shape under §2.3(4)/(5) beyond the ticket.
- Verification scope (not a silent weakening): the spec must verify (a) the peer × header matrix at the endpoint, including rejection before catalog/disk/process/history, (b) the actual shipped nginx path (`docker/nginx.conf`) cannot elicit a launch, and (c) the actual Aspire path (`src/LamuFlix.AppHost/Program.cs:16-18,46`) — direct API endpoint and web proxy behaviour — cannot elicit a launch from a non-local or proxied request. The ticket claim is thereby scoped to shipped nginx/Aspire topologies plus the deployment exclusion above. An absolute guarantee against an arbitrary user-configured stripping loopback proxy would be `blocked: structural` (would require a non-HTTP trust boundary contradicting the ticket's HTTP-endpoint design); it is not silently assumed.

No ticket-text or constitution departure in this ruling: 403 is already declared for the route, `Features:LocalPlay`/`Process.Start` are ticket-named (recon-DEV-458:56), and the constitution retains the LocalPlay/403 rules (`.specify/memory/constitution.md:447-448,668-669`).

### Q1 full initiating question (Keel)

DEV-458 grill Q1/12. Evidence: recon-DEV-458:9 gives ticket scope: file exists before launch, 422 for unmapped format, LocalPlay enabled refuses non-loopback binds, verify proxy cannot expose launch remotely. Lines 31-36 show existing POST /api/movies/{id}/play with 403 and nginx/Aspire proxies. PRODUCT sections 3 and 5 apply. What precise request boundary satisfies the proxy requirement? I recommend launch requires both loopback transport peer and absence of Forwarded, X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto and X-Real-IP; reject before catalog/disk/process/history with existing 403 ProblemDetails, and verify the actual nginx/Aspire path. Cost: proxy-mediated local launching is deliberately unavailable. A loopback proxy stripping all provenance cannot be distinguished from a local client: do you rule deployment must exclude the play route, or require a different trust boundary? Give a deliberate cited section 2.3(4/5) ruling; flag any ticket/constitution departure rather than silently weakening the claim.

Patron answer: the complete ruling above was supplied in commit dd664bf3 and confirmed by Patron directly. The full supplementary reply follows.

[from Patron] DEV-458 grill Q1/12 ruling — section 2.3(4)/(5), deliberate, not assumed.

Verdict: Adopt your recommended request boundary, plus mandatory deployment exclusion for shipped proxies; no new trust mechanism.

1. Request boundary: launch only if raw transport peer HttpContext.Connection.RemoteIpAddress is loopback (IPAddress.IsLoopback, 127.0.0.0/8 and ::1) AND none of Forwarded, X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto, X-Real-IP is present (presence, any value). Otherwise reject before catalog lookup, disk probing, process start and history, reusing already-declared 403 ProblemDetails (PlaybackEndpoints.cs:21-26; GlobalExceptionHandler FeatureDisabledException to 403). Do not enable UseForwardedHeaders for this route. No new route/DTO/status, so 2.3(4) satisfied by cited ruling; LocalPlay/Process.Start are ticket-named per recon-DEV-458:9,56 and PRODUCT.md sections 3 and 5.

2. Stripping loopback proxy: indistinguishable at HTTP layer, so ruled deployment must exclude the play route — shipped proxies must not forward POST /api/movies/{id}/play (deny at nginx, docker/nginx.conf:7-14) or LocalPlay must be false there (already docker profile, recon-DEV-458:33). Such a stripping-proxy deployment is unsupported for LocalPlay. No different trust boundary required; a token/mTLS/named-pipe would be a new API/config shape beyond the ticket.

3. Verification, no silent weakening: spec must verify (a) peer-by-header matrix with rejection before catalog/disk/process/history, (b) actual shipped nginx path cannot elicit launch, (c) actual Aspire path (AppHost Program.cs:16-18,46) cannot elicit launch. Claim is scoped to shipped nginx/Aspire topologies plus that exclusion. An absolute any-proxy guarantee would be blocked: structural, as it needs a non-HTTP trust boundary contradicting the ticket HTTP design — flagged here, not assumed. No ticket or constitution departure in this ruling (constitution LocalPlay/403 at constitution.md:447-448,668-669).

Recorded and committed (dd664bf3) at specs/DEV-458/CONCLUSIONS.md in worktree F:\Dev\LamuFlix.worktrees\feature-458-spec — read section Q1 there as the authoritative text. Proceed to Q2.

---

## Q2 — Startup invariant

### Keel question
What exact startup invariant governs LocalPlay? Recommend: with Features:LocalPlay=true, every effective API listener must be explicitly proven loopback before launch requests can be served; accept localhost/IPv4 loopback/IPv6 loopback, reject wildcards, non-loopback literals or hostnames, mixed loopback/non-loopback sets and unknown/unprovable listener forms. Validate actual effective listeners across URL config, Kestrel and Aspire overrides, not one config string; LocalPlay=false imposes no new bind restriction. No new config key, auth mechanism, dependency, port or project. Cost: enabled deployments using ambiguous hostname/socket bindings fail closed; test-host fixtures must explicitly supply a loopback peer/listener. Please give cited 2.3(5) ruling including fail-start timing and whether Unix sockets/named pipes remain unsupported. This is a behavior decision: Quill must schedule Wisp/framework-documentation recon for the precise hosting hook before plan freeze.

### Patron answer
[from Patron] DEV-458 grill Q2/12 ruling — section 2.3(5) (Features:LocalPlay / Process.Start), deliberate, not assumed.

Verdict: Adopt Keel recommendation as stated, with the fail-start timing and transport scope below. No new config key, auth mechanism, dependency, port, or project; no ticket or constitution departure.

1. Invariant: with Features:LocalPlay=true, every effective API listener must be proven loopback before any request is served. Proven loopback is only: the literal localhost, an IPv4 loopback literal (127.0.0.0/8), or an IPv6 loopback literal (::1). Reject wildcards (0.0.0.0, ::, *, +), non-loopback literals, any other hostname (even one currently resolving loopback), any mixed loopback/non-loopback set, and any unknown or unprovable listener form. Validation must run against the actual effective listeners as resolved after URL configuration, Kestrel configuration, and Aspire/environment overrides are applied — not against one config string such as ASPNETCORE_URLS alone. Basis: ticket scope at recon-DEV-458:9 (refuse startup with LocalPlay enabled on non-loopback binds); recon-DEV-458:26-27 (no bind or loopback validator exists today; binds currently fall to framework/Aspire defaults); PRODUCT.md section 3 and section 5 care item 5; constitution LocalPlay registration rule at constitution.md:447-448.

2. Fail-start timing and LocalPlay=false: this is a startup failure, not a per-request check and not a warning — when LocalPlay=true and the invariant is unmet or unprovable, startup must fail and the process must exit having served no requests (the per-request EnsureEnabled/FeatureDisabledException path is not a substitute). When Features:LocalPlay=false, this ruling imposes no new bind restriction. No test-only bypass key or flag: test-host fixtures must explicitly present a provable loopback listener/peer, exactly the cost Keel stated.

3. Transports and recon gate: Unix sockets and named pipes remain unsupported as LocalPlay listeners — under LocalPlay=true such a listener is an unprovable/unsupported form and fails startup under (2); this ticket adds no Unix-socket or named-pipe support. The precise hosting hook that observes effective listeners late enough yet still before serving is not decided here: Quill must schedule Wisp/framework-documentation recon for that hook before plan freeze, and the plan may not freeze until that recon names the hook and proves the fail-before-serving property. If that recon shows no hook can prove loopback for a listener form the ticket requires, return to Patron rather than silently treating it as proven.

Keel to record this verbatim in CONCLUSIONS.md (Keel owns the append-only record from Q2 on).

---

## Q3 — Launch-time failures

### Keel question

[from Keel] DEV-458 Q3/12: settle launch-time failure semantics. Recommend the existing Infrastructure launcher use the existing filesystem abstraction to check the selected file immediately before IProcessStarter.Start; missing selected file maps to existing 404 ProblemDetails, unmapped format uses existing ValidationException/422 with safe format-level text, both zero process starts and zero history writes. Preserve DEV-438 selection, case/dot-insensitive player mapping, one ArgumentList file argument and existing process-start behavior; do not introduce new shell parsing. After selection: check existence first, resolve player next, start last. A later deletion is an unavoidable check/start race: genuine process-start failures remain generic 500, no retry and no history. Cost: existence check reduces stale launches but is not an atomic filesystem/process transaction. Please give a cited 2.3(4/5) ruling on this complete launcher outcome policy; call out any missing factual evidence rather than assuming a new dependency or data-access seam. Reply with full ruling file path; Keel records.

### Patron answer

[from Patron] DEV-458 grill Q3/12 ruling — section 2.3(4)/(5), deliberate, not assumed.

Verdict: Adopt Keel launcher outcome policy exactly as recommended. Recorded evidence removes the assumed-seam risk; no needs-recon item remains on this question.

1. Ordering and outcomes inside Launch: after DEV-438 selection has produced the primary file (selection itself unchanged), the existing Infrastructure launcher checks existence of the selected file first, resolves the player second, and calls IProcessStarter.Start last. Missing selected file throws the existing Core NotFoundException, mapped to the existing 404 ProblemDetails. Unmapped format throws the existing Core ValidationException with safe format-level text only (format extension, never file path, executable path, or configured player paths), mapped to the existing 422 ProblemDetails. Both failures give zero process starts and, because PlayMovieCommandHandler records history only after Launch returns, zero history writes. Precedence is settled by the stated order: when both apply, existence wins and the result is 404, not 422. Basis: ticket scope at recon-DEV-458:9 (file exists before launch; 422 for unmapped format); PlaybackEndpoints already declares 403/404/422; GlobalExceptionHandler maps NotFoundException to 404 and ValidationException to 422; ProcessMediaPlayerLauncher.cs:24-31 currently throws InvalidOperationException (today a 500) before Start. No new route, DTO, or status, so section 2.3(4) is satisfied by this ruling; LocalPlay/Process.Start is ticket-named, section 2.3(5).

2. Filesystem seam — verified fact, not assumption: System.IO.Abstractions IFileSystem already exists in this codebase and project. DirectoryMediaLibraryScanner and FileSystemLibraryFolderDiscovery already consume IFileSystem; LamuFlix.Infrastructure already references the System.IO.Abstractions package; IFileSystem is already registered (TryAddSingleton IFileSystem to FileSystem in Api HandlerRegistration.cs:32, and in RabbitMqServiceCollectionExtensions.cs:63). Therefore injecting IFileSystem into ProcessMediaPlayerLauncher introduces no new dependency, package, or data-access seam under section 2.3(1)/(2); the only wiring change is resolving that existing service in the existing PlaybackServiceCollectionExtensions factory. Use the abstraction file-existence check (for example IFileSystem.File.Exists on the selected path), not a direct System.IO.File call, matching the existing scanner code. If implementation finds that registration order prevents resolving IFileSystem from the playback factory, return to Patron instead of adding a parallel seam.

3. Preserved behaviour and race boundary: DEV-438 primary-video selection, case/dot-insensitive player mapping, exactly one ArgumentList file argument, UseShellExecute process-start behaviour, and no new shell parsing are all preserved. The existence check is not atomic with process start: a file deleted or made unavailable after the check, a missing player executable, or any other genuine process-start failure remains the existing generic 500 ProblemDetails, with no retry and no history write. Do not special-case, translate, or log-detail those start failures in this ticket. Cost accepted as Keel stated: fewer stale launches, not a filesystem/process transaction.

No ticket-text or constitution departure in this ruling. Keel to record verbatim in CONCLUSIONS.md.

---

## Q4 — Footprint and verification

### Keel question

[from Keel] DEV-458 Q4/12: approve the bounded implementation footprint under 2.3(1/2/3/6). Recommend changes only in existing Infrastructure/Playback launcher and its tests, API playback endpoint/request guard and startup composition plus focused helpers in existing API folders, API integration/host/options tests, and shipped nginx/Aspire configuration only where needed to enforce Q1-Q2; preserve the DEV-438 handler/selector/history order, existing Core port and DTOs. No new dependencies, projects, top-level folders, abstraction layers, database/migrations or secrets. Permit existing GlobalExceptionHandler only if existing exception mappings cannot express approved 403/404/422 without leaking paths; prefer reusing them unchanged. Generated OpenAPI/TS changes only through the established generation chain if gates require a contract refresh. No hand edits. Test requirement includes null/non-loopback/mapped-loopback transport cases and case-insensitive presence of each provenance header (empty and multiple values included), existing disabled behavior, no side effects on all refusals, actual startup listener precedence and nginx/Aspire negative launch smoke; explicitly normalize IPv4-mapped IPv6 loopback before loopback checks. These config/test edits are necessary to ticket behavior, not proxy redesign. Cost: shipped proxy smoke must have concrete runtime evidence, not inferred safety. Approve this footprint and test policy with cited basis, identify any new touch requiring separate ruling, and confirm no UI/taste changes or glossary additions are needed. Full answer file, please.

### Patron answer

[from Patron] DEV-458 grill Q4/12 ruling — section 2.3(1/2/3/6) footprint and test policy, deliberate, not assumed.

Verdict: Approve the bounded footprint and test policy exactly as Keel recommends, with the two tightenings in points 2 and 3. No new touch beyond this list is pre-approved.

1. Approved footprint (2.3(1/2/3/6)): production changes only in (a) the existing Infrastructure/Playback launcher and its DI factory, (b) the API playback endpoint/request guard, startup composition in Api/Program.cs plus focused helpers inside existing API folders, and ServiceDefaults options/startup composition only insofar as the Q2 hosting-hook recon lands there (recon-DEV-458:52-53 already names ServiceDefaults/Extensions.cs and Api/Program.cs as likely landing zones), (c) shipped nginx configuration (docker/nginx.conf) and Aspire AppHost configuration only where needed to enforce the Q1 proxy exclusion and Q2 listener invariant, and (d) tests: launcher unit tests, API integration/host/options tests covering the items in point 4. Core port IMediaPlayerLauncher, DTOs, DEV-438 handler/selector/history order (PlayMovieCommandHandler: catalog, EnsureEnabled, ResolvePrimaryVideo, Launch, history) are preserved unchanged. No new dependencies or packages (IFileSystem/System.IO.Abstractions already present, per Q3 ruling), no new projects, top-level folders, abstraction layers, database schema or migrations, and no secrets. Basis: ticket scope at recon-DEV-458:9; recon-DEV-458:52-57 (no dependency, project, or schema touch implied; LocalPlay/Process.Start ticket-named under 2.3(5)); PRODUCT.md section 3 and section 5 care items 1, 2, 3, 6.

2. GlobalExceptionHandler tightening: the existing mappings already express every approved outcome (FeatureDisabledException to 403, NotFoundException to 404, ValidationException to 422, generic 500 otherwise), as settled in Q1 and Q3, so this ruling does NOT pre-approve editing GlobalExceptionHandler. Any implementation claim that a handler edit is unavoidable returns to Patron for a separate 2.3(6) ruling before the edit, with the leaking-path or inexpressible-case evidence attached. Same return-to-Patron rule for any edit not listed in point 1, including Core, the DEV-438 selector/handler, docker-compose files, web/, or any tracked file outside the listed zones.

3. Generated contracts: OpenAPI (web/src/api/openapi.json) and generated TypeScript (web/src/api/types.ts) may change only through the established C# DTO to OpenAPI to TypeScript generation chain if a gate requires a contract refresh; no hand edits to generated files. Note the route already declares 204/400/403/404/409/422, so a contract diff is not expected; any generated diff must be explained in the PR, not silently accepted.

4. Test policy approved in full: null, non-loopback, and mapped-loopback transport-peer cases; case-insensitive presence of each provenance header (Forwarded, X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto, X-Real-IP) including empty and multiple values; existing LocalPlay-disabled behaviour; zero side effects (no catalog-dependent launch, disk probe, process start, or history write) on every refusal; actual startup listener precedence across URL/Kestrel/Aspire configuration; and nginx/Aspire negative launch smoke. IPv4-mapped IPv6 loopback (for example ::ffff:127.0.0.1) must be explicitly normalised (map to IPv4) before every loopback check, for both the Q1 transport peer and the Q2 effective listeners, and covered by tests. Cost accepted and binding: the shipped proxy smoke needs concrete runtime evidence (an actually running shipped nginx/Aspire path returning refusal with no launch), not inferred safety from reading configuration.

5. No UI, taste, or glossary changes: this ticket changes no user-visible copy, labels, ordering, or UI behaviour, so no [assumed] taste entries and no ASSUMPTIONS.md or glossary/CONTEXT.md additions are needed or approved; if implementation surfaces a user-visible string beyond the existing ProblemDetails titles, that returns to Patron.

No ticket-text or constitution departure in this ruling. Keel to record verbatim in CONCLUSIONS.md.

---

## Q5 — Closing terms and shared understanding

### Keel question

[from Keel] DEV-458 Q5/12: settle loop discipline and confirm shared understanding so I can close the grill. Recommend frozen scope consists solely of Q1 request/proxy boundary, Q2 proven-loopback startup refusal, Q3 existence/422 launcher policy and Q4 necessary configuration/tests/docs; anything else is a follow-up issue, not a finding in this round. Closing bar for code-review/ship-review: Critical/High findings block, as does any in-scope failure of ticket acceptance, security invariant, constitution, API contract or required gate regardless of severity; discretionary abstraction/style changes outside this scope are follow-up. On M, all three Sentry/Ledger/Compass axes are mandatory. Round cap two for plan challenge and two for implementation review; unresolved blocking findings at cap stop for explicit disposition, never automatic pass or a silent third round. Phase A delivers proposed ADR0036 and brief for Quill, no implementation or Gate 1 claim. Before plan freeze Wisp must establish the precise effective-listener hosting hook with Microsoft Learn evidence and fail-before-serving proof; Phase B must run actual nginx/Aspire negative launch smoke plus appropriate analyzer/full tests/property/security/format/mutation/contract gates, preserving SKIPPED/N/A/Could not run labels and no threshold reductions. Cost: runtime and hosting evidence are explicit completion requirements, not assumed PASS. Please confirm these loop terms and that Q1-Q4 plus these terms are sufficient shared understanding, no owner escalation needed, and no taste decision was made. If not, identify the next decision. Reply with full ruling path.

### Patron answer

[from Patron] DEV-458 grill Q5/12 ruling — loop discipline and shared understanding, grill close.

Verdict: Confirmed in full. Q1-Q4 plus these loop terms are sufficient shared understanding to close the grill. No owner escalation, no taste decision, and no next Patron decision is outstanding.

1. Frozen scope: DEV-458 consists solely of the Q1 request/proxy boundary, the Q2 proven-loopback startup refusal, the Q3 existence-first/422 launcher policy, and the Q4 necessary configuration, tests, and docs. Anything else discovered is a follow-up issue, not a finding or scope addition in this round. Basis: ticket scope at recon-DEV-458:9; PRODUCT.md section 3 (ticket Scope and Technical Design authoritative); Q1-Q4 rulings recorded in specs/DEV-458/CONCLUSIONS.md.

2. Closing bar and rounds: Critical/High findings block, and any in-scope failure of ticket acceptance, a security invariant (Q1/Q2), the constitution, the API contract, or a required gate blocks regardless of severity label. Discretionary abstraction or style changes outside frozen scope are follow-ups, never blockers in this round. On this Size M ticket all three review axes (Sentry, Ledger, Compass) are mandatory for code-review/ship-review. Caps are two rounds for plan challenge and two rounds for implementation review; unresolved blocking findings at cap stop for explicit disposition — never an automatic pass and never a silent third round.

3. Phase and evidence gates: Phase A delivers the proposed ADR 0036 (reserved at recon-DEV-458:61-62) and the brief for Quill, with no implementation and no Gate 1 claim. Before plan freeze, Wisp must establish the precise effective-listener hosting hook with Microsoft Learn evidence (Microsoft Learn is authoritative for ASP.NET Core hosting) including fail-before-serving proof; the plan may not freeze on an unproven hook. Phase B must run the actual nginx/Aspire negative launch smoke plus the appropriate analyzer, full-test, property, security, format, mutation, and contract gates, preserving SKIPPED, N/A, and Could not run labels exactly, with no threshold reductions. A gate that could not run is not a pass; runtime and hosting evidence are completion requirements, not assumed PASS.

4. Escalation and taste check: no owner escalation is needed. The Q1 scoping to shipped nginx/Aspire topologies plus the deployment exclusion decides the proxy claim without changing ticket text, and no constitution departure is taken; the only blocked: structural case would be an absolute any-proxy guarantee requiring a non-HTTP trust boundary, which this scope does not claim. No taste decision was made in Q1-Q5: no user-visible copy, labels, ordering, or UI behaviour changed, and no ASSUMPTIONS.md entries arise (Q4 point 5).

Grill closed on Patron side. Keel to record verbatim in CONCLUSIONS.md and report the grill outcome to Bernstein.

---

## Q6 — Failure side-effect wording

### Keel question

[from Keel] DEV-458 Q6/12 narrow consistency clarification before writing brief: Q4 point 4 says no disk probe on every refusal, but Q3 404/422 happen after catalog/selection and the required existence probe. Recommend interpret no catalog/disk/process/history as Q1 request-boundary refusals only; LocalPlay-disabled keeps existing catalog then EnsureEnabled before disk; Q3 launch validation and start failures allow earlier catalog/disk work but never start on 404/422 and never append history on any failure. No change to Q1-Q5 intent. Confirm this exact distinction with cited basis; also ASSUMPTIONS.md will exist only as an empty-of-decisions audit ledger saying no taste decisions, as Conductor explicitly requests the file. Please supply full answer file; then the grill is closed.

### Patron answer

[from Patron] DEV-458 grill Q6/12 consistency clarification — confirmed, no change to Q1-Q5 intent.

Verdict: Keel's exact distinction is adopted. Q4 point 4 is interpreted per refusal class below; where its shorthand (no catalog/disk/process/history) conflicts with this clarification, this clarification governs.

1. Q1 request-boundary refusals (non-loopback peer or any provenance header present): rejected at the endpoint/request guard before the command handler runs, so no catalog lookup, no disk probe, no process start, and no history write. This is the only class covered by the Q4 no-catalog/no-disk wording. Basis: Q1 ruling; PlayMovieCommandHandler.cs ordering (catalog at handler entry) at recon-DEV-458:17.

2. LocalPlay-disabled refusal: unchanged existing order — catalog lookup first, then EnsureEnabled throws FeatureDisabledException (403) before scanner/disk, process start, or history. Basis: PlayMovieCommandHandler.cs:19-24 order (catalog, EnsureEnabled, then ResolvePrimaryVideo) at recon-DEV-458:17; existing disabled test behaviour at recon-DEV-458:41 (403, no disk probe). No new early-exit for the disabled case is introduced by Q1-Q3.

3. Q3 launch-validation and start failures: catalog lookup, DEV-438 selection, and disk work up to and including the launcher existence probe have already legitimately occurred; the invariants are narrower — 404 (missing file) and 422 (unmapped format) never reach IProcessStarter.Start, and no failure of any class (404, 422, or generic 500 from a genuine process-start failure) ever appends playback history, because history is recorded only after Launch returns. Basis: PlayMovieCommandHandler.cs:24-29 (ResolvePrimaryVideo, Launch, then RecordAsync) at recon-DEV-458:17; Q3 ruling ordering existence, player resolution, Start.

ASSUMPTIONS.md: approved as Conductor explicitly requests the file — it exists solely as an empty-of-decisions audit ledger stating that no taste decisions were made, with zero [assumed] entries. This clarifies Q4 point 5 and Q5 point 4, which barred taste entries, not the file's existence; creating the ledger is not a taste decision and needs no [assumed] tag.

No ticket-text or constitution departure. Keel to record verbatim in CONCLUSIONS.md; the grill is then closed.

---

## Q7 — Phase 2 step 3.3 hosting mechanism (Q2 return condition)

### Keel initiating ask

[from Keel] DEV-458 Phase 2 step 3.3 hosting decision required under Q2 return condition and 2.3(5). Read recon-DEV-458-2 section 7, especially lines 42-47, and CONCLUSIONS Q2/Q4. Recommend dedicated API IHostedService.StartAsync validation registered in Api/Program.cs, synchronous completion or throw before server startup; no ServiceDefaults change. Read merged config plus IServerAddressesFeature, resolve effective set with Kestrel/URL/ports/preferHostingUrls precedence, normalize mapped IPv4, reject unprovable forms. Recon G2 says code Listen endpoints cannot be publicly enumerated pre-start and none exist at pinned baseline. Rule whether no code-defined Listen/Unix/named-pipe/handle/systemd endpoints may be introduced in this delivery as an explicit guard/review invariant, with any required such endpoint returning for recon/ruling rather than treating it as proven. G1 config-feature parity and G3 Aspire injection require actual-startup/shipped-path tests already mandated; no config-parser-only proof. Do not silently narrow ticket-required bindings. Please supply full cited ruling in an answer file (you own that file, do not edit Keel brief or CONCLUSIONS); ask back Keel when done. If proof still insufficient, identify exact recon question. No implementation or freeze claimed.

### Keel supplement (same decision)

[from Keel] DEV-458 same hosting decision supplement, not a new grill topic: constitution VII forbids IConfiguration magic-string reads outside options binding. Please require framework configuration inputs to be bound/validated through existing approved options-binding composition or documented framework constants/APIs that satisfy that rule; no ad hoc raw key reads and no new public configuration key. If that cannot satisfy VII, do not waive it silently. Also pin actual pre-server ordering including hosted-service concurrency/registration in required startup tests; nominal StartAsync naming alone is not proof. Include these constraints in the same answer file. Ask back Keel with file path.

### Patron answer (verbatim including supplement)
# [from Patron] DEV-458 Phase 2 step 3.3 — hosting decision ruling (Q2 return condition, §2.3(5))

Verdict: Adopt Keel's recommendation in full. A dedicated API `IHostedService.StartAsync` validation, registered in `Api/Program.cs`, synchronously completing or throwing before server startup; no `ServiceDefaults` change. The no-code-`Listen` guard below is ruled as an explicit review invariant for this delivery. No implementation and no plan freeze is claimed by this ruling.

## 1. Hook, placement, and fail-before-serving timing (§2.3(5); §2.3(1)/(2)/(6) footprint)

- The validator is a dedicated hosted service registered only in `src/LamuFlix.Api/Program.cs`, with any helper confined to an existing API folder. This is inside the Q4-approved footprint: startup composition in `Api/Program.cs` plus focused helpers in existing API folders (CONCLUSIONS Q4 point 1(b)). `ServiceDefaults`/`AddServiceDefaults` is not touched: Q4 approved it only insofar as the Q2 recon landed there, the recon's proof now lands the seam in the API host, and a shared-defaults change would widen the blast radius to other hosts for an API-listener/LocalPlay invariant. No new dependency, project, top-level folder, or layer (§2.3(1)/(2)); no new config key, route, DTO, or status.
- `StartAsync` must complete synchronously on success or throw on any unproven listener. No fire-and-forget, no deferral to `IHostApplicationLifetime.ApplicationStarted`, and `IStartupValidator`/`ValidateOnStart` is not the primary hook. Basis: hosted services start and run sequentially to completion before the middleware pipeline is built and before Kestrel looks up URLs, allocates sockets, and binds (recon-DEV-458-2 lines 18-19); a throw there exits the host with zero sockets bound and zero requests servable (line 20), whereas `ApplicationStarted` fires after bind (line 23) and the options-validator ordering is not pinned for net10.0 (line 25). This satisfies CONCLUSIONS Q2 point 2: startup fails, the process exits having served no requests, and the per-request `EnsureEnabled` path is not a substitute.

## 2. Effective-set resolution — read both surfaces, reject the unprovable

- The hook reads BOTH the framework-bound configuration inputs (the sources Kestrel itself looks up, bound as section 6 below requires) and `IServerAddressesFeature`, including its `PreferHostingUrls` flag (recon lines 30, 37, 45). Reading one string or one surface alone does not satisfy CONCLUSIONS Q2 point 1, which requires validation of the actual effective listeners after URL, Kestrel, and Aspire/environment overrides.
- Precedence, per recon lines 34-38 and 42: if the `Kestrel` configuration section defines endpoints and `preferHostingUrls` is false, the Kestrel-section `Url` values are the effective set and override all cross-server URL configuration; otherwise the effective set is the merged URL family (`ASPNETCORE_URLS`/`--urls`/`urls`/`UseUrls`/`WebApplication.Urls`, with `ASPNETCORE_URLS` overriding the `HTTP_PORTS`/`HTTPS_PORTS` shorthand, itself a wildcard-equivalent bind); if no endpoint is configured anywhere the documented default `http://localhost:5000` applies and is provably loopback. Where the merged-config derivation and `IServerAddressesFeature` disagree, or either is present in a form the hook cannot parse and reconcile, that is unprovable and throws under LocalPlay — the hook must never pick whichever surface happens to pass.
- Acceptance grammar is exactly CONCLUSIONS Q2 point 1 as pinned by recon line 40: only literal `localhost`, an IPv4 loopback literal in 127.0.0.0/8, or `::1` is proven loopback. IPv4-mapped IPv6 forms (e.g. `::ffff:127.0.0.1`) are normalised to IPv4 before the check (CONCLUSIONS Q4 point 4). Wildcards (`0.0.0.0`, `[::]`, `*`, `+`), non-loopback literals, any hostname other than literal `localhost` even if it currently resolves to loopback, any mixed loopback/non-loopback set, and any unknown or unprovable form all throw. With `Features:LocalPlay=false` the hook imposes no bind restriction (Q2 point 2). This narrows no ticket-required binding: URL-family, Kestrel-section, ports-shorthand, and Aspire/environment-injected listeners all remain in scope and are validated, not forbidden.

## 3. G2 — code-defined endpoints: explicit guard/review invariant (ruled)

- Ruled invariant for this delivery: no code-defined Kestrel endpoint may be introduced — no `Listen`, `ListenAnyIP`, `ListenLocalhost`, `ListenUnixSocket`, `ListenNamedPipe`, `ListenHandle`, `UseSystemd`, or `ConfigureKestrel` endpoint configuration anywhere in `src/`. Basis: such endpoints have no public pre-start enumeration API, so no documented pre-serving observer could prove them loopback (recon line 46, G2), and CONCLUSIONS Q2 point 3 already rules Unix sockets and named pipes unsupported LocalPlay listeners that fail startup; Q2 point 3 and Q5 point 3 forbid silently treating an unprovable form as proven.
- This is a guard/review invariant, not a runtime claim: it is enforced by code review plus a source search for those symbols over `src/` in the PR, on top of the pinned baseline at HEAD `e847c08` where the recon found zero such hits and no `Kestrel` config section (recon line 46). The plan and PR must state it as an invariant to be checked, never as proof the runtime validator can see code endpoints.
- If any such endpoint is ever required by this delivery, work stops on that point and returns to Patron for recon/ruling with the endpoint form named; it is not absorbed as proven, and no validator-side special case for it is pre-approved. That return is the Q2 point 3 return condition applied, not a `blocked: structural` owner escalation: the ticket's required bindings (point 2 above) are untouched by the guard.

## 4. G1/G3 — proof standard: actual startup only

- G1 parity and G3 Aspire injection are closed only by the actual-startup and shipped-path tests already mandated, not by config-parser-only tests (recon lines 45, 47; CONCLUSIONS Q4 point 4; Q5 point 3). The test set must therefore exercise, through a really starting shipped-path host: Kestrel-section-over-URL precedence, `preferHostingUrls` in both states, `HTTP_PORTS`/`HTTPS_PORTS` shorthand and its `ASPNETCORE_URLS` override, mapped-IPv4 normalisation, the empty-config default, a mixed set failing startup with no request served, and the actual Aspire-injected API listener environment observed at runtime. If those tests contradict the Learn-derived precedence or show `IServerAddressesFeature` is not reconcilable with merged config at `StartAsync`, the exact recon question to return is: which surface, at `IHostedService.StartAsync` in the net10.0 shipped API host, authoritatively carries the effective listener set for the contradicting case, per Microsoft Learn or observed shipped-path behaviour? No further recon is required before this ruling; proof is sufficient to decide the hook, subject to those mandated tests.

## 6. Supplement — constitution VII binding rule and actual ordering proof (same ruling, added on Keel's supplement)

- Constitution VII is not waived: all configuration is bound to options records with validation and `ValidateOnStart()`, and reading `IConfiguration` by magic string outside options binding is forbidden (constitution `.specify/memory/constitution.md:440-443`, checklist at `:561-562`). Section 2 above is therefore constrained as follows, and where its earlier shorthand (merged config) conflicts with this section, this section governs.
- Framework listener inputs may be consumed only through (a) the existing approved options-binding composition — the `AddOptions<T>().BindConfiguration(<options>.SectionName).ValidateDataAnnotations().ValidateOnStart()` pattern already in `Api/Program.cs:25-28` and owned for validation by ServiceDefaults (constitution `:429-430`) — with `Features:LocalPlay` itself taken from the already-bound `FeatureOptions` in the constitution VII list (`:440-442`), or (b) documented framework constants/APIs that are not ad hoc key strings: `IServerAddressesFeature`/`WebApplication.Urls`, `IServerAddressesFeature.PreferHostingUrls`, host/Kestrel framework-bound server options, and framework `WebHostDefaults`-style constants for the URL/ports/prefer-hosting-urls keys (recon lines 30, 32, 37). No ad hoc raw key reads (no literal `urls`, `Kestrel:Endpoints`, `HTTP_PORTS`, `preferHostingUrls`, or section-path strings in the validator), and no new public configuration key is created — inventing e.g. an allow-list key would be a new API/config shape under §2.3(4)/(5) and is not approved.
- Adding a new options record to carry listener configuration is not pre-approved either: the constitution VII options list is a closed enumeration (`:440-442`), so a new record returns to Patron before creation. Prefer the framework surfaces in (b), which describe endpoints the host itself will bind, over duplicating Kestrel's grammar into a project-owned record that could diverge from the framework's resolution.
- If implementation cannot obtain any required framework input — in particular the Kestrel-section effective `Url` values with their override precedence — through (a) or (b) without a magic-string read, it must not waive VII silently and must not ship the raw read: it stops and returns to Patron with the exact input and the attempted framework surface named. Patron will then either identify a VII-compliant surface or escalate as `blocked: structural — <constitution VII departure question>` for the user under §2.3(b); a constitution departure is the user's, never a silent assumption. Until that is resolved the listener form in question is unprovable and fails startup under Q2 point 2.
- Actual pre-server ordering must be pinned by the required startup tests; naming `StartAsync` in code is not proof. The tests must run a really starting host on the shipped composition and show, with `Features:LocalPlay=true` and a non-loopback or unprovable listener: the validator's `StartAsync` runs and throws before the server binds (no listening socket, no served request, host start fails), before later-registered hosted services are started, and with no concurrent hosted service completing a bind or serving first. Registration order is part of the contract: the validator is registered in `Api/Program.cs` ahead of other hosted services whose start could do externally visible work (current composition registers `StrandedMovieSweeper` at `Program.cs:38`), and a test or composition assertion must fail if that order regresses. These ordering/concurrency cases sit alongside the section 4 precedence and Aspire cases; unit tests calling the validator's `StartAsync` in isolation prove none of them.

## 7. Scope check

No ticket-text or constitution departure: LocalPlay startup refusal is ticket-named (CONCLUSIONS Q2 basis, recon-DEV-458 line 9), the hook adds no API shape (§2.3(4)), and `Features:LocalPlay`/`Process.Start` sit under §2.3(5) with this cited ruling. Keel owns recording; Patron does not edit the brief or CONCLUSIONS here. No implementation, freeze, or Gate 1 status is claimed.


# Q8 — Phase 2 step 6 round 1 full Patron ruling

Source: supplied file patron-DEV-458-step6-rulings.md, recorded verbatim on 2026-11-07. This section controls earlier open proxy mechanism alternatives and refines safe 403/404 semantics. Referenced canvas/code facts retain their supplied provenance and are not independently reverified here. NOT FROZEN; blocking recon remains open.

# [from Patron] DEV-458 Phase 2 step 6 plan challenge round 1 — rulings

Verdict: rulings below settle Sentry H1/H3/M2-M5/L1-L2, Ledger M4 and Compass M1 within Q1-Q4; plan remains NOT FROZEN; no user escalation; VII accessor recon still blocks freeze.

Basis key: ticket four behaviours at recon-DEV-458:11; Q1-Q7 in specs/DEV-458/brief.md and specs/DEV-458/CONCLUSIONS.md; spec FR-001 to FR-017; recon-DEV-458-2:12-48 and :50-59; PRODUCT.md sections 3 and 5 care items 4-5; constitution VII:440-443.

## 1. Sentry H1 + Sentry M3 + Compass M1 + Ledger M3 — shipped-proxy topology and positive control (ACCEPT, pinned)

Ruling: the FR-007 or is removed. Each shipped path gets one pinned mechanism below. The LocalPlay-disabled default is never the substitute for a pinned exclusion where an exclusion is pinned.

- nginx / docker path (docker/nginx.conf, docker/api.settings.example.json:4): PINNED technical exclusion. Add a play-route deny in docker/nginx.conf for POST /api/movies/{id}/play returning 403 at the proxy, ahead of the /api/ proxy_pass at docker/nginx.conf:7-14. The shipped Compose profile keeps Features:LocalPlay false as shipped; that default is retained but is not the mechanism relied on. Editing docker/nginx.conf is inside Q4 shipped-nginx zone; no docker-compose or README edit is approved. Any mechanism documentation beyond the config itself goes in specs/DEV-458 spec/tasks text, per Ledger M3, not in a new README.
- Aspire AppHost path (src/LamuFlix.AppHost/Program.cs:16-18 api WithHttpEndpoint, :46 web WithEndpointProxySupport(false)): PINNED technical exclusion at the proxy layer already present — endpoint proxy support for the web project stays disabled, and no Aspire endpoint proxy may forward the play route. No AppHost edit is approved unless the Phase B smoke shows the play route is in fact forwarded through the web proxy; only then is the minimal AppHost exclusion edit inside Q4 zone, returned to Patron if it exceeds retaining or applying WithEndpointProxySupport(false). The direct api endpoint is not a proxy path; it is governed by the Q1 peer-plus-header guard and Q2 startup refusal.
- Positive control (exact topology, Compass M1): a separate, named LocalPlay-enabled loopback host using the same build, same configured player fixture and same selected-file fixture as the smokes. It binds only a proven loopback listener, runs with Features:LocalPlay true, and receives a direct loopback request with no provenance headers. Expected: 204 and exactly one observed process start. It is never presented as proof that the nginx-disabled or proxy-excluded topology can launch; it proves only that the launcher is not universally broken, satisfying the control purpose in spec US2.4/SC-004 as clarified. Negative smokes run per shipped path above: via nginx expect proxy 403 and zero process starts; via the Aspire web-proxy attempt expect no forwarded launch and zero process starts, plus a header-bearing or non-loopback direct-api attempt expect API 403 and zero starts.
- Smoke discipline unchanged: T022/T023 stay mandatory per path, T032 keeps blocking clearance while any required smoke is Could not run, per Sentry H1 and brief Q5 Phase B evidence bar. G3 Aspire injection remains observed only in that smoke, recon-DEV-458-2:47.

## 2. Sentry H3 — provenance header set (RETAIN exact Q1 set)

Ruling: retain exactly Forwarded, X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto, X-Real-IP. Do not add Via, X-Forwarded-Port, X-Forwarded-Prefix, Forwarded-For variants or CDN-specific headers in this ticket.

- Basis: brief Q1 and CONCLUSIONS Q1 name the five-header set deliberately under 2.3(4)/(5); spec FR-004 mirrors it.
- Reason: a wider denylist does not solve the stripping-proxy case, adjudication-DEV-458-Keel line 13; that case is closed by the pinned shipped exclusions in ruling 1 plus the unsupported-deployment clause in Q1. Any header list remains bypassable by a proxy that adds nothing.
- Residual risk recorded: a non-shipped loopback proxy that forwards play while adding only an unlisted header, or stripping all headers, with LocalPlay enabled, remains unsupported per Q1 indistinguishable-proxy clause. Widening the list would be a Q1 scope change beyond Q1-Q4 frozen scope, brief Q5; it is noted, no ticket, unless a shipped path is shown to emit such a header — then return to Patron as an in-scope shipped-path fact.

## 3. Out-of-scope suggestions — classification against Q1-Q4 (no silent widening)

Frozen scope is only Q1 request/proxy protection, Q2 startup refusal, Q3 existence-first and unmapped-format outcomes, and Q4 necessary config/tests/docs, brief Q5 and CONCLUSIONS Q5 point 1. Anything else is a follow-up, not a finding in this round.

- Sentry M2 throttle / single-flight / rate limit on launches: FOLLOW-UP candidate, not in DEV-458. No throttle, concurrency cap or new abstraction is added. Brief Q3 already settles no retry on start failures and generic 500. A caller that can launch already holds proven loopback plus LocalPlay plus a configured player, Q1/Q3, so the residual fork-bomb actor is the local operator the feature exists for. Disposition: noted, no ticket — not Critical/High, not broken behaviour against the ticket four behaviours at recon-DEV-458:11. Record the accepted residual in CONCLUSIONS so a future hardening ticket can cite it.
- Sentry M5 dash-leading filename as player option / adding a -- terminator: NOT ADOPTED in DEV-458; premise unresolved. FR-012 and Q3 preserve exactly one ArgumentList file argument; adding a separator changes that settled contract and no supplied recon establishes that selected paths are relative dash-leading values or that every configured player accepts a universal terminator. Do not extend T016 beyond hostile filename as one ArgumentList entry in this round. Disposition: needs recon only if pursued later — whether DEV-438 selection can yield a relative dash-leading path and what terminator the configured players support. No ticket now; return to Patron with that recon before any follow-up is filed.
- Sentry L1 containment of selected file inside the movie folder / symlink escape: NOT ADOPTED in DEV-458. Q4 excludes Core/selector edits and Q3 preserves DEV-438 selection unchanged; existence-first FR-008 is the only launcher check in scope. No containment assertion is added on speculation. Disposition: needs recon only if pursued later — whether the DEV-438 selector already guarantees containment. No ticket now; if recon shows an escape reachable under the ticket behaviours, return to Patron for a scope ruling.
- Sentry L2 changing UseShellExecute to false: REJECTED as a behaviour change in DEV-458. Q3 and FR-012 explicitly preserve existing UseShellExecute behaviour with exactly one ArgumentList argument and no new shell parsing, recon-DEV-458:20. No supplied evidence shows a verb-hijack or exploit in the shipped players, and flipping it would undo a decided behaviour and risk breaking configured-player launch. Preserve it; record the accepted residual in CONCLUSIONS. Noted, no ticket.

## 4. Sentry M4 — safe 404 detail (ACCEPT, pinned constraint)

Ruling: extend the FR-010 safe-detail rule to FR-009. The existing NotFoundException to existing 404 ProblemDetails mapping stays, with no GlobalExceptionHandler edit, per Q4 point 2 and FR-015.

- Allowed: the existing generic 404 title, and a path-free detail stating only that the requested playback file was not found, optionally with the route movie identifier already supplied by the caller.
- Forbidden in title and detail: the selected LibraryPath value, any full or partial filesystem path, directory or basename of the selected file, player executable path, and any configured player path. This mirrors Q3 for 422: format and identity level only, never file or player paths, CONCLUSIONS Q3 point 1.
- Implementation constraint: the new launcher NotFoundException throw must itself use a path-free message, inside the approved Infrastructure launcher zone, so no handler edit is needed if the handler surfaces exception text. Tests T015-T018 assert the 404 body contains none of the forbidden values while the status stays 404 with zero process starts and zero history, FR-009/Q6.
- Open fact (recon, section 6): the exact current handler construction for 404 is not in supplied recon. If recon shows the handler or an existing NotFound thrower already surfaces a selected-file path, do not edit the handler or selector silently; return to Patron with the source of the path-bearing message.

## 5. Ledger M4 — existing 403 payload without a false feature-disabled claim (ACCEPT, pinned semantics)

Ruling: keep the existing FeatureDisabledException to 403 ProblemDetails mapping and the existing declared 403 status; no new exception type, no new mapping and no GlobalExceptionHandler edit in DEV-458, per Q4 points 1-2, FR-005/FR-014/FR-015 and spec route declarations at recon-DEV-458:33,37-38.

- Semantic pin: the 403 payload for a request-boundary refusal must not state or imply that LocalPlay is disabled when the refusal reason is peer or provenance headers. The feasible generic payload to land, subject to the recon check below, is a generic 403 title with a detail that local playback cannot be launched from this request, with no peer address, header name or feature-state claim. The LocalPlay-disabled 403 keeps its existing catalog-then-EnsureEnabled order, FR-014/Q6; its payload may remain the existing disabled wording because there the claim is true.
- Test pin: T009/T010 must assert not only 403 and zero side effects but also that a peer/header refusal body contains no disabled claim, no peer address and no header name; the disabled case body is asserted separately.
- Open fact (recon, section 6): supplied recon proves the mapping at recon-DEV-458:37-38 but not whether the handler builds title/detail from the exception message or from fixed strings, nor the exact current strings. If the existing mapping cannot produce the generic payload above without a handler edit or a new exception, the plan must stop at that point and return to Patron for a 2.3(6) ruling with the exact current strings attached. Do not ship a payload that falsely claims the feature is off, and do not invent a new exception or mapping inside the plan.

## 6. Recon required via Bernstein (no Patron code search)

Keel to route through Bernstein/Wisp; these are the only fact gaps in these rulings:

1. VII accessor (already blocking, Sentry M6 / Ledger H1): exact VII-compliant concrete API or accessor for every needed effective-listener input before StartAsync, especially Kestrel-section Url values, on net10.0, or an explicit unavailable result per input. Until then plan Constitution Check stays OPEN, not PASS, and freeze and Gate 1 stay blocked. If no compliant surface exists, Patron either identifies one or it becomes blocked: structural — constitution VII departure as an owner checkbox under 2.3(b); Patron cannot waive VII and cannot close that checkbox.
2. 403/404 payload construction: exact title and detail the existing GlobalExceptionHandler produces today for FeatureDisabledException and for NotFoundException, whether each comes from the exception message or a fixed string, the throw-site messages for DisabledMediaPlayerLauncher and for the primary-video and folder NotFound cases in supplied recon, and any existing test asserting those bodies. This closes ruling 4 and 5 exact strings.
3. Only if Keel or Quill pursues the follow-up candidates later, not blocking this round: Sentry M5 path-form and player-terminator facts, and Sentry L1 DEV-438 containment guarantee. Absent that recon they stay noted with no ticket.

## 7. Escalation check

No ruling above contradicts or changes the ticket four behaviours at recon-DEV-458:11, adds a route, DTO, status, dependency, project, layer, schema or secret, or departs from the constitution. The five-header set, UseShellExecute preservation and the one-argument contract are ticket-settled Q1/Q3 decisions, retained not widened. The only potential owner escalation remains conditional: a proven-absent VII-compliant accessor, which would be blocked: structural at that point, not now. Keel records these rulings in brief/CONCLUSIONS; Patron edits no Keel artifact in this round. Round cap and NOT FROZEN status unchanged.

# Q9 — Phase 2 step 6 supplement: accessor structural escalation and payload reconciliation

Source: Patron supplied patron-DEV-458-step6-supplement-ruling.md, recorded verbatim on 2026-11-07. This ruling supersedes Q8 custom 403 detail / custom NotFound message expectations and resolves the accessor recon as an owner structural decision, not an authorized departure. PLAN NOT FROZEN; Gate 1 closed.

[from Patron] DEV-458 Phase 2 step 6 supplement — I1/VII ruling and Q8 403/404 payload reconciliation

Verdict: blocked: structural — constitution VII departure (owner PR checkbox, section 1 below). No documented VII-compliant alternative for I1 exists in the supplied evidence; Patron does not waive VII and does not approve a silent fail-all implementation. Q8 sections 4/5 payload pins are reconciled at section 3 below with no handler or Core edit. Plan remains NOT FROZEN; Gate 1 stays closed.

## 1. I1 — Kestrel-section effective Url reader: UNAVAILABLE; necessary constitution departure

- No compliant alternative identified. recon-DEV-458-2-2 section 8.4 (I1) exhausts eight documented net10.0 surfaces — IOptions<KestrelServerOptions> (13 properties, no endpoint/Url enumeration), KestrelServerOptions.ConfigurationLoader (endpoints materialise on server start, after hosted services; no read-back), KestrelConfigurationLoader (Load() returns void, no enumeration; reading its Configuration needs banned Kestrel/Endpoints/Url literals), KestrelServer, IServerAddressesFeature/WebApplication.Urls (no pre-bind Kestrel-section values documented), WebHostDefaults (complete field list, no Kestrel/Endpoints/Url constant), WebHostOptions/IOptions<WebHostOptions> (no public documented type, 404), and a new Kestrel-section options record (barred). The Kestrel-over-URL precedence rule is documented, but its decision input is not readable at IHostedService.StartAsync.
- Every remaining route departs from constitution VII: either an IConfiguration magic-string read outside options binding, forbidden at constitution .specify/memory/constitution.md:443 and CONCLUSIONS Q7 supplement 6 (specs/DEV-458/CONCLUSIONS.md:187-190, no literal urls/Kestrel:Endpoints/preferHostingUrls/section-path strings, section 188), or a new options record, barred by the closed VII enumeration at constitution:440-442 and CONCLUSIONS:189. Patron cannot waive VII (CONCLUSIONS:190, :255; role section 2.3(b)).
- Absence is as unprovable as presence: section emptiness is unreadable through the same surfaces (recon-DEV-458-2-2 section 8.4 I1 consequence (i)), so the documented empty-everywhere default http://localhost:5000 (I5) cannot be proven reachable either. Under strict VII plus Q2 fail-closed (CONCLUSIONS Q2 point 2: unmet or unprovable invariant fails startup, specs/DEV-458/CONCLUSIONS.md:45), no LocalPlay=true configuration is certifiable. A silent fail-all (refuse every LocalPlay=true startup) is not an implementation of the ticket: it negates the ticket-required LocalPlay-enabled loopback launch behaviour and the Q8 section 1 positive control (CONCLUSIONS:214, 204 plus exactly one process start), so it would itself contradict the ticket text. Not approved.
- The shipped composition having no Kestrel section and no url-family hits at HEAD (recon-DEV-458-2-2 section 8.1 receipt) is a baseline fact, not a runtime proof of absence, and does not cure I1.

Owner PR checkbox (Gate 1 stays closed until the user answers it; a Patron answer never closes it):

blocked: structural — DEV-458 Q2 requires proving every effective listener loopback before serving when Features:LocalPlay=true, including whether the Kestrel configuration section defines endpoints and with what Url values (that section overrides all URL-family configuration). On net10.0 no documented API reads those values, or proves the section empty, before server start without either (a) reading IConfiguration by Kestrel/Endpoints/Url key strings outside options binding, or (b) adding a new options record binding the Kestrel section — both departures from constitution VII (constitution.md:440-443). Do you approve such a VII departure for this validator (and if so, which of (a) or (b)), or should DEV-458 Q2 be rescoped so LocalPlay=true is not required to be provable against Kestrel-section endpoints?

## 2. Inputs that stand; caveats that do not cure I1

- I0 Features:LocalPlay via existing IOptions<FeatureOptions> binding: AVAILABLE (recon-DEV-458-2-2 section 8.4 I0). I2 via WebHostDefaults.ServerUrlsKey, I3 via WebHostDefaults.HttpPortsKey/HttpsPortsKey, I4 via WebHostDefaults.PreferHostingUrlsKey plus IServerAddressesFeature.PreferHostingUrls, I6 via container IServer to Features.Get<IServerAddressesFeature>(): AVAILABLE as named (recon-DEV-458-2-3 section 8 summary table, lines 5-11). These rulings stand and need no departure.
- I7 (empty vs unpopulated IServerAddressesFeature, population/PreferHostingUrls-copy timing) remains NOT DOCUMENTED (recon-DEV-458-2-3 sections 8.5/8.6; G1 stands). Disposition unchanged: fail closed, and the T005 real-host observation in Phase B is mandatory; parity is never assumed from documentation. This caveat is a Phase B evidence requirement, not a cure for I1 and not a separate owner question.
- Until the section 1 checkbox is answered: plan Constitution Check stays OPEN not PASS, freeze and Gate 1 stay blocked (CONCLUSIONS:255), and no validator implementation is approved — in particular no magic-string read, no new Kestrel options record, no reflection, and no code endpoints (Q7 section 3 guard invariant stands).

## 3. Q8 payload pins reconciled with recon-DEV-458-2-3 section 9 facts — preserve current bodies; sufficient

Section 9 establishes that both bodies are already fixed generic strings: GlobalExceptionHandler.cs:33-36/:86-93 maps NotFoundException (non-capturing arm) to 404 Title Not Found, Type rfc9110#section-15.5.5, no Detail assignment; :45-48/:110-116 maps FeatureDisabledException (non-capturing arm) to 403 Title Forbidden, Type rfc9110#section-15.5.4, no Detail assignment; the only Detail in the handler is the ImdbId conflict at :103-108 (recon-DEV-458-2-3 section 9.2). NotFoundException is parameterless only, base fixed Not found. (src/LamuFlix.Core/Pipeline/NotFoundException.cs:5-9; recon section 9.3), and the DisabledMediaPlayerLauncher message LocalPlay is disabled in this environment. (DisabledMediaPlayerLauncher.cs:9) never reaches the wire.

Ruling:

- 404 (Q8 section 4, Sentry M4): preserve the current generic body and the parameterless throw. A custom path-free NotFound detail or message is impossible without editing GlobalExceptionHandler or making NotFoundException message-carrying (which would reach every parameterless throw site in recon section 9.3); neither edit is preapproved, and neither is required. The current body already satisfies the safety outcome: fixed Title Not Found, no detail, no LibraryPath, path, basename, or player path. Q8 section 4 is superseded only insofar as it anticipated a surfaced detail or a new path-free throw message; its forbidden-content list stands as test criteria.
- 403 (Q8 section 5, Ledger M4): preserve the current generic body. A custom 403 detail (e.g. stating local playback cannot be launched from this request) would require a handler edit; not preapproved and not required. The existing wire body already makes no false feature-disabled claim — Title Forbidden, no detail, no peer address, no header name — because the handler ignores the exception message (recon section 9.5, Ledger M4 feasibility). Q8 section 5 is superseded only insofar as it required that custom detail text.
- New assertions required instead (test-only, inside the Q4-approved test zones; pattern GlobalExceptionHandlerTests.cs:162 / EnrichmentEndpointTests.cs:163, the detail-absence gap at recon section 9.4): for both 403 and 404 assert detail absent, title exactly Forbidden / Not Found, and body contains no exception message (including LocalPlay is disabled in this environment. and Not found.), no exception type name, no filesystem path or basename, no peer address, and no provenance header name.
- No handler/Core edit preapproved: src/LamuFlix.Api/ExceptionHandling/GlobalExceptionHandler.cs, src/LamuFlix.Core/Pipeline/NotFoundException.cs, and src/LamuFlix.Core/Pipeline/FeatureDisabledException.cs are untouched by this ruling. Any future proposal to surface a custom 403/404 detail or a message-carrying NotFoundException returns to Patron under section 2.3(4) (public API shape) and 2.3(6) (tracked file the ticket does not name) before any edit.

## 4. Routing and status

- Keel: record this ruling verbatim in specs/DEV-458/CONCLUSIONS.md, fold sections 1-3 into brief.md/spec/plan/tasks as needed (including the section 1 checkbox on the spec PR and the section 3 test assertions replacing any custom-detail/message tasks), and route the repair through Quill. Patron edits no Keel artifact in this round.
- Plan NOT FROZEN. No implementation, no Gate 1 claim, no VII waiver. The only owner escalation is the section 1 checkbox.

## Q10 — Owner Option A resolves the Q9 Constitution VII departure (2026-11-07)

Authority: owner answer **Option A** on [draft spec PR #336](https://github.com/lamuniercosta/lamuflix/pull/336), as supplied by Bernstein in the resume ask and recorded in task note DEV-458:40 (owner checkbox ticked by lamuniercosta). This is a supplied owner-decision receipt, not an independent GitHub read-back by Keel. Q9 and earlier blocked-status language are historical; this section controls where inconsistent.

- Owner approves the narrowly scoped Constitution VII departure for this DEV-458 API startup validator to read the Kestrel / Endpoints / Url configuration through IConfiguration outside options binding. The reader must inspect section presence/absence and configured endpoint Url values to reconcile the effective listener set under Q7 precedence. I1 is now an **owner-approved exception**, not a newly discovered VII-compliant API. Do not label it unconditional VII PASS.
- Option B (new Kestrel options record) and Q2 rescope were not selected. No new configuration key, dependency, options record, reflection, code-defined endpoint, or broad raw-configuration permission is approved. No global constitution rewrite is part of this reconciliation. Other I0/I2/I3/I4/I6 surfaces and the rest of VII remain binding.
- Q2 scope, fail-closed handling of missing/unprovable/conflicting inputs, mapped-address grammar, pre-serving startup invariant, and the working enabled-loopback positive control remain unchanged. The silent fail-all implementation remains forbidden. I7 timing/parity and actual real-Kestrel startup ordering must still be proved in Phase B, as must shipped nginx/Aspire smoke, source receipt, and required gates.
- Sentry M6 and Ledger H1 are resolved at the planning decision level by explicit owner approval; artifact repair and independent reread must complete before PLAN FROZEN. This is continuation of plan-challenge round 1, maximum two, not an extra challenger round.
- Answering this departure checkbox does not approve Gate 1, merge the spec PR, accept the ADR, authorize implementation before pickup, or prove runtime behavior. ADR 0036 remains Proposed until the explicit Gate 1 lifecycle transition. Gate 1 requires the separate user-owned spec approval/merge receipt, then T000 drift pickup.

Quill must reconcile spec, plan, tasks, checklist, drafting receipt and pr-body-DEV-458 with this decision, preserve the answered owner question as history, name the bounded IConfiguration reader in T001/plan, and distinguish **VII exception approved** from conformance. Keel owns brief/CONCLUSIONS/ADR and freezes only after independent document verification. No new taste ruling or assumed entry.

## Q11 — Keel plan freeze after independent Q10 reread (2026-11-07)

**PLAN FROZEN.** This is the controlling planning status, superseding pre-freeze status labels in the historical Q1-Q10 record. Gate 1 is not approved or merged; ADR 0036 remains Proposed. No implementation, runtime acceptance or required-gate PASS is claimed.

All three plan-challenge reports are present (Sentry 13, Ledger 11, Compass 5). All 29 findings have planning dispositions; prior 27 remain reconciled and Sentry M6 / Ledger H1 are now CLOSED AT PLAN STAGE by owner Option A plus verified Q10 artifact reconciliation. This continues round 1 within the existing maximum of two. No extra challenger round or threshold waiver.

Independent read-only consistency review covered Quill Q10 receipt, changed spec/plan/tasks/checklist/drafting receipt, pr-body-DEV-458, brief/CONCLUSIONS and Proposed ADR. SpecKit prerequisites succeeded for this worktree; no before_analyze/after_analyze hooks are configured. No new ambiguity, scope change, uncovered requirement, or unresolved planning decision found. The Q10 I1 exception remains explicit and bounded; constitution VII is not labelled unconditional PASS. Requirement mapping remains 17 FR + 5 SC across T000-T032, including FR017 to T032. No change to API status/body decisions, five-header boundary, proxy pins or positive-control topology.

Coverage: FR001-003/SC001 to T001-T006; FR004-006/SC002 to T007-T010; FR007 to T020-T024 and SC004 to T022-T023; FR008-013/SC003 to launcher/outcome T013-T018; FR014 to T011; FR015 to T000/T013/T032; FR016 to T026; FR017 to parser/startup/request/launcher/smoke and T032; SC005 to T027-T032. No unmapped requirement or task. Analysis findings: 0 new Critical/High/Medium/Low after the Q10 repair.

Publication handoff: local reconciliation and freeze need commit/push and updated PR #336 body plus adjudication summary publication/read-back by Bernstein/Rigger. The local note/body is not live publication. The review round is not claimed fully published until that receipt exists. Separate user-owned Gate 1 approval/merge precedes Phase B T000 drift pickup. Phase B I7 real-host timing/parity, pre-serving/startup precedence proof, nginx/Aspire smoke, positive control, no-code-endpoint receipt, harness gates and implementation review remain mandatory and unexecuted here.

---

## Gate 1 status

- **Patron ruling:** `gate1: provisional` — `/speckit-analyze` is clean (0 findings after Q10 repair), all 29 plan-challenge findings adjudicated (Sentry 13, Ledger 11, Compass 5; Keel Q11), Keel has frozen the plan, and `checklists/requirements.md` User-Visible Outcome items are ticked. Owner Option A remains an approved bounded VII exception, not unconditional VII PASS. Gate 1 approval/merge remains user-owned and pending; this ruling is not owner approval.

## T000 — Gate 1 merge receipt and Phase B pickup (2026-11-08)

**Gate 1 merge recorded; T000 complete; Phase 1 (T001–T006) unblocked.** This receipt controls current pickup status over the retained Phase A provisional / Gate 1 closed wording in brief, spec, plan, tasks and the history above. Frozen behavior and scope are unchanged.

Authority: Bernstein T000 pickup ask; task note `DEV-458:4-14`; `recon-DEV-458:4-5,20-27`. Spec [PR #336](https://github.com/lamuniercosta/lamuflix/pull/336) merged at `0fc9dc87`. This is the supplied merge receipt, not an independent GitHub read-back by Keel. Pickup worktree is `F:/Dev/LamuFlix.worktrees/feature-DEV-458-harden-the-local-launcher`, branch `feature/DEV-458-harden-the-local-launcher`, recon HEAD `40a3e613828e3046bc14646f67a059ec804a673a`; Phase A base `659a2e9b`. Branch was verified before edits; working tree was clean.

### Read-only pickup analysis

Compared brief, spec, plan and tasks with the supplied Phase B recon. Recon reports no spec-set drift from Phase A base to pickup HEAD and retains the existing launcher/endpoint/exception reuse points (`recon-DEV-458:9-18,24-27`). No independent source search, measurement, or tracker query was used to extend those facts. No behavior or task-order amendment is required by this recon.

SpecKit prerequisite initialization **Could not run**: `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` rejects the `feature/DEV-458-harden-the-local-launcher` branch as nonnumeric. Direct read-only analysis used the explicitly named `specs/DEV-458/` artifacts instead; this is not a successful prerequisite-script run. No before_analyze or after_analyze hooks are configured.

| Requirement coverage | Tasks |
| --- | --- |
| FR-001–003 / SC-001 | T001–T006 |
| FR-004–006 / SC-002 | T007–T010 |
| FR-007 / SC-004 | T020–T024; runtime proof T022–T023 |
| FR-008–013 / SC-003 | T013–T018 |
| FR-014 | T011 |
| FR-015 | T000 / T013 / T032 |
| FR-016 | T026 |
| FR-017 | T003–T005 / T009–T011 / T016–T018 / T022–T023 / T032 |
| SC-005 | T027–T032 |

Analysis findings: **0 new Critical, High, Medium or Low**; 22 requirements, 33 tasks, 22/22 covered (100%); 0 new ambiguities, 0 conflicting duplicates, no unmapped tasks or newly opened return condition. Checkpoint and pipeline tasks map to their parent requirements and closing bar. Constitution VII remains an **owner-approved bounded departure for I1**, not unconditional PASS; the rest remains binding. API-only startup composition, the exact five-header boundary, existing generic 403/404 mappings, existence-before-player precedence, named positive control, shipped proxy exclusions, and serial test-file ownership remain consistent with the brief and recon.

Next: implement Phase 1 T001–T006 within the frozen scope. T004/T005 real-Kestrel precedence, ordering, pre-bind and I7 timing/parity proof remain mandatory; contradictory runtime evidence returns for recon. T022/T023 nginx/Aspire smoke, positive control and Aspire listener observation, T025 explicit ADR Accepted lifecycle, T027–T029 gates, and three-axis implementation review remain outstanding. This pickup performs no production edits, accepts no runtime behavior, and claims no implementation gate PASS.

---

## Steps 7/8 — Implementation review adjudication ruling (Ledger F1, Compass F4, Compass F6)

Verdict: Adopt Keel's recommended dispositions for Ledger F1, Compass F4, and Compass F6 in full alongside the clear blockers (missing T022–T024 runtime evidence, validator registration after outbox/Rabbit hosted services, and case-sensitive nginx exclusion).

- **Ledger F1 (`docker/nginx.conf:8-10` explanatory comments) — CONFIRM Low in-scope rule violation (blocking; delete comment only):** Delete only the three comment lines and keep the deny rule intact (`AGENTS.md` Non-negotiables: no explanatory comments and no config-file exemption; `specs/DEV-458/tasks.md:17,99` T020: mechanism text stays in `specs/DEV-458/`; `specs/DEV-458/CONCLUSIONS.md:115,212` Q5.2 closing bar and Q8.1).
- **Compass F4 (`T004` missing real-Kestrel `HTTPS_PORTS` and mapped-listener rows) — RETAIN Low coverage blocker under FR-017:** Do not waive any row of the specified real-Kestrel startup matrix; add the missing `HTTPS_PORTS` and mapped-listener (`::ffff:127.0.0.1`) real-Kestrel startup cases (`specs/DEV-458/spec.md:83,161,177` FR-001/FR-017; `specs/DEV-458/tasks.md:56` T004; `specs/DEV-458/CONCLUSIONS.md:93,115,183` Q4.4, Q5.2, Q7.4).
- **Compass F6 (bespoke real-Kestrel fixture plus shipped DI order assertion) — NO SEPARATE FINDING; fold into F2:** Keep the T004-authorized `LocalPlayRealKestrelHostFixture` and fold the shipped-composition DI registration-order regression assertion (proving `LocalPlayListenerValidationHostedService` is registered ahead of outbox/Rabbit/`StrandedMovieSweeper` and any other externally active hosted services) into the F2 remediation (`specs/DEV-458/tasks.md:56-57` T004–T005; `specs/DEV-458/CONCLUSIONS.md:115,191` Q5.2, Q7.6).

