# UX Requirements Quality Checklist: DEV-501 Subtitles tab

**Purpose**: Unit tests for English — validate Subtitles tab UX requirements quality against `design.md` + `spec.md`  
**Created**: 2026-11-12  
**Feature**: [spec.md](../spec.md) · [design.md](../design.md)  
**Depth**: Standard · **Audience**: Reviewer (Gate 1 / plan challenge) · **Focus**: Scanability, a11y, non-disruptive states, D5 active indicator

## Requirement Completeness

- [ ] CHK023 Are local tracks + Off required above remote results in every acquisition state? [Completeness, design.md States / Spec §FR-012]
- [ ] CHK024 Are language control label and bilingual option format ("English — Native") specified from server catalog DTOs (no handwritten web catalog)? [Completeness, design.md Accessibility / Spec §FR-006]
- [ ] CHK025 Are truncated release name + full value on hover and keyboard focus both required? [Coverage, design.md Layout / Spec §FR-012]
- [ ] CHK026 Is the active check restricted to the actually active downloaded track in LocalTrackList (never candidate rows; D5)? [Clarity, design.md / Spec §FR-012 / D5]
- [ ] CHK027 Are Get disabled reasons required to be visible text plus `aria-disabled`/`aria-describedby` to the status line? [Coverage, design.md Accessibility]

## Requirement Clarity

- [ ] CHK028 Is “unavailable” quota rendering distinguished from zero remaining and from inferred reset? [Clarity, Spec §FR-012 / ASSUMPTIONS]
- [ ] CHK029 Does download-quota exhaustion copy require naming the provider reset when known, distinct from search-rate-limit copy? [Clarity, design.md Copy / ASSUMPTIONS / D1]
- [ ] CHK030 Is stale candidate UX specified as inline re-search prompt without automatic paid retry? [Clarity, design.md States / Spec §FR-007]
- [ ] CHK031 Is download-in-flight UI specified as “Getting…” with all Gets locked (no auto-retry)? [Clarity, design.md States]
- [ ] CHK032 Is empty PreferredLanguages / null defaultLanguage required to prompt without auto-search? [Clarity, Spec §FR-006 / catalog ruling]

## Scenario Coverage

- [ ] CHK033 Are empty-results, no-language-configured, search-rate-limited, download-quota-exhausted, timeout/failure, and read-only scenarios all required to keep video playing? [Coverage, design.md States / Spec US3]
- [ ] CHK034 Is changed-language completion required to add the local track without replacing current remote results? [Coverage, Spec §FR-013]
- [ ] CHK035 Is client-side refusal of Get when downloadAvailability.enabled is false required (never fire POST)? [Coverage, design.md Data needs / Spec §FR-011]
- [ ] CHK036 Are reduced-motion and forced-colors constraints specified (text state change, not motion-only spinner; percent/count as text)? [Coverage, design.md Accessibility]

## Consistency & Boundaries

- [ ] CHK037 Do UX requirements forbid client-side re-sort of server-ranked candidates? [Consistency, design.md Explicit non-goals]
- [ ] CHK038 Do UX requirements exclude SubDL affordances, delay/style controls, and new top-level routes from this tab? [Boundary, design.md Explicit non-goals / Spec Out of scope]
- [ ] CHK039 Is sentence-case assumed copy consistent across ASSUMPTIONS, design.md, and Spec What the User Will See? [Consistency]
- [ ] CHK040 Does Palette-owned design.md remove candidate-row `[x]`/`[ ]` / CandidateRow active-check per D5 (verify against design, do not edit here)? [Consistency, D5]

## Notes

- This checklist tests whether UX requirements are written well enough to implement and review — not whether the UI already works.
- Palette owns `design.md`; D5 candidate active-check repair is Palette-owned.
- PLAN FROZEN is recorded in Keel PLAN_ADJUDICATION.md; owner Gate 1 remains unapproved and runtime receipts OPEN. Unchecked boxes remain Gate 1 reviewer attestations.
