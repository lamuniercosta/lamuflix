# Feature Specification: ServiceDefaults (Serilog, OpenTelemetry OTLP, Health Checks, TimeProvider)

**Feature Branch**: `feature/307-spec`

**Created**: 2026-10-02

**Status**: gate1: provisional - Gate 1 closed pending the owner answers to the Q1 and Q2 structural checkboxes (see `CONCLUSIONS.md`) and a further four `needs decision:` questions this drafting pass raised (see `DRAFTING_RECEIPT.md` §Escalations and `plan.md` §8)

**Input**: DEV-307 (parent DEV-284, size M, UI false) - "Implement shared hosting defaults in `src/LamuFlix.ServiceDefaults/Extensions.cs` to standardise logging, telemetry, health probes, and time management." Scope 1 Serilog console JSON with an OTLP exporter for OpenTelemetry logs; Scope 2 OpenTelemetry traces and metrics with the OTLP exporter at `http://localhost:4317` or `OTEL_EXPORTER_OTLP_ENDPOINT`, standard instrumentation for ASP.NET Core, HttpClient, Npgsql and RabbitMQ, `ActivitySource` "LamuFlix"; Scope 3 `/health/live` and `/health/ready`; Scope 4 `TimeProvider.System` as a singleton. Plus the ticket's Decision dated 2026-09-28 and its Acceptance addition on `TracingDecorator`. Plus Phase A grill outcome: `brief.md`, `CONCLUSIONS.md` (Q1, Q2, Decisions A and B), `ASSUMPTIONS.md`, note `DEV-307` recon summary, `specs/PRODUCT.md`, `.specify/memory/constitution.md`

**Short name**: `service-defaults`

**Two structural questions are open and this specification does not answer them.** The exact package set and pins (Q1) and the health response body contract with the Degraded mapping (Q2) are owner checkboxes carried on the spec PR, per `CONCLUSIONS.md`. Every requirement below that depends on one of them is marked `[Q1-pending]` or `[Q2-pending]` and states only what is already decided. Nothing in this document pre-empts a ruling, and no gap was filled with an assumption.

## User Scenarios & Testing

### User Story 1 - Structured logs a machine can read, on the console and on the collector (Priority: P1)

As someone diagnosing a running API, I need every log line to be structured JSON rather than formatted text, written to the console where the container runtime captures it and forwarded to the telemetry collector, so that a log search, a trace and a metric all describe the same event with the same fields.

**Why this priority**: Scope 1 is the ticket's first listed item and the one every other story's evidence depends on - a trace or a metric that names a failure nobody can find in the logs is not diagnosis. It is also the smallest story that can be delivered and proved on its own, before any exporter, probe or span change exists.

**Independent Test**: Start the API, log through the normal abstraction, read the console output, and resolve the log service and its OTLP exporter through the built service provider. The console line is a JSON object with the message and its properties as fields rather than one interpolated string; the exporter is registered and the collector endpoint is the configured one. No collector needs to be reachable for the assertion to hold.

**Acceptance Scenarios**:

1. **Given** a running API, **When** anything is logged through the logging abstraction, **Then** the console receives one JSON object per event, with the message and each structured property as its own field, and no property is flattened into the message text.
2. **Given** a running API, **When** the logging pipeline is composed, **Then** Serilog is the logger the host uses and is configured exactly once, so no event is written twice.
3. **Given** a running API, **When** a log event is raised, **Then** the same event is forwarded to the OpenTelemetry logging provider and exported over OTLP, rather than through a second, separate OTLP logging sink.
4. **Given** a running API, **When** no collector is listening, **Then** logging still succeeds and the console output is unaffected; the exporter failing does not fail the host or the request.
5. **Given** the logging configuration, **When** it is read, **Then** there is no second Serilog OTLP sink and no Seq sink, neither of which the ticket asked for.

---

### User Story 2 - One trace and one set of metrics that cross every boundary (Priority: P1)

As someone diagnosing the asynchronous pipeline, I need the request that triggers enrichment, the message it publishes, the worker that consumes it and the metadata-provider call it makes to appear in one trace, with the named instrumentation already attached, so that I can find where a slow enrichment went wrong without correlating four unrelated timelines by timestamp.

**Why this priority**: Constitution VI makes a single trace across API, broker, worker and provider non-negotiable, and Scope 2 is what turns the existing trace-propagation code into an actual exported trace. It sits above the probes because a probe that reports Unhealthy tells you something is wrong, while this story tells you what.

**Independent Test**: Build the host and read the composed providers from the service provider: the tracer provider exists, the meter provider exists, the OTLP exporter is attached to all three signals, and every instrumentation the ticket names is registered. Then start one handler span under a listener and confirm it is recorded under the application's own source name. No collector needs to be listening.

**Acceptance Scenarios**:

1. **Given** a running API, **When** the telemetry pipeline is composed, **Then** traces and metrics are exported over OTLP to a collector, and the endpoint defaults to `http://localhost:4317`.
2. **Given** a running API with `OTEL_EXPORTER_OTLP_ENDPOINT` set, **When** the telemetry pipeline is composed, **Then** the exporter uses that endpoint, through the standard OpenTelemetry environment variable rather than a configuration key invented for this project.
3. **Given** a running API, **When** the tracer provider is read, **Then** instrumentation is registered for ASP.NET Core, HttpClient, Npgsql, the RabbitMQ client's own publisher and subscriber activity sources, and the application's own activity source.
4. **Given** a running API, **When** the application starts a span, **Then** the span is recorded under the application's source name and the meter provider records against the application's meter, and neither name appears as a string literal anywhere in the wiring.
5. **Given** a running API, **When** the RabbitMQ client publishes and consumes, **Then** those activities are collected through the client's own built-in activity sources, with no separate RabbitMQ instrumentation package in the dependency graph.
6. **Given** the dependency graph, **When** it is read, **Then** the existing OpenTelemetry and Npgsql versions are unchanged; this work adds packages alongside them rather than moving them.
7. **Given** a running API, **When** a log, a span and a metric are produced by one request, **Then** all three are exported over the same OTLP endpoint and describe the same operation.

---

### User Story 3 - A liveness probe that answers "is this process up?" and nothing more (Priority: P1)

As whatever supervises the container, I need a liveness endpoint that answers without touching any dependency, so that a slow or unavailable database restarts nothing and a failing dependency is reported by readiness rather than mistaken for a dead process.

**Why this priority**: AC1 makes it the ticket's first acceptance criterion and it is the cheapest story to prove - no checks run, no dependency can fail it. It comes before readiness because a readiness endpoint is only meaningful once there is a liveness endpoint to complement.

**Independent Test**: Call the liveness route on a running API while every dependency is deliberately broken, and call it on an API whose health checks are all replaced by failing stubs. It answers 200 in both cases. Nothing about a dependency can change the answer.

**Acceptance Scenarios**:

1. **Given** a running API, **When** the liveness route is requested, **Then** it answers 200.
2. **Given** a running API whose database and broker are both unreachable, **When** the liveness route is requested, **Then** it still answers 200, because it runs no dependency check at all.
3. **Given** a running API whose every registered health check is failing, **When** the liveness route is requested, **Then** it still answers 200.
4. **Given** a running API, **When** the liveness route is requested without authentication, **Then** it is served, because the route is anonymous.
5. **Given** the two health routes, **When** the host's endpoints are mapped, **Then** both are mapped exactly once, by the shared defaults, and neither host duplicates the mapping.

---

### User Story 4 - A readiness probe that reflects only the two dependencies that gate traffic (Priority: P1)

As whatever decides whether to send traffic to this instance, I need a readiness endpoint whose answer depends on the database connection and the broker channel and on nothing else, so that an instance which cannot serve is pulled out of rotation, and an instance which can serve is not pulled out because an unrelated optional integration is unhappy.

**Why this priority**: AC2 makes it the ticket's second acceptance criterion, and it is the story where the "only these two dependencies" boundary has real teeth - the metadata provider is registered today and is currently part of readiness, which is the one behavioural change this ticket makes to existing wiring.

**Independent Test**: Replace the registered checks with stubs and call the readiness route in four states - all healthy, database failing, broker failing, metadata provider failing - and read each answer. It is 200 in the first and last cases and 503 in the middle two, and the failing case never names the metadata provider at all.

**Acceptance Scenarios**:

1. **Given** a running API whose database and broker checks both report healthy, **When** the readiness route is requested, **Then** it answers 200.
2. **Given** a running API whose PostgreSQL connection check fails, **When** the readiness route is requested, **Then** it answers 503.
3. **Given** a running API whose RabbitMQ channel check fails, **When** the readiness route is requested, **Then** it answers 503.
4. **Given** a running API whose metadata-provider check fails, **When** the readiness route is requested, **Then** it still answers as though that check were absent, because readiness covers the database and the broker only. The check stays registered and still runs for unfiltered health reporting.
5. **Given** a running API, **When** the readiness route is requested without authentication, **Then** it is served, because the route is anonymous.
6. **Given** a running API, **When** the readiness route is requested, **Then** only the two checks tagged for readiness run; a check registered without that tag contributes nothing to the answer.
7. **Given** a readiness route answer of 503, **When** the response is read, **Then** it is an RFC 7807 problem document carrying the same trace identifier every other error response carries, so a report can be tied to the request that produced it.
8. **Given** a readiness route answer of 503, **When** the response body is inspected field by field, **Then** it contains no exception message, no check description and no connection string, host, port or other machine detail, because those are secrets and internal detail and a probe is reachable without authentication.
9. **Given** a running API, **When** the readiness route is requested, **Then** the same writer produces its body as every other error response in the application, rather than a hand-rolled shape that could drift from it.

---

### User Story 5 - A rejected request is not a server fault (Priority: P1)

As someone reading the error rate, I need a request that failed because the input was invalid to be visible as a client mistake rather than counted as an error, so that a spike in invalid input from one client does not read as an outage and does not page anyone.

**Why this priority**: The ticket's Decision dated 2026-09-28 and its Acceptance addition name this file and this behaviour explicitly, so it is decided ticket scope rather than an open choice. It is placed after the transport stories because it changes what the traces from Story 2 mean, not whether they exist.

**Independent Test**: Run one handler under a listener and throw each of the two exception kinds in turn. The validation failure leaves the span's status unset and records a handler outcome of validation failed; any other exception sets the status to error and records the exception's type. Read both back from the recorded span.

**Acceptance Scenarios**:

1. **Given** a handler span and an expected validation failure, **When** the exception is caught, **Then** the span's status is left unset and an outcome attribute naming a validation failure is set on it.
2. **Given** a handler span and any other exception, **When** the exception is caught, **Then** the span's status is set to error and an error-type attribute naming the exception's type is set on it.
3. **Given** a handler span and an expected validation failure, **When** the exception is caught, **Then** the exception is still rethrown unchanged, so the existing HTTP mapping to 422 is unaffected.
4. **Given** a handler span and any other exception, **When** the exception is caught, **Then** the exception is still rethrown unchanged.
5. **Given** the recorded type on a non-validation exception, **When** it is read, **Then** it is the exception type's full name, so two exceptions with the same short name in different namespaces do not collapse into one value.
6. **Given** both exception kinds, **When** the tests run, **Then** both paths are proved by a test each, and a change that made the two behave identically fails the suite.
7. **Given** the tracing decorator, **When** its outcome and type attribute keys are read, **Then** they come from the one constants holder that already holds this decorator's other keys, not from literals written at the call sites.

---

### User Story 6 - Time comes from one injected source (Priority: P2)

As someone writing a test about an expiry or a lease, I need the clock to be a resolvable dependency I can substitute, so that the code under test never reads the machine's wall clock and a test never has to wait for real time to pass.

**Why this priority**: Scope 4 is the smallest item in the ticket, and the two call sites that register the clock already exist in Infrastructure, so this story is about who owns the registration rather than about adding capability. It is last of the ticket's four scopes because nothing else in the ticket depends on it.

**Independent Test**: Build the host through the shared defaults and resolve the clock from the service provider, then check which registration owns it. It resolves to the system clock, and the owner is the shared defaults rather than one of the two per-feature registrations that exist today.

**Acceptance Scenarios**:

1. **Given** a host built through the shared defaults, **When** the clock is resolved from the service provider, **Then** exactly one registration satisfies it and it yields the system clock.
2. **Given** a host built through the shared defaults and then through the persistence and metadata-provider registrations, **When** the clock is resolved, **Then** it still resolves to one instance, not three competing registrations of which one wins is an accident of call order.
3. **Given** a test that substitutes its own clock, **When** it resolves the clock, **Then** the substitution takes effect, because the registration does not overwrite an existing one.
4. **Given** the wiring, **When** no code is searched for the banned wall-clock members, **Then** none is introduced by this work, and existing timestamps keep coming from the injected clock.

---

### User Story 7 - Evidence that the change did what it claimed and nothing more (Priority: P2)

As the reviewer of this change, I need the observable claims - that telemetry really is exported, that both probes really answer the right codes, that the two span paths really differ - proved by tests, and the gates and boundaries recorded, so that this can be merged on evidence rather than on the fact that the wiring looks right.

**Why this priority**: These are the ticket's own acceptance criteria plus the pipeline's standing gates, and they are collected last so each story's own test is already in place. None of them changes what the ticket delivers.

**Acceptance Scenarios**:

1. **Given** the built host, **When** the service provider is read, **Then** the trace, metric and log providers and the OTLP exporter on each of them are present, so that registration is asserted rather than assumed; the test does not require a reachable collector.
2. **Given** a built host with all health checks replaced by stubs, **When** both routes are called in each readiness state, **Then** every code in AC1 and AC2 is observed, with no test depending on a container.
3. **Given** a 503 readiness answer, **When** the problem document is inspected, **Then** it carries the trace identifier, carries the check list, and carries no exception text - the last is asserted by looking for the absence, not merely by not checking.
4. **Given** the changed C# files, **When** the three static-analysis gates and the format check run, **Then** each exits zero, with complexity additionally run at the tighter post-implementation threshold; a gate that could not run is reported as such and never folded into a passing verdict.
5. **Given** the changed files, **When** the diff is inspected against the recorded baseline, **Then** no file outside the agreed list changed, no database schema changed, no secret or machine path was introduced, and no gate threshold moved.
6. **Given** the dependency graph, **When** the vulnerable-packages gate runs, **Then** it is run and its exit code recorded, because this work adds packages.

### Edge Cases

- **A probe that cannot run its checks because a dependency is down**: the readiness route answers 503 with a problem document, and the liveness route still answers 200, so a dependency outage restarts nothing. This split is the whole reason the two routes exist.
- **A failing check whose exception carries a connection string**: the check reports an unhealthy result, and the response body carries the check's name and status and nothing else. A probe is reachable without authentication, so a connection string in a body is a disclosure.
- **A check that throws rather than returning a result**: the health-check framework treats it as a failure; the writer does not distinguish, and nothing from the thrown exception reaches the body.
- **A readiness aggregate that is neither fully healthy nor fully unhealthy**: see the escalation on the Degraded mapping. Under the ticket's decided membership, no registered readiness check can currently report that state, which is why this edge case is carried as an owner question rather than settled here.
- **The metadata provider failing while the database and broker are fine**: readiness answers as though the metadata provider were not registered, because it is not one of the two dependencies that gate traffic. An invalid provider key therefore no longer takes the instance out of rotation.
- **A metadata-provider check with no readiness tag at all**: it still runs for any unfiltered health reporting, so removing the tag removes it from readiness without disabling it.
- **A host that calls the shared defaults twice**: logging and telemetry are configured once, so no event is written twice and no provider is registered twice.
- **A collector that is not listening**: telemetry export fails and is swallowed by the exporter's own retry policy; the host starts, requests are served, and logging to the console is unaffected. Readiness is not made to depend on the collector.
- **A request whose span never starts, because nothing is listening for the source**: the tracing decorator's existing null-safe calls still apply, and no attribute call throws. Both exception paths behave identically when there is no listener, which is why the tests install one.
- **An exception type whose short name is not unique across the assembly**: the full name distinguishes them, so the recorded values do not merge.
- **A validation failure that is genuinely a server fault**: this story cannot tell the two apart, because the ticket defines the split by exception type and not by cause. A validation exception thrown by a genuine internal inconsistency is recorded as a client mistake; that is the ticket's decision, and the error-rate reading it produces is the consequence.
- **A health route called before startup has finished**: the readiness route reports the checks as not yet run, which is the framework's own behaviour; the liveness route is unaffected.
- **Both health routes mapped twice, once by the shared defaults and once by a host**: this would be a defect. The mapping lives in the shared defaults only, and a host that also maps them is a finding rather than a configuration to accept.
- **A machine with no collector and no containers at all**: the tests do not need either, because the checks are replaced by stubs and the exporter is asserted through the service provider rather than by a round trip.
- **An integration test that would need the API's entry point**: booting the real host needs the API project to be reachable from the test project, which today it is not. The entry point itself is already visible - .NET 10 source-generates the public `Program` for a top-level-statement project - so the project-reference gap is the whole of it, and it is carried as an escalation rather than assumed.

## Requirements

### Functional Requirements

- **FR-001** (Scope 1, AC3, US1): The shared defaults compose the logging pipeline exactly once per host. No host composes it a second time, and no event reaches the console twice.
- **FR-002** (Scope 1, US1): Console output is one structured JSON object per event, with the message and every structured property as separate fields. No property is interpolated into the message text.
- **FR-003** (Scope 1, AC3, US1): Log events reach the collector through the OpenTelemetry logging provider, which the Serilog configuration forwards to. There is no second Serilog OTLP sink and no Seq sink.
- **FR-004** (Scope 1, US1): A collector that is unreachable does not fail the host, fail a request, or affect console output. No telemetry-sink failure is allowed to become an application failure.
- **FR-005** (Scope 2, AC3, US2): Traces, metrics and logs are exported over OTLP. All three signals are configured, not just the two the ticket names in its Scope 2 heading, because AC3 names all three.
- **FR-006** (Scope 2, US2): The collector endpoint defaults to `http://localhost:4317` and is overridden by the standard OpenTelemetry environment variable. No project-specific configuration key is introduced for the endpoint.
- **FR-007** (Scope 2, US2): Instrumentation is registered for ASP.NET Core, HttpClient and Npgsql, and for the RabbitMQ client's own publisher and subscriber activity sources. No RabbitMQ instrumentation package enters the dependency graph; the client's built-in sources are collected instead.
- **FR-008** (Scope 2, US2): The application's activity source name and its meter name come from the existing constants holder that already holds this decorator's attribute keys and the enrichment span and metric names. No telemetry name is written as a literal at a call site.
- **FR-009** (Scope 2, US2): The existing OpenTelemetry and Npgsql package versions are unchanged. The work adds packages alongside them; it does not move a version the repository already chose.
- **FR-010** (Scope 3, AC1, US3): The liveness route exists, answers 200, and runs no dependency check. A failing or absent dependency cannot change its answer.
- **FR-011** (Scope 3, AC2, US4): The readiness route exists and answers 200 or 503 according to the PostgreSQL connection and the RabbitMQ channel, and to those two only.
- **FR-012** (Scope 3, Decision A, US4): The metadata-provider check loses its readiness tag and stays registered. It contributes nothing to the readiness answer and still runs wherever health checks run unfiltered.
- **FR-013** (Scope 3, constitution.md:232-234, US4): A PostgreSQL connection check is registered, in the persistence layer rather than in the shared defaults, because the shared defaults stay independent of this application's data access. It carries the readiness tag.
- **FR-014** (Scope 3, US3, US4): Both health routes are reachable without authentication, and both are mapped exactly once by the shared defaults. No host duplicates the mapping and no host maps either route itself.
- **FR-015** (Scope 3, constitution.md:196-212, US4): A readiness answer of 503 is an RFC 7807 problem document carrying the trace identifier that every other error response in the application carries, written through the same problem-writing path rather than a shape written for the probe alone.
- **FR-016** (Scope 3, constitution.md:243-249, US4): No exception message, no check description, no check data and no connection string, host or port appears in any health response. A probe is anonymous, so its body is treated as public.
- **FR-017** (Scope 3, `[Q2-pending]`, US4): The success and failure body contract for both health routes - which fields, which content type, which status mapping for a non-fully-healthy aggregate - is the Q2 owner checkbox and is **not decided by this document**. Until it is answered, the tasks that implement the response writer do not start, and this specification states only the trace-identifier requirement (FR-015) and the disclosure bar (FR-016), both of which come from the constitution rather than from Q2.
- **FR-018** (Scope 4, US6): The system clock is registered as a singleton by the shared defaults. Because the shared defaults run first, the clock has exactly one owner, and the two per-feature registrations that exist today stop being the deciding registration.
- **FR-019** (Scope 4, US6): The registration does not overwrite an existing one, so a test that substitutes its own clock still wins. Nothing in this work changes how existing code obtains time.
- **FR-020** (Decision 2026-09-28, AC4, US5): The tracing decorator catches an expected validation failure separately from every other exception. On a validation failure it leaves the span status unset and sets the handler-outcome attribute to the validation-failed value; on every other exception it sets the status to error and sets the error-type attribute.
- **FR-021** (Decision 2026-09-28, AC4, US5): Both paths rethrow the caught exception unchanged, so the existing HTTP mapping from validation failure to 422 and every other exception's mapping are unaffected.
- **FR-022** (Decision B, `[assumed]`, US5): The error-type value for a non-validation exception is the exception type's full name, not its short name. This is a logged taste assumption with its basis in `ASSUMPTIONS.md`, not a silent choice.
- **FR-023** (Decision 2026-09-28, AC4, US5): The new handler-outcome attribute key is added to the existing constants holder next to the keys the decorator already uses, and neither it nor the error-type key is written as a literal at the call site.
- **FR-024** (Acceptance addition, US5, US7): A test proves each of the two paths separately - validation failure leaves the status unset with the outcome set, and any other exception sets the error status with the type set - by reading the recorded span under an installed listener. A change that made the two paths behave identically fails the suite.
- **FR-025** (Acceptance addition, US7): Both span paths are proved against a real listener rather than by reading the code. The decorator's null-safe behaviour when no listener is installed is not the thing under test, so the tests install one.
- **FR-026** (AC1, AC2, US7): Both routes are exercised through a real host with the registered checks replaced by stubs, covering every code AC1 and AC2 name, with no test depending on a container or a collector.
- **FR-027** (AC3, US7): The existence of the trace, metric and log providers and of the OTLP exporter on each is asserted through the built service provider. Registration is asserted; a round trip to a collector is not, because no collector runs in continuous integration.
- **FR-028** (AC1, AC2, US4, US7): A 503 readiness response is asserted to carry the trace identifier and to carry no exception text. The absence of exception text is asserted as an inspection of the body, not merely by the absence of an assertion about it.
- **FR-029** (`[Q1-pending]`, US1, US2, US4, US7): The exact package set and the version of every pin are the Q1 owner checkbox and are **not decided by this document**. The specification constrains the graph only where the constitution and the ticket already do: the packages the ticket names must be present, the RabbitMQ instrumentation package must not be, the existing OpenTelemetry and Npgsql versions must not move (FR-009), and the pins follow the repository's central-package-management rule.
- **FR-030** (constitution.md:346-376, US7): Every changed C# file passes the analyzer, complexity and inspection gates and the format check, with complexity additionally run at the tighter post-implementation threshold. A gate that could not run is reported as such and is never folded into a passing verdict.
- **FR-031** (constitution.md:346-376, US7): The vulnerable-packages gate is run because this work adds packages, and its exit code is recorded.
- **FR-032** (US7): No database schema, no migration, no model snapshot, no port, no architectural layer, no secret, no machine-specific path and no gate threshold changes. Nothing outside the agreed file list changes, and no file the ticket does not name is deleted or rewritten.
- **FR-033** (US7): Existing options validation, and the two per-feature clock registrations, are left in place. The ticket retains the options validation the shared defaults already perform and does not restate or restructure it.
- **FR-034** (US7): The health routes are documented in the committed OpenAPI document. **Deferred to DEV-20 by the Q2 checkbox; not delivered here.** DEV-20, the Epic 6 web scaffold ticket, owns the C# to OpenAPI to TypeScript chain for both routes, and DEV-320 owns the generated TypeScript client from the committed `openapi.json` afterwards. **Reopening it for this ticket needs a new owner checkbox, not a re-ask of Q2.** The deferral is a charter 2.3b departure from `constitution.md:359-360` for this ticket only; the document cannot be built here without a new package and a new top-level folder (`PRODUCT.md:43-44`). No task in this ticket writes `openapi.json`, calls `AddOpenApi` or touches `types.ts` (tasks.md T048). See `DRAFTING_RECEIPT.md` §Escalations, question 1.
- **FR-035** (Scope 1, Scope 2, AC3, US2, US7): The OMDb API key never appears in an exported span tag or in a log record. The provider puts it in the request query (`OmdbMetadataProvider.cs:214-216`), and this work adds both the HttpClient instrumentation and the OTLP log export that carry `url.full` and the `IHttpClientFactory` request log line off the box, so the absence is **proved by a test** rather than assumed. `Instrumentation.Http` already redacts the `url` query by default on `net10.0` (recon R3), and the requirement stands anyway: the redaction opt-out and the `ILogger` path are separate from that default, and a test guards each.

### Key Entities

- **Shared defaults**: the one composition root both hosts call. It owns logging, telemetry, health-check registration, clock registration and the options validation that already lives there, and it is the only place the health routes are mapped. It stays independent of this application's data access.
- **Health probe pair**: the liveness route, which runs no check, and the readiness route, which runs only the checks tagged for readiness. Both are anonymous and both are mapped once.
- **Readiness membership**: the set of checks carrying the readiness tag. It is exactly the PostgreSQL connection and the RabbitMQ channel, and it is the boundary Decision A drew by removing the metadata provider from it without unregistering it.
- **Telemetry signal set**: traces, metrics and logs, each with an OTLP exporter pointed at one endpoint that defaults to `localhost:4317` and is overridable by the standard environment variable.
- **Activity source and meter**: the application's own instrumentation identity, whose name is held in the existing constants holder rather than written as a literal.
- **Handler span outcome**: the two-valued distinction the tracing decorator records - a validation failure, which leaves the span unset, and any other failure, which marks it an error and names the exception type.
- **Clock registration**: the single resolvable time dependency, owned by the shared defaults and substitutable by a test.
- **Problem document on a probe response**: the same error shape the rest of the application uses, carrying the trace identifier, so that a probe failure is correlatable and a probe's disclosure surface is the same as any other anonymous endpoint's.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Starting the API and logging one event produces exactly one JSON object on the console whose fields include the message and each structured property separately, and the same event is forwarded to the OpenTelemetry logging provider rather than to a second sink.
- **SC-002**: With no collector listening, the API starts, serves requests and logs to the console normally; 0 telemetry-sink failures surface as application failures.
- **SC-003**: The built host exposes a tracer provider, a meter provider and a log provider, each carrying an OTLP exporter, and the exporter resolves to `http://localhost:4317` by default and to `OTEL_EXPORTER_OTLP_ENDPOINT` when that is set.
- **SC-004**: Every instrumentation the ticket names is registered: ASP.NET Core, HttpClient, Npgsql, the RabbitMQ client's publisher and subscriber sources, and the application's own source; 0 telemetry names appear as string literals at the wiring sites, and 0 existing OpenTelemetry or Npgsql version moves.
- **SC-005**: The liveness route answers 200 in all three states tested - with dependencies healthy, with both dependencies unreachable, and with every registered health check replaced by a failing stub. 0 of the 3 states can change its answer.
- **SC-006**: The readiness route answers 200 when its two dependencies are healthy and 503 when either fails, and answers 200 when the metadata-provider check fails with both other dependencies healthy - so 1 of 4 probed states is the metadata provider and 0 of them change the answer.
- **SC-007**: A 503 readiness response is an RFC 7807 problem document carrying the trace identifier, and 0 of its fields contain an exception message, a check description, a connection string, a host or a port. The absence is asserted, not merely unchecked.
- **SC-008**: The metadata-provider check is still registered after this change and still runs wherever health checks run unfiltered; 0 checks were unregistered to achieve SC-006.
- **SC-009**: Both routes are mapped exactly once, by the shared defaults; 0 hosts map either route, and 0 duplicated mappings exist.
- **SC-010**: A validation failure leaves the handler span's status unset with the validation-failed outcome attribute set, and any other exception sets the error status with the full exception type name; each path is proved by its own test, so 0 changes can make the two identical without failing the suite.
- **SC-011**: The clock resolves to exactly one registration and yields the system clock, and a test-supplied clock still wins; 0 host configurations resolve to competing clock registrations.
- **SC-012**: The analyzer, complexity (at both the implementation and the tighter post-implementation threshold), inspection and format gates exit zero on every changed C# file, and 0 gate results are reported as passing when they did not run. The vulnerable-packages gate is run and its exit code recorded.
- **SC-013**: 0 files change outside the agreed list; 0 database schema changes, 0 migrations, 0 model snapshot changes, 0 secrets, 0 machine paths and 0 gate or harness threshold changes.
- **SC-014**: 0 tags on the span captured for one OMDb lookup and 0 captured log events contain the OMDb API key. The check is made against a sentinel value that appears nowhere else in the tree, against the activity's **whole** tag collection rather than one named tag, and against every captured log entry's message, state and exception text; the query-redaction opt-out appears 0 times in `src` and `tests`.

## Assumptions

- **Carried `[assumed]` ruling**: Decision B, the error-type value for a non-validation exception is the exception type's full name rather than its short name. Basis in `CONCLUSIONS.md` Decision B and `ASSUMPTIONS.md`, from the OpenTelemetry error-type registry's recommendation of the canonical class name. This is the only value-level taste ruling in this ticket.
- **Carried `[assumed]` ruling**: the readiness checks carry the internal labels `postgres` and `rabbitmq`. Whether those labels are ever **exposed** in a response body is not assumed - it is inside the Q2 checkbox, and `ASSUMPTIONS.md` says so explicitly.
- **No user-interface taste decisions apply.** Nothing in this ticket is copy, layout or wording. Every structural choice is a Patron ruling in `CONCLUSIONS.md`, a ticket line, or an owner checkbox, and none was assumed here.
- **The two open structural questions are the Q1 and Q2 checkboxes, and this specification deliberately leaves them open.** Where a requirement depends on one it is marked `[Q1-pending]` or `[Q2-pending]` and states only what the ticket and the constitution already decide. FR-017 and FR-029 are the two such requirements, and neither was given a placeholder value.
- **The OpenAPI requirement is deferred with a named owner rather than dropped or faked.** FR-034 records the ticket-constitution requirement, and the Q2 checkbox now carries the deferral itself: the committed `openapi.json` and the generated TypeScript client ride **DEV-20** and **DEV-320** respectively. That is a constitution departure from `constitution.md:359-360` accepted by the user under charter 2.3b for this ticket only, not a decision this document made. It began as escalation 1 (`DRAFTING_RECEIPT.md`), where it was unsatisfiable rather than owned.
- **Out of scope** (restated from the ticket and the brief): a health signal for the worker in compose, which the constitution requires but the ticket does not list; Seq; runtime and Entity Framework Core instrumentation; wiring the shared defaults into the worker host, which today builds on the older data layer and is not named by the ticket; service discovery and resilience defaults from the same template family; and any second Serilog sink.
- **The worker host is untouched, and that is a decision rather than an oversight.** The shared defaults are a web-host composition root, and the ticket names one caller. Extending the clock and options registration to the worker is a separate ticket.
- **Two per-feature clock registrations already exist and are left alone.** Persistence and the metadata provider each register the system clock defensively. Because the shared defaults run first, this ticket changes which registration decides, not how many exist. Removing the other two would mean rewriting files the ticket does not name, so it is not done here.
- **Options validation is retained, not reworked.** The shared defaults already bind and validate seven option records. The ticket's title mentions options validation because that work exists; it does not ask for it to change, so it does not.
- **The health routes are the first endpoints this application exposes.** Every earlier feature's plan recorded the API contract chain as untouched because the feature added no endpoint. This one does, which is why the chain's artefacts are a live question here rather than a formality.
- **The framework's own default status mapping is a fact, not a ruling.** The middleware maps a fully healthy aggregate to 200 and an unhealthy one to 503 without configuration. What happens to an aggregate that is neither is the part Q2 asks about, and it is not settled by that default.
- **No performance or volume target is invented.** The ticket states none, and none is manufactured from the implementation. The only timing the work records is the gate runtimes the pipeline already measures.
- **Verification of the two span paths runs against a real listener.** Reading the decorator would prove nothing about which attribute is set, and the null-safe path when no listener is installed would make both branches look identical - which is the failure this story exists to prevent.