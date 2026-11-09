# Runtime acceptance — DEV-457

Date: 2026-11-09

Delivery SHA (exact DEV-457 implementation HEAD exercised): 22df57237bd1a2b68a538abba40c6998398dad2b

Dependency SHAs/receipts: DEV-458 delivered at cb4f74a0b52cf8d773782123e9fc300fb334437d (PR #352); DEV-459 delivered at 40a3e613 (PR #343), per note DEV-457 Wisp T001 Phase B pickup recon receipt (14:04).

Scrubbing: This file contains no real library paths, player paths, secrets, tokens, passwords, or connection strings. Validation used process-local non-secret values only; their values are not recorded here.

## Compose config (T006, config-only portion)

- `docker compose -f docker-compose.daily-driver.yml config` with process-local non-secret validation environment: PASS, exit 0.
- Without operator credentials: exit 1 (fail-closed required-variable errors for Postgres and RabbitMQ credentials) — expected fail-closed behaviour, not a failure of the config.
- Rendered output verified: project name `lamuflix-daily-driver` (dedicated, non-default); services Postgres and RabbitMQ only; explicit `127.0.0.1` host bindings on both published ports; named volumes `postgres-data` and `rabbitmq-data` scoped under the project; no api/web/ollama services; no committed credentials or defaults.

## Unexecuted criteria — Could not run

- SC-001 host-API connection / service start beyond config render: Could not run. Starting the durable services and connecting a host API was not executed in this pass; only `config` rendering was run. No `up`, readiness, or host-API connection evidence is claimed.
- SC-002 durability (FR-004): Could not run. No isolated scratch-library import was performed, so no restart/recreate-with-retained-volumes survival evidence exists for an imported movie or API readiness. No isolated durable RabbitMQ test queue or persistent test message was established or observed before/after restart. Volume existence in the rendered config alone is insufficient and is not claimed as durability proof. Prerequisite host Ollama was unavailable (see below), which also blocks the host-API-dependent portion of T006.
- SC-005 / T007 BS Player playback: Could not run. No local movie was imported through the real import path into an isolated scratch library; loopback was not confirmed for a LocalPlay exercise; LocalPlay was not enabled; `POST /movies/{id}/play` was not called; no BS Player (or any player) visible-open, returned status, or playback-history evidence exists. No automated `IProcessStarter` substitute was used or claimed.
- Host Ollama prerequisite (T006/T007): Could not run. No host `ollama` binary was found on PATH; `curl` to `http://127.0.0.1:11434/api/tags` and `http://localhost:11434/api/tags` both failed with exit 7 (connection refused) on 2026-11-09. Therefore `ollama pull/show/list all-minilm`, model availability, and real host API embedding-startup validation on the unmodified production startup path were not executed. `/health/ready` was not used as an Ollama probe and closes nothing.

## Status

- T006 remains unchecked. T007 remains unchecked.
- Ticket acceptance/Done is NOT claimed. The unmet runtime acceptance above remains blocking closure per Keel ruling on note DEV-457 (lines 76-80) and FR-012.
