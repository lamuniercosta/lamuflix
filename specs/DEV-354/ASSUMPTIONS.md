

## DEV-354 Q5 - Patron assumed option spelling and defaults (2026-10-22)

[assumed] Patron chooses RecommendationOptions.BlendSelectedWeight=1.0 (a), BlendProfileWeight=1.0 (b), AlreadyWatchedPenalty=0.25 (c), NotInterestedPenalty=1.0 (d). Ticket Scope 1 requires coefficients on RecommendationOptions but does not specify names/defaults (recon-DEV-354-2:104). Full question and cited ruling: CONCLUSIONS.md Q5; mirrored decision: brief.md Q5. This is a provisional design choice pending completion of the technical grill; it does not freeze the plan or certify current source shape.


## DEV-354 Q6 - Profile comparison policy (2026-10-22)

[assumed] Patron chooses weighted Jaccard between candidate GenreWeights values and DEV-353 unnormalized GenrePreferences; positive cosine clamped to [0,1] between candidate embedding and profile centroid; equal arithmetic mean of comparable channels, one alone when only one is comparable, zero when neither is. Genre comparability requires candidate genres and positive profile mass; plot comparability requires valid finite nonzero 384-vectors. Malformed nonempty vectors fail explicitly. Use stable scaled cosine and preserve raw Q5 a/b coefficients. Ticket does not prescribe these formulas or equal channel weighting. Full exchange: CONCLUSIONS.md Q6; basis recon-DEV-354-2:104,109,150-162. Acquisition and provenance remain open.


## DEV-354 Q7 - Reason contract and text (2026-10-22)

[assumed] Patron chooses Core Structural/Personal kinds and wire {kind,text} with lowercase structural/personal; deterministic existing Structural order followed by Personal reasons for positive comparable profile channels when b>0. Text templates: Matches your <genre> interest (positive overlapping genre, OrdinalIgnoreCase then Ordinal order, sanitized display), Matches your viewing history (positive profile plot match), and Structural fallback No shared features identified if otherwise empty. Caller-safe plain text uses DEV-338 sanitization and rendering-context escaping. Named-movie Personal text remains undecided. Full question/ruling: CONCLUSIONS.md Q7; mirror: brief.md Q7; basis recon-DEV-354-2:105,110,131,135-162. No freeze or Gate 1.

## DEV-354 Q8 - Generic Personal explanation policy (2026-10-22)

[assumed] Patron selects only the Q7 generic positive-channel Personal templates Matches your <genre> interest and Matches your viewing history; no Because you watched <title> template or separate contributor-provenance type. Named-title text in Scope 2 is an example, and AC-2 still prohibits any unwatched movie attribution. Actual profile/embedding acquisition remains required and unresolved; generic text does not replace it. Full exchange: CONCLUSIONS.md Q8; mirror: brief.md Q8; basis recon-DEV-354-2:105,110,155-162; DEV-354:35; specs/DEV-353/spec.md:112,114.

## DEV-354 Q9 - Acquisition type names and export workflow (2026-10-22)

[assumed] Patron selects the proposed IRecommendationProfileSource, RecommendationProfileInputs, EfRecommendationProfileSource, BlendScorer and ProfileSimilarityCalculator names and exact new source/test files listed in brief.md Q9. Patron chooses an explicit opt-in test-hosted normalized-v1 export operation in OpenApiContractTests.cs, followed by the existing generate:api/check:api workflow; its new discoverable invocation must be specified and tested by Quill. Ordinary tests remain read-only. Exact persistence mappings and DI registration require narrow recon before final authorization. Full exchange: CONCLUSIONS.md Q9; basis DEV-354:35,40; recon-DEV-354-2:104-111,128-170. No freeze or Gate 1.


## DEV-354 Q10 - Query and composition policy (2026-10-22)

[assumed] Patron chooses a new PlayedAt read query in the already approved EfRecommendationProfileSource, source registration compatible with scoped DbContext, and direct construction/calls for pure TasteProfileCalculator, BlendScorer and ProfileSimilarityCalculator without artificial DI wrappers. Preserve all launch signals and watched fallback semantics. Full exchange: CONCLUSIONS.md Q10; mirror: brief.md Q10; basis DEV-354:44 and Q6-Q9. Absent vector storage/readers and incomplete composition-root path identities are evidence gaps, not assumed inputs. New opt-in normalized-v1 test-host export remains Q9 policy. No freeze or Gate 1.




## DEV-354 Q11 - Proof and phase-order policy (2026-10-22)

[assumed] Patron selects the Q11 proof matrix and dependency-aware ordering recorded in brief.md Q11, retaining Q2 review caps and Q7-Q10 contract/acquisition policies. Quill specifies the new discoverable opt-in export invocation and tests; Build implements and executes them in Phase B. Exact composition-root identities from DEV-354:48 are recon facts, not assumptions. No numeric gate threshold, existing exporter command, vector readiness, scope reduction, freeze or Gate 1 is assumed. Full exchange: CONCLUSIONS.md Q11; basis DEV-354:35,40,44,46,48 and recon-DEV-354-2:104-111,128-170.


## DEV-354 Q12 - Final ratification (2026-10-22)

[assumed] Patron ratifies the Q1-Q11 recorded design/taste choices, proof strategy, closing bar and caps for Quill drafting and closes the grill at 12/12. No new taste choice or evidence assumption is introduced. Q12 does not waive vector acquisition or other Phase B prerequisites and does not grant plan freeze or Gate 1. Full exchange: CONCLUSIONS.md Q12; controlling current status and obligations: brief.md Q12.

## DEV-354 post-Q12 - Existing paired exporter policy clarification (2026-10-22)

[assumed] Patron selects extension of existing OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether / WriteRegeneratedOutput as the single normalized-v1 writer and retains case-insensitive LAMUFLIX_REGENERATE_OPENAPI=true. This satisfies Q9-Q12 and supersedes earlier NEW-operation/new-selector mirrors and LAMUFLIX_OPENAPI_EXPORT wording without rewriting history. Existing writer/root/path discovery is a recon fact, not an assumption. Preserve exactly-one-root discovery within eight ancestors, absolute baseline/snapshot paths and IsWithinRoot guards, paired normalized /openapi/v1.json writes, lowercase typed {kind,text}, opt-in writes both and unset/non-true writes neither with proof beyond content equality. After DTO changes regenerate both, clear env even on failure, run final ordinary drift/equality/no-write checks, then web generate:api/check:api, consumer migration/tests. Full recorded Q&A: CONCLUSIONS.md Post-Q12 exporter clarification; controlling mirror: brief.md CONTROLLING POST-Q12 EXPORTER CLARIFICATION. Basis DEV-354:58-59; recon-DEV-354-3:55-65; CONCLUSIONS.md:112-113,136-141,203,211-212; prior ASSUMPTIONS.md:23. Grill remains closed at 12/12; other decisions and Phase B STOP persist. Only decision records updated; no plan challenge, freeze or Gate 1.


## DEV-354 post-Q12 - Scorer-error-policy clarification (2026-10-22)

[assumed] Patron selects ArgumentOutOfRangeException for negative or nonfinite blend members; existing IValidatableObject field-level ValidationResult through the established validation/startup binding path for a=b=0; and InvalidOperationException without clamping when otherwise-finite inputs produce a nonfinite calculated raw score. Preserve Q5 finite nonnegative a/b/c/d, a > 0.0 || b > 0.0 without adding a+b, raw finite blend and no final clamp/division/outer renormalization. Existing invalid-weight and options-binding conventions are recon facts (recon-DEV-354-3:14,16-17); the calculated-score exception is a Patron policy choice, not a claimed existing precedent. Full recorded Q&A: CONCLUSIONS.md Post-Q12 scorer-error-policy clarification; controlling mirror: brief.md CONTROLLING POST-Q12 SCORER-ERROR-POLICY CLARIFICATION; authority DEV-354:58,61 and CONCLUSIONS.md Q5. Specification policy only, not implementation approval. spec.md, plan.md and tasks.md await separate corrections; Phase B STOP and closed 12/12 grill persist. No source edits, tests, gates, generators, plan challenge, freeze or Gate 1.

## DEV-354 C3 - Personal genre-reason multiplicity (2026-10-22)

[assumed] Patron selects and settles as a contract one Personal genre reason per canonical positive-mass overlapping genre in OrdinalIgnoreCase then Ordinal order, rather than exactly one from the first qualifying genre. This clarifies the Q7/Q8 text/cardinality policy without changing canonicalization, eligibility, safe display, Structural-first order, plot-channel reason or fallback. Authority: DEV-354:75-76; finding: findings-DEV-354-Compass:12. Full available recorded Q&A: CONCLUSIONS.md C3 Personal genre-reason multiplicity; controlling decision: brief.md CONTROLLING C3 PERSONAL GENRE-REASON MULTIPLICITY. Quill mirrors the contract in spec/plan/tasks and multi-genre fixtures in the next routed correction. Closed 12/12 grill, Phase B NOT READY and T009 STOP persist; no plan freeze, Gate 1 or implementation approval.
