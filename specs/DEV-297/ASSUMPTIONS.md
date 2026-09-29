# DEV-297 assumptions

- [assumed] Q7 query-string keys use lower camel case: `text`, `genreIds`, `actorIds`, `runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown`, `yearMin`, `yearMax`, `statuses`, `inWatchlist`, `sort`, `direction`, `page`, and `pageSize`. Repeated array keys preserve element order. Basis: DEV-297:30-31 requires a codec for the FsCheck round trip; `specs/PRODUCT.md` §4 lets Patron settle taste.
