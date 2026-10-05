# DEV-396 - Assumptions

- [assumed] Phase order: fresh DEV-308 retrospective first, then OTel + same-commit OMDb redaction, health membership/tags, HandlerOutcome decorator, conditional readiness, final evidence/gates and delivery review. Keel recommendation Q5, accepted by Patron (CONCLUSIONS Q5).
- [assumed] 0018 slug and title: `0018-observability-composition-in-service-defaults.md`, `# 0018. Observability composition in ServiceDefaults; health membership and handler outcome spans`; Keel may tighten the wording without a new ruling. Basis: CONCLUSIONS ADR-R1.
- [assumed] 0018 layout follows the newest records: bullets `Status`, `Date`, `Ticket: DEV-396 (completes DEV-307)`, then Context, Decision, Consequences, plus the `Relationship to earlier records` section from ADR-R3. Considered Options appears only if a rejected alternative is non-obvious (ADR-FORMAT.md:21-23). Basis: CONCLUSIONS ADR-R3/R4.
- [assumed] P2 new IntegrationTests collection and file names (for example `TelemetryCompositionCollection`, `ServiceDefaultsTelemetryTests.cs`) are Quill's choice; only placement and non-parallelism are ruled. Basis: CONCLUSIONS Q8 P2.
- [assumed] P3 measurement bound: disposal over 12 s, or IntegrationTests suite growth over 25%, comes back as a Medium finding. Basis: CONCLUSIONS Q8 P3; no recon measurement exists.
- [assumed] P1 positive redaction check asserts that the `Uri` value lacks `apikey` and the sentinel, not the exact `?*` rendering. Basis: CONCLUSIONS Q8 P1; the 10.0.12 format is unread.
