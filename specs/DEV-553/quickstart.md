# Quickstart / verification scenarios: DEV-553

**Authority**: `brief.md` Task order; Done when; Q2, Q4, Q5, Q7–Q9; `spec.md` SMOKE-001–003 / FR-008 / FR-011–013.

## Preconditions

1. Gate 1 Spec Kit PR merged (ordinary owner Gate 1; Spec Kit PR stays `specs/**` only).
2. DEV-551 delivery merged; **Wisp** pickup confirms provider ID and selected-candidate evidence for the record (else stop `needs recon`). **Keel** pickup reanalysis is read-only.
3. ADR 0054 proposed draft available in `PRIVATE_TICKET_DIR` (`0054-subtitle-download-record-persistence-port.md`; brief/conclusions durable fallback) for Build acceptance with the port. Spec Kit PR excludes the ADR file.
4. Compose (or local) API + web with subtitle download configured.

## Backend proofs (automated)

| Stage | Scenario | Expectation |
|---|---|---|
| Persistence | Canonical key / adjacent vs `Subs\` / concurrent upsert | Unique discovery-key join; adjacent and `Subs\` paths distinct; concurrent same-key writes from separate contexts both complete with one complete row |
| Unit (success) | Successful publish + upsert | Record stores provider/release/candidate-language/hash/score-or-null/UTC for canonical discovery relative path; returned Core track projects the same provider/release with discovery-derived label/language and existing identity |
| Unit (success) | Null-score successful write | Unknown match percent still writes the row with a null score |
| Unit (success) | Publication failure | No provenance write |
| Unit (failure path) | Upsert throw / ambiguous / cancelled after publish | Immediate success response; discovery-derived label; null provenance on returned track; file discoverable; Error log; no delete/rollback/retry |
| Unit / property | Ranking | Hash → known score desc (missing scores after known at same hash rank) → UTC desc; stable ties; unrecorded last; first-appearance partitions by discovery-derived language |
| Unit (list) | Join / language / fallback / deleted file / Q9 | Batch enrich by canonical discovery key; discovery-derived language wins over conflicting stored candidate language; null provenance fallback; missing file omitted while row may remain; after same-key failed/ambiguous replacement, list shows old row if replacement did not commit or new row if it committed |
| Regression | Existing `SubtitleSidecarDiscoveryTests` | Pass byte-stable — **no** test edits |
| Endpoint (wire, before OpenAPI) | Successful download + list projections | Distinct `provider` / `releaseName` when known; discovery-derived `label` preserved on known-provenance responses |
| Endpoint (wire) | Explicit JSON nulls | Both responses emit JSON `null` for unknown fields independently, including fallback |
| Endpoint (wire) | Two English downloads, two providers | Listed with distinct provider/release |
| Endpoint (wire) | Hand-added `Film.en.srt` | Today’s label; null `provider` / `releaseName` |
| Endpoint (wire) | Row exists, file deleted | Track not listed; row retained |
| Endpoint (wire) | Same-key re-download after external delete | Upserted provenance matches the new download |
| Endpoint (wire) | Subs publish-then-list | Same track ID and provenance round-trip |

Endpoint assertions are authored before DTO mappings, pass after both mappings, and must be green before OpenAPI → TypeScript regeneration and web work (`brief.md` Task order items 3–4). After T012/T014, T015 confirms OpenAPI string-or-null schema and TypeScript `string | null` for both fields plus drift.

## Web proofs (automated)

| Scenario | Expectation |
|---|---|
| Language headings | One heading per language keyed by wire `language`; first-appearance order (FR-011) |
| Labels | Compose `release name · provider` only when both fields known; otherwise wire label |
| Partial provenance | One known field does not invent a half label; selector keeps wire label (FR-008) |
| Manual track | Hand-added / unrecorded track remains selectable (SC-002) |
| Order | Incoming backend order preserved inside each group |
| Off | Single Off outside headed groups |
| Download refresh | Downloaded track stays selected through reorder/relabel |
| In-flight choice | Newer manual selection or Off wins over stale refresh |
| Absent track | Existing absent-track handling preserved as regression (FR-013) |
| Types | Generated `SubtitleTrackDto` only |

## Compose smoke (SMOKE-001–003) — downstream checkpoint

Not a local Build task. Run against the assembled app after the applicable gate set on the rebased delivery head (downstream sequence: Gauge Phase 3 step 5 → Size M review → rebase + rebased-head gates → smoke), matching `plan.md` / `tasks.md`:

1. Two-provider English downloads → headed English group with two `release · provider` labels and one Off.
2. Hand-added sidecar → filename-derived label under its language heading; selectable.
3. Download from the player → selection survives the reload.

## Gate reminders (Gauge Phase 3 step 5 + rebased head)

Exact-diff analytics on the delivery revision, then the applicable set: Roslyn, cyclomatic (incl. refactor threshold), InspectCode, mutation, property tests, vulnerable packages, format, full `dotnet test`, web gates, OpenAPI/types drift. Repeat the applicable set on the rebased delivery head. Report exit-2 as the gate reports them — never as PASS. Exit 1 or `Could not run` blocks (`brief.md` Task order item 5). Do not describe Gauge Phase 3 step 5 as hosted.
