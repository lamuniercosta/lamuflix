# DEV-540 — Plot embedding evaluation

- Branch `feature/DEV-540`, base `1bbc8a63` (merge base with `main`), evaluated revision `ebf5488f`, evaluated 2026-11-21.
- Harness: DEV-545 leave-one-out (`LeaveOneOutRunner`) and snapshot/quality
  (`QualityReportRunner`) harness with the DEV-543 nearest-seed For You
  ranking path (`GetForYouRecommendationsQueryHandler` via
  `HiddenSeedEvaluationCopy`). No `src/` change; the experiment adds input-text
  composition plus preflight only.

## Variants

- (a) Baseline: plot-only input, model `all-minilm` (production behavior:
  `ProcessFeaturesCommandHandler` embeds the plot).
- (b) Enriched text, model `all-minilm`: `Title`, genres, top keywords by
  rarity, then `Plot`. Fixed experiment cap of **3** keywords
  (`PlotEmbeddingExperiment.EnrichedKeywordCap`). DEV-518 rules apply through
  `SimilarityScorer.PrepareKeywordWeights`: rarity weight `ln(N/n_k)`,
  two-movie minimum, Themes stoplist
  (`aftercreditsstinger`, `duringcreditsstinger`, `sequel`, `3d animation`).
  Ordering is descending rarity with deterministic ordinal keyword-name
  tie-break, mirroring `ThemeEvidence`. Implemented in
  `tests/LamuFlix.Evaluation/RealLibrary/EmbeddingInputComposer.cs`.
- (c) Plot-only input (identical text to (a)), model `nomic-embed-text:v1.5`,
  isolating the model change.

## Model card (variant c)

- Ollama library page lists the `nomic-embed-text:v1.5` tag
  ([ollama.com/library/nomic-embed-text](https://ollama.com/library/nomic-embed-text),
  fetched 2026-11-21).
- Upstream config gives `hidden_size` 768 and `n_positions` 8192
  ([config.json](https://huggingface.co/nomic-ai/nomic-embed-text-v1.5/resolve/main/config.json),
  fetched 2026-11-21). Served output dimensions on the host are unverified:
  the model is not pulled here (see preflight).
- Token counting against each model's tokenizer and the proposed 256-token
  `all-minilm` limit check are unmeasured: both need the live models.
  The offline bound check (`PlotEmbeddingTokenBudget.IsWithinLimit`, tested at
  0, 256, 257) pins the limit semantics so the measured run only has to supply
  the two tokenizer counts; truncation behavior stays unrecorded until then.

## Measured runs

Seed sets, bars, and protocol are shared across all variants: DEV-545 five
seed titles (Fistful 575, About Time 518, A Few Good Men 515, Aladdin 519,
Wind Rises 831), For You take 30 and Related take 10, quality bars
`0.15,0.20,0.225,0.25,0.30,0.35,0.40` with the same strong-match bar `0.225`.

| Variant | HR@10 liked | HR@10 watched | MRR liked | MRR watched | Seeds (liked/watched) |
|---|---|---|---|---|---|
| (a) plot-only `all-minilm` | Could not run | Could not run | Could not run | Could not run | Could not run |
| (b) enriched `all-minilm` | Could not run | Could not run | Could not run | Could not run | Could not run |
| (c) plot-only `nomic-embed-text:v1.5` | Could not run | Could not run | Could not run | Could not run | Could not run |

Baseline-relative snapshot diffs (For You plus two Related seeds), For You
candidates clearing the bar, bar coverage lines, and the three-seed rank and
reason rows: Could not run for every variant.

Live preflight (`scripts/run-plot-embedding-preflight.ps1`, report at
`artifacts/plot-embedding/preflight.json`, git-ignored) reports all three
live prerequisites missing on this host:

- Could not run: Ollama model `all-minilm` is not served at the configured
  endpoint (`http://localhost:11434/`, from `OllamaOptions.Endpoint`).
- Could not run: Ollama model `nomic-embed-text:v1.5` is not served at the
  configured endpoint.
- Could not run: scratch evaluation store identity is unverified
  (`ConnectionStrings__DefaultConnection` absent, so no scratch lane marker;
  presence-only check).

The preflight refuses readiness before any probe unless the endpoint is local
(`localhost`, `127.0.0.1`, `::1`; redirects are not followed) and the
connection string carries a scratch lane marker (`scratch`, `evaluation`, or
`memory`). A non-local endpoint or a production-looking connection string
yields its own Could not run line and no readiness signal.

No embedding request left this host: the preflight only probed the local
endpoint, which refused the connection. No (b)/(c) embeddings were stored
anywhere, and production `movie_features` data and schema are untouched
(no live database access occurred).

## Deterministic results (offline)

- `PlotEmbeddingExperimentTests`: 33/33 passed. Covers (a)/(c) plot-only
  equivalence, (b) section order (title, genres, keywords, plot), the
  3-keyword cap, rarity ordering, ordinal tie-break, stoplist and two-movie
  minimum exclusions via the real `PrepareKeywordWeights`, empty-metadata
  handling, preflight available/missing paths, non-local endpoint refusal
  without probing, scratch lane-marker refusal without store probing,
  endpoint locality, scratch marker, and 256-token limit-bound checks.
- Live preflight script executes exit 0 and records the Could not run report
  above.

## Recommendation: keep the baseline

The closing bar requires both HR@10 and MRR to improve on the same seeds
with meaningful rank and reason rows. There are no measured live results, so
no variant clears the bar. Keep the production plot-only `all-minilm`
embeddings; do not change the text or the model on this evidence.

No follow-up implementation ticket: no change won. If the live run is
repeated where Ollama and the scratch store exist, re-run the preflight,
embed (b)/(c) inputs to scratch storage, and fill the tables above before
revisiting this recommendation.

## Alternate-dimension consequences (not applicable)

No dimension change is proposed, so there are no `EmbeddingDimensions.Value`,
`vector(384)`, or migration changes and no ADR review. If a future variant
with a different output size wins, that change needs an ADR covering the new
`EmbeddingDimensions.Value`, the `vector(N)` column width, and the migration
plan before merging.

## Change verification

- `git diff` touches only `tests/LamuFlix.Evaluation/RealLibrary/` (4 files),
  `scripts/run-plot-embedding-preflight.ps1`, and this note. No changes
  under `src/`, no migration added, no public API shape change.
- The run leaves production `movie_features` data and schema intact and uses
  the local Ollama endpoint exclusively.
