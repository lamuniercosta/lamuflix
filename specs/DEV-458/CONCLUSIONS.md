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
