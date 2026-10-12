# Clarifications: DEV-321 — Library browse view

**Created**: 2026-10-11
**Feature**: `specs/DEV-321/spec.md`
**Authority**: `specs/DEV-321/brief.md` (grill closed Q10/12); `specs/DEV-321/CONCLUSIONS.md`; `specs/DEV-321/ASSUMPTIONS.md`.

**Note**: L-ticket clarification record (`/speckit-clarify`). The grill answered every open question; no clarification in this file adds, drops, or changes ticket scope, and none departs from the constitution (brief Q10: no `blocked: structural` item).

## Closed clarifications (from brief.md — recorded here, not re-decided)

- C1 (Q1): Additive `MovieSummary` → `MovieSummaryResponse` widening on `GET /api/movies` with the five-member wire table; no per-tile fetches, migration, or join; stop and re-ask Patron if either is needed.
- C2 (Q2): Native History API + sole-owner URL adapter; canonical keys; two alias families read-only with `replaceState`; always-emit keys; push/replace/popstate ownership; server owns range validation.
- C3 (Q3): SPA defaults Title/Ascending/1/24; list/group/malformed/case/boolean/encoding rules. Supersedes Q2's initial Core-defaults instruction (Core has no usable browse default). Validator bounds confirmed (page ≥ 1, size 1–100).
- C4 (Q4): Supplied-poster display with reserved-ratio title-labelled placeholder; seeded real-DB + RTL proof; no live-population claim; no enrichment/worker repair; follow-up DEV-408 (created, per task note).
- C5 (Q5): Replace readiness `App.tsx` + orphaned test (forced edit); one `QueryClientProvider` in `main.tsx`; `index.css` styling; TanStack Query only; Phase B waits for user merge of DEV-320 #124 + Wisp generated-client/types/signal recon.
- C6 (Q6): One normalized-key query via merged generated client with cancellation; skeleton vs retain-content rules; hand-written boundary with Retry and key-change reset; 422 invalid-filter (never retried) vs recoverable errors including 400, classified by HTTP status not error-key vocabulary; adapter history ownership.
- C7 (Q7): Root location; 2:3 grid; tile format (em dash, `<n> min`, one decimal, exact chip labels); control surface (numeric-ID genre/actor entry, no new facet endpoint); pagination incl. past-end behavior; a11y; no performance target.
- C8 (Q8): Exact production/web/test envelope; `web/src/library/` is a feature folder; compile-forced construction sites; conditional schema config; regeneration paths; no hand-edited generated files.
- C9 (Q9): Gates, Phase A/B order, file binding, paging-SQL assertion change, Q9 property-test amendment (no new C# URL round-trip tests; SPA adapter owns round-trip proof).
- C10 (Q10): Closing bar, frozen scope, round caps; grill closed at Q10/12.

## Open items

None. Any future question arising during Keel analysis, Palette design, or implementation returns to Patron per the charter; ticket-scope changes or constitution departures escalate as owner checkboxes at Gate 1.
