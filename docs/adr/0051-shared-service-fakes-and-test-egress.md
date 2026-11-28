---
status: proposed
---

# Share provider fixtures and constrain test HTTP egress

**Ticket** DEV-549

Integration tests, browser e2e, and developer launches need reproducible provider responses without live credentials. Share the response data across existing hosts, require fake endpoint wiring on fake launches, and enforce loopback at the .NET test transport so a missing stub or a redirect cannot send an ordinary integration test to a real provider.

## Shared fixture contract

Keep one scrubbed WireMock JSON mapping set per provider under `tests/fakes/<service>/`. Load the same files through thin readers in `LoopbackWireMock` and `web/e2e/stub.mjs`. Store every existing OMDb fixture there with a single copy. Share canonical TMDb and OpenSubtitles success responses and the existing canned Ollama `/api/embed` response. Keep existing host-local fault cases, malformed responses, and control routes local, subject to full OMDb consolidation. Scrub credentials, tokens, and account data from mappings and detached bodies before check-in. Canonical matchers exclude the listed credential-only query parameters in both readers and retain functional query constraints.

The Node reader supports method, path or `urlPath`, query parameters, status, headers, and `jsonBody` or `bodyFileName`. An unsupported matcher fails explicitly so a fixture cannot silently match a broader request. This lets both hosts reuse the same response data with their existing tooling. Enable TMDb and OpenSubtitles in browser e2e against the fixture host for browse, subtitle search, and one fake download. Web unit tests continue to use MSW. [Patron Q1, Q2, and Q7](../../specs/DEV-549/CONCLUSIONS.md)

## Developer and seat launches

The documented Compose command explicitly targets the application services in the `fakes` profile. Those services have distinct names and depend on the existing infrastructure and the shared Node fixture host. Only the fake API mounts the checked-in, secret-free `docker/api.settings.fakes.json` at `/app/appsettings.Production.json`. This keeps provider URLs and placeholder keys in mounted JSON under constitution Principle VII and isolates the git-ignored live settings. Before readiness, the fixture host validates the same required read-only JSON mounted by the fake API. It requires the expected fixture authority and path for every provider and loads all required nonempty mapping sets. A failure identifies the intended fixture host or provider and prevents the fake API from starting. The fake API takes provider settings from that JSON without provider environment overrides.

AppHost defaults `UseServiceFakes` on in Development. It starts the fixture resource through core `AddContainer`, using the same Node image and the same `stub.mjs` and `tests/fakes` bind mounts as e2e and Compose. The fake wiring supplies resource-derived provider URLs and non-secret placeholders only. This reuses the container host with the existing AppHost dependencies.

Both launch paths require a fake endpoint for every configured provider. Missing wiring fails setup or startup and names the affected host, preventing accidental live access while preserving typed options binding and request-time subtitle settings behavior. The fake launch paths are confined to development and seat use. Normal Compose, non-fake AppHost, and Kubernetes keep their baseline wiring. [Patron Q3 and Q10](../../specs/DEV-549/CONCLUSIONS.md)

## Integration-test transport and live searches

Use one guard in `tests/LamuFlix.IntegrationTests` for all named and typed `IHttpClientFactory` clients through test-host overrides and for directly constructed adapter clients through a shared helper. Ordinary requests may go only to `localhost` or an IP literal accepted by `IPAddress.IsLoopback`, without DNS resolution. A denial names the host and occurs before the transport sends.

A primary handler that follows redirects automatically can bypass the guard. Disable automatic redirects on guarded clients and resolve and check every relative or absolute `Location` through the same pipeline before sending. Preserve POST-to-GET for 301/302/303 and the original method for 307/308, strip Authorization when scheme, normalized host or effective port changes, refuse HTTPS downgrades, and fail on exceeding the five-hop limit. This policy applies to the .NET test clients.

One bounded `Live` search per OMDb, TMDb, and OpenSubtitles independently opts in through its provider-specific `LAMUFLIX_TEST_*` process environment key. Each smoke owns and disposes a fresh client with an immutable allowance for its provider host, isolating it from concurrently running ordinary tests. With no key, the case skips and names only the variable. CI receives no keys and discovers those skips through its existing workflow. Keep credentials out of tracked files, fixtures, logs, and test output. OpenSubtitles uses only its API key for search. [Patron Q4, Q5, and Q9](../../specs/DEV-549/CONCLUSIONS.md)

## Consequences

Both readers track the same mapping subset. Recordings can drift as providers change. Use the README workflow to re-record with a local test key, scrub the mapping and detached bodies, and verify the result in both hosts.

Fake and live Compose variants reuse infrastructure and share host ports. Operators run one variant at a time and account for shared development state. The bounded redirect follower adds test-only maintenance for method and credential rules. Live searches use a per-client exception and require local keys.

The canned embed response supports e2e and fake mode. [ADR-0012](ADR-0012.md) governs default embedding tests. [ADR-0025](ADR-0025-isolated-ollama-test.md) governs the separate real-model opt-in on the aligned Ollama pin.

## Alternatives considered

- A shared project or package adds a dependency for files both hosts can already read.
- A separate WireMock process for Node duplicates hosting and complicates e2e controls.
- Aspire JavaScript hosting adds a package when the core container API can reuse the Node host.
- A primary-handler connection hook moves enforcement below the per-hop request pipeline used to verify denied sends and credential handling.
- CI-injected keys would make default CI depend on external credentials and provider availability.

Decision authority: specs/DEV-549/brief.md and specs/DEV-549/CONCLUSIONS.md, including the Step 6 ruling. The ADR is proposed until the Build PR delivers the design.
