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
