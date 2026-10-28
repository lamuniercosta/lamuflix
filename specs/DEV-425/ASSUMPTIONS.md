# DEV-425 assumptions

Patron-approved taste decisions; see CONCLUSIONS.md Q4. These do not waive structural decisions or gate requirements.

- [assumed] Label: Director. Placeholder: Search directors. Empty search/list message: No directors found.
- [assumed] Facet order: Name then Id using existing database facet ordering. Client suggestions: deterministic name/id ordering, case-insensitive name substring search, at most eight suggestions.
- [assumed] Show movie-count badges in suggestions; disambiguate shared names with identity/count while preserving separate identities. Palette defines accessible presentation within these semantics.
- [assumed] Control copy: Clear Director, Loading directors, Could not load directors, Retry, and Director <id> for a selected identity missing from facets. See CONCLUSIONS.md Q5.
