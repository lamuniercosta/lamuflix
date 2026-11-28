# Feature Specification: HTTP client outbound address guard and architecture rules

**Feature Branch**: `feature/550-spec`

**Created**: 2026-11-28

**Status**: gate1: provisional

**Input**: Ticket DEV-550 (size M); grill-closed `brief.md` (Q1–Q6); `CONCLUSIONS.md`; Maestri notes `DEV-550`, `recon-DEV-550`; `specs/PRODUCT.md`. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Patron decisions / Q6; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR. Compose and home-cluster provider reachability receipts remain Phase B / delivery acceptance work.

## Clarifications

### Session 2026-11-28

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Shared registration path (`ConfigureHttpClientDefaults`, configure-not-replace, OpenSubtitles `SocketsHttpHandler` with `AllowAutoRedirect = false`), mandatory and stricter OpenSubtitles address sets with IPv4-mapped normalization, transport constraints (`UseProxy = false`, HTTP/2 ceiling), DI discovery plus negative control, architecture namespace segments and EF exceptions, test-only override location and file envelope, closing bar and ordering are settled in `brief.md` Q1–Q6 and `CONCLUSIONS.md`. No taste assumptions. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

Nothing visible: this ticket hardens outbound HTTP connect-time address checks and adds architecture enforcement tests. Library browse, playback, subtitles UI, enrichment, and SPA behaviour are unchanged for the operator-facing screens.

## Scope

**In scope** (`brief.md` Baseline and scope / Patron decisions Q1–Q6 / File envelope / Verification / Task order; ticket Wanted 1–3 and Done when; `recon-DEV-550` lines 10–16 and 32–37):

1. **Shared outbound connect-time guard**: One idempotent internal Infrastructure registration via `ConfigureHttpClientDefaults`, invoked from each of the three existing client registration extensions as the Q1 installation points, so every named and typed factory HTTP client registration in each participating production service composition carries the shared guard — including any registration outside those three extensions — without editing host composition. Configure the effective `SocketsHttpHandler.ConnectCallback` without replacing the handler. Switch OpenSubtitles to `SocketsHttpHandler` and keep `AllowAutoRedirect = false`. Preserve OMDb quota, current resilience, and timeouts. Built-in APIs only; no new dependency. Aligns with Q3, FR-007, and dynamic DI discovery.
2. **Dial-time address policy**: One internal address table with a mandatory deny set for all clients and a stricter set for the entire OpenSubtitles client. Normalize IPv4-mapped IPv6 before matching. Resolve once per connection attempt, validate candidates, dial only a validated exact `IPEndPoint`. Denied addresses never reach the connector. Connect failure reason carries no secrets or query strings. Spec and delivery PR body both list the ranges and cite IANA IPv4/IPv6 Special-Purpose Address Registries, AWS EC2 IMDS, and Alibaba Cloud ECS metadata. Configured OMDb, TMDb, and Ollama may still reach private or loopback destinations subject to the mandatory set.
3. **Guarded transports and proofs**: Direct transport with `UseProxy = false` and a hard ceiling of HTTP/2 so every supported connection path runs the address check. Production DI coverage discovers factory registrations dynamically, inspects effective primary handlers for the shared callback (and subtitle redirect setting), and includes a negative control with an unguarded effective primary handler. Request-level verification uses controlled resolution and connection seams. Starting coverage is the four individually listed providers (OMDb, TMDb, Ollama, OpenSubtitles) plus dynamic discovery — not a fixed registration count.
4. **Architecture rules**: Exact namespace segments `Util`, `Utils`, `Utility`, `Common`, `Helper`, `Helpers`, and `Misc` forbidden across production application assemblies (ordinary partial-name matches remain valid). EF Core dependencies, including method-body references, allowed only under `LamuFlix.Infrastructure.Persistence` and its subnamespaces, with exactly two allow-list entries by exact full type name: `LamuFlix.Infrastructure.Enrichment.OutboxRelayHostedService` and the Api entry-point `Program` type. NetArchTest at type granularity. Existing ArchitectureTests and Valid/Violating fixture conventions; include a fixture whose EF dependency occurs only inside a method body.
5. **Test override and file boundary**: Default-false internal Infrastructure guard option set only in test service composition; no configuration binding or deployment key. Existing subtitle integration tests may opt in narrowly when they need a guarded local endpoint, and must still prove the mandatory deny set holds with the flag enabled. Ticket-required guard, request behaviour, and DI coverage stay under `tests/LamuFlix.IntegrationTests/`. The two FsCheck address-policy properties live under `tests/LamuFlix.UnitTests/` (constitution IX; `brief.md` Q5 amendment / `CONCLUSIONS.md` analyze round 1 item 1). `InternalsVisibleTo` for `LamuFlix.IntegrationTests` is in scope if needed for integration coverage; for `LamuFlix.UnitTests` only if the properties need access to the internal policy.
6. **Verification and closing bar**: Green applicable C# gates on the exact delivery head; property checks of address-policy invariants in UnitTests; mutation under existing harness policy; separate redacted Compose and home-cluster reachability receipts for OMDb, TMDb, Ollama, and OpenSubtitles after verification. Size M three-axis review through Low; two review rounds and at most two fix commits per round; exact-head gate evidence must be preserved or re-proven after any review fixes. Delivery PR body publishes the approved ranges and source citations. Serialize with DEV-551 (shared subtitle registration file). No ADR. No Gherkin.

**Mandatory deny set (every client)** — listed here per Q2/Q3 documentation ruling:

- `169.254.0.0/16` (includes `169.254.169.254`)
- `fe80::/10`
- `100.100.100.200`
- `fd00:ec2::254`
- `0.0.0.0/8`
- `::/128`
- `224.0.0.0/4`
- `ff00::/8`
- `255.255.255.255`

**Additional OpenSubtitles denies (entire client; test flag relaxes only this set)**:

- `127.0.0.0/8`
- `::1`
- `10.0.0.0/8`
- `172.16.0.0/12`
- `192.168.0.0/16`
- `100.64.0.0/10`
- `fc00::/7`
- `198.18.0.0/15`

Basis citations for the ranges: [RFC 6890](https://www.rfc-editor.org/rfc/rfc6890) · [IANA IPv4 Special-Purpose Address Registry](https://www.iana.org/assignments/iana-ipv4-special-registry) · [IANA IPv6 Special-Purpose Address Registry](https://www.iana.org/assignments/iana-ipv6-special-registry) · [AWS EC2 Instance Metadata Service](https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/configuring-instance-metadata-options.html) · [Alibaba Cloud ECS instance metadata](https://www.alibabacloud.com/help/en/ecs/user-guide/view-instance-metadata/)

**Out of scope**:

- Egress proxies or network policies.
- Relocating `OutboxRelayHostedService` or the Api `Migrate` call into Persistence.
- Follow-up provider clients beyond the shared defaults path (future tickets inherit the guard via defaults).
- New NuGet/npm dependency; new project, top-level folder, or architectural layer; database schema; public API/OpenAPI/TypeScript shape; configuration keys or deployment edits; `Features:LocalPlay`, secrets hardcoding, or `Process.Start`.
- Editing host `Program.cs` or rewriting existing files outside the approved file envelope without a cited Patron ruling.
- ADR or Gherkin deliverable.
- Anything else is a follow-up issue, not a finding in this round.

**Precedence**: Ticket Wanted / Done when / Where it lands / Needs are approved authority (`PRODUCT.md` §3). Brief Q1–Q6 and `CONCLUSIONS.md` govern plan boundaries. Only Keel declares PLAN FROZEN after independent reread. Inventory clarification in `CONCLUSIONS.md` Q6 supersedes the recon headline registration count of five: planning uses the four individually listed typed providers plus dynamic DI discovery.

**Dependencies**: Serialize with DEV-551 (either order). Recheck `SubtitleProviderServiceCollectionExtensions.cs` at pickup.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Outbound connect-time guard**
Shared dial-time check installed once for factory-created HTTP clients so the address resolved for a connection is validated before any socket is opened.
_Avoid_: hostname-only allowlists as a substitute for address checks; replacing the primary handler in a way that drops the callback

**Mandatory deny set**
Addresses every client must refuse, including link-local and documented cloud-metadata endpoints, after IPv4-mapped IPv6 normalization.
_Avoid_: treating configured provider private/loopback reachability as exempt from this set

**OpenSubtitles stricter set**
Mandatory set plus loopback and private ranges applied to the entire OpenSubtitles client; the test-only option may relax only this stricter set.
_Avoid_: binding the override from configuration or deployment; relaxing the mandatory set

**Dynamic DI discovery**
Enumerating named and typed clients from production service composition and inspecting their effective primary handlers, rather than asserting a fixed registration count.
_Avoid_: treating recon’s raw tally of five as an acceptance criterion

Consumed vocabulary (already in use; not redefined): typed HTTP client, `SocketsHttpHandler.ConnectCallback`, `ConfigureHttpClientDefaults`, NetArchTest, `LamuFlix.Infrastructure.Persistence`, Compose, home cluster.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Shared outbound guard on every factory HTTP client (Priority: P1) 🎯 MVP

Operators running LamuFlix get one shared connect-time guard on every named and typed factory HTTP client registration in each participating production service composition — including registrations outside the three Infrastructure extensions — installed via those three extensions as the Q1 install points, without host composition edits.

**Why this priority**: Without the shared registration and transport constraints, address policy and proofs have nothing to enforce consistently.

**Independent Test**: Production DI composition discovers each named/typed factory client in each participating production service composition independently of the guard-registration helper (not only the three extension call sites), requires nonempty discovered coverage for each confirmed composition, asserts the effective primary handler carries the shared callback (and OpenSubtitles keeps `AllowAutoRedirect = false`), proves repeated installation through the three install points applies the shared defaults once while the effective subtitle handler retains its callback and redirect setting, and runs the same discovery and inspection path against an added unguarded factory registration that must be found and fail — aligning with Q3, FR-007, and T007 dynamic discovery.

**Acceptance Scenarios**:

1. **Given** a participating production service composition (starting providers registered through the three existing Infrastructure extensions as install points), **When** DI coverage discovers every named and typed factory registration in that composition independently of the guard-registration helper — including any registration outside those three extensions — and inspects effective primary handlers, **Then** each confirmed composition yields nonempty discovered coverage, each discovered client’s effective primary handler carries the shared connect callback on a `SocketsHttpHandler`, and OpenSubtitles retains `AllowAutoRedirect = false`.
2. **Given** an effective primary handler without the shared callback, **When** the same independent discovery and inspection path runs as a negative control, **Then** the check fails.
3. **Given** an added factory registration that does not receive the shared defaults, **When** the same independent discovery and inspection path runs, **Then** the coverage finds that registration and fails the guard check without updating a fixed client list.
4. **Given** OMDb, TMDb, and Ollama registrations, **When** the guard is installed, **Then** OMDb quota and the existing resilience and timeout settings for OMDb, TMDb, and Ollama remain in place.
5. **Given** repeated invocation of the three install points on one service collection, **When** DI coverage inspects the effective handlers, **Then** the shared defaults apply once and the effective OpenSubtitles handler still carries the shared callback with `AllowAutoRedirect = false`.

---

### User Story 2 - Dial-time address policy blocks metadata and unsafe subtitle targets (Priority: P1)

Outbound connections refuse mandatory-denied addresses on every client; OpenSubtitles also refuses private and loopback targets unless the test-only override relaxes only that stricter set.

**Why this priority**: Ticket Done when requires refusal of `169.254.169.254` on every client and private subtitle download refusal with public allowance.

**Independent Test**: Controlled resolver and connector seams show mandatory denial before connection, subtitle private denial and public allowance, configured provider private/loopback reach under the mandatory rules, IPv4-mapped and mixed-candidate cases, allowed-candidate fallback within one resolution, resolver fail-closed cases, controlled followed-redirect refusal, independent boundary/singleton classification examples, and scoped test-flag behaviour — without live provider traffic.

**Acceptance Scenarios**:

1. **Given** any discovered guarded client and a resolution to `169.254.169.254` (or another mandatory-denied address) with no allowed candidate, **When** a connection is attempted, **Then** the guard reports a connect failure, logs a denial reason category, and the captured log omits request URLs, query values, and secret-header values.
2. **Given** the OpenSubtitles client and a subtitle download link that resolves to a private address in the stricter set, **When** a connection is attempted with the test override off, **Then** the connection is refused before dial.
3. **Given** the OpenSubtitles client and a resolution to an allowed public address, **When** a connection is attempted, **Then** the validated exact `IPEndPoint` reaches the controlled connector.
4. **Given** configured OMDb, TMDb, or Ollama destinations that are private or loopback but outside the mandatory deny set, **When** a connection is attempted, **Then** those destinations remain reachable subject to the mandatory rules.
5. **Given** IPv4-mapped IPv6 candidates or mixed allowed/denied candidates, **When** the guard resolves once and validates, **Then** mapping happens before matching, denied candidates never dial, and only a validated endpoint is dialed.
6. **Given** the test-only override enabled for a subtitle local-endpoint case, **When** policy is evaluated, **Then** only the stricter OpenSubtitles set is relaxed and the mandatory set still denies.
7. **Given** one resolution result containing more than one validated allowed candidate, **When** the first allowed dial fails under the existing timeout and cancellation budget, **Then** the guard may fall back to another allowed candidate from that same result, every connector invocation still receives a validated exact endpoint, and denied candidates never dial.
8. **Given** a resolver exception, empty result, or (when the seam can represent it) null result, **When** a connection is attempted, **Then** the guard fails closed with zero connector calls and a redacted denial reason.
9. **Given** a guarded client that follows redirects and a controlled response whose next hop resolves to a denied address, **When** the followed connection is attempted, **Then** the hop is refused before connect; any explicit OpenSubtitles follow path confirmed at pickup issues through the guarded client and likewise refuses a private `Location`.

---

### User Story 3 - Architecture rules for helper namespaces and EF placement (Priority: P2)

The repository enforces the helper-namespace ban and Persistence-only EF Core placement already followed in practice, with exact exceptions for the outbox relay and Api `Program` migration call.

**Why this priority**: Wanted 3 and Done when require both rules to pass on production assemblies and fail on violating fixtures; the stream is independent of the guard work.

**Independent Test**: Both NetArchTest rules pass on production assemblies. Valid fixtures pass. Violating fixtures fail, including a method-body-only EF violation. Fixtures prove ordinary partial namespace names stay legal and that EF exception matching is exact by full type name. Fixture assertions stay independent of the production pass.

**Acceptance Scenarios**:

1. **Given** production application assemblies, **When** the namespace rule runs, **Then** exact namespace segments `Util`, `Utils`, `Utility`, `Common`, `Helper`, `Helpers`, and `Misc` are absent, while ordinary partial-name matches remain legal.
2. **Given** production application assemblies, **When** the EF rule runs, **Then** EF Core dependencies outside `LamuFlix.Infrastructure.Persistence` and its subnamespaces fail except for the exact full type names of `OutboxRelayHostedService` and Api `Program`.
3. **Given** Valid fixtures (including partial-name namespace cases that must remain legal) and Violating fixtures (including one whose EF dependency occurs only inside a method body, plus cases that prove EF allow-list matching is exact by full type name), **When** the rules run against those fixtures, **Then** each Valid fixture passes, each Violating fixture fails, and fixture assertions stay independent of the production pass.

---

### Edge Cases

- Hostname allowlists that look safe but resolve to a mandatory-denied or OpenSubtitles-stricter address.
- IPv4-mapped IPv6 forms of denied IPv4 ranges, including mapped equivalents of approved boundary and singleton addresses.
- Mixed candidate lists where some addresses are allowed and some are denied; allowed-candidate fallback within one resolution when an earlier allowed dial fails.
- Independent expected examples for every approved range boundary and singleton address (including mapped forms where applicable), accounting for neighboring ranges in expected outcomes.
- Resolver exception, empty result, and null result (only if the selected seam can represent null) — all fail closed with zero dials.
- Controlled followed redirects to denied addresses on redirect-following clients, plus any pickup-confirmed explicit OpenSubtitles follow path.
- Proxy or HTTP/3 paths that would bypass `ConnectCallback` — guarded clients must not use them (`UseProxy = false`, HTTP/2 ceiling).
- Test override enabled while still required to refuse the mandatory deny set.
- Recon headline registration count of five versus four identified typed clients — acceptance uses dynamic discovery, not the headline count.
- Captured denial logs that must never include request URLs, query values, or secret-header values.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST install one shared outbound connect-time guard on every named and typed factory HTTP client registration in each participating production service composition, including registrations outside the three Infrastructure client registration extensions; installation uses `ConfigureHttpClientDefaults` via one idempotent internal extension invoked from each of those three extensions as the Q1 install points, without editing host composition files (aligns with Q3, FR-007, and T007 dynamic discovery).
- **FR-002**: System MUST configure the effective `SocketsHttpHandler.ConnectCallback` without replacing the primary handler; OpenSubtitles MUST use `SocketsHttpHandler` with `AllowAutoRedirect = false`; OMDb quota and the existing resilience and timeout settings for OMDb, TMDb, and Ollama MUST remain.
- **FR-003**: System MUST refuse the mandatory deny set on every client after normalizing IPv4-mapped IPv6 addresses, resolve once per connection attempt, validate candidates, and dial only a validated exact endpoint; denied addresses MUST never reach the connector; within one resolution result a failed allowed connection MAY fall back to another allowed candidate under the existing timeout and cancellation budget, and every connector invocation MUST receive its validated exact endpoint; resolver exceptions, empty results, and (when representable) null results MUST fail closed with zero connector calls; when no candidate is allowed the guard MUST report a connect failure and log a denial reason category, and captured logs MUST omit request URLs, query values, and secret-header values.
- **FR-004**: System MUST apply the additional OpenSubtitles deny set to the entire OpenSubtitles client; a default-false internal test-only option MAY relax only that stricter set when set explicitly in test service composition, with no configuration binding or deployment key.
- **FR-005**: System MUST keep configured OMDb, TMDb, and Ollama private or loopback reachability subject to the mandatory deny set.
- **FR-006**: Guarded clients MUST use direct transport with proxy use disabled and a hard ceiling of HTTP/2 so every supported connection path runs the address check.
- **FR-007**: Tests MUST discover factory registrations through production service composition independently of the guard-registration helper, require nonempty discovered coverage for each confirmed participating composition, assert the shared callback on effective primary handlers (and OpenSubtitles redirect setting), prove repeated installation through the three install points applies the shared defaults once while the effective subtitle handler retains its callback and redirect setting, run the same discovery and inspection path against an added unguarded factory registration that must be found and fail, and detect an unguarded new registration without updating a fixed list.
- **FR-008**: Tests MUST verify request-level denial and allowance with controlled resolution and connection seams for the four starting providers and discovered clients, including mandatory denial, subtitle private/public behaviour, configured-provider private/loopback reach, mapped IPv6, mixed candidates, allowed-candidate fallback within one resolution, resolver exception/empty/(nullable) null fail-closed cases, independent expected examples for every approved range boundary and singleton (with mapped equivalents where applicable), controlled followed-redirect refusal (plus any pickup-confirmed explicit subtitle follow path), scoped test flag, proxy/HTTP-version boundary, and captured denial logs that omit request URLs, query values, and secret-header values — without live provider requests for automated cases. Preserve existing request-path and retention assertions.
- **FR-009**: Property checks in `LamuFlix.UnitTests` MUST cover mandatory-deny invariance under the subtitle test override and equivalent address classification after IPv4 mapping, under existing harness thresholds (constitution IX; brief Q5 amendment).
- **FR-010**: ArchitectureTests MUST enforce the seven exact helper namespace segments and Persistence-only EF Core placement with exactly two exact full-type allow-list exceptions (the approved source types only), using the same predicates for production and independent fixture assertions, including Valid fixtures that prove ordinary partial namespace names stay legal and that EF exception matching is exact for the allowed types, a Violating near-match fixture whose EF dependency is outside Persistence with a full type name distinct from both allowed types, an independent method-body-only EF Violating fixture, and — when generated-artifact attribution is required — attribution only with exact origin evidence of an approved source type plus a nested or near-match Violating case for that attribution logic, using existing Valid/Violating conventions.
- **FR-011**: Delivery MUST obtain separate redacted Compose and home-cluster reachability receipts for OMDb, TMDb, Ollama, and OpenSubtitles; runtime acceptance remains pending until both receipts exist.
- **FR-012**: Work on `SubtitleProviderServiceCollectionExtensions.cs` MUST be serialized with DEV-551 (either order), with a shared-file recheck at pickup.

### Key Entities *(include if feature involves data)*

- **Address deny membership**: Classification of a candidate IP against the mandatory set and, for OpenSubtitles, the stricter set, after mapping.
- **Effective primary handler chain**: The resolved handler used by a named or typed client, inspected for the shared callback and subtitle redirect setting.
- **Architecture exception type**: Exact full type name allow-listed for EF placement (`OutboxRelayHostedService`, Api `Program`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Automated coverage shows a request targeting `169.254.169.254` (and other mandatory-denied addresses) is refused on every discovered guarded client before connection.
- **SC-002**: Automated coverage shows an OpenSubtitles download link that resolves to a private stricter-set address is refused, and one that resolves to an allowed public address is allowed.
- **SC-003**: DI coverage fails when a factory client’s effective primary handler lacks the shared guard, including a dedicated negative control and detection without a fixed client list.
- **SC-004**: Both architecture rules pass on production assemblies and fail on their violating fixtures, including the method-body-only EF case.
- **SC-005**: Applicable C# gates are green on the exact delivery head under existing harness policy (including property and mutation lanes with honest SKIPPED/N/A labels); web gates are scope-empty on the approved file envelope.
- **SC-006**: Separate redacted Compose and home-cluster receipts show OMDb, TMDb, Ollama, and OpenSubtitles remain reachable after the change.
- **SC-007**: Size M review closes every verified in-scope finding through Low within two rounds and at most two fix commits per round.

## Assumptions

- Grill Q1–Q6 and `CONCLUSIONS.md` are the complete decision set for inventable behaviour; Quill does not invent further structural or taste rulings.
- Recon’s four individually listed typed providers plus dynamic discovery are the planning inventory (`CONCLUSIONS.md` Q6 inventory clarification).
- Existing hostname checks such as `SubtitleRequestUriPolicy` remain; this ticket adds address-level connect-time enforcement rather than replacing hostname policy (ticket Problem / Wanted; brief does not remove hostname policy).
- No ASSUMPTIONS.md taste entries: brief Q6 records no taste assumptions.
