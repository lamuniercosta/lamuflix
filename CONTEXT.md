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
Pending, Enriched, NotFound, or Failed on the Movie aggregate. `MarkEnriched`, `MarkNotFound`, and `MarkFailed` are legal only from Pending. `RequestEnrichment` is legal only from Enriched, NotFound, or Failed.
_Avoid_: state, job status

**Failure Category**
A caller-safe taxonomy.
_Avoid_: error type, raw exception

**Metadata Provider**
_Avoid_: hardcoding OMDb in Core names

**Movie Aggregate**
The in-memory domain root for one library movie and its enrichment workflow. Created only through `Movie.Create`. It is not the EF Movie entity.
_Avoid_: Data.Models.Movie, EF entity

**Enrichment Attempt**
`EnrichmentAttempts` / `enrichment_attempts`: the monotonic cumulative count of successful atomic claims (processing attempts started) on a Movie aggregate. It starts at 0 and adds exactly one per successful claim; refused claims, `MarkEnriched`, `MarkNotFound`, `MarkFailed`, and `RequestEnrichment` add zero. It is preserved across manual retries and never reset. Values stored before DEV-390 can overcount historical attempts and cannot be retroactively corrected.
_Avoid_: a completed-outcome count, a retry counter that resets

**Transport Retry Attempt**
`EnrichmentRequested.Attempt`: the message sequence ordinal carried on the wire. Manual retry and stranded requeue restart it at 1; the retry policy advances it by one per retry decision. It is never copied into or used to reset the persisted counter.
_Avoid_: the persisted counter, enrichment attempts

**Stranded Movie**
A Pending movie whose enrichment claim lease has aged out, or a Pending movie left without a queued `EnrichmentRequested` message. The sweeper re-enqueues `EnrichmentRequested` without claiming.
_Avoid_: stuck job, abandoned claim

**Retry Queue**
`enrichment.retry`. Holds a republished attempt until `RabbitMqOptions.RetryDelay` has passed, then dead-letters it back to `enrichment.requested`. Both `Retry` and `RetryDelayed` go through it.
_Avoid_: a fixed-delay exchange, backoff queue

**Dead-Letter Queue**
`enrichment.dead-letter`. Holds work that cannot be retried: a terminal failure, an unreadable body, a failed or uncertain republish, or broker delivery-limit exhaustion. It rejects publishes when full.
_Avoid_: error queue, DLQ without the name

**Publisher Confirm**
The broker's acknowledgement that a message was stored. A confirm does not prove routing; a mandatory publish that cannot be routed fails instead. `EnqueueAsync` returns only after both settle.
_Avoid_: an ack, a broker receipt

**traceparent**
The W3C trace-context header carried in message headers, with `tracestate` when one exists. The consumer starts a span parented to it.
_Avoid_: correlation id, trace header

**Watchlist**
Membership of a Movie aggregate, independent of enrichment status. `AddToWatchlist` throws when the movie is already in the watchlist. `RemoveFromWatchlist` throws when it is absent.
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

**Mutant linkage**
Whether the mutant Stryker activates is the one executing inside the process that runs the tests. A broken linkage makes every mutant survive regardless of test quality (DEV-382).
_Avoid_: "tests are weak" for a 0-kill run before linkage is proven

**Artifact kill**
A mutant reported Killed by a test that fails because of Stryker's instrumentation itself (e.g. ArchitectureTests rejecting injected `Stryker.*` types), not because of the mutation. The mutation gate fails on any artifact kill.
_Avoid_: counting it as a kill

**Mutation receipt**
The committed, compact evidence of one mutation run: SHAs, Stryker version, effective config, mutated files, per-mutant status and killedBy names, native exits, and process and assembly identity. Bulky reports stay outside the repo.
_Avoid_: report (the full Stryker output)
