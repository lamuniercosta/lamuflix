# DEV-321 — Patron assumptions

- [assumed] Q2: any filter or sort edit resets `page` to 1; only pagination controls change `page` alone (Keel recommendation).
- [assumed] Q2: user-initiated changes use `pushState`; alias/default/malformed normalization uses `replaceState`; back/forward (`popstate`) and initial load restore state without pushing (Keel recommendation).
- [assumed] Q2: `pageSize` UI value is fixed for this ticket (no page-size selector) (Keel recommendation).
