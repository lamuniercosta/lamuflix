# DEV-543 grill conclusions

## Q1 — Scope boundary

Keel asked: “Can we freeze the scope to nearest-seed For-you ranking, strong-match eligibility and best-seed reasons, plus removal of Related taste-profile scoring and reasons, with the DEV-518 similarity, DEV-544 candidate pool, DEV-541 caps/paging/exclusions and card/API shape outside this ticket? I recommend that boundary because it follows the ticket scope summarized in recon-DEV-543 lines 9–13 and permits isolated ranking and contract tests. Please cite the ticket and charter basis and send the ruling to Keel.”

Patron replied in the connected seat message: “Confirmed scope boundary per ticket Wanted items 1-12, Out of scope, and specs/PRODUCT.md:23-24,38.” The ruling confirms that boundary for this grill question. The separate file referenced in the reply is outside the authorized evidence roots.

Basis: DEV-543 ticket Wanted items 1–12 and Out of scope, as cited by Patron. See also recon-DEV-543 lines 9–13 and specs/PRODUCT.md lines 23–24 and 38.

---

## Q2 — Related scoring and profile cleanup

**Keel**

Shall we settle Related without the taste profile on the ticket default? I recommend DEV-518 similarity with the existing AlreadyWatchedPenalty and disliked exclusion, and removing profile reasons. An unrelated review must not change a result score. Remove the ticket-named legacy profile members after caller checks, keeping the watched penalty. Basis: recon-DEV-543-2 lines 36–49, 57, 107.

**Patron**

Confirmed — settle Related rail on the ticket default: pure DEV-518 similarity with existing AlreadyWatchedPenalty and disliked exclusion, remove profile reasons so unrelated reviews do not change Related scores, and delete the ticket-named legacy profile members (keeping AlreadyWatchedPenalty) after caller checks, recording this in ADR 0045. Basis: DEV-543 Wanted items 9, 11, 12, Done-when item 3, Open points (recon-DEV-543-2 lines 36-49, 57, 107), and specs/PRODUCT.md:23-24.


---

## Q3 — Playback recency

**Keel**

How should recency apply to signed seed contributions? I recommend a configurable 0.25 floor on the existing decay multiplier for positive seeds with playback, unscaled positives without playback, and the unscaled maximum disliked similarity required by Wanted item 4. A liked seed at the floor still carries a quarter of its weight. This preserves the exact dislike formula and the raw strong-match bar. Basis: recon-DEV-543-2 lines 28–34 and 105–111.

**Patron**

Confirmed — apply recency scaling to positive seeds with playback events using a configurable 0.25 lower bound (floor) on exp(-RecencyDecayLambda * age), leave positive seeds without playback events (including watchlist seeds) unscaled, keep disliked negative seeds unscaled per Wanted item 4 (disliked weight * max DEV-518 similarity to any disliked seed), and evaluate strong-match eligibility on raw pair similarity and breakdown before seed weight or recency scaling. Basis: DEV-543 Wanted items 4, 5, 7, Open points (recon-DEV-543-2 lines 28-34, 105-111), and specs/PRODUCT.md:23-24,28.


---

## Q4 — Shared strong-match policy

**Keel**

Shall the spec keep the single DEV-544 strong-match policy for both surfaces, with the ticket default bar 0.225 and Collection, Director or Themes at value 1 as the strong-reason list? I recommend that default and require DEV-545 evidence for any proposed tuning, so an eligibility change can be measured on both surfaces. Basis: recon-DEV-543-2 lines 32 and 102–108.

**Patron**

Confirmed — reuse the single shared DEV-544 strong-match option set for both surfaces, with default bar 0.225 and strong-reason list Collection, Director, or Themes at value 1, requiring DEV-545 quality report evidence for any tuning. Basis: DEV-543 Wanted item 5, Open points (recon-DEV-543-2 lines 32, 102-108), and specs/PRODUCT.md:23-24.


---

## Q5 — Seed precedence and empty results

**Keel**

Shall we define the empty-result boundary as no qualifying positive seed after self-exclusion? I recommend applying ordinary ranking to real watchlist seeds in a watchlist-only library, returning empty when there are no positive seeds or no strong matches, and adding no fallback list. Resolve strongest seed kind once before recency, keeping a watched-and-liked movie as liked and a disliked movie negative only. This preserves the ticket examples and prevents self-matching or padding. Basis: recon-DEV-543-2 lines 28–34 and 104–110.

**Patron**

Confirmed — resolve each movie to its single strongest seed signal before recency scaling (disliked is negative only; liked > watched-without-review > watchlist, so watched-and-liked counts as liked only), apply ordinary nearest-seed ranking when real watchlist positive seeds exist (excluding the candidate itself from its positive seeds), and return an empty list with no fallback or padding when no positive seeds exist or no candidate has a strong-match positive seed after self-exclusion. Basis: DEV-543 Wanted items 1, 2, 3, 5, Open points (recon-DEV-543-2 lines 28-34, 104-110), and specs/PRODUCT.md:23-24.


---

## Q6 — Card seed ranking and deterministic ties

**Keel**

Shall the card seed use the same recency-adjusted weighted similarity as the positive ranking contribution, restricted to strong positive pairs? I recommend that interpretation, with movie ID ascending as a deterministic final tie-breaker for equally scored strong seeds and equally ranked candidates where the existing candidate ordering has no further key. This makes reasons reproducible without changing qualification or outranking stronger candidates. Basis: recon-DEV-543-2 lines 30–34 and PRODUCT.md sections 3–4.

**Patron**

Confirmed — select the card best seed from strong-match positive seeds using the same recency-adjusted weighted similarity (effective seed weight * DEV-518 similarity) used in the positive ranking contribution, and use movie ID ascending as the deterministic final tie-breaker for equally scored strong seeds and equally ranked candidates where existing candidate ordering has no further key (log deterministic tie-breaker tagged [assumed] in specs/DEV-543/ASSUMPTIONS.md). Basis: DEV-543 Wanted items 3, 5, 6, 7 (recon-DEV-543-2 lines 30-34) and specs/PRODUCT.md:23-24,28-29.


---

## Q7 — Review closing bar

**Keel**

What closes the review loop? I recommend requiring all accepted in-scope findings at every severity, including Low, to be fixed and verified, explicit dispositions on the three Size L review axes, and passing applicable repo gates plus the ticket acceptance evidence. That prevents an accepted contract, runtime or lower-severity defect from being carried into the handoff. Basis: DEV-543 Done when and Wanted item 13, PRODUCT.md sections 1 and 5, and charter Size L review discipline.

**Patron**

Confirmed — the review loop closes only when all accepted in-scope findings at every severity (including Low) are fixed and verified, explicit dispositions are recorded on all three Size L review axes, and all applicable repo gates plus the ticket Done-when acceptance and Wanted item 13 speed/quality evidence pass. Basis: DEV-543 Wanted item 13 and Done-when criteria, specs/PRODUCT.md:12,34-39, and standing charter Size L review bar.


---

## Q8 — Review round cap and grill closure

**Keel**

Shall the fix and review loop cap be two rounds, with any remaining blocking findings reported against the closing bar and the phase held open? I recommend two to allow remediation and a verification round while keeping the scope bounded. This is separate from the grill cap of 12. Please give the complete ruling inline and confirm whether this closes the grill on Q1–Q8.

**Patron**

Confirmed — cap the fix and review loop at two rounds (remediation plus verification, separate from the 12-question grill cap), reporting any remaining blocking findings against the Q7 closing bar and holding the phase open; this closes the DEV-543 grill on Q1-Q8. Basis: standing charter Size L review-loop cap and specs/PRODUCT.md:12,34-39.

---

## Gate 1 — Provisional ruling

**Verdict**: `gate1: provisional` — zero structural owner checkboxes (`blocked: structural`).

- `/speckit-analyze` is clean and all 31 three-axis plan challenge findings (Sentry, Ledger, Compass) are adjudicated and PLAN FROZEN in `specs/DEV-543/PLAN_ADJUDICATION.md`, `specs/DEV-543/plan.md:9`, and `specs/DEV-543/tasks.md:9`.
- `specs/DEV-543/checklists/requirements.md:32-36` has all "User-Visible Outcome" items ticked (`SMOKE-001` and `SMOKE-002` in `specs/DEV-543/spec.md:18-32`).
- Zero ticket contradictions or constitution departures (`specs/DEV-543/spec.md:8-10`, `specs/DEV-543/plan.md:37-59`, `specs/PRODUCT.md` §5); ordinary owner Gate 1 merge remains with the user on the spec PR (`T000`).


