# DEV-371 assumptions

- [assumed] Container image tags are pinned to explicit versions: Postgres (e.g., `postgres:16.4`), RabbitMQ (e.g., `rabbitmq:4.0.0`, a currently supported stable version in the 4.x line), instead of floating tags. The engines are decided (`constitution.md:295`); the tag strings are taste (`PRODUCT.md` §4). The pins are tags, not digests. RabbitMQ.Client 6.8.1 speaks AMQP 0-9-1 to 4.x containers.
- [assumed] One container is shared per container type per test run (xUnit v3 assembly fixture), with isolation by a fresh database and a fresh queue per test. Container-per-test is rejected as needlessly slow. Taste (`PRODUCT.md` §4).
- [assumed] `Npgsql.EntityFrameworkCore.PostgreSQL` and the `Testcontainers.*` packages take the latest stable versions compatible with the EF Core 9.0.0 line pinned in `Directory.Packages.props`. The package identities are decided (`constitution.md:295`; DEV-371 Scope §1/§2/§4); the version numbers are taste.

Owner decisions and Patron rulings are not assumptions; see [spec.md](spec.md).
