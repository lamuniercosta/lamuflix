# DEV-19 Conclusions

## Q1 — EF model and persistence mapping

**Accept A.** EF maps `MovieRecord`, `ActorRecord`, `DirectorRecord`, and `GenreRecord` in the ticket's Infrastructure/Persistence adapter. `MovieConfiguration` configures `MovieRecord`, retaining typed columns and VO converters; skip navigations create the implicit join tables. The repository maps between persistence records and the Core Movie aggregate. This mapping is adapter implementation, not a new architectural layer; no owner checkbox is required.

- DEV-19 Scope & Technical Design 1 and 2 (`artifacts/DEV-19/receipt.json:4`, description lines 9-15) requires skip navigations, the `movies` table, VO conversions, nullable columns, and the named indexes. It does not specify `IEntityTypeConfiguration<Core.Movie>`; naming `MovieConfiguration` does not require mapping Core directly. A preserves those requirements.
- `CONTEXT.md:38-40` explicitly defines the Movie Aggregate as distinct from the EF Movie entity. `specs/PRODUCT.md:23-24` makes CONTEXT the architecture reference and its decisions already owner-approved. Constitution IV (`.specify/memory/constitution.md:171-173`) still calls that aggregate EF-tracked and conflicts with this settled distinction; apply the explicit CONTEXT decision rather than re-opening it as a new constitution departure.
- Constitution I (`.specify/memory/constitution.md:101-119`) keeps Infrastructure dependent on Core and recognises the existing repository port. Keeping persistence records and their mapping within its adapter introduces no project, port, or intervening service layer; it satisfies `specs/PRODUCT.md:18` and care list 2 (`specs/PRODUCT.md:44`).

## Q2 — Database identifier naming and implied relationship tables

**Accept A; care list 3 approved within ticket scope.** Explicitly configure lowercase snake_case table and column names, including skip-navigation join tables and their FK columns. Use `movies`, `actors`, `directors`, `genres`, `movie_actors`, `movie_directors`, and `movie_genres`; examples of column names are `library_path`, `imdb_id`, `movie_id`, and `actor_id`. Keep join entities implicit, with no explicit join CLR classes. No naming-convention dependency is added.

- DEV-19 Scope 2 (`artifacts/DEV-19/receipt.json:4`, description lines 10-15) fixes `movies` and requires per-entity configurations. Snake_case for the remaining identifiers is an explicit assumed convention consistent with that name, not a claim that the ticket spells out every identifier. Configure names in those configurations, including the skip-navigation relationship configuration.
- DEV-19 Scope 1 (same receipt, description line 9) replaces `MovieActor`, `MovieDirector`, and `MovieGenre` with skip navigations. The corresponding actors/directors/genres tables and movie-to-target join tables implement those named relationships and therefore count as the ticket's own implied tables under `specs/PRODUCT.md:45`; this does not authorise unrelated tables or relationships. Q1 defines their persistence-record representation.
- `specs/PRODUCT.md:34-45` assigns these care-list decisions to Patron. Explicit naming keeps schema identifiers independent of CLR persistence-record suffixes and makes this convention visible in the configurations; no new dependency or architectural layer is needed. The assumed names are logged in `ASSUMPTIONS.md`.
