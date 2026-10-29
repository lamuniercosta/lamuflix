---
status: Accepted
---

# Separate disposable local Aspire orchestration from compose

DEV-332 introduces a local-only AppHost for Postgres, RabbitMQ, Api and Vite, with the SDK dashboard and F5 support, alongside unchanged docker-compose workflows. AppHost owns disposable containers rather than attaching a host Api to compose infrastructure whose ports are unpublished; compose changes and deployment are explicitly outside the ticket. This preserves the ticket boundary at the cost of two separate development entrypoints and disposable AppHost data.

Adapt orchestration to the existing Api configuration contracts: pgvector and current migrations through `DefaultConnection`, mapped RabbitMQ endpoint and credential fields through `RabbitMqOptions`, and the Api HTTP endpoint through Vite's existing `VITE_API_BASE_URL`. Use core executable hosting with PATH-resolved Node and the installed Vite entrypoint rather than adding a JavaScript hosting dependency; resolve the web directory from AppHostDirectory and pass a separate argument array. Keep npm install as prerequisite, disable web endpoint proxying on localhost 5173, retain option validation and readiness checks, and prove Windows launch/port behavior on a live host. No worker, Ollama resource, new schema, public API, production defaults or frontend contract changes are introduced.

Basis: ticket note DEV-332:27-31; recon-DEV-332:54; recon-DEV-332-2:40-47,53-79,85-87; recon-DEV-332-3:13-21; Patron Q1, Q3-Q8 and PC1-PC4 in [CONCLUSIONS.md](../../specs/DEV-332/CONCLUSIONS.md). Accepted at plan freeze under Patron PC1; the [plan challenge freeze receipt](../../specs/DEV-332/PLAN_CHALLENGE.md) closes the planning findings. Patron Gate 1 and all Phase B implementation/runtime gates remain separate.


