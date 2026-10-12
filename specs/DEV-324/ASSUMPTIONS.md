# DEV-324 — Taste assumptions (Patron)

- [assumed] Icon: an inline SVG bookmark, outlined when off and filled when on, with no icon-library dependency. Basis: S&TD 1 "bookmark icon"; PRODUCT.md §4.
- [assumed] Toggle accessible name: "Add to watchlist" / "Remove from watchlist", with `aria-pressed` reflecting state. Copy is English, matching the existing SPA copy (`App.tsx`).
- [assumed] Quick-filter button label: "Watchlist", shown as a pressed toggle when `?watchlist=true` is set.
- [assumed] Error copy for a failed toggle, shown inline and non-blocking: "Couldn't update watchlist. Try again."
- [assumed] Empty state when the filter is on and nothing matches: "Your watchlist is empty."
- [assumed] Turning the filter on or off resets paging to page 1 and keeps the other filters (CONCLUSIONS Q11).
