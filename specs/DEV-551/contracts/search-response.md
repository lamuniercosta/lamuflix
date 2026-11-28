# DEV-551 search response contract (additive)

Authority: `brief.md` / `CONCLUSIONS.md` Q1–Q3, Q7; constitution Enumerations serialized by `Name`. This file names the **additive** wire shape for Spec Kit and OpenAPI generation. It does not invent routes or remove legacy fields.

## Request (unchanged)

- `GET /api/movies/{id}/subtitles/search?language={code}`
- No provider query parameter (Q1).
- One requested language per call (Q5).

## Legacy response fields (retained, unchanged)

**Do not restate unverified field/type/nullability tables here.** Recon-DEV-551 names the DTO records (`SubtitleSearchResponse.cs` search candidate, quota, availability, and response records) but does not enumerate exact legacy wire fields, types, or nullability. Source of truth for the unchanged legacy contract is the existing runtime/OpenAPI contract for those DTOs. DEV-551 keeps every legacy field and value semantics; baseline OpenSubtitles-only parity (including each legacy projection rule below) is proven in **T010** wire tests (Q3; SC-001).

Legacy quota projects the **first usable provider in configured order**, never summed (Q3).

Legacy download availability = library policy **AND** at least one provider permits download (Q3).

## Additive candidate field

| Field | Shape | Notes |
| --- | --- | --- |
| `candidates[].providerId` | string | Stable configured provider name. **Never** the server-owned file reference (Q3). |

## Additive providers array

Ordered array including **off** rows for configured providers skipped before I/O (Q3).

| Field | Shape | Notes |
| --- | --- | --- |
| `providers[].providerId` | string | Stable configured provider name |
| `providers[].state` | string (closed) | See states below; C# type is an Enumeration (or equivalent) serialized by `Name` |
| `providers[].reason` | string (closed) | Safe code only — never upstream message text, URLs, or credentials (Q3); C# type is an Enumeration (or equivalent) serialized by `Name` |
| `providers[].quota` | object\|null | Same shape as legacy `quota`; null when not observed |

### Closed `state` values (Patron-approved Ledger S5; Q2–Q5/Q9 outcomes)

| State | Meaning |
| --- | --- |
| `off` | Configured provider skipped before I/O (disabled or missing required credentials) |
| `unusable` | Provider-local startup refusal of an **unsafe** (non-HTTPS) base URL; never sends requests (Q2). Credential-free dual opt-in plain HTTP is a separate allowed path under Q4, not this refusal. |
| `ok` | Settled search completed without provider-local failure (candidates may be empty) |
| `quota` | Search throttling / search 429 mapped to this provider |
| `timeout` | Provider branch hit its independent timeout while the caller remained active |
| `failed` | Provider-local failure outcome: either no formable declared search input (no I/O), or upstream failure / 5xx class after I/O began (Q5). Deterministic: `no_search_input` and `provider_unavailable` both use `failed`. |

Caller cancellation is **not** a provider state; it propagates as cancellation (Q5).

### Closed `reason` values (deterministic; Patron-approved Ledger S5; subset aligns with existing `SubtitleProblemReasons` where applicable)

Reasons are **exclusive** by the predicate/precedence rules below. Simultaneous faults take the first matching pre-I/O rule; request-time reasons apply only after the provider is classified usable.

#### Pre-I/O / startup classification (exclusive; first match wins; Q2/Q4/Q8)

| Precedence | Predicate | State | Reason |
| --- | --- | --- | --- |
| 1 | `Enabled=false` (disabled / skipped before I/O) | `off` | `off` |
| 2 | Else unsafe (non-HTTPS) base URL refused at startup (Q2/Q4). Scoped to unsafe URLs; does not forbid credential-free dual opt-in plain HTTP under Q4. | `unusable` | `unsafe_base_url` |
| 3 | Else missing/invalid required configuration: missing required credentials **or** invalid timeout **or** invalid minimum gap (Q2/Q8). Contributes to Q2 409 when no usable provider remains. | `off` | `provider_not_configured` |
| — | Else provider is usable for search attempts | *(proceed to request-time)* | — |

#### Request-time / post-settlement reasons (only when usable; exclusive by observed outcome; Q5)

| Reason | Required state | When (exactly one applies) |
| --- | --- | --- |
| `ok` | `ok` | Settled successful search that produced one or more candidates |
| `empty` | `ok` | Settled successful empty search (zero candidates from that provider) |
| `search_rate_limited` | `quota` | Search 429 (not download-quota invention) |
| `provider_timeout` | `timeout` | Independent provider timeout |
| `provider_unavailable` | `failed` | Upstream failure / 5xx class after I/O began |
| `no_search_input` | `failed` | No formable declared search input after skipping unavailable identifiers; safe provider-local failure **without** requiring I/O (Q5). Not used for a missing earlier identifier when a later declared input is available. |

Every settled provider row MUST carry exactly one of the reasons above. There is no optional/empty reason slot. `off`, `provider_not_configured`, `unsafe_base_url`, and `no_search_input` never overlap: the first three are pre-I/O exclusive by precedence 1–3; `no_search_input` is request-time only on a usable provider.

DEV-552 owns any extension of per-provider fields beyond this closed set (Q3).

## Settlement and usability freeze (Q2/Q3/Q5; brief.md:69)

- **Usability freeze**: Provider usability for the request is fixed at **request-time acquisition configuration validation**. Subsequent timeout, quota, failure, or no formable search input settles that provider’s outcome and **MUST NOT** flip a configured-usable multi-provider search into the Q2 no-usable 409.
- **Pre-I/O settlement**: Classified `off` and `unusable` (startup-refused) rows are **settled outcomes** under Q3 as soon as classification completes. A provider can settle before upstream I/O.
- **Q2 search 409**: Always includes additive ProblemDetails extension key `providers` with the classified rows in configured order (`providerId`, `state`, safe `reason`, nullable `quota`; quota null when unobserved).
- **Request-level errors**: Existing validation/domain errors that take precedence **before or instead of** provider settlement **omit** the `providers` key entirely.
- **Legacy settled errors**: A legacy search error that has settled provider outcomes carries the same `providers` extension. Preserve legacy HTTP statuses and existing ProblemDetails fields.

## HTTP status compatibility (deterministic; disjoint; first match wins)

Evaluate in this precedence order. Rows are mutually exclusive partitions of the search response path (Q2/Q3).

| Precedence | Situation | Status | Notes |
| --- | --- | --- | --- |
| 1 | Request-level validation / domain errors (before or instead of settled provider outcomes) | existing | Unchanged request-level errors (Q3). ProblemDetails `providers` key **absent**. |
| 2 | No enabled provider usable / all unusable (including sole OpenSubtitles missing/invalid) | 409 | Q2 / Constitution VII request-time acquisition configuration validation. Usability frozen here. ProblemDetails `providers` **present** with ordered classified off/unusable rows. |
| 3 | **One-provider configuration**: sole configured OpenSubtitles is usable (no configured siblings) | existing legacy statuses and values | Unchanged under one-provider configuration (Q3); additive `providerId` / `providers` still present. May include **non-200** settled legacy outcomes — assert against baseline; **do not invent** a status. Timeout/quota/failure/no-formable-input settle outcomes without changing this usability partition. |
| 4 | **Multi-provider configuration** with ≥1 usable provider after config-time usability freeze (zero or more candidates) | 200 | Covers one usable plus off/refused siblings **and** multi-usable (including zero-candidate). Off/refused siblings and later timeout/quota/failure/no-formable-input are named outcomes, **not** 409 (Q2/Q3; usability freeze). |

Partition rule: “one usable plus off/refused” requires configured siblings → precedence 4 (200). “Single configured OpenSubtitles usable” has no siblings → precedence 3 (legacy). They do not overlap. Timeout/quota/failure/no-formable-input never move a request from precedence 3/4 into precedence 2.

T010 wire tests MUST cover each precedence branch: **request-level existing errors** (`providers` absent); all unusable → 409 (`providers` present, ordered classified rows); one usable plus off/refused → 200; multi-usable zero-candidate → 200; **simultaneous-fault first-match** pre-I/O cases; **and** one-provider OpenSubtitles legacy outcomes including any legacy **non-200** settled baseline outcome plus each legacy quota and download-availability projection — without guessing status (unchanged baseline for legacy assertions).

## ProblemDetails extension (settled **search** errors only)

Additive extension key **`providers`** carries the same ordered provider outcome rows as the search-response `providers` array (safe codes only; key name matches the ruled providers shape — Q3). This extension is part of the **search** wire contract only (plan-challenge M4):

| Error path | Extension |
| --- | --- |
| Request-level validation/domain errors preempting settlement | **Omit** `providers` |
| Q2 no-usable search 409 (classified off/unusable rows settled pre-I/O) | **Include** ordered `providers` (quota null when unobserved) |
| Legacy search error with settled provider outcomes | **Include** `providers` |

## Download error contract (unchanged; no `providers`)

Download retains the **existing** ProblemDetails shape and statuses under Q2 and ADR 0041. It does **not** carry the additive `providers` extension.

| Download path | Contract |
| --- | --- |
| ADR 0041 read-only precedence | Evaluated **before** acquisition validation; existing read-only response unchanged |
| Sole missing/invalid / bound-provider unusable acquisition 409 | Existing download 409 ProblemDetails **without** `providers`; zero paid calls (T015) |
| Stale / invalid / evicted Candidate ID | Existing stale/invalid response unchanged; zero paid calls (T015) |

T015 verifies download status precedence, unchanged ProblemDetails (absence of `providers`), and zero paid calls. T026 remains search-only generation/proof and does not assert download ProblemDetails.

## Truncation

When one aggregate exceeds Candidate ID capacity, return only highest-ranked bindable rows (Q6 order). **No** new response field for truncation; log a safe count only (Q7). Every Candidate ID returned in that response MUST resolve immediately (not already stale/evicted by that same search).

## Generation and coverage

C# DTO → `web/src/api/openapi.json` → generated `web/src/api/types.ts` (contract chain). Closed `state` and `reason` MUST be Enumerations (or equivalent) at the Core subtitle outcome boundary, serialized by JSON `Name` as strings in the approved API DTO scope (constitution API and Contract Rules; Ledger S2; Patron S5). Preserve all existing web tests (Q10).

Before and after generation, require:

1. **Wire-level coverage** — IntegrationTests (T010) assert the additive JSON fields, exclusive `state`/`reason` pairs under the pre-I/O precedence and request-time rules above (including **simultaneous-fault first-match** cases and invalid timeout/gap → `provider_not_configured`), the disjoint status precedence matrix (**request-level existing errors** with `providers` absent; all unusable → 409 with ordered `providers` present; one usable + off/refused → 200; multi-usable zero-candidate → 200; one-provider OpenSubtitles legacy including any baseline non-200 settled outcome and each legacy projection against unchanged baseline), **discriminating Q2 usability-freeze fixture** (multi-provider configuration whose only config-usable provider settles with timeout, search 429, failure, or no formable input, with an off/refused sibling — for each of those four settlements assert **200**, **empty candidates**, and ordered named `providers` outcomes; not Q2 409), and ProblemDetails extension presence/absence rules above.
2. **Generated-schema coverage** — OpenAPI/TypeScript drift checks and regenerated `types.ts` include additive candidate/`providers` fields and the ProblemDetails `providers` extension.
