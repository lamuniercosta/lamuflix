# Requirements Quality Checklist: DEV-518 scoring contracts

**Purpose**: Unit tests for English — validate clarity/completeness of the ordered scorer, keyword-count lifecycle, consumer wiring, and evidence contracts in `spec.md` / `plan.md` / `tasks.md` before Gate 1.
**Created**: 2026-11-19
**Audience**: Quill (author), Keel (analyze), Gate 1 reviewers
**Depth**: Formal pre-Gate-1 requirements gate (not runtime verification)
**Status**: Draft complete for analyze. Not PLAN FROZEN. Not a Gate 1 claim.

## Scope & boundaries

- [x] CHK001 Are the six ordered criteria, default weights summing to 15, and contribution/Score reconciliation stated?
- [x] CHK002 Is “anything else is a follow-up issue, not a finding in this round” present for loop/frozen scope (Q6)?
- [x] CHK003 Are Charter §2.3 surfaces (deps, project/layer, schema, public API, LocalPlay/secrets/Process.Start, unnamed file rewrites) explicitly non-goals?

## Scorer / reasons

- [x] CHK004 Are Collection/Director/Cast binary-or-scaled values and reason templates specified without inventing copy beyond the ticket table?
- [x] CHK005 Are Themes rarity-weight, stoplist, MinimumKeywordMovies, ThemesCeiling, and Q4 zero-norm rules complete?
- [x] CHK006 Is Plot rescale (floor/ceiling/clamp) and removal of SimilarStoryThreshold explicit?
- [x] CHK007 Are graded display thresholds (Themes/Plot/Genre) requiring value > 0 and value >= threshold, with below-threshold contribution retained, pinned?
- [x] CHK008 Is legacy year/runtime/rating/writers/Identical feature profile removal required in score, reasons, and weights?

## Keyword-count lifecycle

- [x] CHK009 Is the catalog read for N and distinct n_k from one library population specified with no schema change?
- [x] CHK010 Are singleton cache + scoped calculator, per-operation snapshot, success-only invalidation, and version-token stale-load rejection specified against GenreWeights precedent?

## Consumers & options

- [x] CHK011 Are GetRecommendationsQueryHandler, MmrSelector, and SeedCollectionGrouper named as shared-scorer callers with For You relevance and Related blending otherwise unchanged except Genres default 2?
- [x] CHK012 Are SimilarityWeights/RecommendationOptions field changes (Themes, Plot, removals, thresholds, ReviewPlotWeight preserved) complete?

## Evidence, docs, gates

- [x] CHK013 Are the five Related seed titles the DEV-545 ordered set, with literal IDs and measured snapshots Phase B only?
- [x] CHK014 Are DEV-545 compare six-criteria output, before/after Related + For You snapshots, Fistful home-library acceptance, and PR attachment required?
- [x] CHK015 Are docs/recommendations.md table pin, CONTEXT.md vocabulary alignment, and named test surfaces listed?
- [x] CHK016 Are Size L three-axis closing bar (Risk/Standards/Spec), Low included, and two-round cap specified (Q8–Q9)?
- [x] CHK017 Are static gates, tests, mutation, and honest non-PASS labels required without threshold lowering?

## Notes

- Intent signals: shared similarity function, ordered evidence, Themes rarity, DEV-545 evidence chain.
- Checklist generated under `/speckit-checklist` with focus “scoring contracts / Gate 1 readiness”; clarifying questions skipped because brief Q1–Q9 already fixed scope, risk, and depth.
- Items are checked only as English completeness of the drafts. Keel analyze / independent reread pending; no PLAN FROZEN / Gate 1 / implementation PASS claim.
