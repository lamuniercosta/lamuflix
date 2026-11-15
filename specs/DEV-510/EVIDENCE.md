# DEV-510 Operational runbook and evidence slots

Template owned by Quill drafting from `brief.md` Operational runbook section (Q6/Q7). Slots start **pending**. Fill with measured results only — never substitute ticket estimates (~60% reduction / ~1.3 KB/source) or invent latency targets. No secrets, connection strings, tokens, vectors, profile content, or identity dumps. Prefer safe references to disposable evidence locations.

**Worktree (spec phase)**: `F:/Dev/LamuFlix.worktrees/feature-510-spec`  
**Branch**: `feature/510-spec`  
**Recon baseline HEAD**: `6edd0d16f0dd51950c56e21f2c866467dc5dff77`

## Status board

| Stage | Status | Notes |
| --- | --- | --- |
| Planning / freeze | PLAN FROZEN | Keel independent reread, 2026-11-15; see PLAN_ADJUDICATION.md |
| Gate 1 (spec PR merge) | pending | User merges; Keel records receipt |
| Implementation verification | pending | Gates + parity + size/cache tests at exact SHA |
| Home promotion | pending | After user-owned delivery merge; authorized operator only |
| Runtime acceptance (≥24h) | runtime-pending | Remains open until operator receipt exists |

---

## 1. Baseline (pre-change)

| Field | Value |
| --- | --- |
| Baseline SHA | pending |
| Configuration / serializer / options identity (safe) | pending |
| Effective payload ceiling (bytes) | pending: read IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes |
| Observed liked-source / library count | pending |
| Serialized `CachedProfile` total bytes | pending |
| Component split (bytes) | pending |
| Calculated headroom vs ceiling / vs 80% budget | pending |
| Safe evidence references | pending |

**Rules**: Measure with production-equivalent serializer/options including `Generation` and `CreatedAt`. Do not mark complete if unavailable — leave pending / Could not run. Capture before representation change.

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
| Reproduction setup (safe) | pending |
| Whether / where 1,048,576-byte rejection occurs | pending |
| Distinct from serialization-only measurement? | pending |
| Actual observations / safe log references | pending |
| Conclusion | pending |

---

## 3. Fixed scoring snapshot (parity baseline)

| Field | Value |
| --- | --- |
| Snapshot / input provenance (safe) | pending |
| Candidate-set provenance (safe) | pending |
| Pre-change SHA | pending |
| Pre-change title/order | pending |
| Pre-change exact scores | pending |
| Pre-change `Because you liked X` reasons | pending |
| Post-change SHA | pending |
| Post-change title/order / scores / reasons | pending |
| Discrepancies | pending (none allowed for acceptance) |

**Rules**: Exact equality; unchanged formula/weights; no committed sensitive home content.

---

## 4. Matched `for-you` latency

Matched inputs and cache-state classification must be comparable across pre/post. Keep pre-change and post-change timing samples and statistics in separate slots. Invented speedup target used? **No** — publish measurements only. Unmeasured fields stay pending / Could not run.

### 4a. Pre-change (baseline)

| Field | Value |
| --- | --- |
| Inputs / cache-state classification | pending |
| Cold / uncached timing samples | pending |
| Warm-cache timing samples | pending |
| Sample count | pending |
| Summary statistics | pending |
| Build SHA | pending |

### 4b. Post-change

| Field | Value |
| --- | --- |
| Inputs / cache-state classification (matched to 4a) | pending |
| Cold / uncached timing samples | pending |
| Warm-cache timing samples | pending |
| Sample count | pending |
| Summary statistics | pending |
| Build SHA | pending |

---

## 5. Synthetic fixture and budget proof

| Field | Value |
| --- | --- |
| Fixture shape / liked-source count | 725 (fivefold vs ticket baseline 145) `[assumed]` |
| Retained maps representative? | pending |
| 384-float vectors location | uncached inputs only |
| Compact serialized bytes | pending |
| Strictly below 80% of effective ceiling? | pending |
| Current home also below 80%? | pending |
| Actual cache admission / reuse evidence | pending |
| Bounded-query evidence (one additional batched query) | pending |

---

## 6. Observability / warning

| Field | Value |
| --- | --- |
| Warning fraction source of truth | pending (default 0.8; existing options) |
| Structured payload-byte observation present? | pending |
| Effective-budget observation present? | pending |
| Warning-fraction boundary tests (below / equal / above) | pending |
| Payload-ceiling boundary tests (below / equal / above as applicable; T002 no-distributed-cache semantics) | pending |
| Invalid-configuration / ValidateOnStart regression for warning setting | pending |
| No per-hit re-serialization on warm reuse? | pending |
| No vector/identity/secret logs? | pending |

---

## 7. Gate receipts

| Gate | Command | HEAD | Exit / verdict |
| --- | --- | --- | --- |
| Roslyn analyzers | `./scripts/run-roslyn-analyzers.ps1` | pending | pending |
| Cyclomatic complexity | `./scripts/run-cyclomatic-complexity.ps1` | pending | pending |
| InspectCode | `./scripts/run-jetbrains-inspectcode.ps1` | pending | pending |
| Property tests | `./scripts/run-property-tests.ps1` | pending | pending |
| Vulnerable packages | `./scripts/run-vulnerable-packages.ps1` | pending | pending |
| Format | `dotnet format --verify-no-changes` | pending | pending |
| Full tests | `dotnet test` | pending | pending |
| Mutation (if eligible) | `./scripts/run-mutation.ps1` | pending | pending |
| Web gates | `./scripts/run-web-gates.ps1` | pending | pending |

**Rules**: Record exact checked SHA. Never rename SKIPPED (scope-empty) / SKIP / N/A / Could not run to PASS. Never lower thresholds. Surviving mutants require tests.

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

T002 hard stop before T003+: fixed pre-change scoring snapshot and measured plot/maps/envelope split plus realistic retained-content 725-source projection must demonstrate feasibility below the frozen 80% effective budget. Missing/unavailable/infeasible proof stops for recon/Patron; pending is not permission to edit. Original ticket/recon citations refer to the earlier snapshot; targeted seams are current note lines 3-7.

| Additional receipt | Value |
| --- | --- |
| Shared registered serializer/options identity and pinned API verified (T001) | pending |
| Retained maps/envelope measured bytes and representative 725-source projection (T002) | pending |
| T002 parity snapshot and feasibility checkpoint passed? | pending; T003+ blocked |
| Post-change profile-cache-hit + uncached embedding batch query count/cost and vector volume (4b/T010) | pending |
| Empty sources zero additional query; missing/partial rows; request key equality; cancellation | pending |
| Observation/admission byte-decision agreement below/equal/above effective ceiling | pending |
| Finite warning domain: zero, one, negative, >1, NaN, +/-infinity | pending |
| Numeric-only PayloadBytes/BudgetBytes/Fraction LoggerMessage fields | pending |
| Over-ceiling warn before admission, coalesced successful return, no false warm hit | pending |
| Legacy key ignored; versioned compact rebuild; unchanged generation/expiry | pending |

Use the same registered serializer/options on the same envelope, never independent JSON defaults. Mutation from worktrees: pwsh -NoProfile -File ./scripts/run-mutation.ps1 without --since; preserve configured kill-share/file-span/timeout safeguards.
