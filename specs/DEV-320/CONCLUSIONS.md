# DEV-320 - Conclusions (Patron rulings)

Append-only. Sources: ticket text at recon-DEV-320:15-30, recon-DEV-320:1-14, lamuflix-team-charter, `specs/PRODUCT.md`, `.specify/memory/constitution.md`.

## Q1/12 - Contract boundary and generation approach

**Keel asked:** Confirm the contract boundary and generation approach? Keel recommends treating committed `web/src/api/openapi.json` as the sole client-generation input, generating `web/src/api/types.ts` with openapi-typescript, and making `web/src/api/client.ts` a small openapi-fetch client consuming those generated types. MSW consumes the same generated contract. Do not modify backend DTOs, OpenAPI routes/statuses/schema, hand-roll duplicate types, or introduce endpoint wrappers. Cost: regenerating types whenever committed OpenAPI changes. Interpret ticket wording "generate client.ts" as the typed runtime client backed by generated `types.ts`, without changing deliverables.

**Patron ruling: ACCEPT.** No ticket contradiction, no constitution departure; nothing escalates.

- Dependencies and client: ticket :22-23 names openapi-typescript and openapi-fetch, so the §2.3(1) dependency item is settled by ticket text; constitution :335 lists the same pair for the web stack. Reading `client.ts` as an openapi-fetch runtime over generated `types.ts` reconciles ticket :22 with charter :24 and constitution :470-472; no deliverable is added or dropped.
- MSW handlers: pinned at `web/src/mocks/handlers.ts` (ticket :25; ticket-named, under `web/src`, not a new top-level folder). Request/response shapes come only from generated `types.ts`, never hand-written (constitution :471-472, charter :24); fixture data is authored, types are not. No handler-generator dependency is approved.
- LocalPlay: `/api/movies/{id}/play` (openapi.json:445) is mocked only, nothing executes, so §2.3(5) is not triggered (recon-DEV-320:14 item 12(5)).

Noted for a later grill item, not Q1 scope: constitution :337 and :470-471 require an OpenAPI drift check in CI; whether DEV-320 adds a types.ts regeneration/drift check is a separate question.
