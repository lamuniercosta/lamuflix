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
