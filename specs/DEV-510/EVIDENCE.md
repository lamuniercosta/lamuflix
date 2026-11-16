# DEV-510 Operational runbook and evidence slots

Template owned by Quill drafting from `brief.md` Operational runbook section (Q6/Q7). Slots start **pending**. Fill with measured results only — never substitute ticket estimates (~60% reduction / ~1.3 KB/source) or invent latency targets. No secrets, connection strings, tokens, vectors, profile content, or identity dumps. Prefer safe references to disposable evidence locations.

**Worktree (spec phase)**: `F:/Dev/LamuFlix.worktrees/feature-510-spec`  
**Branch**: `feature/510-spec`  
**Recon baseline HEAD**: `6edd0d16f0dd51950c56e21f2c866467dc5dff77`

## Status board

| Stage | Status | Notes |
| --- | --- | --- |
| Planning / freeze | PLAN FROZEN | Keel independent reread, 2026-11-15; see PLAN_ADJUDICATION.md |
| Gate 1 (spec PR merge) | complete (T000) | Spec PR #415 merge receipt in CONCLUSIONS.md at 22261b35 |
| Serializer seam (T001b) | complete | Compile accessibility + runtime same-singleton exact-envelope/`GetOrCreateAsync` proof at f6647681 |
| Pre-change baseline (T002) | measured; **checkpoint FAILED** | Baseline slots filled from the live seam over real home data. The frozen 725-source / 0.8 feasibility projection is **infeasible** on realistic retained maps, so T003+ is blocked for recon/Patron disposition |
| Implementation verification | pending | Gates + parity + size/cache tests at exact SHA |
| Home promotion | pending | After user-owned delivery merge; authorized operator only |
| Runtime acceptance (≥24h) | runtime-pending | Remains open until operator receipt exists |

---

## 1. Baseline (pre-change)

| Field | Value |
| --- | --- |
| Original unmodified pickup SHA | `79e14969b1832db9075de96e6087cfa101fde20a` (unmodified pickup; worktree base) |
| T001b serializer-seam SHA | `f6647681239dfec87856a1ebad0af5930b6ae422` |
| Configuration / serializer / options identity (safe) | `AddLamuFlixPersistence` registers one `CachedProfileSerializer` singleton via `AddSingleton(instance)` + `AddHybridCache().AddSerializer<CachedProfile>(instance)`, injected into `HybridCachedRecommendationProfileSource`. Serializer: `System.Text.Json` with `JsonSerializerDefaults.Web`. Options: `TasteProfileCacheOptions.Expiration` = 00:05:00. Envelope: `CachedProfile(TasteProfile Profile, long Generation, DateTimeOffset CreatedAt)`. |
| Effective payload ceiling (bytes) | 1,048,576 (read from resolved `IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes`) |
| Observed liked-source / library count | 4 liked sources; 14 genre preferences; 449 movies / 449 feature vectors / 27 watched / 0 playback events in the home library |
| Serialized `CachedProfile` total bytes | **41,908** (live seam, real `CreatedAt`) |
| Component split (bytes) | envelope only 307 · genre preferences 410 · profile plot centroid 4,804 · liked aggregate channels 8,632 · disliked aggregate channels 4,971 · `LikedSources` total 22,776 — of which per-source plot vectors 18,818 and retained maps 3,958 |
| Calculated headroom vs ceiling / vs 80% budget | Current home profile is 4.0% of ceiling and 5.0% of the 838,860-byte strict budget — comfortably inside. Per-source retained maps measured **989.5 B/source**; removing source plot vectors alone would save 4,704.5 B/source. **725-source feasibility on realistic retained maps FAILED — see section 1c.** |
| Safe evidence references | `output/dev510-baseline/report.json` (untracked, disposable local measurement); snapshot digest `output/dev510-baseline/snapshot.sha256` |

**Rules**: T002 measures through the live T001b registered singleton `IHybridCacheSerializer<CachedProfile>` / options on the exact `CachedProfile(Profile, Generation, CreatedAt)` envelope over the **unchanged** pre-representation profile (brief.md PA1 amendment Explicit shared serializer; PA1 amendment Serializer setup before baseline / CONCLUSIONS.md sequencing ruling at 85feb708). Record both original pickup SHA and seam SHA plus serializer/configuration provenance. Do not mark complete if unavailable — leave pending / Could not run. Capture before representation change. T001b compilation alone is not runtime measurement proof; no assumed serializer invocation on the no-`IDistributedCache` path.

**Measurement provenance (T002)**: home cluster was reachable read-only, so these are real home values, not estimates. A `pg_dump` of the home `lamuflix` database was loaded into a disposable local `pgvector` container, and a temporary untracked xUnit probe drove the production `AddLamuFlixPersistence` registration (`HybridCachedRecommendationProfileSource` + `GetForYouRecommendationsQueryHandler`) against that replica. The probe file and the dumped home rows were deleted before this commit; only the derived numbers and digests remain. The component ladder anchors a fixed `CreatedAt`, so its total (41,900 B) differs from the live 41,908 B by 8 bytes of timestamp rendering — both are recorded rather than reconciled.

---

## 1c. T002 hard feasibility gate — 725 sources vs strict 80% budget

**Verdict: FAILED on realistic retained maps. The frozen 725-source / 0.8 projection is not feasible for the representation the ticket authorizes (retain cast/genre/director maps; remove only per-source plot vectors).** Per `tasks.md` T002 this blocks T003+ and is recorded for recon/Patron disposition; 725 and 0.8 stay frozen, no thinning, no silent relaxation.

| Measurement (real home library, production calculator, source plot vectors removed) | Bytes | Fraction of 1,048,576 |
| --- | --- | --- |
| 25 sources | 65,589 | 6.3% |
| 100 sources | 250,408 | 23.9% |
| 145 sources (ticket baseline) | 465,340 | 44.4% |
| 300 sources | 940,943 | 89.7% (over 80%) |
| 449 sources (entire current library, all liked) | 1,377,125 | 131.3% (**over the ceiling**) |
| Strict 80% budget | 838,860 | 80.0% |

Projection to 725 sources from the measured per-source distribution (per-source retained bytes: mean 1,281.7, median 1,141, p90 2,072; envelope without sources 801,187 B):

| Projection basis | 725-source bytes | Fraction of ceiling | Under strict 80%? |
| --- | --- | --- | --- |
| Per-source mean | 1,730,430 | 165.0% | No |
| Per-source median | 1,628,412 | 155.3% | No |
| Per-source p90 | 2,303,387 | 219.7% | No |

**Why the earlier 4-source number looked feasible**: the live profile has only 4 liked sources, so a per-source average taken from it (989.5 B/source → 736,512 B at 725, exactly 70.2% of ceiling) extrapolates from a biased, tiny sample. Rebuilding the same compact representation from the library's real cast maps gives 1,281.7 B/source mean and a much larger fixed envelope.

**Where the bytes actually go at library scale** (449-source measured decomposition): `Liked` aggregate channels **800,238 B** (20,459 cast entries + 325 directors + 22 genres), `LikedSources` retained maps 575,938 B (25,446 per-source cast entries), genre preferences 642 B, envelope 307 B, plot centroid 0 (no vectors present in this synthetic liked-only profile).

**Consequence**: removing per-source plot vectors is *not* sufficient. The dominant cost is the aggregate `Liked.Cast` map (one entry per distinct cast member across all liked movies) plus per-source cast lists — both of which task Q3/PA1 explicitly requires be retained. Reaching 725 sources under 838,860 B would require either capping/thinning retained cast maps (forbidden: "no thinning", FR-011), or a different budget/ceiling decision by Patron. This is a citation-bearing stop, not a Build-lane judgement: it is an acceptance-parameter question the ticket froze.

---

## 1b. Post-change payload

| Field | Value |
| --- | --- |
| Post-change SHA | pending |
| Configuration / serializer / options identity (safe) | pending |
| Effective payload ceiling (bytes) | pending |
| Observed liked-source / library / profile count | pending |
| Serialized `CachedProfile` total bytes | pending |
| Component split (bytes) | pending |
| Calculated headroom vs ceiling / vs 80% budget | pending |
| Safe evidence references | pending |

**Rules**: Fill only after the representation change (T010). Keep every unmeasured field pending / Could not run. Do not copy baseline values forward as if measured post-change. No secrets/vectors/identities.

---

## 2. No-distributed-cache limit behaviour

| Field | Value |
| --- | --- |
| Reproduction setup (safe) | `AddHybridCache().AddSerializer<CachedProfile>(probeSerializer)` with **no** `IDistributedCache` registered (confirmed: no `IDistributedCache` service descriptor present), and a probe serializer that emits a requested byte count so payload size is controlled independently of content. Repeated with `AddDistributedMemoryCache()` for comparison. |
| Whether / where 1,048,576-byte rejection occurs | Resolved ceiling 1,048,576 B. Below (1,038,090 B, 0.99×) and exactly at (1,048,576 B, 1.00×): **no error log, entry admitted and reused**. Above (1,059,062 B, 1.01×), 2×, and 8×: `Error` log from `Microsoft.Extensions.Caching.Hybrid.HybridCache`, event id 1 — `Cache MaximumPayloadBytes (1048576) exceeded.` No exception is thrown to the caller in any case. |
| Distinct from serialization-only measurement? | **Yes, and materially.** Serializing the envelope through the registered singleton and counting `WrittenCount` never rejects anything — it is a pure byte count. The ceiling is enforced inside HybridCache admission and surfaces only as a logged `Error` plus a non-admitted entry, not as a throw. This is exactly the distinction the ticket warns not to assume away. |
| Actual observations / safe log references | `firstRead` returned normally at every ratio (0.99×, 1.00×, 1.01×, 2×, 8×), both with and without a distributed cache. In the local-cache-enabled case the over-ceiling value is **still served from the entry the caller just created** and a second read reuses it (`entryWasReused = true`, serialize count stays 1). With `DisableLocalCache` and a distributed cache present, an over-ceiling value is not admitted: the factory is invoked again on the next read (`entryWasReused = false`), and the `Error` is logged once per attempt. |
| Conclusion | With no `IDistributedCache`, HybridCache does **not** reject or truncate an over-ceiling `CachedProfile`: it logs `MaximumPayloadBytes exceeded` at `Error` and then returns the value to the caller, and with the local cache enabled that oversized entry **continues to be served and reused**. So an at-or-over-ceiling profile degrades to "logged error, still working in-process" rather than a failure. Consequences for T007/FR-011: (a) a size-only throw must not be invented — there is none here; (b) the no-distributed-cache behaviour is *silent over-ceiling tolerance*, so `MaximumPayloadBytesExceeded` counting (section 8) is the real production signal, and the 0.8 warning is the only pre-emptive guard; (c) "below the ceiling" in this deployment is a hygiene/observability requirement, not an enforcement boundary, so the 725/0.8 gate is what actually protects a future distributed cache. |

---

## 3. Fixed scoring snapshot (parity baseline)

| Field | Value |
| --- | --- |
| Snapshot / input provenance (safe) | Real home library replica (`movies`, `movie_features`, `movie_genres`, `movie_actors`, `movie_directors`, `playback_events`) with the production `GetForYouRecommendationsQueryHandler` and `HybridCachedRecommendationProfileSource`; candidate set = the handler's own eligible pool (unwatched, not disliked, browsed in title order). `Take = 10`, default `RecommendationOptions`. Liked source titles: `A Few Good Men`, `About Time`, `Aladdin`, `Anchorman The Legend of Ron Burgundy`. |
| Candidate-set provenance (safe) | Derived by the handler from the seeded library at measurement time; not hand-picked. Reproduce by replaying the same library snapshot. |
| Pre-change SHA | `f6647681239dfec87856a1ebad0af5930b6ae422` |
| Pre-change title/order | 1 Good Will Hunting · 2 The Holdovers · 3 Birdman or (The Unexpected Virtue of Ignorance) · 4 Lost in Translation · 5 No Other Choice · 6 The Devil Wears Prada · 7 Thank You For Smoking · 8 The Wolf of Wall Street · 9 Despicable Me · 10 Primetime |
| Pre-change exact scores | 0.6605670473216091 · 0.46859104972530025 · 0.4854485132614298 · 0.5400349842221308 · 0.3960042015225052 · 0.48767275404615285 · 0.46518203267629793 · 0.38851043937664537 · 0.4262674773822673 · 0.3765578040052854 |
| Pre-change `Because you liked X` reasons | Good Will Hunting → About Time; The Holdovers → About Time; Birdman → About Time; Lost in Translation → About Time; No Other Choice → About Time; The Devil Wears Prada → About Time; Thank You For Smoking → Anchorman The Legend of Ron Burgundy; The Wolf of Wall Street → Anchorman The Legend of Ron Burgundy; Despicable Me → Aladdin; Primetime → A Few Good Men. Each item also carries `Matches your viewing history` plus 0–3 `Matches your <Genre> interest` reasons. |
| Post-change SHA | pending (T010) |
| Post-change title/order / scores / reasons | pending (T010) — must match the above exactly |
| Discrepancies | pending (none allowed for acceptance) |

**Snapshot digest**: `output/dev510-baseline/snapshot.sha256` = `9b66ab6e08f9a5091fe48712ccebd9c1b7faeffbfc95c11795bbcb1235e259a2` (SHA-256 of the serialized snapshot object). A post-change T010 run must reproduce this digest for exact parity. Note the ordering is MMR-selected, not score-sorted (e.g. rank 2 scores 0.4686 above rank 3 at 0.4854), which is why parity must compare the emitted list, not a re-sorted score list.

**Rules**: Exact equality; unchanged formula/weights; no committed sensitive home content. Only titles, scores, reasons and a digest are recorded — no vectors or identity dumps — and the home rows themselves were deleted.

---

## 4. Matched `for-you` latency

Matched inputs and cache-state classification must be comparable across pre/post. Keep pre-change and post-change timing samples and statistics in separate slots. Invented speedup target used? **No** — publish measurements only. Unmeasured fields stay pending / Could not run.

Both series use the identical seeded library, the same `GetForYouRecommendationsQueryHandler`, `Take = 10`, and default options. Cold = profile cache invalidated before the sample; warm = profile entry already present.

### 4a. Pre-change (baseline)

| Field | Value |
| --- | --- |
| Inputs / cache-state classification | Cold: profile cache miss (uncached EF profile read + candidate embedding batch + full candidate scoring/Mmr). Warm: profile cache hit (serializer not re-invoked, no per-hit re-serialization) + the same uncached candidate embedding batch and scoring. |
| Cold / uncached timing samples | 3448.259, 3521.994, 3348.63, 3325.487, 3541.032, 3551.89, 3550.094, 3739.078, 3659.547, 3674.7, 3745.629, 3539.224 ms |
| Warm-cache timing samples | 3763.493, 3717.454, 4398.51, 3937.198, 3543.502, 5804.306, 5626.873, 3905.062, 3472.11, 3473.494, 4685.233, 4593.053 ms |
| Sample count | 12 per state (after one unrecorded JIT/EF-model warm-up pass) |
| Summary statistics | Cold: min 3325.487 / median 3545.563 / p95 3742.026 / max 3745.629 / mean 3553.797 ms. Warm: min 3472.11 / median 3921.13 / p95 5706.718 / max 5804.306 / mean 4243.357 ms. |
| Build SHA | `f6647681239dfec87856a1ebad0af5930b6ae422` |

**Isolated profile-source timing** (same run, `HybridCachedRecommendationProfileSource.GetTasteProfileAsync` only) shows the cache effect the end-to-end number buries. Cold samples: 14.664, 13.155, 11.535, 12.37, 14.925, 15.706, 11.678, 12.098, 12.102, 11.427, 13.038, 11.88 ms — min 11.427 / median 12.236 / p95 15.276 / max 15.706 / mean 12.881. Warm samples: 0.411, 0.529, 0.448, 0.549, 0.658, 0.72, 0.654, 0.668, 0.65, 0.657, 0.636, 0.59 ms — min 0.411 / median 0.643 / p95 0.691 / max 0.72 / mean 0.598. The end-to-end `for-you` path is ~3.3–3.9 s and is dominated by full-library candidate loading and scoring, so profile-cache savings (~11.6 ms at the median) are invisible at that scale. Warm end-to-end runs are **not** faster here — they are nominally slower (median 3921 vs 3546 ms), which is noise plus allocation pressure in a ~3.5 s workload, and no speedup is claimed. Post-change comparison must be read against the isolated profile-source figures, not the noisy end-to-end ones.

### 4b. Post-change

| Field | Value |
| --- | --- |
| Inputs / cache-state classification (matched to 4a) | pending (T010) |
| Cold / uncached timing samples | pending (T010) |
| Warm-cache timing samples | pending (T010) |
| Sample count | pending (T010) |
| Summary statistics | pending (T010) |
| Build SHA | pending (T010) |

---

## 5. Synthetic fixture and budget proof

| Field | Value |
| --- | --- |
| Fixture shape / liked-source count | 725 (fivefold vs ticket baseline 145) `[assumed]` |
| Retained maps representative? | **Yes — measured, and this is the blocker.** A 725-source fixture built from real home-like retained maps cannot fit the budget: measured 449-source compact profile is 1,377,125 B (131.3% of ceiling) and every 725-source projection from the measured distribution is over 100% (see section 1c). A fixture that *does* pass would have to thin the retained cast maps, which the ticket forbids. |
| 384-float vectors location | uncached inputs only |
| Compact serialized bytes | Current 4-source home profile: 41,908 B live (22,776 B of it `LikedSources`, of which 18,818 B is per-source plot vectors). At library scale (449 sources, vectors removed): 1,377,125 B. |
| Strictly below 80% of effective ceiling? | Current home: **yes** (5.0% of the 838,860 B budget). 725 sources on realistic retained maps: **no** — measured/projected 155–220% of ceiling. Section 1c gate **FAILED**. |
| Current home also below 80%? | **Yes** — 41,908 B = 4.0% of ceiling, 5.0% of the 838,860 B strict budget. |
| Actual cache admission / reuse evidence | Real `GetOrCreateAsync` admission through the registered singleton at f6647681: first read serializes once (`SerializeCount` 1, `LastSerializedBytes` 41,908) and a repeat read reuses the entry without re-serializing. See section 2 for the over-ceiling admission semantics. |
| Bounded-query evidence (one additional batched query) | pending (T007/T009 — US3; this baseline predates the extra liked-source query) |

---

## 6. Observability / warning

| Field | Value |
| --- | --- |
| Warning fraction source of truth | pending (default 0.8; existing options) — T008 |
| Structured payload-byte observation present? | pending (T008) |
| Effective-budget observation present? | pending (T008) |
| Warning-fraction boundary tests (below / equal / above) | pending (T007) |
| Payload-ceiling boundary tests (below / equal / above as applicable; T002 no-distributed-cache semantics) | pending (T007). **T002 supplies the semantics they must encode**: below and equal-to ceiling admit silently and reuse; above ceiling logs `Cache MaximumPayloadBytes (1048576) exceeded.` at `Error`, throws nothing, and (local cache enabled, no distributed cache) still serves and reuses the oversized entry. A boundary test asserting a throw would be asserting behaviour this deployment does not have. |
| Invalid-configuration / ValidateOnStart regression for warning setting | pending (T007) |
| No per-hit re-serialization on warm reuse? | pending (T007/T008); baseline shows serialize count stays 1 across repeat reads on the unchanged representation |
| No vector/identity/secret logs? | pending (T008); baseline logging emitted only the HybridCache ceiling `Error` line |

---

## 7. Gate receipts

| Gate | Command | HEAD | Exit / verdict |
| --- | --- | --- | --- |
| Roslyn analyzers | `./scripts/run-roslyn-analyzers.ps1` | pending | pending (T011) |
| Cyclomatic complexity | `./scripts/run-cyclomatic-complexity.ps1` | pending | pending (T011) |
| InspectCode | `./scripts/run-jetbrains-inspectcode.ps1` | pending | pending (T011) |
| Property tests | `./scripts/run-property-tests.ps1` | pending | pending (T011) |
| Vulnerable packages | `./scripts/run-vulnerable-packages.ps1` | pending | pending (T011) |
| Format | `dotnet format --verify-no-changes` | pending | pending (T011) |
| Full tests | `dotnet test` | pending | pending (T011) |
| Mutation (if eligible) | `./scripts/run-mutation.ps1` | pending | pending (T011) |
| Web gates | `./scripts/run-web-gates.ps1` | pending | pending (T011) |

**Rules**: Record exact checked SHA. Never rename SKIPPED (scope-empty) / SKIP / N/A / Could not run to PASS. Never lower thresholds. Surviving mutants require tests. No gate was run in T002 — the measurement probe was untracked and is not a gate receipt.

---

## 8. Post-merge home promotion and 24-hour observation

| Field | Value |
| --- | --- |
| Delivery PR / merge (user-owned) | pending |
| Authorized operator promotion receipt | pending |
| Deployed SHA | pending |
| Observation start (with time zone) | pending |
| Observation end (with time zone) | pending |
| Span ≥ 24 hours? | pending |
| Exercised workload / profile count | pending |
| `MaximumPayloadBytesExceeded` count | pending (must be **0**) |
| Measured warm-cache latency | pending |
| Runtime acceptance status | **runtime-pending** |

**Rules**: No seat merges or implies deployment authorization. Delivery remains `runtime-pending` until this receipt exists.

## PA1 mandatory evidence additions

Sequencing (brief.md PA1 amendment Serializer setup before baseline / CONCLUSIONS.md at 85feb708): T001 read-only → T001b serializer-only install/proof → T002 baseline through live seam → T003+. T002 hard stop before T003+: fixed pre-change scoring snapshot and measured plot/maps/envelope split plus realistic retained-content 725-source projection must demonstrate feasibility below the frozen 80% effective budget. Missing/unavailable/infeasible proof stops for recon/Patron; pending is not permission to edit. Original ticket/recon citations refer to the earlier snapshot; targeted seams are current note lines 3-7. PA1 amendment Explicit shared serializer (fca46e8c) plus sequencing ruling (85feb708) place shared `IHybridCacheSerializer<CachedProfile>` registration/injection in T001b (already-authorized persistence files + registration/HybridCache suites), not T008.

| Additional receipt | Value |
| --- | --- |
| T001 read-only pickup recon / Gauge baseline (no production edits) | recorded in task note `DEV-510` (analyzers SKIPPED scope-empty, vulnerability PASS, full tests FAIL with the known Scalar-test disposition); no all-green baseline claimed |
| Original unmodified pickup SHA | `79e14969b1832db9075de96e6087cfa101fde20a` |
| T001b compiler/type-check of pinned shared `IHybridCacheSerializer<CachedProfile>` registration signature and shared-envelope accessibility (authorized worktree) | complete at f6647681 — `dotnet build` exit 0, 0 errors; `AddSerializer<CachedProfile>(instance)` accepts the shared singleton and the internal envelope is reachable |
| T001b runtime same-singleton exact-envelope measurement + real `GetOrCreateAsync` admission proof | complete at f6647681 — 10/10 `TasteProfileCacheRegistrationTests` + 4/4 `HybridCachedRecommendationProfileSourceCacheTests` pass; T002 independently re-proved the same singleton measurement and admission against home data |
| T001b serializer-seam SHA + serializer/configuration provenance | `f6647681239dfec87856a1ebad0af5930b6ae422`; `CachedProfileSerializer` singleton, `JsonSerializerDefaults.Web`, `TasteProfileCacheOptions.Expiration` 00:05:00, ceiling 1,048,576 B |
| Shared singleton serializer/options identity for admission and construction-only exact-envelope measurement | proven — constructor-only measurement and real admission both invoke the same registered singleton on `CachedProfile(Profile, Generation, CreatedAt)` |
| Retained maps/envelope measured bytes and representative 725-source projection (T002) | **measured and INFEASIBLE** — see section 1c: 449 sources = 1,377,125 B (131.3%); 725-source projections 1,628,412–2,303,387 B (155–220%). Envelope 307 B; `Liked` aggregate cast map is the dominant term (800,238 B at 449 sources) |
| T002 parity snapshot and feasibility checkpoint passed? | **Snapshot: captured** (section 3, digest `9b66ab6e…e259a2`). **Feasibility checkpoint: FAILED** — T003+ blocked for recon/Patron disposition. 725 and 0.8 remain frozen; no thinning applied or proposed by Build |
| Post-change profile-cache-hit + uncached embedding batch query count/cost and vector volume (4b/T010) | pending |
| Empty sources zero additional query; missing/partial rows; request key equality; cancellation | pending (T003) |
| Compact-profile runtime observation/admission byte-decision agreement below/equal/above effective ceiling (T007/T008; reuse T001b seam, no reinstall) | pending; T002 supplies the observed over-ceiling semantics they must match |
| Real `GetOrCreateAsync` admission/reuse without per-hit re-serialization | proven for the pre-change representation (serialize count stays 1 across repeat reads); compact-profile reuse pending T007/T008 |
| Separate no-`IDistributedCache` limit-behaviour proof (T002) | **complete** — section 2: no rejection or truncation; `Error` log `Cache MaximumPayloadBytes (1048576) exceeded.`, no throw, oversized entry still served/reused while local cache is enabled |
| Finite warning domain: zero, one, negative, >1, NaN, +/-infinity | pending (T007) |
| Numeric-only PayloadBytes/BudgetBytes/Fraction LoggerMessage fields | pending (T008) |
| Over-ceiling warn before admission, coalesced successful return, no false warm hit | pending (T007/T008) |
| Legacy key ignored; versioned compact rebuild; unchanged generation/expiry | pending (T005b/T009) |

Admission and construction-only byte measurement MUST use the same registered singleton serializer/options on the exact `CachedProfile(Profile, Generation, CreatedAt)` envelope; thin internal serializer and internal envelope accessibility are permitted inside the two authorized persistence files during T001b; no new layer/package; do not invent an exact `AddSerializer` signature; never reflection or independently assumed JSON defaults; no assumed serializer invocation without distributed cache. T001b compilation alone is not runtime proof of byte/admission agreement; T002 must still prove separate no-distributed-cache behaviour. T007/T008 reuse the T001b seam. Mutation from worktrees: pwsh -NoProfile -File ./scripts/run-mutation.ps1 without --since; preserve configured kill-share/file-span/timeout safeguards. Home measurement/promotion and prior gate/runtime-pending statuses remain as on the status board above. Round-1 derivative repair awaits Keel independent reread; prior no-outstanding claim is withdrawn until that reread.
