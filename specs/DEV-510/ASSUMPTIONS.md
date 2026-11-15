# DEV-510 assumptions

## A1 [assumed] - Quantified headroom

Patron Q4 interprets several-times-library headroom as a fivefold fixture: 725 liked movies relative to the ticket baseline of 145. Retained maps must be representative, with 384-float per-source embeddings in uncached inputs. The serialized compact CachedProfile must be strictly below 80 percent of the effective 1,048,576-byte default ceiling. No reduction of source count, map content, or scoring data is permitted to meet this bound.

Basis: recon-DEV-510 lines 58-65; CONCLUSIONS.md Q4. This is a confirmed acceptance interpretation, not a measured payload claim.

## A2 [assumed] - Warning default

Patron Q4 chooses configurable near-limit warning fraction 0.8. Cover below, equal-to, and above-boundary behavior, using the effective payload ceiling. Configuration has one source of truth; do not hardcode thresholds into production checks.

Basis: ticket work item 5, recon-DEV-510 line 64; CONCLUSIONS.md Q4.

## A3 [assumed] - Current-home payload headroom

Patron Q6 confirms that the current 145-liked-movie home profile must also serialize strictly below the Q4 80 percent budget. Actual bytes and library counts must be measured and recorded; the ticket baseline is not a live measurement.

Basis: recon-DEV-510 lines 64-65; CONCLUSIONS.md Q4 and Q6.

At initial grill closure, no additional assumptions, taste rulings, or question-cap defaults were introduced. Seven questions were asked; all received explicit Patron rulings. No structural escalation or constitution departure was requested or approved.

## A4 [assumed] - Warning validation domain and diagnostic choice

Patron PA1 chooses finite 0 < WarningFraction <= 1 (default 0.8 remains A2), existing validation-message style and source-generated numeric logging only. Boundaries include NaN/infinities. Basis: ticket item 5, original recon line 64, targeted seams 3-7, CONCLUSIONS.md PA1. No silent revision of A1-A3: infeasible or unavailable budget proof stops for Patron; an owner checkbox is required only for ticket contradiction or constitution departure.
