# DEV-322 - Taste assumptions (Patron)

Per specs/PRODUCT.md §4. Each is `[assumed]`; rationale in CONCLUSIONS.md.

- [assumed] Sort dropdown labels: `Title (A-Z)`, `Title (Z-A)`, `Year (oldest)`, `Year (newest)`, `IMDb (lowest)`, `IMDb (highest)`, `Runtime (shortest)`, `Runtime (longest)`. Default `Title (A-Z)` (omitted from URL). (Q2)
- [assumed] Status chip labels: `Pending`, `Enriched`, `Not found`, `Failed`. (Q8)
- [assumed] Text search debounce 300 ms; placeholder `Search titles`. (Q9)
- [assumed] Year slider 1900..current year step 1; runtime slider 0..240 min step 5, top stop shown as `240+ min`. (Q7)
- [assumed] Runtime checkbox label `Include unknown runtime`; default unchecked, omitted from the URL when unchecked. (Q6)
- [assumed] Genre option shows name plus count badge, sorted by name (facet order, EfMovieCatalogFacetsTests); cast list sorted by name with a `Filter cast` text box. (Q1, Q3)
- [assumed] A `Clear filters` button resets every panel key except `sort`/`direction`; empty result copy `No movies match these filters.` (Q5)
