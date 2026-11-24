# Contract note: Version endpoint (DEV-547)

**Status**: Draft — frozen by Patron Q1 / brief; no owner structural checkbox

**Authority**: `brief.md` Q1–Q2 and Plan challenge decisions 1 / 6 / 7; `CONCLUSIONS.md` Q1–Q2, P2.6-1, P2.6-2; `recon-DEV-547:58,63,83–90` (Wanted 5, Done when 2)

**Route**: dedicated `GET /api/version` under the existing `/api` group (`MapApiEndpoints`)

## Response

Successful body is always HTTP `200 OK` with JSON:

```json
{
  "release": "v0.1.0",
  "commit": "8a2deb9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5f"
}
```

When release metadata is unset, empty, or whitespace:

```json
{
  "release": null,
  "commit": "8a2deb9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5f"
}
```

When commit metadata is unset, empty, or whitespace:

```json
{
  "release": null,
  "commit": "unknown"
}
```

| Field | Required | Meaning |
|---|---|---|
| `release` | yes (nullable) | Release tag string when set (for example `v0.1.0`); JSON `null` when `LAMUFLIX_RELEASE` is unset, empty, or whitespace (untagged builds, manual promotion, Compose). UI renders the value as opaque text. |
| `commit` | yes (string) | Full source SHA from effective process `LAMUFLIX_COMMIT` when configured; `"unknown"` when unset, empty, or whitespace. API does not shorten the SHA; the UI shortens a full SHA for display and renders `"unknown"` verbatim. |

## Metadata sources (Q2; Patron P2.6-2)

| Name | Role |
|---|---|
| `LAMUFLIX_COMMIT` | Configured only via image build args and final image `ENV`. CI passes the exact full source SHA to both Dockerfiles. Compose and promotion omit any runtime `LAMUFLIX_COMMIT` assignment so the baked image value is preserved. A separately operator-supplied runtime value (outside those repo configs) becomes the effective process value: full non-whitespace passes through; empty or whitespace yields `"unknown"`. No second variable. |
| `LAMUFLIX_RELEASE` | Injected at home promote time from the same safely serialized `RELEASE_TAG` used for manifests and `RELEASE.json` (empty on manual promotion); Compose leaves empty. |

## Test split

- **HTTP / integration**: Capture effective process metadata and normalize it once per host when mapping the route. Use a fresh `WebApplicationFactory` host per metadata case in a dedicated nonparallel xUnit collection; save both prior env values and restore them in `finally` (including host-start and assertion failures). Assert HTTP 200 via `TypedResults.Ok`; configured full commit; explicit JSON `null` with absent release; configured release tag; empty/whitespace normalization; `"unknown"` local fallback; and the P2.6-2 override cases (absent runtime assignment preserves baked value; explicit nonblank operator value; explicit empty/whitespace operator value). Follow repo Method_Condition_Expected naming and AAA headers. Peer: `HealthEndpointTests` (`recon-DEV-547:14`). T001 records the host-fixture seam and existing mapper signature.
- **OpenAPI / generated types**: assert route and schema/type shape (`release` nullable string, `commit` string). Wire normalization stays in HTTP tests.

## Process metadata (Q2; constitution PR 855–857; brief decision 1)

`LAMUFLIX_COMMIT` and `LAMUFLIX_RELEASE` are process/deployment metadata (image ENV / pod env / Compose), not application Options. Capture them once per host when mapping the route and normalize there. The selected process read needs neither Options nor an additional layer — not via `IConfiguration["…"]` magic strings and not via a startup-validated Options record that rejects unset/blank. Preserves Q2 `"unknown"` / JSON `null` behaviour. No application/domain use case / `AddHandler` for this API-only route (Principle II N/A; Q1 unchanged). Wire with `.MapVersionEndpoints()` on the existing `ApiEndpoints` chain; response type is sealed `VersionDto`.

## Contract chain (roles; constitution PR 967–969; Patron P2.6-1; `recon-DEV-547:83–90`)

Authorized generator-only outputs (never hand-edited; no `web/src/test/*` authorized):

1. `web/src/api/openapi.json`
2. `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`
3. `web/src/api/types.ts`
4. `web/src/mocks/handlers.ts`

Flow:

1. Add sealed C# response DTO `VersionDto` and map `GET /api/version` under `src/LamuFlix.Api` via `.MapVersionEndpoints()` on `Endpoints/ApiEndpoints.cs` returning `TypedResults.Ok`.
2. **OpenAPI baseline + Verify snapshot** (T006, separate from T007): run the opt-in integration test `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` with `LAMUFLIX_REGENERATE_OPENAPI=true` scoped to that invocation so it writes `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together. Then run `DriftMatchesCommittedBaseline` with the switch cleared. Never hand-edit. Stop before T007 if the generated diff contains unrelated drift versus pickup HEAD; resolve through this reviewed generation flow before continuing.
3. **Generated client + MSW** (T007, separate): from `web/`, run `npm run generate:api` (`node scripts/generate-api.mjs`, npx-free) producing exactly `web/src/api/types.ts` and `web/src/mocks/handlers.ts` from the committed OpenAPI document — never hand-edit. Then `npm run check:api`.
4. Web consumes generated types only — no hand-rolled duplicates. T001 checks the evidenced paths and invocation for pickup drift (`recon-DEV-547:83–90`).

## Unchanged / out of scope for this contract

- No field on the optional DEV-560 status response (dedicated route keeps DEV-560 optional).
- No GHCR `v0.1.0` image tags.
- No change to health probes (`/health/ready`, `/health/live`).
