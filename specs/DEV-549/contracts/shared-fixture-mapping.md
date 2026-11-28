# Contract: Shared fixture mapping subset (DEV-549)

**Status**: Draft  
**Authority**: `brief.md` Q1–Q2, Q7 and Step 6 plan-challenge decisions; `CONCLUSIONS.md` Step 6; ticket Wanted 1–2  
**Consumers**: `tests/LamuFlix.IntegrationTests` WireMock loader (via existing `LoopbackWireMock` host) and `web/e2e/stub.mjs`

## Format

- Checked-in WireMock JSON mapping files under `tests/fakes/<service>/`.
- Optional response bodies via `bodyFileName` referring to files in the same service folder (or the relative path WireMock already expects for mapping folders).
- Fixtures are scrubbed: no real API key, token, or account data.
- Configuration placeholders use the non-secret literal `fake-key` where a credential value is required in checked-in fake settings.

## Supported request matchers (both readers MUST implement)

| Field | Meaning |
|---|---|
| method | HTTP method |
| path or urlPath | Path matcher (exactly one of these forms as used by the fixture set) |
| query params | Functional query-string constraints present in the fixture set (see credential exclusions) |

## Supported response fields

| Field | Meaning |
|---|---|
| status | HTTP status code |
| headers | Response headers |
| jsonBody | Inline JSON body |
| bodyFileName | External body file |

## Credential-only query parameter exclusions

Both readers (`LoopbackWireMock` / IntegrationTests loader and `stub.mjs`) MUST exclude only the listed credential-only query parameters from canonical match criteria. Functional query parameters remain matched. Fake-mode configuration continues to use `fake-key` placeholders. OMDb zero-copy consolidation is preserved.

| Provider folder | Excluded credential-only query parameter(s) | Notes |
|---|---|---|
| `tests/fakes/omdb/` | `apikey` | Matches OMDb adapter query shape |
| `tests/fakes/tmdb/` | `api_key` | Matches TMDb adapter query shape |
| `tests/fakes/opensubtitles/` | *(none)* | OpenSubtitles credentials use the `Api-Key` request header, not a query parameter |
| `tests/fakes/ollama/` | *(none)* | Canned embed response; no provider credential query param |

An alternate synthetic credential value (not `fake-key` and not a live key) MUST still match the shared fixture in **both** hosts for each provider that lists an exclusion above. Matching MUST NOT depend on the credential value.

## Failure rule

- An unsupported matcher or field encountered by either reader MUST fail load/startup with a clear error identifying the mapping. Silent ignore is forbidden.
- Credential exclusions are the explicit list above only; inventing additional ignored params is out of scope.

## Scrub and secret-scan gate (existing tooling)

- Before staging mappings and detached body files under `tests/fakes/`, run the repo’s existing gitleaks / secret-scan rules against those paths and perform a manual scrub review for account data. Record redacted outcomes (no secret values in receipts).
- Delivery head MUST carry the hosted gitleaks workflow receipt (`.github/workflows/gitleaks.yml`). No new secret-scan tooling, dependency, or CI workflow edit for this ticket. Planning does not run delivery checks.

## Host-local (NOT in `tests/fakes`)

- E2E control routes under the existing stub control prefix.
- Fault injection, malformed bodies, and stateful controls that already live inline in adapter tests or `stub.mjs` (except OMDb copies that must move for Done-when consolidation).

## Services in this ticket

| Folder | Contents |
|---|---|
| `tests/fakes/omdb/` | Full OMDb set (every former copy from WireMock tests and `stub.mjs`) |
| `tests/fakes/tmdb/` | Canonical TMDb success bodies for browse flows |
| `tests/fakes/opensubtitles/` | Canonical OpenSubtitles success bodies for search + one download |
| `tests/fakes/ollama/` | Existing canned `/api/embed` response only |

DEV-554–DEV-563 add further service folders later; not stubbed here.

## Non-goals

- New npm/NuGet parser dependency or new secret-scan tooling.
- Moving the e2e stub to a WireMock container.
- Replacing MSW web unit handlers.
- Replacing real `OllamaIntegration` embeddings.
