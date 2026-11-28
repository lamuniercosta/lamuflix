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
6. **Scope is movies only**; TV search kinds are not declared.
   - Basis: ticket *Wanted* 2; *Out of scope* bullet 1.

## Grill answers

- **Q1 (transport). Accept Keel's recommendation.** Use the v2 API: `GET /api/v2/subtitles/search` (`type=movie` with `tmdb_id`) and `GET /api/v2/subtitles/{nId}/download` in its default zip format. The key goes in the `X-API-Key` header and only to the validated HTTPS `BaseUrl` origin, never in a URI. A redirected or separately hosted archive fetch carries no credential and must hit an allowed download host. No `unpack=1` and no `format=file`. URI/error/log/trace redaction stays as defence in depth. No key is ever committed. `Enabled=true` with a blank key means the provider is off.
  - Basis: the ticket says to check the HolyArrs hints against SubDL's own docs (*Problem*, last sub-bullet). recon-DEV-554:108 shows the v2 header auth and endpoints, so the header is within the ticket, not a change to it.
  - Zip-only keeps *Wanted* 4 (archive reader) and *Done when* bullet 2 meaningful. *Wanted* 1 still requires the search and download host options, which act as an allowlist.
  - `PRODUCT.md` §5 care item 5 (secrets): the ticket's *Wanted* 3 and *Done when* bullet 3 are met more strongly when the key never appears in a URI. *Wanted* 1 ("with no key it is off") covers the blank-key case.
- **Q2 (quota). Accept Keel's recommendation.** Download quota comes from a coherent `X-RateLimit-Limit`/`-Remaining`/`-Reset` snapshot on the v2 download response, and only where SubDL's docs identify those headers as download usage. Search-response counters are search usage and never fill download quota. Absent, malformed, partial or ambiguous values give `Unknown`. The reset time is never inferred, so there is no assumed midnight-UTC reset. There is no `/api/v2/me` call, polling or bootstrap in DEV-554. This refines standing ruling 2.
  - Basis: ticket *Wanted* 5 and *Open points* bullet 2 ("whatever SubDL's docs name; unknown counts show as unknown"); recon-DEV-554:110.
  - Adding a `/me` call would add an unrequested outbound call and change request volume, which the ticket does not ask for. If wanted later, it is a follow-up: `noted, no ticket`.
- **Q3 (shared zip-reader contract). Accept Keel's recommendation.**
  - **Limits:** each caller passes validated limits. SubDL defaults are 8 MiB compressed, 32 MiB total expanded and 128 entries, and the existing SRT payload cap still applies at publication. Every entry counts toward the entry cap. Actual decompressed bytes are bounded across all entries, including ignored extensions, rather than trusting the archive headers.
  - **Rejections:** reject malformed archives, any limit breach, no `.srt` entry, and unsafe names (rooted, drive-qualified, or `..` traversal with `/` or `\`). Each has a distinct reason that is safe to show. Nothing is extracted to disk.
  - **Choosing the `.srt` entry:** first an exact normalized stem match to the candidate release name, then the most shared distinct release-name tokens, then the largest actual size, then ordinal entry-name order, then archive order. The selected bytes pass the existing handler SRT checks.
  - Basis: ticket *Wanted* 4 (caps, paths never used as file paths, SRT checks reused) and *Done when* bullet 2. The ranking follows the ticket default in *Open points* bullet 1 and standing ruling 1; the token tier and the final tie-breaks only make "best name match" and its ties deterministic. The SRT seam is at recon-DEV-554:87.
  - Rejecting the whole archive for a traversal name satisfies *Done when* bullet 2 (`../x.srt` rejected) and stays within the ticket. The cap values are spec detail, not a care-list item.
- **Q4 (pickup and integration boundary). Accept Keel's recommendation.**
  - **Pickup:** DEV-551 is a hard prerequisite. At pickup, confirm that its provider registry, per-provider quota state and shared contract suite have landed, then build on those seams. The recorded WireMock fixtures, cleaned of keys, go where DEV-549 puts fakes. If DEV-549 has not landed by fixture time, confirm the location with the Conductor before writing any fixture. There is no parallel fake framework.
  - **File scope (care item 6):** the work may touch:
    - the new files in `Infrastructure/Adapters/` and `Core/Options/`;
    - only the DEV-551 registration/composition code needed to wire the provider in;
    - the existing integration and unit tests that cover what changes;
    - `README.md`, `docker/api.settings.example.json` and `SubtitleSetupDocumentationTests`, as *Wanted* 6–7 require.
    Any other tracked file needs a fresh ruling.
  - **Out of bounds:** the handler's SRT seam and the public API shape stay as they are, so care item 4 is not triggered. There are no schema, web, new project/layer/dependency, `Features:LocalPlay` or `Process.Start` changes (care items 1, 2, 3 and 5).
  - Basis: ticket *Needs* ("DEV-551. Run after DEV-549 if possible"); *Wanted* 5–7; *Where it lands* table; recon-DEV-554:83–98 and :111 (neither DEV-551 nor DEV-549 artifacts are tracked at HEAD); `PRODUCT.md` §5 care list.
- **Q5 (title-and-year search). Accept Keel's recommendation.**
  - Title and year form one search kind.
  - At pickup, check the current v2 docs and the sanitized fixtures for how the year maps:
    - if v2 documents a year request filter, use it;
    - if not, v2 must document a result-year field, and results are filtered to the requested year before any candidate is returned.
  - The year is never silently dropped, and no candidate with a different year is returned. A contract case covers the same title in a different year.
  - If v2 documents neither, pause the transport work and send a `needs recon` request through the Conductor, then come back for a Patron ruling. The legacy endpoint is not approved here.
  - Basis: ticket *Wanted* 2 (title and year as a search kind) and *Done when* bullet 1 (contract suite passes); recon-DEV-554:108–109 (v2 documents title search; the year filter is documented only in the legacy docs); Q1 ruling (v2 transport).
- **Q6 (delivery evidence). Accept Keel's recommendation.**
  - **Contract suite:** the DEV-551 shared contract suite runs against sanitized, recorded WireMock responses. It covers all three search kinds, the year case, the zip download, and download quota both known and unknown. WireMock is the CI path.
  - **Options:** tests cover enabled with a key and enabled with a blank key (provider off).
  - **Key leakage:** a sentinel key is checked at every log level and on traces and errors, including an upstream failure and a redirected download.
  - **Archive:** tests cover each of the three caps (compressed, expanded, entry count), traversal names, a missing `.srt`, a corrupt archive, the ranking, and rejection by the existing SRT checks.
  - **Property tests** use the repo's existing FsCheck, with no new dependency. They show the choice does not depend on entry order whenever the deciding ranks differ, and they probe each limit at its boundary.
  - **Gates:** the normal size-M backend gates run at build and rebase.
  - **Delivery PR:** it must include a real search on the home cluster with one download, stating the movie, the languages, the candidate count and that the download succeeded, with credentials removed.
  - Basis: ticket *Wanted* 1–7 and *Done when* bullets 1–4; the `task-pipeline` note, Phase 3 step 5 and Phase 5; care item 1 (no new dependency).
- **Q7 (closing bar, frozen scope, round cap). Accept Keel's recommendation; the grill can close.**
  - **Closing bar:** a concrete in-scope Critical or High finding blocks. So does any demonstrated breach of the ticket's acceptance, credential safety or archive safety, whatever its severity. Every other finding is recorded and dispositioned as advisory or follow-up.
  - **Frozen scope:**
    - the movie-only SubDL v2 provider and its options;
    - the secure zip reader, returning through the existing SRT seam;
    - download quota through DEV-551;
    - focused tests and fixtures;
    - setup docs;
    - the home-cluster receipt.
    Anything else becomes a follow-up, not a finding in this round.
  - **Round cap:** two review rounds, with up to two fix commits per round. Round 1 reviews the full diff; round 2 reviews only the fix diff. At the cap, any unresolved blocker goes to the owner on the PR, and the acceptance bar stays in force.
  - Basis: ticket *Wanted*, *Done when* and *Out of scope*; `task-pipeline` standing constraints; `agent-pipeline` Loop Discipline. This is a standing ruling, cited once for this chain.
- **Step 3.3 (Principles VII/VIII and SubDL options). Amend VII narrowly, behind an unchecked owner checkbox. VIII is met with vendor-neutral Core names, so it needs no exception.**
  - **Why VII must be amended:** VII lists the options records as a closed set (constitution.md:782-807). Its subtitle exception covers only `Subtitles:OpenSubtitles` (:788-801). The ticket requires typed `Subtitles:SubDl` options (`Enabled`, `ApiKey`, `BaseUrl`, hosts), and with no key the provider is off (recon-DEV-554:37, :63-64). Nothing in the current text allows either. That is a constitution departure, which charter §2.3(b) sends to the owner. Patron judges it necessary because the ticket text is authoritative. The spec PR therefore carries `blocked: structural — approve the VII amendment adding the SubDL acquisition options exception?` as an unchecked checkbox, and Gate 1 stays closed until the owner ticks it.
  - **What the amendment says (SubDL only; nothing else in VII changes):**
    - The vendor-neutral options record is added to the VII list. It binds `Subtitles:SubDl` as typed options with a validator.
    - SubDL is effectively disabled when `Enabled` is false or the key is blank. In that state it makes no outbound call, it is not an error, and it never fails startup.
    - When enabled but invalid (for example a non-HTTPS `BaseUrl`, a missing host, or a missing or non-positive cap):
      - startup does not fail;
      - SubDL is unusable and makes zero outbound calls;
      - the per-provider state (DEV-551) reports it as misconfigured, with the key redacted.
    - Settings are validated before any outbound use.
    - The archive caps (archive bytes, unpacked total, entry count) are required typed values. Code supplies no defaults, because VII forbids guessed numeric defaults (:800-801). A missing cap makes SubDL invalid. The values go in the example and home config, and the spec states them for the owner to approve.
    - These stay unchanged:
      - OpenSubtitles and other providers;
      - `SaveEnabled` and read-only precedence;
      - existing-track serving;
      - `GET /api/subtitles/languages`;
      - secret isolation.
  - **How the 409 works with several providers (spell this out in the amendment, because it changes behaviour today's text describes):**
    - **Search** returns 409 only when no provider has effective, valid settings. SubDL being off or invalid while another provider is valid is not a 409: the search returns the valid providers' results and shows SubDL's state.
    - **Download** returns 409 when the chosen candidate's provider has missing or invalid settings. The read-only library policy is checked before that configuration failure (:795-796).
    - **What the owner is approving:** today a blank OpenSubtitles key gives 409. After this, the same blank key with a valid SubDL gives SubDL results. The checkbox text must say so.
  - **VIII naming:** Core types are named for what they do, not for the vendor. For example `ArchiveSubtitleProviderOptions` and its validator; the spec picks the final names. The ticket's "SubDL options and validator" (recon:64) names a purpose, not identifiers. The vendor name may appear only:
    - on the Infrastructure adapter (for example `SubDlSubtitleProvider`);
    - in the `Subtitles:SubDl` configuration-key string (:855-857).
    No VIII exception is requested.
  - **Order of steps:**
    1. On the spec PR: the constitution edit, with the version bump and Sync Impact Report required by its amendment procedure, plus the unchecked checkbox.
    2. The owner ticks the checkbox and merges.
    3. T000 runs before any build task and checks, with recorded evidence:
       - the owner checkbox is ticked on the merged spec PR (`gh pr view`);
       - `origin/main:.specify/memory/constitution.md` contains the amended VII clause and the new version line;
       - DEV-551's provider seam and per-provider state are on main. They are absent at the spec HEAD (recon:111), and the 409 rule depends on them;
       - the cap values the owner approved.
       If any check fails, T000 fails and no build starts.
  - **If the owner rejects:**
    - The constitution edit is reverted on the spec branch.
    - The ticket cannot be delivered as written, so DEV-554 goes back to the owner to re-scope under §2.3(a), and the build does not start.
    - Patron offers no workaround. Nesting SubDL under `SubtitleProviderOptions` would still break the `Subtitles:OpenSubtitles` path clause.
  - Basis: constitution VII :782-807, VIII :852-857; recon-DEV-554:37, :63-64, :111; charter §2.3(b) and care item 4. No owner approval is presumed.
