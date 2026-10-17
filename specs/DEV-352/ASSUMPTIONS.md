# DEV-352 — Assumptions

Grill closed with Patron at 8/12 questions on 2026-10-17, with supplemental drafting decisions Q9–Q10 recorded at 10/12 on 2026-10-17.

- `[assumed]` Q9 `InvalidTransitionException` current-state token for `UnmarkNotInterested` when `IsNotInterested == false`: use `"Interested"` (paired with `"Watched"`, `"NotWatched"`, `"NotInterested"`) rather than `"NotNotInterested"`. `"Interested"` here denotes only the absence of the negative mark, not positive preference (accepted from Keel's Q9 recommendation).

No UI assumptions were made: label wording, visual controls, sort order and empty-state copy are outside this ticket.

Domain, storage, API, DTO, test and loop choices are deliberate cited Patron rulings Q1–Q10 in CONCLUSIONS.md, summarized in brief.md; they are not silent assumptions. Patron confirmed zero ticket contradictions or constitution departures and zero structural owner checkboxes. Gate 1 remains pending.

Unknown facts are not assumptions: obtain needs recon through Bernstein if required evidence is absent from recon-DEV-352.
