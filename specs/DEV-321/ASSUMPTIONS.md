# DEV-321 — Patron assumptions

- [assumed] Q2: any filter or sort edit resets `page` to 1; only pagination controls change `page` alone (Keel recommendation).
- [assumed] Q2: user-initiated changes use `pushState`; alias/default/malformed normalization uses `replaceState`; back/forward (`popstate`) and initial load restore state without pushing (Keel recommendation).
- [assumed] Q2: `pageSize` UI value is fixed for this ticket (no page-size selector) (Keel recommendation).
- [assumed] Q3: SPA default sort is `Title` `Ascending` (Keel recommendation).
- [assumed] Q3: SPA default `pageSize` is 24, subject to the validator bound condition in CONCLUSIONS Q3 (Keel recommendation).
- [assumed] Q4: poster placeholder keeps the poster aspect ratio and uses the movie title as its accessible text; the same placeholder covers null and image-load-error (Keel recommendation).
- [assumed] Q5: responsive grid styling lives in `web/src/index.css`; one `QueryClientProvider` is installed in `main.tsx` (Keel recommendation).
