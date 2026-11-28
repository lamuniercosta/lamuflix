# DEV-554 — Patron conclusions

Grill opened by Keel, 2026-11-28. Sources read: ticket (via recon-DEV-554), note DEV-554, `specs/PRODUCT.md`.

## Standing rulings (pre-grill)

1. **Multiple `.srt` entries in one archive** — take the ticket default: best name match to the release name; ties go to the largest entry.
   - Basis: ticket *Open points for the spec*, bullet 1 (default stated) and *Wanted* 4 ("the spec decides the rule").
2. **Download-count source** — whatever SubDL's own API docs name (header or field), checked on the day; an absent or unparseable count surfaces as `SubtitleQuota.Unknown`.
   - Basis: ticket *Open points for the spec*, bullet 2; *Wanted* 5.
3. **Zip reading uses the BCL `System.IO.Compression.ZipArchive`; no new NuGet package** (care item 1). A third-party archive library would need its own ruling.
   - Basis: `specs/PRODUCT.md` §5 care list 1; ticket *Out of scope* (SRT only, zip only), so the BCL covers the need.
4. **Archive reader lives in `src/LamuFlix.Infrastructure/Adapters/`** as a concrete class beside the adapters, not a new project or layer (care item 2).
   - Basis: ticket *Where it lands* row 1; `specs/PRODUCT.md` §2 (no speculative abstractions).
5. **Superseded by Q1.** The original ruling accepted the key in the query string.

## Grill answers

- **Q1 (transport). Accept Keel's recommendation.** Use the v2 API: `GET /api/v2/subtitles/search` (`type=movie` with `tmdb_id`) and `GET /api/v2/subtitles/{nId}/download` in its default zip format. The key goes in the `X-API-Key` header and only to the validated HTTPS `BaseUrl` origin, never in a URI. A redirected or separately hosted archive fetch carries no credential and must hit an allowed download host. No `unpack=1` and no `format=file`. URI/error/log/trace redaction stays as defence in depth. No key is ever committed. `Enabled=true` with a blank key means the provider is off.
  - Basis: the ticket says to check the HolyArrs hints against SubDL's own docs (*Problem*, last sub-bullet). recon-DEV-554:108 shows the v2 header auth and endpoints, so the header is within the ticket, not a change to it.
  - Zip-only keeps *Wanted* 4 (archive reader) and *Done when* bullet 2 meaningful. *Wanted* 1 still requires the search and download host options, which act as an allowlist.
  - `PRODUCT.md` §5 care item 5 (secrets): the ticket's *Wanted* 3 and *Done when* bullet 3 are met more strongly when the key never appears in a URI. *Wanted* 1 ("with no key it is off") covers the blank-key case.
6. **Scope is movies only**; TV search kinds are not declared.
   - Basis: ticket *Wanted* 2; *Out of scope* bullet 1.
