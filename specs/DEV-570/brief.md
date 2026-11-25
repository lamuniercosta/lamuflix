# DEV-570 — Movie details About section

## Authority and scope

- Size M. Intake `feature/570-spec` at `ef6dd3e694aa82d3bddd7effe3e31f96b5289a75` in `F:/Dev/LamuFlix.worktrees/feature-570-spec`. The subsequent commits on the spec branch record Patron's rulings.
- Ticket authority is the verbatim Scope and Acceptance in `recon-DEV-570` lines 49–68, per `specs/PRODUCT.md` §3. The implementation map is in recon lines 15–38. See [CONCLUSIONS.md](CONCLUSIONS.md) for the cited §2.3 rulings and [ASSUMPTIONS.md](ASSUMPTIONS.md) for the Writer/Writers label.
- Frozen delivery scope covers projecting stored writers, genres and collection name through the existing movie details response, displaying them with directors and cast in About, preserving the existing cast interaction, and verifying that flow. Maturity rating and mood tags are excluded. Anything else is a follow-up issue, not a finding in this round.
- Palette `design.md` does not apply. The ticket has only `size:M` and no `ui:` tag (recon line 76 and task-pipeline Phase 2 step 4). Quill should capture the ticket's layout and accessibility requirements in the ordinary spec and plan.

## Grill record

### Q1 — What happens to cast names without an actor selection?

**Keel recommendation** — Reuse the existing page selection path for mapped names. Keep unmapped names as plain text. Preserve comma-separated stored order and add a regression test for both paths.

**Patron ruling** — Approved. A mapped name in About opens the existing MoreWith panel. Unmapped names stay plain text. Keep the existing hero cast behavior. Basis comes from recon lines 24–26 and 52–55 and PRODUCT §§3 and 5. Recorded at `fab6c94e`.

### Q2 — How does the response represent missing values?

**Keel recommendation** — When metadata exists, return `writers` and `genres` as present string arrays, empty when missing, and `collectionName` as a present nullable string. Keep outer `metadata` null when absent. Project the stored values as-is and verify actual endpoint JSON. Refresh OpenAPI, its baseline and generated TypeScript before aligning Zod and wiring the page.

**Patron ruling** — Approved. The existing route and mapper project the three ticket fields. Test actual JSON for populated and missing cases. Basis comes from recon lines 16–23, 35, 51 and 66–67 and PRODUCT §§3 and 5. Recorded at `1b819604`.

### Q3 — What should render when no rows are eligible?

**Keel recommendation** — Hide the entire About section for null metadata or when every row has no data. With any eligible row, render the heading-labelled description list in ticket order, hide empty rows, apply count-based director/writer labels and wrap values at phone width. Keep the current null-metadata notice.

**Patron ruling** — Approved. Use Director/Directors and Writer/Writers by displayed count. Use the fixed labels Cast, Genres and Collection. Cover full, missing-writers-only, all-empty and null-metadata cases, singular/plural labels and accessible naming. Writer/Writers is logged as `[assumed]`. Basis comes from recon lines 24, 28 and 52–68 and PRODUCT §§3 and 4. Recorded at `d29282fb`.

### Q4 — Which files may the implementation touch?

**Keel recommendation** — Limit the change to the DTO mapper, API and contract tests, generated contract files, feature parser, details page and adjacent tests. Permit a small colocated About component and scoped styling if needed, and a minimal CastSection edit only to share actor-selection rendering.

**Patron ruling** — Approved. Use the file envelope below and sanctioned generators. Preserve the hero cast and reuse the existing MoreWith panel. A file outside the envelope needs a fresh Patron ruling. Basis comes from recon lines 16–38, 51–60 and 66–68 and PRODUCT §§3 and 5. Recorded at `aaf9ca86`.

### Q5 — What closes review on the frozen scope?

**Keel recommendation** — Cap review at two rounds and two fix commits per round. Close every verified in-scope finding through Low across all three axes. Require the applicable build gates, API/contract and web verification, and windowed and phone-width Chromium screenshots through Test URLs on the same real library movie. The owner merges the spec PR before Build. Apply the standing Palette trigger to the verified tags.

**Patron ruling** — Approved. Patron closed the grill after Q5. All three axes, the applicable gates, the ticket’s API/contract and web verification, and both Chromium screenshots are required. The confirmed `size:M`-only tag leaves Palette `design.md` outside this phase. Gate 1 provisional still requires clean spec analysis and plan-challenge adjudication. Basis comes from recon lines 11 and 49–76, task-pipeline §§0 and 2.2 and Phases 2–3, and Test portal policy. Recorded at `adc31200`.

## Implementation approach and file envelope

- Add the stored fields to the existing `MovieMetadataResponse` projection in `src/LamuFlix.Api/Endpoints/LibraryResponses.cs` on `GET /api/movies/{id}`. The projection uses existing data and preserves the outer null-metadata case.
- Extend `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` and `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`. Regenerate `web/src/api/openapi.json`, `web/src/api/types.ts` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` through the sanctioned tooling. Generated files require regeneration for subsequent corrections.
- Under `web/src/features/movie-details/`, update `movieDetailsApi.ts` to validate the new fields against the generated contract and update `MovieDetailsView.tsx` and focused adjacent About/view tests. Use one focused colocated `AboutSection`. Scoped About styling in `MovieDetailsView.css` is permitted if needed. Touch `CastSection.tsx` only if needed to share existing actor-selection rendering with About.
- Place `About <title>` below the hero, actions, existing browser player and launch feedback, and above the trailer. Use a `dl` labelled by the heading with `dt`/`dd` pairs, existing grey label and white value tokens, comma separators for multiple values and wrapping at phone width. Row order is Director(s), Cast, Writer(s), Genres, Collection. About uses the existing selection state and MoreWith panel.
- The §2.3 ruling confines the change to the projection, parser, page rendering and tests above. Any file outside that envelope returns to Patron. Patron found no §2.3 owner escalation.

## Verification and task ordering

1. **API and contract.** Add persisted endpoint JSON assertions for stored values and order, empty writers/genres and an explicit null collection name where absent. Preserve the existing null-metadata assertion and cover the new field shape in the OpenAPI contract test. Update the DTO and use the sanctioned process to regenerate the OpenAPI baseline, Verify snapshot and generated TypeScript. Verify contract drift and the API integration assertions before handing the contract to the web phase.
2. **Web.** Align `movieDetailsApi.ts` with `components` from the generated types before consuming the new data in the view. Cover row values and order, Director/Directors and Writer/Writers, missing writers with other rows still visible, optional collection, all About rows empty, and null metadata with the existing notice. Verify the heading labels the description list, names are shown in stored order with comma separators, a mapped About name opens the existing panel, and an unmapped name is plain text. Retain the hero selection regression and trailer placement.
3. **Integrated Chromium receipt.** After the API and page work together, use the connected Test URLs portal and the same real library movie for both Chromium screenshots. Record the local test route and viewport dimensions and show the About section in a windowed view and at phone width. Include both screenshots in the ticket's delivery evidence.
4. **Build and review.** Follow the Size M pipeline after the spec PR is merged. Ordered implementation phases lead into refactor and the Phase 3 step 5 gates covering Roslyn, cyclomatic complexity, InspectCode, scoped mutation for production C#, property tests under the standing rule, vulnerable packages, format, `dotnet test`, and `scripts/run-web-gates.ps1`. Run the three-axis review with Sentry, Ledger and Compass. Record applicable gate outcomes without calling scope-empty `SKIPPED`, harness opt-out `SKIP` or mutation `N/A` PASS. Exit 1, `Could not run` and an unaccepted property-test exit 2 block under the standing pipeline.

## Closing bar and review cap

Review closes when every verified in-scope finding through Low is resolved and the findings artifact records the fixes and other dispositions from Sentry, Ledger and Compass. Cap review at **two rounds** and remediation at **two fix commits per round**. The required delivery evidence includes the endpoint JSON assertions, passing OpenAPI drift check and regenerated types, web unit/component coverage, applicable C# and web gates, and the windowed and phone-width Chromium screenshots. The grill used **5 of 12** questions. The only taste assumption is the Writer/Writers label from Q3.

Phase 2 drafting and plan challenge use this brief and recon. The freeze receipt is recorded in CONCLUSIONS.md. Gate 1 requires the owner's spec PR merge. Build and Chromium acceptance evidence are later deliverables.

## Plan challenge decisions

Patron's rulings are recorded in CONCLUSIONS.md at b983c14a and 52500bc1. They refine Q1–Q5 within the ticket scope.

- Keep Q2 projection as-is. Assert required string arrays and required-and-nullable collectionName in OpenAPI, plus explicit null in endpoint JSON. Keep strict generated-contract validation and test the existing details error path for malformed values and missing keys.
- At About render, omit empty and whitespace-only list members. Use the remaining values and their displayed count for eligibility and crew labels while keeping the original text and relative order. When the people lookup is pending or failed, names without a match stay plain text. The inherited name-based selection can collide for equal normalized names and retains the existing behavior.
- Use one focused AboutSection. A separate row builder is a Build choice. Q4 permits scoped MovieDetailsView.css edits if needed, and additive required fields in existing typed test/mock fixtures only when T001 or compiler evidence shows need. Generator-owned handlers.ts and types.ts change through regeneration. Any hand edit to handlers.ts or a production change outside Q4 returns to Patron.
- T001 inventories typed consumers and confirms the pinned contract commands read-only. T004 performs the paired OpenAPI export after DTO projection with opt-in cleanup, then regenerates TypeScript. T005 requires endpoint tests, OpenAPI schema and drift tests, and npm run check:api.
- T009 includes the new parser cases and existing BS.Player and LocalPlay launch regressions. Chromium verifies tokens and wrapping. SMOKE-002 requires an enriched movie with mapped cast in the Test URLs seed. A missing candidate blocks the receipt.
- This slice adds no domain invariant. Patron ruled it eligible for the standing property-test opt-out. Property-test exit 2 still blocks until Bernstein records the opt-out with that reason in the DEV-570 task note.
