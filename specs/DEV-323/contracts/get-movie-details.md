# GET /api/movies/{id} (DEV-323 additive)

- Route, 200/404 semantics, outer response, and metadata nullability unchanged.
- `MovieMetadataResponse` gains: `posterUrl` (nullable), `rottenTomatoesRating` (nullable), `metaScore` (nullable), `directors: string[]`, `actors: string[]` (empty when none).
- Regenerate `web/src/api/openapi.json` and the verified snapshot through the existing `OpenApiContractTests` facility; web consumes the fields via DEV-320-generated `types.ts`.
