# DEV-306 Assumptions

- [assumed] Name the two integration collection definitions PostgresCollection and RabbitMqCollection; reuse one fixture instance per matching collection. Basis: Keel Q3 recommendation; ticket note DEV-306:15 requires collection fixtures but leaves labels open; CONCLUSIONS.md Q3.
- [assumed] Name the explicit normal PostgreSQL context path CreateMigratedContext, so consumer changes visibly distinguish startup-migrated contexts from empty migration-test databases. Basis: Keel Q2 recommendation for explicit naming; CONCLUSIONS.md Q2. Exact async empty-database/reset member spelling follows existing conventions without changing the ruled behavior.

No user-interface taste decisions apply. Structural and lifecycle choices are deliberate Patron rulings in CONCLUSIONS.md, not silent assumptions.
