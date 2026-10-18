# Dark-glass restyle: visual review closeout

Reviewed on 2026-10-18, using Luna for artifact reconnaissance, the bounded list CSS changes, and web gates. The parent independently read the changes, inspected screenshots, repeated the contrast measurement, and exercised the rebuilt layouts.

## Checkout and scope

- Worktree: `F:\Dev\LamuFlix.worktrees\dark-glass-restyle`.
- Branch: `feature/dark-glass-restyle`.
- Base HEAD: `126a2b1b8628f93a0fee54eb7942136ab241f2ea`. Review covers the uncommitted working tree, identified by [web source hashes](evidence/web-source-sha256.txt), rather than that commit alone.
- The seven outstanding library screenshots were inspected. All nine library states were then recaptured and inspected after the small fixes below.
- The mock server served the rebuilt `web/dist` on localhost:5199. Its generated posters and movies are review fixtures.
- Details-page visual checks at the three widths were supplied by the previous session. They were not repeated in this closeout; the full component suite was rerun.

The current authority is `F:\Dev\lamuflix-layout-direction.md` (SHA-256 `83e6f4e74dd49f0ca4598b5c34f8a7b743797e7fd8af9364d5bf414f63750fc7`). The reference is `F:\Dev\lamuflix-layout-draft.html` (SHA-256 `e37878642505e1a3319506a6202819e632dff864c5ecb9fd749050a0f1dba0d2`), with requirements mirrored in `F:\Dev\lamuflix-restyle-ticket.md`. These replace the historical DEV-432 sidebar/drawer design. A [desktop draft capture](evidence/draft-library-1440.png) is retained for comparison.

## Changes during closeout

1. Left-align mobile status chips and Retry beneath the title.
2. At 768–1023px, move nonempty list statuses onto a second row while preserving year, runtime, rating and watchlist controls. At 768px the first title now has 308px rather than roughly 68px of space. Empty statuses do not create another row.
3. Restore the missing shared heading class: 28px on desktop/tablet and the existing 24px mobile override.
4. Add the selected 1440px default-grid screenshot to the repository README.

## Screenshot results

| Width | Default grid | Filters open | List | Document scroll width |
|---|---|---|---|---|
| 375px | [shot](screenshots/lib-default-375.png) | [shot](screenshots/lib-filters-375.png) | [shot](screenshots/lib-list-375.png) | 375px in all three |
| 768px | [shot](screenshots/lib-default-768.png) | [shot](screenshots/lib-filters-768.png) | [shot](screenshots/lib-list-768.png) | 768px in all three |
| 1440px | [README selection](screenshots/lib-default-1440.png) | [shot](screenshots/lib-filters-1440.png) | [shot](screenshots/lib-list-1440.png) | 1440px in all three |

The layout toggle remains on one line; card titles truncate to one line; missing posters show the film icon; the heading reads “All movies”. All captures show the same 20-result page from 34 fixture movies in either layout. Genre tiles intentionally scroll inside their strip. Filters remain inline and push results down, with all six groups and the footer visible in the 375px extended capture.

At 375px and 768px, the first status chip and its title share the same left edge (98px and 128px respectively). Year/runtime hide below 768px and remain visible at 768px. Raw metrics and capture logs are preserved in [layout-results.json](evidence/layout-results.json) and beside each screenshot.

## Contrast remeasurement

Reused the earlier screenshot-based method: force the ambient layer to white, hide ambient poster images, record current element rectangles, sample the median rendered background, and compute WCAG relative luminance against the actual selected foreground token. The new capture uses the final build and current coordinates; it does not reuse the old List rectangle.

| Sample | Measured | Required |
|---|---|---|
| Inactive List, dim #C2C8D1 on rendered #1E1F25 | 9.76:1 | 4.5:1 |
| Weakest sampled secondary text, count on #4D4E52 | 4.94:1 | 4.5:1 |
| Weakest sampled primary text, Import/genre tiles | 5.37:1 | 4.5:1 |
| Weakest sampled boundary token, cast input | 4.79:1 | 3:1 |
| Amber stars on #4D4E52 | 3.76:1 | 3:1 |

All 28 sampled items passed. The previous inactive List result was 3.74:1; the current dark plate and dim text pairing resolves that failure. See [rendered white-background capture](evidence/contrast-final.png), [element coordinates](evidence/contrast-final.json), and [complete results](evidence/contrast-final-results.txt). These are targeted contrast samples, not a complete accessibility audit.

## Differences from the clickable draft

These are recorded visual differences, not claims of additional user approval.

| Area | Build versus draft | Relationship to decisions |
|---|---|---|
| Glass and glow | Build darkens the area beneath text; fixture poster colours differ from the draft gradients. | Explicit contrast correction in the decision record; the full-strength draft glow is superseded. |
| Mobile breakpoint | Build switches at 768px; draft switches at 760px. | Explicitly required by the ticket. |
| Mobile search | Build puts the full-width search above Library/Watchlist within the second header row; draft puts it below the tabs. | Both preserve a full-width search beneath the wordmark. |
| Import | Build has a labelled icon action; draft is icon-only. | The ticket explicitly requests a labelled icon button. |
| Icons | Build uses inline SVG; shapes differ from the draft’s Tabler webfont. Filters omits the draft’s leading adjustments icon. | Inline SVG is explicitly required; the missing decorative icon is a visual difference. |
| Filter groups | Build orders Runtime before Release year; draft orders Year before Runtime. At 768px the build uses two columns. | All required groups remain present; no drawer or popover is introduced. |
| Grid statuses | Build places status pills at the poster’s upper left and Retry below the metadata; draft combines status and Retry near the poster bottom. | Required status and Retry behavior remains; this is a visible placement difference. |
| Watchlist | Build uses warm-white bookmark glyphs on lighter glass controls; draft uses amber selected glyphs on darker controls. | Selected state remains visible; this is a visual colour/treatment difference. |
| List | Build uses individual grid rows without the draft’s table header; thumbnails are 48px rather than 36px. Status moves below the title on tablet and mobile. | All required data remains at tablet/desktop widths; mobile still hides only year/runtime. The extra status row fixes title crowding. |
| Fixture data and pagination | Build shows a real 20-result page from 34 fixture movies; draft shows its fixed sample collection. | Preserve the application’s existing pagination and API contract rather than the mockup’s sample counts. |

No unresolved usability defect was found in the nine reviewed library captures after the fixes. The visual differences above remain visible for design review; this record does not assert pixel-for-pixel parity or a new approval of those differences.

## Verification

Executed from `web/` after the source changes:

| Command | Result |
|---|---|
| `npm run lint` (includes generated API freshness, ESLint and Prettier) | Exit 0 |
| `npx tsc --noEmit` | Exit 0 |
| `npx tsc --noEmit -p tsconfig.node.json` | Exit 0 |
| `npm test` | Exit 0; 334 tests, 32 files |
| `npm run build` | Exit 0 |

Logs: [lint](evidence/lint.txt), [tests](evidence/tests.txt), [build](evidence/build.txt). The [typecheck log](evidence/typecheck.txt) is empty because both commands succeeded without output. The parent also checked `git diff --check` and the actual final browser layouts.

`web/public/mockServiceWorker.js` is unchanged: both working-tree and HEAD Git blob IDs are `14c9979d9523cf9d4aaa3c01264f7c5c70205a40`.

All existing frontend changes are preserved. The temporary review server was stopped after verification. No commit, PR, hosted-check clearance, merge, or user design approval is implied by this local closeout.

## Logo follow-up (2026-10-18)

After the user reviewed the live restyle, they requested a logo and favicon. The initial LF monogram was then replaced, at their request, with an amber figure operating a reel camera on a tripod, paired with the existing serif wordmark. The header uses the SVG mark; the browser receives SVG and 16/32/48px ICO favicons, plus a 180px Apple touch icon. A standalone logo is available at `web/public/lamuflix-logo.svg`. The camera version was inspected at 16px, 32px and larger sizes; lint and build passed again, with receipts in `evidence/camera-lint.txt` and `evidence/camera-build.txt`.

The [branded desktop capture](screenshots/library-branded-1440.png) now appears in the main README. The original nine screenshots, source hashes, and contrast results above remain evidence for the pre-branding closeout. Branding was additionally checked at 375, 768, and 1440px without horizontal page overflow, with the header image loading and every icon URL returning HTTP 200. Tablet tab padding was reduced slightly to preserve search space. The existing 22 App tests passed; lint and the build (including both TypeScript configurations) passed. The review server remains available on localhost:5199 for the user.

## Approved LF identity (2026-10-18)

The user selected and approved the chubbier square LF monogram after reviewing successive previews. The amber letters form the operator and camera; the head, arm and inset lens use warm white. The mark has exact 100-by-100 geometric bounds, 28-unit letter stems, a shared baseline, and tighter spacing. This replaces the literal camera-operator illustration described above. The user also approved the heavier modern sans-serif wordmark, replacing the earlier serif treatment.

Applied the approved geometry to `web/public/favicon.svg` and `web/public/lamuflix-logo.svg`, regenerated the 16/32/48px ICO and 180px Apple touch icon, and updated the header wordmark to the system Arial Black/Arial stack at weight 900. Icon URLs carry `v=lf-square` so the browser can fetch the approved version instead of a cached earlier favicon.

At the user's request, the header now loads `web/public/lamuflix-mark.svg`, a transparent version without the dark background rectangle. The live header and README screenshot were refreshed and visually checked; lint and build passed again. The favicon assets retain their existing background.

The rebuilt page was independently inspected at [375px](screenshots/approved-375.png), [768px](screenshots/approved-768.png), and [1440px](screenshots/library-branded-1440.png). Each document scroll width equals its viewport width; the logo loads and header controls remain separate. The README screenshot now shows the approved identity. All four icon/logo URLs return HTTP 200. Lint, the build with both TypeScript configurations, and the 22 existing App tests pass; see [lint](evidence/approved-lint.txt), [build](evidence/approved-build.txt), and [tests](evidence/approved-tests.txt). The service worker remains unchanged. The full 334-test suite above was run during the earlier closeout; only the 22 App tests were rerun for this branding change.

## Approved text-height lockup (2026-10-18)

The user subsequently approved the lower-height variant, with the symbol matching the visible top and bottom of the name text. The full transparent logo SVG now matches that approved preview exactly. The header renders the combined SVG at 24px high so the symbol-to-name alignment is preserved as one asset. Its accessible name remains LamuFlix, and the two shell assertions now locate the brand link by that accessible name. The standalone mark uses the approved 100-by-75 proportions; favicon and Apple touch exports center that mark within their square canvases. Updated asset URLs use `v=text-height`.

Final captures at [375px](screenshots/text-height-375.png), [768px](screenshots/text-height-768.png), and [1440px](screenshots/library-branded-1440.png) show the new lockup. At each width the logo loads at approximately 165-by-24px and the document scroll width equals the viewport width. The tablet layout preserves the logo's intrinsic width beside navigation. Lint, build with both TypeScript checks, and all 22 App tests pass; receipts are [lint](evidence/text-height-lint.txt), [build](evidence/text-height-build.txt), and [tests](evidence/text-height-tests.txt). All five logo/icon URLs return HTTP 200. The service worker remains unchanged. The README image shows this final version, and the local review server remains available on port 5199.

## PR preparation (2026-10-18)

Rebased the restyle cleanly onto `origin/main` at `38f5602c892ead971ff52f63da7558eb6de98da5`. The feature commit after rebase is `21f566aae821eea195ad8caddbfeb4da5d4a4cfa`. Removed 55 newly introduced descriptive comments from 14 source files to follow the repository convention; the cleanup checked that non-comment content was unchanged before formatting.

Reran `pwsh -NoProfile -File scripts/run-web-gates.ps1 -All` on the rebased source: exit 0, lint PASS, TypeScript PASS, all 334 tests in 32 files PASS, production build PASS. See [final web gate output](evidence/pr-web-gates.txt) and [final web source hashes](evidence/pr-web-source-sha256.txt). The PR changes only the product README, visual review evidence, and frontend assets/source/tests. Backend files, the OpenAPI/generated client, mock handlers, and the MSW worker are unchanged from the rebased base. Backend gates were not run for this frontend-only change.

The PR is prepared as a draft for code/security review. The user approved the visual result; this receipt does not claim formal ship-review clearance or hosted-check success.
