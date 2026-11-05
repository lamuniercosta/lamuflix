# DEV-461 — Taste Assumptions

- `[assumed]` **Icon row order (left-to-right)**: `Watched`, `Thumbs up`, `Thumbs down`, `Watchlist` — matches the numbered order in `DEV-461` `## Scope` (`recon-DEV-461` lines 148–151).
- `[assumed]` **Accessible names & tooltip copy (title-bearing)**:
  - **Watched**: reflects toggle state with the movie title (e.g., `Mark Watched, {title}` when unwatched; `Watched, {title}` / unmark toggle when watched), preserving the existing DEV-449 title-suffix convention (`MovieFeedbackActions.tsx:50`).
  - **Thumbs up**: `Like, {title}` when inactive (`None` or `Disliked`); `Clear review, {title}` (or `Liked (click to clear), {title}`) when `Liked` is active.
  - **Thumbs down**: `Dislike, {title}` when inactive (`None` or `Liked`); `Clear review, {title}` (or `Disliked (click to clear), {title}`) when `Disliked` is active.
  - **Watchlist**: `Add to Watchlist, {title}` when not in watchlist; `Remove from Watchlist, {title}` when in watchlist.
- `[assumed]` **Tooltip trigger**: Every icon exposes its tooltip on both pointer hover and keyboard focus.
- `[assumed]` **Default review recommendation weights**: `ReviewPlotWeight = 1.0`, `LikedReviewWeight = 1.0`, and `DislikedReviewWeight = 1.0` in `RecommendationOptions` / `SimilarityWeights` (configurable and validated at startup).
- `[assumed]` **Personal liked-review reason chip copy & tie-breaking**: Personal reason chip text is `"Because you liked {X}"` (Suggested by `DEV-461` `recon-DEV-461` line 162, with `{X}` sanitized via existing reason sanitation rules); when multiple liked movies have equal positive review affinity for a candidate, ties are broken deterministically by movie ID.
- `[assumed]` **Superseding Q9 clarification — exact accessible names & tooltip copy (no alternatives)**: Both `aria-label` (accessible name) and tooltip text use `{action}, {title}` with `aria-pressed` conveying current state independently of action wording:
  - **Watched**: `Mark Watched, {title}` (when unwatched) / `Unmark Watched, {title}` (when watched).
  - **Thumbs up**: `Like, {title}` (when `None` or `Disliked`) / `Clear liked review, {title}` (when `Liked`).
  - **Thumbs down**: `Dislike, {title}` (when `None` or `Liked`) / `Clear disliked review, {title}` (when `Disliked`).
  - **Watchlist**: `Add to Watchlist, {title}` (when not in watchlist) / `Remove from Watchlist, {title}` (when in watchlist).



