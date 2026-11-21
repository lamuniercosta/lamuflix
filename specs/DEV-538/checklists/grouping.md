# Requirements Quality Checklist: For You grouping and empty states

**Purpose**: Unit tests for English — validate clarity, completeness, and consistency of DEV-538 grouping, empty-state, and accessibility requirements before Gate 1
**Created**: 2026-11-21
**Feature**: [spec.md](../spec.md)
**Depth**: Standard | **Audience**: Spec PR reviewer | **Focus**: Row assignment / empty states / a11y semantics

## Row assignment and caps

- [x] CHK001 Is Wanted row order (series → liked/watched seeds → watchlist → More for you) stated as a testable sequence?
- [x] CHK002 Is “at most one movie per started collection” quantified with a selection comparator (year, then API rank)?
- [x] CHK003 Are the four seed-row and six card themed-row caps named as shared constants in one place?
- [x] CHK004 Is best-seed uniqueness defined so a multi-seed movie cannot appear in two themed rows?
- [x] CHK005 Are singleton fold and overflow-into-More-for-you rules explicit for series, seed, and watchlist rows?
- [x] CHK006 Is More for you exempt from the themed six-card cap while still receiving folded/overflow cards?

## Paging, Swap, and empty triggers

- [x] CHK007 Does the spec require themed rows to freeze after the initial GET?
- [x] CHK008 Are Load more and Swap scoped to More for you only with cross-row deduplication?
- [x] CHK009 Is rule 6 limited to a successful initial empty GET (not empty follow-ups)?
- [x] CHK010 Is zero-card More for you with `hasMore` required to keep Load more reachable?
- [x] CHK011 Is omission of More for you defined when it has neither cards nor controls?

## Empty-state and accessibility wording

- [x] CHK012 Are 6a and 6b distinguished by a named seed-presence signal rather than by guessing from an empty list alone?
- [x] CHK013 Is exact 6a copy preserved and 6b copy specified with the shared library link label?
- [x] CHK014 Are labelled region + real `h2` requirements stated for every populated row?
- [x] CHK015 Is carousel keyboard/Previous/Next parity with the existing Related rail required?
- [x] CHK016 Is seed-line hiding required for For You rows while default callers keep the seed line?

## Contract and Gate 1 boundary

- [x] CHK017 Is the owner checkbox for amending Done when line 54 present and scoped to the two additive fields on one GET?
- [x] CHK018 Does the spec require Related JSON to stay free of null `startedCollectionId` on the wire?
- [x] CHK019 Are scoring/ranking/schema/dependency/navigation/home redesign explicitly out of scope?
- [x] CHK020 Is the Size L closing bar (axes, severities, two-round / two-fix-commit cap) recorded without inventing PLAN FROZEN?

## Notes

- Generated under `/speckit-checklist` with defaults (no interactive answers needed): depth Standard; audience Spec PR reviewer; focus clusters = row assignment, empty states, a11y.
- All items pass against `brief.md` Q1–Q8 and `spec.md` Scope / FR / US sections (2026-11-21).
