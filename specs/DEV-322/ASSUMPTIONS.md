# DEV-322 - Taste assumptions (Patron)

Per specs/PRODUCT.md §4. Each is `[assumed]`; rationale in CONCLUSIONS.md.

- [assumed] Sort dropdown labels: `Title (A-Z)`, `Title (Z-A)`, `Year (oldest)`, `Year (newest)`, `IMDb (lowest)`, `IMDb (highest)`, `Runtime (shortest)`, `Runtime (longest)`. Default `Title (A-Z)` (omitted from URL). (Q2)
- [assumed] Status chip labels: `Pending`, `Enriched`, `Not found`, `Failed`. (Q8)
- [assumed] Text search debounce 300 ms; placeholder `Search titles`. (Q9)
- [assumed] Year slider 1900..current year step 1; runtime slider 0..240 min step 5, top stop shown as `240+ min`. Current year = viewer's local browser year captured at panel mount, no live New Year rollover. (Q7, F5 clarification)
- [assumed] Runtime checkbox label `Include unknown runtime`; default unchecked; sent as `runtimeIncludeUnknown=true|false` whenever the runtime group is sent, omitted only with the whole group. (Q6, Q11)
- [assumed] Genre option shows name plus count badge, sorted by name (facet order, EfMovieCatalogFacetsTests); cast list sorted by name with a `Filter cast` text box. (Q1, Q3)
- [assumed] A `Clear filters` button resets every panel key except `sort`/`direction`; empty result copy `No movies match these filters.` (Q5)
- [assumed] Results count line `12 movies` / `1 movie` / `0 movies` (pluralized over `totalCount`); error lines `Movies could not be refreshed.` (results, prior rows kept, no retry button), `Genres could not be loaded.` and `Cast could not be loaded.` (facet lists). (design.md §8)
