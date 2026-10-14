# DEV-323 - Patron grill conclusions

Grill: `/grill-with-docs DEV-323`, Patron-driven, 12 questions (cap reached exactly). 2026-10-12.
Sources: ticket text (note `DEV-323`:8), `recon-DEV-323` (cited `R:<line>`), `specs/PRODUCT.md` (cited `P:§`), seat charter.
Worktree `F:/Dev/LamuFlix.worktrees/feature-323-spec`, branch `feature/323-spec`, base `cb6fe48`.

## Owner escalation (spec PR checkbox; Gate 1 stays closed until answered)

- [ ] **blocked: structural — Drop `backdrop`, `tagline` and `writers` from DEV-323 and deliver them in a follow-up ticket?**
  The ticket names them, but no column, table or wire field holds them (R:18-20, R:58), so delivering them means a schema change plus enrichment-source work beyond a 1d `size:M` ticket. Dropping them changes what the ticket delivers (P:§5 Ticket change), so it is the owner's call.
  Patron recommends **yes**. The spec proceeds on that basis. If the owner says no, the spec gains a schema/enrichment slice and is re-planned. Rigger files the follow-up ticket only once the owner ticks yes.

## Rulings

Q1. **Backend in scope: extend the GET `/api/movies/{id}` response additively.** (P:§5 item 4 API shape)
- The AC asks for complete metadata on the details page, and the wire carries only title, synopsis, year, runtime, IMDb rating and IMDb id (R:20, R:52).
- Add nullable `posterUrl`, `rottenTomatoesRating`, `metaScore`, plus `directors: string[]` and `actors: string[]` (empty array when none) to `MovieMetadataResponse`, backed by Core `MovieMetadata`/port and the `EfMovieCatalog.GetDetailsAsync` projection (R:17-19). The storage already exists (R:58), so there is **no schema change** (item 3 not triggered).
- Forced edits are in scope: frozen-shape tests (R:32, R:59), the OpenAPI baseline and snapshot regeneration (R:34). No other route, DTO or status code changes.

Q2. **Backdrop, tagline and writers:** see the escalation above. There is no partial or placeholder rendering of these fields in this ticket.

Q3. **Play endpoint and the LocalPlay gate stay unchanged.** (P:§5 item 5)
- POST `/api/movies/{id}/play` already exists with 204/400/403/404/422 (R:23). The gate stays in the adapter, server-side (R:25). The UI never decides playability itself.
- No backend playback, `FeatureOptions`, `Process.Start` or secrets edits. The legacy MVC `MoviesController`/`MovieService` play path is not named by the ticket and stays untouched (R:44, R:60-61; item 6).

Q4. **No feature-flag endpoint.** The Play button always renders and is enabled. The 403 modal is the gate UX the ticket names. (P:§5 item 4; ticket AC)

Q5. **No router dependency.** (P:§5 items 1-2)
- Deliver `MovieDetailsView` as a component that takes a `movieId` prop. `App.tsx`/`main.tsx` are not rewired (item 6; the ticket names a view only, R:61).
- Navigation and route mounting belong to the SPA epic's routing/browse work. *Unverified:* that a sibling ticket under DEV-286 owns routing. If Keel finds none, mount via the existing `App.tsx` with no new dependency, and treat it as a forced edit.

Q6. **Data access: native `fetch` in a small typed module inside the feature folder** (`web/src/features/movie-details/` or similar). No query or state library and no generic API-client layer (P:§2 Speculative Abstractions; items 1-2). Use relative `/api/...` URLs. No `vite.config.ts` proxy edit, because tests use MSW.

Q7. **Contract chain: generate `web/src/api/types.ts`.** Add the `openapi-typescript` npm **devDependency** (exact pinned version) and a `package.json` script that regenerates it from `web/src/api/openapi.json`. (P:§5 item 1; charter contract-chain rule)
- The rule forbids hand-rolled DTO duplicates, and `types.ts` does not exist yet (R:53). This is the minimal codegen with no runtime dependency.
- *Unverified:* whether another DEV-286 ticket already owns codegen. If one does, defer to it and consume its output.

Q8. **Play response handling.** 204: brief inline success status. 403: modal. 404: inline "not found" error. 400/422/5xx/network: inline generic error. The button is disabled while the request is in flight, which prevents double-launch.

Q9. **Modal implementation:** use a hand-built accessible dialog (`role="dialog"`, `aria-modal`, labelled title, focus moves to Close, Escape and Close dismiss, focus returns to Play). There is no UI-library dependency. Native `<dialog>` `showModal` is avoided because the jsdom test environment does not support it reliably (sound engineering practice).

Q10. **Details load states:** loading, 404 not-found, other error, and loaded. Loaded with `metadata: null` shows the title and format plus a no-metadata notice. Each optional field and badge renders only when its value is present: no "N/A" text and no empty badges.

Q11. **Tests:**
- Web: RTL + MSW (named in the ticket) covering full render, partial/null metadata, the GET 404/error states, and Play 204/403(modal open, close, focus)/404/error.
- Backend: xUnit for the new projection and mapping fields (EfMovieCatalog integration, endpoint mapping, e2e frozen shape updated). Existing gates and contract tests apply unchanged.

Q12. **Glossary:** the canonical terms are **Movie Details** (the view and query) and **Playback** (CONTEXT.md:74). Keel/Quill use them in the spec. Patron makes no edit to `CONTEXT.md`, and no ADR is needed (M ticket, no new layer).

## Routing
- No recon outstanding. The two *unverified* points (Q5 routing owner, Q7 codegen owner) are Keel's to confirm against the DEV-286 sibling tickets via Wisp, if needed.
- No YouTrack change now. The follow-up ticket depends on the owner's checkbox.

## Patron stack amendment and analysis repair authority (2026-10-12)

Patron rules: **follow the constitution stack: TanStack Query, Zod, the DEV-320 openapi-fetch client, and generated MSW handlers. Prior Q6 is superseded. No constitution departure and no new owner checkbox.** Authority: DEV-323:60 finding (1), DEV-323:63-67 Patron ruling and next action, and the Conductor clearance in this ask. The cited constitution basis is the stack mandate at constitution:313-335 and generated client/MSW rules at constitution:470-472, as recorded in DEV-323:60; these are note-supplied citations, not a new read of the constitution outside the named artifacts. Plan Constitution Check must carry the principle-number citations required by constitution:529, also recorded there.

This amendment overrides the retained original Q6 and any native-fetch/no-query instruction. Q7 remains deferred to DEV-320 ownership (recon-DEV-323:78-83): DEV-323 consumes and regenerates delivered outputs for its additive contract, never creates a competing client/generator or hand-writes generated artifacts. TanStack Query and Zod are the decided stack, not a constitution exception. Exact package pins, delivered client/handler/provider paths and any necessary consuming configuration edits are not found in these roots; preserve those as targeted Phase B recon, not invented file scope or delivery evidence.

No further Patron decision is needed for Quill to repair the drafts. Apply the existing numbered findings in DEV-323:60 as one bounded round 1 fix list: (1) align stack and constitution citations across drafts; (2) separate Phase A checkbox/PR preparation from Phase B, requiring merged spec PR and answered owner checkbox before build; owner no requires re-plan; (3) remove conflicting parallel markers, serialize same-file tests and dependent Core -> EF -> API/tests -> OpenAPI -> generated outputs -> web work; (4) retain implementation TDD/contract checks but make final gates Gauge-owned after Cog refactor, with unaccepted property-test exit-2 blocking; (5) align file inventory to this brief, omit unapproved query/handler edits, list the verified snapshot output, and keep OpenApiContractTests source read-only for regeneration and unlocated caller paths deferred.

The backdrop/tagline/writers owner checkbox stays unanswered and Gate 1 stays closed. Deferred Phase B recon is preserved: delivered DEV-320 tooling/client/generated handlers and stack integration, direct-link hosting support, and forced Core constructor/test caller paths. DEV-320 delivery and parentage remain NOT DELIVERED / NOT EVIDENCED at the supplied recon HEAD. This records a decision and unblocks draft repair only; analysis is still NOT CLEAR pending Quill repair and Keel read-only re-analysis. It does not freeze the plan or authorize build, gates, commits or delivery.
## Patron ruling: Zod exact pin (2026-10-14)

Patron rules: **declare `zod` as a direct runtime `dependencies` entry in `web/package.json` pinned exactly to `"4.6.5"` (no caret); regenerate `web/package-lock.json` with `npm install` so the existing `node_modules/zod` 4.6.5 entry becomes the top-level direct resolution. No other version, no devDependency.** §2.3 item 1.
- Use is already authorized by the Patron stack amendment above (CONCLUSIONS.md, 2026-10-12); this ruling settles only the pin deferred there (recon-DEV-323:85, 107, 117).
- 4.6.5 is already resolved in the lock (package-lock.json:5329-5331) and satisfies the existing dev-transitive range `^3.25.0 || ^4.0.0` from eslint-plugin-react-hooks (package-lock.json:2542, 5347), so the lock gains one root entry and no new tarball; exact pinning matches the openapi-fetch/openapi-typescript precedent (recon-DEV-323:84).
- `web/package.json` and `web/package-lock.json` are forced edits of this ruling and join the DEV-323 file inventory; overlap with open PR #132 (DEV-407) on those two files is a merge-order matter for the Conductor (recon-DEV-323:103), resolved by rebase, not by a different pin.
