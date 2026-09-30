# DEV-19 Assumptions

- [assumed] Q2: Database identifiers use lowercase snake_case. Beyond the ticket-mandated `movies`, table names are `actors`, `directors`, `genres`, `movie_actors`, `movie_directors`, and `movie_genres`; column names follow the same convention. Basis: DEV-19 Scope 1 relationship replacement and Scope 2 table name; CONCLUSIONS.md Q2.

- [assumed] Q5: Use runtime_minutes for Duration/Runtime, release_year for Year/ReleaseYear, plot for Synopsis, poster_url for legacy Poster, rotten_tomatoes_rating for RottenTomatoes, and meta_score for MetaScore. Preserve the metadata title separately as metadata_title. Basis: ticket Scope 2, current Core MovieMetadata, legacy Movie fields, and CONCLUSIONS.md Q5.
