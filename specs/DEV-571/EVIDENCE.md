# DEV-571 Phase 6 evidence (T017–T018)

Scope: API and observable row/grid parity plus the Q4 rollout receipt. T019 gates and T020 review are **not** run or claimed here. No secret, key, token, or connection string value is recorded below.

## Provenance

| Field | Value |
|---|---|
| Worktree | `F:/Dev/LamuFlix.worktrees/feature-dev-571` |
| Branch | `feature/dev-571` |
| Phase 5 handoff commit | `38776c212cfb3725374979def4b61ae970332a93` |
| Merge base / Gate 1 amendment | `9e94faa16358ffeefdd0c76a75a79264ee7486bc` (PR #508, Constitution 2.1.0) |
| Delivery | this commit: `DEV-571 - Phase 6 T017 T018 API row parity and Q4 receipt` |

## T017 — API, row/grid, and SMOKE-001–003 coverage

| Check | Command / artifact | Outcome | Evidence |
|---|---|---|---|
| API short-row contract | `dotnet test tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj --blame-hang --blame-hang-timeout 10m --filter 'FullyQualifiedName~OutsideLibraryRecommendationEndpointTests\|FullyQualifiedName~EfRecommendationCandidateSourceBudgetTests\|FullyQualifiedName~EfMetadataRequestGateTests\|FullyQualifiedName~MigrationTests'` | **PASS** | 38 passed, 0 failed, 0 skipped; new filtered-short-row route test asserts 200, ordered `tmdbId` values, exact `tmdbId/title/releaseYear/posterPath/reason` field set; disabled-allowance source test admits only the cached lower survivor with zero TMDb/OMDb calls |
| OpenAPI drift | same project, `--filter 'FullyQualifiedName~OpenApiContractTests.DriftMatchesCommittedBaseline'` | **PASS** | 1 passed; generated document matches `web/src/api/openapi.json` and the verified snapshot |
| Web gates | `pwsh -NoProfile -File scripts/run-web-gates.ps1 -BaseRef 9e94faa16358ffeefdd0c76a75a79264ee7486bc` | **PASS** | lint (`check:api` clean), tsc, 86 files / 1326 Vitest tests passed, Vite build; e2e typecheck included in build |
| Compose browser smoke | `pwsh -NoProfile -File scripts/run-e2e-smoke.ps1` | **PASS** | exit 0; `38 passed (2.0m)`; 32 stream-acceptance cases; every DEV-571 SMOKE test green in the real Compose stack |
| Dotnet format | `dotnet format --verify-no-changes --no-restore` | **PASS** | exit 0 after applying formatter to the new integration test |

SMOKE mapping in `web/e2e/zz-dev571-outside-library.e2e.ts`:

- **SMOKE-001** — a below-floor candidate (rating 5.9, votes 2500) ordered ahead of a later complete-pass survivor is absent; the next eligible survivor takes the visible slot in unchanged TMDb-ID order, the library grid stays visible, and the API card field set is unchanged. The cached/confirmed-absence variant with live OMDb disabled is proven by `EfRecommendationCandidateSourceBudgetTests.GetOutsideLibraryRecommendationsAsync_DisabledAllowance_CachedNextSurvivorFillsTheRow` (quota `DailyLimit = 0`, fresh known-failure evidence for the higher-ranked candidate, fresh confirmed-absence evidence for the lower survivor, zero outbound calls).
- **SMOKE-002** — two otherwise eligible candidates share collection `777001`; only `Collection Representative` is returned and rendered, with the later duplicate absent.
- **SMOKE-003** — discovery loading, empty (200 `[]`), and error (503 ProblemDetails) keep the section hidden while the library grid remains usable. The loading/error/empty grid independence is also pinned in `web/src/features/for-you/OutsideLibrarySection.test.tsx` (54 focused tests passed with `ForYouPage.test.tsx`).

Web regression evidence uses the generated `components['schemas']['OutsideLibraryRecommendation']` type; no hand-rolled TypeScript contract duplicate was added.

Non-pass attempts retained for honesty: the first e2e run exited 1 because the seeded-watched POST returns 409 when the earlier DEV-561 smoke has already marked both seeds watched (2 failures); the helper now accepts 409 as the already-watched idempotent outcome. The second run passed all 38 browser tests but exited 1 because the post-test Q4 psql readback hit `too many clients already`; the readback was moved before Playwright starts browser-backed API traffic. The first web-gates run exited 1 on Prettier style in the two new e2e files; `prettier --write` fixed them and the final run above passed.

## T018 — Q4 rollout receipt (stubbed local Compose environment)

Rollout prerequisites were applied in order by `web/e2e/run.mjs`: generated settings provision `Omdb:Quota:DailyLimit = 100` before stack start, the API starts with `Database:ApplyMigrationsOnStartup = true`, the candidate-evidence table is seeded/read back, and only then are browser tests run against the deployed guard.

| Receipt item | Evidence | Outcome |
|---|---|---|
| Candidate-evidence migration applied | `web/e2e/.runtime/evidence/q4-rollout-receipt.json`: `candidateEvidenceRows: 5`; migration `20261126212829_AddOutsideLibraryCandidateEvidence` in the checked diff; `MigrationTests` passed in the targeted integration run | **PASS** (local Compose) |
| Positive allowance provisioned and read back | generated `web/e2e/.runtime/api.settings.json` carries `Omdb:Quota.DailyLimit = 100`; readback reports `configuredDailyAllowance: 100`, `chargedUsageUtcDate: 2` (<= allowance) | **PASS** (local stubbed environment) |
| Revised guard deployed after read-back | settings written in `prepare()` before `docker compose up`; readback runs after API readiness, streaming acceptance, and seeding; guard code is in the reviewed commit; no code fallback or bypass | **PASS** (local Compose) |
| Ordinary enrichment through guarded adapter | `waitForSeededState()` requires both seeds to reach `Enriched` through the stubbed OMDb path before discovery seeding; all 38 tests passed | **PASS** (stubbed) |
| Two consumers share the pool, failures charged and attributable | `EfMetadataRequestGateTests.TryAcquireAsync_TwoConsumers_SharePoolAndChargeFailedAttemptsPerConsumer`: 2 failed discovery sends + 1 failed enrichment send charge 3/3 shared reservations, a fourth send is denied with zero transport, and each transport records its own call count | **PASS** (real relational persistence) |
| Invalid configuration remedy | no invalid allowance was used in this rollout; disabled/absent/zero/invalid behaviour and provisioning-only remedy remain covered by T005 option tests; no bypass was added | **N/A** in this environment |
| Provider plan limit at or below shared use | OMDb is a loopback stub in this environment; a real plan limit cannot be read back here | **Could not run** — live acceptance pending |

The value `100` is an explicit non-secret provisioning value for the disposable Compose/e2e environment only; it is not a code default or fallback. `docker/api.settings.example.json` carries the same key so the plain stack can provision it. Real Compose/home deployments still require their own credential-redacted migration, plan-derived allowance read-back, and guarded-enablement receipt before live acceptance; stubbed smoke is not substituted for that live proof.

## Carry-forward to T019

T019 remains open. Orphan deletion and the full gate matrix (Roslyn, complexity, InspectCode, full property/ordinary/integration tests, architecture tests, format verification, vulnerable packages, Stryker, API contract drift) are owned by its own task; the checks above are targeted Phase 6 evidence, not final gate labels.
