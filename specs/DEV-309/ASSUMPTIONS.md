# DEV-309 assumptions

- [assumed] Q1: Name the internal flat Api query-binding record BrowseMoviesRequest if the owner approves that binding approach. Basis: Keel's round-1 recommendation; PRODUCT.md:28-29. The name does not authorize changing the ticket's direct-binding requirement.

- [assumed] Q8: Use LibraryResponses.cs in Api/Endpoints for the internal sealed BrowseMoviesResponse, MovieSummaryResponse, MovieDetailsResponse and MovieMetadataResponse records if the owner approves DTO substitution. Basis: Keel's round-2 option (ii), existing feature folder and PRODUCT.md:28-29. Names/file grouping are taste; Q8's field types and owner decision are not assumptions.
