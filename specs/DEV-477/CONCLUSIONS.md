# DEV-477 Conclusions

## Phase 3 Scope Ruling — Poison-Row Handling vs. ADR-0028

Verdict: ACCEPT — Approve DEV-477's poison-row behavior (log `PoisonRow`, mark `ProcessedAt` durable without incrementing published count, and continue the relay pass); record the superseding decision here in `specs/DEV-477/CONCLUSIONS.md` and do not edit `docs/adr/0028-confirmed-enrichment-outbox-relay.md`.

- **Ticket Authority (`note DEV-477:5-6`, Charter §2.3)**: DEV-477 explicitly scopes `OutboxRelayHostedService` so payload deserialization failure marks the poison row dead/processed and continues the pass rather than failing the whole relay; current ticket scope is authoritative and overrides prior DEV-420 rulings (`docs/adr/0028-confirmed-enrichment-outbox-relay.md:19,42`).
- **Implementation & Telemetry Alignment (`39eb183e`, `docs/adr/0028-confirmed-enrichment-outbox-relay.md:27`)**: `OutboxRelayHostedService.cs:227-230,362-366,388-425` logs structured error `PoisonRow` (row ID, category, exception type name only), persists `ProcessedAt` under the host token, skips `onRowPublished()` so `lamuflix.outbox.published` still counts only confirmed-and-marked publications, and continues the batch while database/publish/marker-save failures still abort the pass.
- **ADR-0028 Disposition (Charter §2.3 item 6)**: `docs/adr/0028-confirmed-enrichment-outbox-relay.md` (`Status: Draft`, `Ticket: DEV-420`, lines 3-5) is an unnamed tracked file not forced by DEV-477's acceptance criteria or any verification gate; this `specs/DEV-477/CONCLUSIONS.md` record is required and sufficient without amending `docs/adr/0028-confirmed-enrichment-outbox-relay.md`.
