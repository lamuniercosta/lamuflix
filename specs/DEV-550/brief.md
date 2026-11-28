# DEV-550 — Phase 2 brief

## Baseline and scope

Planning baseline is branch `feature/550-spec` at `6bd8be6712c6239065b22609db1599b4f91d3955`, Size M. The governing sources are DEV-550 Wanted, Done when, Out of scope, Where it lands and Needs, plus `recon-DEV-550` lines 10–16 and 32–37 and `specs/PRODUCT.md` §§2–5. [CONCLUSIONS.md](CONCLUSIONS.md) records the Patron exchange.

The frozen scope covers a shared connect-time guard for named and typed HTTP clients, tighter outbound address policy for OpenSubtitles, the DI and request behavior proofs, and two architecture rules with violating fixtures. Compose and home-cluster provider checks are required for delivery. Egress proxies, network policies, relocation of the two EF exceptions, and follow-up provider clients are outside scope. Any other work is a follow-up issue.

Recon identifies OMDb, TMDb, Ollama and OpenSubtitles by registration and finds neither explicit named registrations nor direct construction of `HttpClient` under `src/`. Its headline total of five conflicts with the four individually identified `AddHttpClient` sites. The raw tally also includes the subtitle primary-handler construction. Use the identified clients as starting coverage and discover actual registrations dynamically.

## Patron decisions

### Q1 — Shared registration

Patron approved `ConfigureHttpClientDefaults` with one idempotent internal Infrastructure extension invoked from each of the three existing registration extensions. Configure the effective `SocketsHttpHandler.ConnectCallback` without replacing the handler. Switch OpenSubtitles to `SocketsHttpHandler` and preserve `AllowAutoRedirect = false`. Preserve OMDb quota, current resilience and timeouts. Use built-in APIs in the existing projects. Host composition files stay outside the edit envelope. Basis is ticket Wanted 1–2 and Open points, recon lines 10–12 and 32–37, and PRODUCT §§2–5.

### Q2 — Address policy

Patron approved one internal address table with a mandatory deny set and a stricter OpenSubtitles set. Normalize IPv4-mapped IPv6 before matching. The guard resolves once per connection attempt, validates candidates, and dials only a validated exact `IPEndPoint`. A denied address never reaches the connector. Report a connect failure with a reason free of secrets and query strings when no allowed address is available. Configured OMDb, TMDb and Ollama can still reach private or loopback destinations subject to the mandatory rules.

- Mandatory for all clients — `169.254.0.0/16`, `fe80::/10`, `100.100.100.200`, `fd00:ec2::254`, `0.0.0.0/8`, `::/128`, `224.0.0.0/4`, `ff00::/8`, and `255.255.255.255`. This includes the ticket's `169.254.169.254` case.
- Additional denies for the entire OpenSubtitles client — `127.0.0.0/8`, `::1`, `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`, `100.64.0.0/10`, `fc00::/7`, and `198.18.0.0/15`. The test flag relaxes this set only.

The spec and implementation PR body both list the ranges and cite their basis. [IANA IPv4 Special-Purpose Address Registry](https://www.iana.org/assignments/iana-ipv4-special-registry) · [IANA IPv6 Special-Purpose Address Registry](https://www.iana.org/assignments/iana-ipv6-special-registry) · [AWS EC2 Instance Metadata Service](https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/configuring-instance-metadata-options.html) · [Alibaba Cloud ECS instance metadata](https://www.alibabacloud.com/help/en/ecs/user-guide/view-instance-metadata/)

### Q3 — Guarded transports and proof

Patron approved direct transport with `UseProxy = false` and a hard ceiling of HTTP/2. Every supported connection path must run the address check. Discover factory registrations through production service composition and inspect the effective primary handler for the shared guard. Include a negative control with an unguarded effective primary handler. Use controlled resolution and connection seams for request-level verification. Q3 places documentation in the spec and PR body and supersedes the initial XML-doc instruction. Basis is ticket Wanted 1–2, Done when, Q1–Q2, and the repository comment rules.

### Q4 — Architecture checks

Patron approved exact namespace segments `Util`, `Utils`, `Utility`, `Common`, `Helper`, `Helpers`, and `Misc` across production application assemblies. Ordinary partial-name matches remain valid. The EF rule detects EF Core dependencies including method-body references outside `LamuFlix.Infrastructure.Persistence` and its subnamespaces. The full type names for `OutboxRelayHostedService` and the API's generated `Program` type are the two exact allow-list entries. NetArchTest checks at type granularity. Use the existing ArchitectureTests and Valid/Violating fixture conventions and include a fixture whose EF dependency occurs only inside a method body. Basis is ticket Wanted 3, Done when and Out of scope, ADR 0015 as cited there, and recon lines 13–16.

### Q5 — Test override and file boundary

Patron approved a default-false internal Infrastructure guard option set explicitly in test service composition. The option has no configuration binding or deployment key. An existing subtitle test can opt in narrowly when it needs a guarded local endpoint. Tests must still prove the mandatory deny set holds with the flag enabled. Patron specifically approved the conditional fixture and Infrastructure project edits below under PRODUCT §5. Other existing-file changes require a ruling.

Patron amended Q5 during analyze round 1, item 1. Keep all ticket-required guard, request behavior and DI tests in IntegrationTests, as specified by the ticket Where it lands and documented in `recon-DEV-550` lines 10, 16 and 28. Constitution IX assigns the two pure FsCheck address-policy properties to the existing UnitTests project. This preserves ticket scope and satisfies the constitution. [CONCLUSIONS.md](CONCLUSIONS.md) records the ruling (commit `ed5cf8e1`).

### Q6 — Closing bar and sequence

Patron approved the frozen scope above and closed the grill. Close every verified in-scope finding through Low and record Sentry, Ledger and Compass dispositions. The cap is **two review rounds and two fix commits per round**. Require green applicable C# gates on the exact delivery head and separate redacted Compose and home-cluster receipts for OMDb, TMDb, Ollama and OpenSubtitles. The approved plan uses dynamic DI discovery and the four named starting providers. No ADR or Gherkin deliverable. There were no taste assumptions or structural blockers.

## File envelope

- `src/LamuFlix.Infrastructure/Adapters/MetadataProviderServiceCollectionExtensions.cs`, `OllamaEmbeddingServiceCollectionExtensions.cs` and `SubtitleProviderServiceCollectionExtensions.cs` in that directory. Add the shared internal guard, transport registration, address policy and test-only options in the same existing directory.
- `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` plus new fixtures in its existing `Fixtures/Valid/` and `Fixtures/Violating/` directories.
- New guard, request behavior and DI integration coverage under `tests/LamuFlix.IntegrationTests/`. The existing `OpenSubtitlesSubtitleProviderTests.cs`, `SubtitleSearchDownloadEndpointTests.cs` and `SubtitleProviderHealthRegistrationTests.cs` may opt in only when they exercise a guarded local endpoint.
- Add the two FsCheck address-policy properties under `tests/LamuFlix.UnitTests/`, using its existing Infrastructure and FsCheck references.
- The existing Infrastructure project file may add `InternalsVisibleTo` for `LamuFlix.IntegrationTests` if needed for the integration coverage, and for `LamuFlix.UnitTests` only if the properties need access to the internal policy. Host `Program.cs` and `OutboxRelayHostedService.cs` participate only as exact architecture-rule exceptions. This plan adds no external dependencies, projects, layers, schema changes, public API contracts, configuration keys or deployment edits. Existing file changes outside this envelope need a Patron ruling.

## Verification

- Production DI coverage builds the real service compositions, discovers named and typed clients and inspects their final handler chains for the shared callback and the subtitle redirect setting. An unguarded primary-handler control must fail the same check. Coverage must detect an unguarded new registration without updating a fixed list.
- Deterministic request tests send to the ticket's metadata endpoint through each discovered client and verify denial before connection. Use controlled DNS answers and a controlled connector to show that a subtitle download link to a private address fails, a public resolution reaches the approved endpoint, and configured provider traffic retains private or loopback reachability. Exercise IPv4-mapped IPv6, mixed candidates, relevant CIDR edges, the scoped test flag, and the proxy and HTTP-version boundary. No live provider requests are needed for the automated cases.
- Run both FsCheck properties in `LamuFlix.UnitTests`. Cover mandatory-deny invariance under the subtitle test override and equivalent address classification after IPv4 mapping. Keep the existing harness thresholds.
- Run both architecture rules against production assemblies and their positive and negative fixtures. Include the EF method-body-only violation and exact exception matching. Keep the fixture assertions independent of the production pass.
- Gauge runs Roslyn, cyclomatic complexity, InspectCode, property tests, vulnerable-package scan, format verification, full `dotnet test`, and mutation testing per the existing harness. Record conditional skips as such. Web gates are scope-empty on the approved file envelope.
- Delivery runtime checks record environment, candidate revision and reachability result per provider for both Compose and the home cluster. Redact credentials and sensitive URLs. Runtime acceptance remains pending until both receipts exist.

## Task order

1. Serialize the work with DEV-551 because both touch `SubtitleProviderServiceCollectionExtensions.cs`. Either delivery order is allowed. Recheck the shared file at pickup.
2. Establish the shared factory registration, effective-handler transport constraints, normalized address policy and test-only option before depending on them in production registration and request proofs.
3. Add dynamic DI coverage and the negative control, followed by per-client request, address-policy property and subtitle regression checks. The NetArchTest rules and fixtures can proceed independently of the guard work.
4. Run the applicable gates after both streams converge, then obtain the two deployment receipts from the delivery candidate. The Size M review and exact-head merge bar follow the pipeline.

Quill can draft the spec, plan and tasks from this brief. Gate 1, implementation gates and runtime acceptance follow in their own phases.

## Phase 2 plan-challenge refinements

These refinements apply Q1–Q6 to the implementation and proof plan. The ticket scope, approved range lists, file envelope, gate cadence, and delivery receipt requirements continue to govern.

- Q1/Q3 registration and discovery. Prove that repeated calls through the three install points apply the defaults once and that subtitle setup changes the effective guarded handler in place. Derive client names from production service composition independently of the guard registration. Exercise the same discovery and inspection path with an additional unguarded factory registration and assert that it is found and fails. T001 starts from the Api composition identified in recon line 13 and inventories any other participating hosts at the pickup head.
- Q2/Q3 connection and proof. Test composition supplies internal resolver and connector delegates. Production uses system DNS and the socket connector, with the address policy applied before every connector call. The stricter-set test option controls only address policy. Within one resolution result, a failed allowed connection can fall back to another allowed candidate under the same cancellation and timeout budget. Every connector invocation receives its validated exact endpoint. Resolver failures and empty results fail closed. If the seam can represent a null result, it fails closed as well. Test followed redirects through the guarded transport with controlled responses. Preserve the OpenSubtitles automatic-redirect setting and check any existing explicit follow path identified at pickup. Add independent truth examples for the approved range boundaries and singleton addresses, including IPv4-mapped forms where applicable. Verify that the logged denial contains a reason and omits request URLs, query values and secret headers.
- Q4 architecture proof. At read-only T001 pickup, record the intended production assembly set and baseline EF dependency exceptions outside Persistence, including how the installed rule reports generated artifacts. Use one predicate for production and fixture assertions. Keep namespace boundaries exact and the two approved full type names. A generated artifact may be attributed to an approved source type only with evidence of its origin. An ambiguous or additional source type stops at the existing recon or Patron gate before rule work.
- Q5/Q6 standards. Keep address literals stable under mutation and mutable computation out of static initialization. Confirm test friendship, UnitTests property-test placement and harness discovery at pickup. Use each project's existing assertion and mocking conventions. Identify the operator evidence owner for T017 using the pipeline assignment.

## PLAN FROZEN (2026-11-28)

Keel froze this plan after the independent Phase 2 step 6 reread of branch `feature/550-spec` at `ed5cf8e1` and its working-tree drafts. Fix 6 conforms to constitution IX and both private mirrors match the reviewed plan and tasks. Read-only Spec Kit analysis found no outstanding issues. See [CONCLUSIONS.md](CONCLUSIONS.md) for the verification receipt. Owner Gate 1 is pending the user merge of the Spec Kit PR.
