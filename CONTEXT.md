# Project vocabulary

The canonical terms for this codebase. Use these words in code, specs, commits,
and conversation; when a term here and the code disagree, one of them is wrong.

`/grill-with-docs` populates this during the alignment stage - it is the output
of settling what things are called, not a form to fill in up front. Add a term
when a discussion reveals two names for one concept, or one name for two.

Format: the term, what it means here, and the names it displaces. The
`_Avoid_:` line matters most - it is what stops the old name creeping back.

---

**Movie**
_Avoid_: Filme, Film

**Library**
A set of movies on disk under LibraryOptions.RootPath.

**Import**
_Avoid_: Criar, Create, Scan (for single folder case)

**Enrichment**
_Avoid_: job, task, processing

**Enrichment Status**
Pending, Enriched, NotFound, Failed.
_Avoid_: state, job status

**Failure Category**
A caller-safe taxonomy.
_Avoid_: error type, raw exception

**Metadata Provider**
_Avoid_: hardcoding OMDb in Core names

**Watchlist**
_Avoid_: MinhaLista, My List

**Playback**
_Avoid_: Assistir, Watch

**Playback Event**
A record of a play launch.
_Avoid_: watch log, history entry

**Recommendation**
A suggested library movie from the details page.

**Reason**
A caller-safe explanation attached to a recommendation.

**Taste Profile**
A recency-weighted aggregate of the user's watched movies.

**Feedback**
Explicit Watched / Not Interested marks.
