# DEV-331 — Assumptions record

No unconfirmed taste assumptions are used. This ticket introduces no UI wording, ordering, empty states or visual choices, so there are no `[assumed]` taste entries.

Technical choices are deliberate Patron rulings, not silent assumptions: bounded first-page matching and deterministic tie-breaking (Q3/Q3a), shared existing matching/resilience configuration (Q3a/Q5), query-key auth and passive health (Q4/Q5), documented w500 poster construction (Q5), and the test/gate plan (Q6/Q8). Their costs and cited bases are recorded in `CONCLUSIONS.md` and `brief.md`.

The official API schemas provide no required/nullability guarantees; defensive mapping is explicitly decided, not an assumption of non-null payload fields. Retry-After is not documented by TMDb; handler support and synthetic tests do not assert that TMDb sends it (`recon-DEV-331-2:T8,T11`).

No assumption closes the consolidated Q7 constitution owner checkbox. Gate 1 remains closed. No live API operation, Phase B clearance, ADR landing, implementation, test pass or review clearance is inferred from this planning record.
