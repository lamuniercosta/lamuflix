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

**Candidate Movie Folder**
A directory at or below the Library root, including the root itself, containing at least one supported video file directly in it. It is imported exactly once as one Movie, and traversal continues into its subdirectories. A directory with no supported video file directly in it is only traversed, never imported. Directories reached through a link below the root are never candidates.
_Avoid_: scan folder, movie directory, container folder (for a candidate)

**Import**
_Avoid_: Criar, Create, Scan (for single folder case)

**Primary video**
The one eligible file selected from a movie folder by the shared selection rule: files directly in the folder with a supported extension, excluding files whose name carries a sample, trailer, featurette, or extras token, choosing the largest and then the ordinal-lowest full filename. Choosing it is a selection outcome, not a scan.
_Avoid_: main file, biggest file, scanned file (when meaning the selection outcome)

**Rescan**
Re-probe an existing movie folder and update only the stored Format of that existing Movie through a Movie domain method, preserving identity, folder path, title, year, metadata, enrichment state, and user flags. Distinct from Import: Rescan updates Format on an existing Movie; Import creates a Movie.
_Avoid_: re-import, refresh metadata, upsert import

**Enrichment**
_Avoid_: job, task, processing

**Enrichment Status**
Pending, Enriched, NotFound, or Failed on the Movie aggregate. `MarkEnriched`, `MarkNotFound`, and `MarkFailed` are legal only from Pending. `RequestEnrichment` is legal only from Enriched, NotFound, or Failed.
_Avoid_: state, job status

**Duplicate IMDb id**
An IMDb id held by one Movie row at a time: the unique index `IX_movies_imdb_id` admits a single holder. When enrichment resolves a movie to an id another row already holds, the movie ends as Failed with the non-retryable `duplicate_imdb_id` category. A manual match to a held id is rejected with 409 Conflict and enqueues nothing.
_Avoid_: duplicate movie, collision, conflict (unqualified)

**Failure Category**
A caller-safe taxonomy.
_Avoid_: error type, raw exception

**QuotaExhausted**
QuotaExhausted is an enrichment failure caused by denial of admission under the local OMDb daily request quota; it is distinct from provider HTTP 429 and does not itself imply deferral.
_Avoid_: RateLimited (for local quota admission denial), next-day deferral (as implied by this category alone)

**QuotaDeferred**
The settlement outcome for a movie whose metadata lookup stopped on provider quota exhaustion: it stays Pending, keeps its attempt, and is re-dispatched at `EnrichmentDeferredUntil`. It is not a failure and never enters retry.
_Avoid_: quota failure, quota retry, paused import

**EnrichmentDeferredUntil**
The persisted next-UTC-midnight instant (from the injected TimeProvider) before which a QuotaDeferred movie must not be claimed, swept, or dispatched.
_Avoid_: retry-after, next retry time, pause timestamp

**quota-deferred** (routing)
The routing action and durable queue (`enrichment.quota-deferred`) carrying the same-attempt `EnrichmentRequested` until `EnrichmentDeferredUntil`, when it dead-letters into requested.
_Avoid_: retry queue, delayed retry

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

**Worker**
The `EnrichmentConsumer` hosted inside `LamuFlix.Api`. The standalone Worker project is retired.
_Avoid_: a separate host, resurrecting LamuFlix.Worker

**Sweeper**
The `StrandedMovieSweeper` hosted service. It re-enqueues stranded movies and never claims.
_Avoid_: claiming stranded movies, a second consumer

**Feature generation**
Deriving a movie plot embedding for that movie.
_Avoid_: treating plot embedding as metadata enrichment

**Feature transport attempt**
One delivery attempt in a feature-generation retry chain, restarted at 1 by recovery.
_Avoid_: EnrichmentAttempts, EnrichmentRequested.Attempt

**Feature recovery**
Identifying movies with a nonblank plot and no stored features and requesting feature generation without claiming metadata enrichment.
_Avoid_: claiming feature work, enrichment stranded reclaim for features

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

**Thumbnail**
A JPEG frame extracted at ten percent of a movie duration into the thumbnail cache by the thumbnail generator.
_Avoid_: poster, cover, preview image

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
An aggregate of viewing history and explicit review preferences: a recency-weighted aggregate of the user's watched movies, extended with separate liked and disliked Review aggregates over plot embedding, genres, cast, and director.
_Avoid_: preferences snapshot, user profile, review history

**Feedback**
Explicit Watched and Review actions about a movie.
_Avoid_: Not interested (as an independent state), marks, flags

**Review**
An explicit opinion about a movie with state `None`, `Liked`, or `Disliked`, independent of watching and watchlist intent. A movie holds one Review; selecting the active thumb clears it to `None`, selecting the opposite thumb switches directly. Persisted by Value and exposed on the wire by the lowercase Name (`none` / `liked` / `disliked`). Legacy Not interested routes and flags are compatibility projections of `Disliked`, never a second source of truth. Distinct from the external metadata Rating (IMDb/TMDb star value) and from an engineering code review.
_Avoid_: Rating, score, vote, reaction, like/dislike flag, code review

**Mutant linkage**
Whether the mutant Stryker activates is the one executing inside the process that runs the tests. A broken linkage makes every mutant survive regardless of test quality (DEV-382).
_Avoid_: "tests are weak" for a 0-kill run before linkage is proven

**Artifact kill**
A mutant reported Killed by a test that fails because of Stryker's instrumentation itself (e.g. ArchitectureTests rejecting injected `Stryker.*` types), not because of the mutation. The mutation gate fails on any artifact kill.
A test whose kills exceed `gates.mutation.maxKillShare` percent (default 50) of tested mutants is a suspected artifact kill, usually one that carries state across mutant runs; the gate fails on it, unless fewer than 20 mutants were tested. So is a test that killed mutants in more than `gates.mutation.maxKillFileSpan` distinct source files (default 15), at any sample size, unless its class is listed in `gates.mutation.killFileSpanExempt` with a reason; the share limit still applies to it.
_Avoid_: counting it as a kill

**Mutation receipt**
The committed, compact evidence of one mutation run: SHAs, Stryker version, effective config, mutated files, per-mutant status and killedBy names, native exits, and process and assembly identity. Bulky reports stay outside the repo.
_Avoid_: report (the full Stryker output)

**Genre weight**
The library rarity multiplier for a genre; rarer genres carry more weight in genre similarity.
_Avoid_: genre preference, genre popularity score

**GenreWeights**
An immutable table of genre weights for one library catalog snapshot.
_Avoid_: user taste weights, recommendation option weights

**Genre similarity**
The overlap of two movies genre sets, weighted by library rarity.
_Avoid_: unweighted genre overlap, overall recommendation score

**Keyword**
Positive external metadata keyword ID with a trimmed display name; identity and overlap use the ID.
_Avoid_: Tag, genre, free-text keyword label without ID

**Full cast**
All valid actor names returned by the extended credits cast payload, without billing cutoff; excludes crew and character/order features.
_Avoid_: Top-billed cast, credits with crew, character list

**Base cast**
Existing primary-provider `Actors` names, retained separately for disabled/ablation parity.
_Avoid_: Overwritten cast, merged-only cast

**Franchise collection**
Exact positive `belongs_to_collection` ID with display name; never inferred from title or collection-name similarity.
_Avoid_: Title-prefix franchise, fuzzy collection match, user playlist

**Unavailable signal**
No authoritative replacement from the provider lookup: preserve the corresponding persisted signal.
_Avoid_: Empty signal, cleared signal

**Known empty**
A valid empty payload (or valid no-collection) from the provider: replace/clear that signal.
_Avoid_: Unavailable signal, failed lookup

**Trailer key**
The validated YouTube video identifier selected during metadata enrichment and stored with enriched movie metadata so the Movie Details page can embed a privacy-friendly trailer. Absence is null (including existing rows) and hides the Trailer section. Never a full provider URL.
_Avoid_: trailer URL, YouTube link, video URL, embed URL (when meaning the stored value)

**Ordered language preferences**
Explicit server-configured provider-language codes in selection priority order; no invented default language.
_Avoid_: browser Accept-Language as authority, silent default language

**Selected subtitle**
Compatible SRT in the first configured preferred language that has an eligible result.
_Avoid_: multi-language pack, best overall subtitle across all languages

**Sidecar**
Subtitle file alongside the primary video with exact basename: `Film.mkv` → `Film.srt`.
_Avoid_: language-suffixed names (`Film.eng.srt`) as BS Player autodetection

**Preparation**
Explicit per-movie acquisition/save operation, separate from byte-range video streaming.
_Avoid_: auto-download on stream, background subtitle job

**Existing track**
An authorized exact-basename SRT already on library storage; its language may be unknown.
_Avoid_: treating missing language as noMatch for an already-present file
