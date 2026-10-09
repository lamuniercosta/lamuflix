# DEV-318 - Patron conclusions

Grill (Phase 2 step 1) with Keel. Budget 12 questions.

## Q1/12 - Meaning of "Worker" in the ticket

**Question (Keel):** Recon-DEV-318:41-56,61,78,108 establish that `EnrichmentConsumer` in the API host is the
active Worker and the old `LamuFlix.Worker` is retired. Keel recommends interpreting the ticket's Worker as that
active hosted consumer, reusing the ApiEndToEnd `WebApplicationFactory` plus Postgres/RabbitMQ Testcontainers and
WireMock, and strengthening existing coverage rather than creating or resurrecting a separate worker host. Cost: the
test proves the deployed in-process worker boundary, not process isolation. Does this fulfil the ticket with no
scope change?

**Ruling:** Approved - the ticket's Worker is the active `EnrichmentConsumer` hosted in the API host; reuse the
ApiEndToEnd stack; no new host, project, or dependency. No scope change, not `blocked: structural`.

- Ticket text (DEV-318:10-11) names `WebApplicationFactory` driving import, a Testcontainers RabbitMQ consumed by the
  worker, WireMock OMDb, and GET status; a `WebApplicationFactory` host is in-process by construction, so the ticket
  itself scopes the worker to the hosted consumer. Recon-DEV-318:41,61,78,108 show that consumer and the OMDb
  client are registered in that host and asserted present by `ApiEndToEndFactory.cs:51`; recon:56 records the
  `LamuFlix.Worker` retirement (ADR-0019), a prior decision this ticket neither makes nor reverses.
- Constitution VI (:218-224) requires one trace API -> RabbitMQ -> Worker -> metadata provider via
  `traceparent`/`tracestate` header inject/extract; that crossing is real over the broker wire even in-process.
  Constitution :288 and :304 place the worker end-to-end and single-trace-id scenario in `LamuFlix.IntegrationTests`
  with exactly these tools (Testcontainers, `WebApplicationFactory`, WireMock.Net). PRODUCT.md:10 names the Worker
  by role (background enrichment over RabbitMQ), which the hosted consumer fills.
- Condition (because in-process removes process isolation): the trace assertion MUST start from the HTTP request
  and prove continuity through the wire headers - consumer span parented on the extracted RabbitMQ publish context -
  not a shared TraceId alone (recon-DEV-318:133 citing DEV-314 brief:61; recon:136 confirms no such test exists).
  Stale constitution :108 / PRODUCT.md:10 references to `LamuFlix.Worker` are not this ticket's to edit:
  noted, no ticket.
