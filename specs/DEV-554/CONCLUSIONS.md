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
5. **API key in the query string is accepted** because SubDL requires it; redaction of request URIs before logging/tracing/error text is mandatory and is the security control (care item 5, secrets).
   - Basis: ticket *Problem* bullet 3 and *Wanted* 3; *Done when* bullet 3 (no key in any log line at any level).
6. **Scope is movies only**; TV search kinds are not declared.
   - Basis: ticket *Wanted* 2; *Out of scope* bullet 1.
