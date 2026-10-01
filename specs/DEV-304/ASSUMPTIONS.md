# DEV-304 - Assumptions

- [assumed] Q5: Only matching trailing (YYYY) or [YYYY] with four ASCII digits is a year marker. Trim the final folder name and preceding title; preserve title content, dots and underscores. No/malformed/invalid marker leaves the whole trimmed name and Year=null; a valid year-only name keeps Year and uses the whole trimmed name as Title. Basis: CONCLUSIONS.md Q5; recon-DEV-304 §2/§4; ReleaseYear.cs:30-43; specs/PRODUCT.md:28-29.
- [assumed] Q6: The four ticket-named extensions use a scanner-private static allowlist, OrdinalIgnoreCase on every OS, and existing MediaFormat normalisation. Basis: CONCLUSIONS.md Q6; recon-DEV-304 §2/§4/§13; specs/PRODUCT.md:18/28-29.
- [assumed] Q7: Primary means largest supported top-level video by byte length, then filename ascending using StringComparer.Ordinal. Basis: CONCLUSIONS.md Q7; recon-DEV-304 §2/§13; specs/PRODUCT.md:28-29.
- [assumed] Q8: Enumeration is top-level only; nested videos do not qualify, and a subfolder-only video produces InvalidOperationException. Basis: CONCLUSIONS.md Q8-Q9; recon-DEV-304 §2/§13; IMediaLibraryScanner.cs:5-8; specs/PRODUCT.md:28-29.
