# DEV-19 Conclusions

## Q1 — EF model and persistence mapping

**Accept A.** EF maps `MovieRecord`, `ActorRecord`, `DirectorRecord`, and `GenreRecord` in the ticket's Infrastructure/Persistence adapter. `MovieConfiguration` configures `MovieRecord`, retaining typed columns and VO converters; skip navigations create the implicit join tables. The repository maps between persistence records and the Core Movie aggregate. This mapping is adapter implementation, not a new architectural layer; no owner checkbox is required.

- DEV-19 Scope & Technical Design 1 and 2 (`artifacts/DEV-19/receipt.json:4`, description lines 9-15) requires skip navigations, the `movies` table, VO conversions, nullable columns, and the named indexes. It does not specify `IEntityTypeConfiguration<Core.Movie>`; naming `MovieConfiguration` does not require mapping Core directly. A preserves those requirements.
- `CONTEXT.md:38-40` explicitly defines the Movie Aggregate as distinct from the EF Movie entity. `specs/PRODUCT.md:23-24` makes CONTEXT the architecture reference and its decisions already owner-approved. Constitution IV (`.specify/memory/constitution.md:171-173`) still calls that aggregate EF-tracked and conflicts with this settled distinction; apply the explicit CONTEXT decision rather than re-opening it as a new constitution departure.
- Constitution I (`.specify/memory/constitution.md:101-119`) keeps Infrastructure dependent on Core and recognises the existing repository port. Keeping persistence records and their mapping within its adapter introduces no project, port, or intervening service layer; it satisfies `specs/PRODUCT.md:18` and care list 2 (`specs/PRODUCT.md:44`).
