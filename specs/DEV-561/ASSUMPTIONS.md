# DEV-561 assumptions

- [assumed] Equal discovery scores sort by TMDb ID ascending. Patron approved this stable tie-break in Q5, using the existing recommender's ID ordering as precedent.

- [assumed] Card reasons name the two highest-weight seed movies, with ascending library MovieId breaking ties. Patron approved the copy cap and ordering in Q7.

- [assumed] Use **Outside-library recommendation** in the glossary, with _Avoid_: wanted movie. Patron approved the term in Q8.

- [assumed] Hide the discovery section during loading and after a discovery fetch error. Patron approved independent library-grid behavior in Q9.

- [assumed] Discovery card link wording follows the existing “View on <provider>” pattern with the candidate title and a new-tab hint (accessible name “View {title} on TMDB (opens in a new tab)”), and the poster fallback reads “Poster unavailable”. Recorded under the Q9 approval for accessible new-tab TMDb links and a poster fallback.
