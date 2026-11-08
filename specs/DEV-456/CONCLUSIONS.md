# DEV-456 grill conclusions

Evidence authority: recon-DEV-456, recorded base 659a2e9b739c092dc8565ee31ed6bc5bcc4b68e3. Decisions below are Patron rulings; no implementation or Gate 1 approval is claimed.

## Q1 — Route, access, and side effects

Keel asked: The ticket fixes GET /api/movies/{id}/stream. Recommend a read-only binary response using the current library caller access policy, with no playback-history writes, launcher, Process.Start, secrets, or Features:LocalPlay dependency. Compose LocalPlay=false must stream. This prevents GET/seek requests becoming playback events and keeps existing launch behavior intact. Approve or amend with cited basis and explicit section 2.3 ruling.

Patron answered: APPROVED as recommended; full cited ruling follows.

[from Patron] DEV-456 Q1 ruling: APPROVED as recommended — record in specs/DEV-456/CONCLUSIONS.md (Keel-owned; Patron does not edit concurrently).

Verdict: GET /api/movies/{id}/stream is a read-only binary response under the current library caller access policy, with no playback-history writes, no launcher, no Process.Start, no secrets, and no Features:LocalPlay dependency; Compose with Features:LocalPlay=false must still stream.

Cited basis (facts only from recon-DEV-456):
- Ticket-named route is already decided, not a new Patron invention: recon §2 (ticket names GET /api/movies/{id}/stream, resolver reuse, library-root containment, full/partial/range/seeking via Compose nginx) and recon §10.4 (route ticket-named → decided).
- Resolver reuse only, not the play path: recon §4 — production caller PlayMovieCommandHandler.cs:24 calls ResolvePrimaryVideo only after launcher.EnsureEnabled() at :22 and before launcher.Launch at :26; the stream handler would be a fourth ResolvePrimaryVideo caller. Reusing the launcher-bearing play handler would import LocalPlay gating and playback side effects the ticket does not name.
- §2.3.5 (Security & Local Execution, PRODUCT.md §5 item 5): ruled — streaming does not touch Features:LocalPlay, Process.Start, or secrets. Basis: recon §10.5 — the flag today only swaps the launcher (DisabledMediaPlayerLauncher throws FeatureDisabledException→403), Compose example sets Features.LocalPlay=false, the ticket never mentions LocalPlay, and keeping the flag untouched (resolver reuse only) is the ticket-conformant reading. Therefore LocalPlay=false must not 403 the stream.
- §2.3.4 (API Shape, PRODUCT.md §5 item 4): ruled — the only API shape fixed here is the ticket-named GET route returning the file bytes read-only. Basis: recon §10.4 — all other shape (200/206, 416, headers, OpenAPI binary documentation) is not yet in ticket or spec and remains for later grill questions, not this ruling. No playback-history write: recon §10.4 notes the play command records history via IPlaybackHistory; a GET (and each range/seek re-request) is not a play command, so writing history would turn reads/seeks into playback events with no ticket basis.
- Access policy: recon §7 — ThumbnailEndpointTests Routes_ThumbnailEndpoint_MirrorsTheLibraryCallerAccessPolicy is the existing precedent for a read-bytes endpoint mirroring the library caller access policy; apply the same policy to stream.
- §2.3.3: no schema change is implied or approved by this ruling (recon §10.3 — ticket names no tables; streaming is read-only over movies).
- Not blocked: structural — recon §10 closes: nothing found forces a ticket-text contradiction or a constitution departure. Existing launch behaviour stays intact because PlayMovieCommandHandler and PlaybackEndpoints are untouched by this ruling.

---

## Q2 — Range contract and machinery

Keel asked: Recommend ASP.NET Core built-in seekable file-result range processing on an open readable seekable file, without whole-file loading or a custom parser. Freeze no Range as 200 full bytes; satisfiable single bytes range as 206 with exact Content-Range/Content-Length; Accept-Ranges: bytes; unsatisfiable valid byte range as 416 with Content-Range bytes */length; media Content-Type from resolved format; inline delivery without an attachment filename. Malformed, unsupported, or multiple ranges follow framework behavior only after Wisp doc-evidences it before spec freeze. Do not promise multipart or treat every malformed Range as 416. Cost: framework edge-case semantics and required full/seek verification. Approve/amend with citations or identify missing recon facts.

Patron answered: APPROVED with freeze split; full cited ruling follows.

[from Patron] DEV-456 Q2 ruling: APPROVED with freeze split below — record in specs/DEV-456/CONCLUSIONS.md (Keel-owned; Patron does not edit concurrently).

Verdict: Use ASP.NET Core built-in seekable file-result range processing on an open readable seekable stream. No custom Range parser, no whole-file loading, no multi-range multipart promise. Freeze 200/206/416 as in the contract below; malformed, unsupported, and multiple Range handling is NOT frozen here and follows the selected framework behaviour only once Wisp doc-evidences it before spec freeze.

Frozen contract (section 2.3.4 API Shape ruling, specs/PRODUCT.md section 5 item 4):
- GET with no Range: 200, full bytes, Accept-Ranges: bytes.
- Satisfiable single bytes range: 206, exact Content-Range and Content-Length for the served span, Accept-Ranges: bytes.
- Unsatisfiable valid byte range: 416 with Content-Range: bytes */length.
- Content-Type from the resolved primary video format; delivery inline, no attachment filename (no Content-Disposition attachment).

Not frozen (do not write into spec as promises):
- Malformed Range syntax, unsupported range unit, multiple ranges, and any If-Range interplay: no promised status, no multipart/byteranges promise, and do not label every malformed Range as 416.

Cited basis (facts only from recon-DEV-456):
- Ticket already names the verification surface: recon section 2 and section 6 — full, partial, invalid ranges, and seeking verified via Compose nginx, against the four sample MP4s. The 200/206/416 split is the API-shape ruling that recon section 10.4 explicitly reserves for Patron (success 200 vs 206, 416 for invalid ranges, Accept-Ranges/Content-Range/Content-Type/Content-Disposition all listed as undecided and needing a deliberate ruling in CONCLUSIONS.md).
- Machinery has no in-repo precedent to extend: recon section 3 — zero hits for range machinery or file-serving range support in src/ and tests/; the only file-serving precedent is ThumbnailEndpoints TypedResults.Stream with no range support. Recon section 6 — framework result types can do range processing without a package, flagged as engineering assessment, not doc-verified.
- Section 2.3.1 (Dependencies): no new NuGet or npm package is approved or needed on this basis. Recon section 10.1 — no package is needed for the obvious implementation and Directory.Packages.props has no file/range-serving package; a ruling is triggered only if a package is proposed, and none is here.
- Format source for Content-Type: recon section 4 — ResolvePrimaryVideo returns PrimaryVideo(Path, Format), so the media type derives from the already-resolved format rather than a filename guess or a new mapping table invented in the endpoint.
- Resolved stream, not a reopened path: Q1 ruling already fixes resolver reuse and library caller access; serving from the open seekable stream keeps one resolved file for the whole response, including seeks.

Exact recon questions for Wisp (doc-evidence with microsoft-learn before spec freeze; Patron does not invent these behaviours):
1. In .NET 10 / ASP.NET Core, which seekable file-result type and enableRangeProcessing setting implements built-in range processing for an open Stream, and does it require the stream to be seekable with a known length?
2. What status and headers does that machinery emit in each case: no Range; single satisfiable bytes range; single unsatisfiable valid bytes range (confirm 416 and Content-Range bytes */length); malformed Range header; unsupported range unit; multiple ranges (single-part 206, multipart/byteranges, 200 full body, or ignored)?
3. Are Accept-Ranges: bytes, Content-Range, and Content-Length emitted automatically by that machinery, or must the endpoint set any of them itself?
4. Does the machinery honour or ignore If-Range, and does a malformed Range fall back to 200 full body rather than 416?
5. What is the framework-blessed mapping source from a file extension or format to the media Content-Type for the resolved video formats, if one exists?

Cost accepted: framework semantics constrain the edge cases above; that is the price of no custom parser, and full plus seek verification through Compose nginx (recon section 6 topology, Range header not overridden by nginx) is the required proof. If Wisp evidence contradicts the frozen 200/206/416 split, return to Patron for an amended ruling before freeze; do not silently re-freeze.

Not blocked: structural — no ticket-text contradiction and no constitution departure (recon section 10 closing line); this is a section 2.3.4 shape ruling on what the ticket left open.

Grill status: paused for the exact Wisp documentation questions above. Q1 and Q2 exchanges are persisted. The brief is not frozen; containment, remaining error mapping, Compose verification detail, file scope and loop terms still require Patron decisions. No implementation, spec freeze, or Gate 1 approval is claimed.

---

## Q3 — Recon supplement and contract completion

Keel asked: Wisp confirms core 200/206/416, documents Results.Stream enableRangeProcessing and MIME provider, but Learn does not specify malformed, unsupported-unit or multiple ranges, automatic lengths/Accept-Ranges, or exact 416 Content-Range. Recommend explicitly superseding Q2 requirement for pre-freeze doc proof of unspecified edges: retain framework behavior, characterize these edges at implementation time without promising statuses or multipart, and assert frozen headers through real API and Compose tests. Use FileExtensionContentTypeProvider read-only lookup on dot plus resolved MediaFormat.Extension; fallback application/octet-stream, no invented MIME table. Pass no entityTag/lastModified; do not promise conditional/If-Range statuses. Document 200/206 as binary Stream, 416 explicitly, from C# metadata through regeneration. This freezes requirements rather than inventing missing facts; failed frozen-header tests block delivery. Approve/amend with citations and explicit 2.3.4 ruling.

Patron answered: APPROVED with explicit supersession of Q2 pre-freeze doc-proof precondition for Learn-unspecified edges. Full ruling follows.

[from Patron] DEV-456 Q3 ruling: APPROVED as recommended, with explicit supersession below — record in specs/DEV-456/CONCLUSIONS.md (Keel-owned; Patron does not edit concurrently).

Verdict and section 2.3.4 ruling (API Shape, specs/PRODUCT.md section 5 item 4): The Q2 precondition that Wisp doc-prove every edge before spec freeze is superseded for the Learn-unspecified edges only. The frozen 200/206/416 contract, frozen headers, MIME rule, no-conditional rule, and OpenAPI documentation rule below are frozen now. Malformed Range syntax, unsupported range units, and multiple ranges retain framework behaviour, are characterized at implementation time, and carry no promised status and no multipart/byteranges promise in the spec.

Supersession (explicit): This ruling supersedes the Q2 clauses that malformed/unsupported/multiple Range handling follows framework behaviour only once Wisp doc-evidences it before spec freeze, and that return to Patron is required before re-freeze on doc contradiction. Replacement rule: where recon-DEV-456-2 marks a behaviour not found in roots, no doc proof is required to freeze the specification, no status is promised, and implementation-time characterization does not amend the frozen contract unless it contradicts a frozen item, in which case delivery is blocked and the contradiction returns to Patron.

Frozen items retained and now test-gated:
- No Range 200 full bytes; satisfiable single bytes range 206 with exact Content-Range and Content-Length; unsatisfiable valid byte range 416 with Content-Range bytes */length; Accept-Ranges: bytes on 200 and 206. Basis: recon-DEV-456-2 section 3 confirms 200, 206 with Content-Range and requested bytes only, and 416 in Microsoft Learn for ASP.NET Core 10; section 9 states the frozen split is confirmed, not contradicted. Section 4 marks automatic Content-Length, automatic Accept-Ranges, and section 3 marks exact 416 Content-Range as not doc-stated, so these are asserted through real API and Compose tests rather than assumed automatic, and a failed frozen-header test blocks delivery. Recon-DEV-456-2 section 9 expressly allows the endpoint to set Accept-Ranges itself if the framework does not.
- Machinery: Results.Stream / TypedResults.Stream (equivalently Results.File family) with enableRangeProcessing: true over the open stream, disposed after the response is sent. Basis: recon-DEV-456-2 section 2 — overload signature, alias for File(Stream, ...), range support returning 206 or 416, and stream disposal after send, matching the Q2 resolved-stream rule.
- MIME: FileExtensionContentTypeProvider read-only lookup on dot plus resolved MediaFormat.Extension; fallback application/octet-stream; no invented endpoint MIME table and no mutation of the provider at request time. Basis: recon-DEV-456-2 section 6 — provider is the framework mapping between extensions and MIME types with default mappings, is not thread-safe for concurrent writes but read-only lookups after configuration can be a singleton; MediaFormat exposes a normalized lower-cased dot-stripped Extension; no MediaFormat to Content-Type mapping exists in the worktree. Exact default values per extension are not found in roots (section 6 and section 8 item 5), which is why the provider is queried at runtime instead of copying an invented table. Recon-DEV-456-2 section 4 confirms application/octet-stream is the framework default when no content type is supplied.
- Conditionals: pass no entityTag and no lastModified; do not promise If-Range, If-None-Match, If-Modified-Since, or any conditional/304 status for this route. Basis: recon-DEV-456-2 section 5 — If-Range is only evaluated when entityTag or lastModified is set, and section 5 also ties Last-Modified/ETag 304 behaviour to those parameters; passing neither keeps conditional semantics out of the frozen contract. Passing fileDownloadName null keeps inline delivery with no Content-Disposition, recon-DEV-456-2 section 4.
- OpenAPI: document 200 and 206 as binary Stream responses, and 416 explicitly, in C# endpoint metadata, then regenerate the committed contract chain; never hand-edit generated files. Basis: recon-DEV-456-2 section 7 — file results contribute no response metadata automatically, the Learn example uses Produces for 200 and 206 with Stream and an explicit 416, and Stream maps to type string format binary; recon-DEV-456 section 8 fixes the C# to openapi.json to types.ts regeneration chain and forbids hand-editing generated files.

Characterization at implementation time (not a spec promise): record observed framework behaviour for malformed Range, unsupported unit, multiple ranges, seekable/known-length precondition (recon-DEV-456-2 section 2 and section 8 item 1, not found in roots), and If-Range with neither validator set (section 8 item 6) as test characterization only. These observations must not be written into the spec as guaranteed statuses; if an observation contradicts a frozen item above, that is a delivery blocker, not a silent spec change.

Not blocked: structural — no ticket-text contradiction and no constitution departure; the ticket names full/partial/invalid-range/seeking verification (recon-DEV-456 section 2), and this ruling only decides the section 2.3.4 shape the ticket and recon-DEV-456 section 10.4 left to Patron.

---

## Q4 — Library-root containment

Keel asked: Recommend authorize stored movie folder before ResolvePrimaryVideo, then returned primary file before opening. Canonicalize root/candidates with full paths, platform path comparison and separator boundaries; reject outside-root paths and symlink/reparse-point chains. Do not rely on LibraryPath validation or a string prefix. Canonical-relative folders are normal; legacy rooted folders work only when contained. Preserve resolver selection; open only an authorized regular file with one response-lifetime handle. Follow containment precedents locally without extracting or rewriting thumbnail/discovery subsystems. Cost: linked library folders/files fail closed. Same-machine malicious concurrent filesystem replacement is outside scope; missing/replaced files at open fail safely. Freeze containment and trusted-library-mount threat boundaries with cited section 2.3 rulings?

Patron answered: APPROVED and frozen as recommended. Full ruling follows.

[from Patron] DEV-456 Q4 ruling: APPROVED as recommended; containment and threat boundaries frozen — record in specs/DEV-456/CONCLUSIONS.md (Keel-owned; Patron does not edit concurrently).

Verdict: Authorize the stored movie folder before ResolvePrimaryVideo and authorize the returned primary file again before opening it. Both checks canonicalize the library root and the candidate to full paths, compare with platform path rules on separator boundaries, and reject outside-root candidates and any symlink or reparse-point chain. Do not rely on LibraryPath validation or a string-prefix check. Resolver selection is unchanged. Only an authorized regular file is opened, once, on the single response-lifetime handle already ruled in Q2/Q3.

Cited basis (recon-DEV-456 section 5, section 10, and recon-DEV-456-2 section 2):
- The gap is real and current: LibraryPath validates only non-blank and no dot-dot substring and does not enforce root containment; DirectoryMediaLibraryScanner.Resolve returns any rooted path as-is, so a stored rooted value outside the root would flow into ResolvePrimaryVideo output today, and no existing test rejects an outside-root rooted path.
- Two authorizations, not one: authorizing only the folder does not authorize the file the resolver selects inside it, and authorizing only the returned file would let resolution traverse an outside folder first. Folder-before-resolve and file-before-open are both frozen.
- DEV-459 interaction: after DEV-459, stored folders are canonical relative and are the normal case; legacy rooted folders are accepted only when the canonicalized candidate is contained, and a rooted path is never a bypass. Recon-DEV-456 section 5 flags exactly this relative-to-root plus absolute/backfill window, and recon-DEV-456 section 2 records DEV-459 items 1 and 4.
- Precedents are followed locally, not extracted: ThumbnailConsumer TryAuthorizeSource (full-path root and candidate, IsUnderRoot, HasReparsePointInChain), ThumbnailCache TryResolveContainedPath, and FileSystemLibraryFolderDiscovery IsContained with link rejection are the three containment precedents in recon section 5. The stream implementation mirrors that pattern in its own path; it does not extract a shared helper from, or rewrite, the thumbnail or discovery subsystems.
- Single handle: recon-DEV-456-2 section 2 — the stream result disposes the open stream after the response is sent, so the authorized file is opened once and that handle owns the response lifetime; no reopen by path after authorization.

Section 2.3 rulings (specs/PRODUCT.md section 5):
- Item 6 File Scope: ruled — implementing containment locally in the stream path is in scope; extracting or rewriting ThumbnailConsumer, ThumbnailCache, or FileSystemLibraryFolderDiscovery into a shared containment helper is not approved by this ruling and would need its own deliberate ruling. Recon-DEV-456 section 10.6 names exactly that reshaping as a section 2.3.6 ruling, and the recommendation correctly avoids it.
- Item 4 API Shape: no new status is frozen here. Recon-DEV-456 section 9 records that no mapped exception for outside library root exists yet; the outside-root, missing-folder, and no-eligible-video error statuses remain for the error-mapping question and are not invented in this ruling.
- Item 5 Security and Local Execution: containment does not touch Features:LocalPlay, secrets, or Process.Start; Q1 ruling stands unchanged.
- Item 3 Database Schema: no schema change is approved or implied; DEV-459 migration remains sanctioned only in DEV-459, per recon-DEV-456 section 10.3.

Threat boundary frozen: the library mount is a trusted local library (Compose mounts it read-only at /media/library, recon-DEV-456 section 5). Within that boundary, stored-path escape, rooted-path bypass, and linked folders or files fail closed, including at open time for missing or replaced files, which fail safely without serving bytes. Same-machine malicious concurrent filesystem replacement racing authorization against open (TOCTOU by a local attacker with write access to the trusted mount) is outside this ticket; it is not promised, tested, or silently claimed. Cost accepted: legitimately linked library folders or files under the root are rejected rather than served.

Not blocked: structural — no ticket-text contradiction and no constitution departure; the ticket itself names library-root containment (recon-DEV-456 section 2), so this ruling decides how, not whether.

---

## Q5 — Errors, lifetime, and cancellation

Keel asked: Recommend malformed movie id 400; absent movie or missing folder/file including disappearance before open 404; no eligible primary video 409; containment/link denial 403; unexpected filesystem access denial or IO fault before headers sanitized 500, preserving current global default. Existing validation exceptions retain 422 where applicable. ProblemDetails must never expose paths/exception text. Framework owns 416 with required header; do not force ProblemDetails for 416. Once response starts, cancellation/read failure aborts with framework cleanup without second JSON or bogus success. Transfer open stream lifetime to result; dispose on pre-result failure and completion/cancellation, never close before send. No history/catalog mutation. Cost: 403 distinguishes safety denial, operational unreadability is 500. Approve/amend explicit error/API mapping and cancellation acceptance with cited basis.

Patron answered: APPROVED as recommended. Full ruling follows.

[from Patron] DEV-456 Q5 ruling: APPROVED as recommended; error mapping, response lifetime, and cancellation acceptance frozen — record in specs/DEV-456/CONCLUSIONS.md (Keel-owned; Patron does not edit concurrently).

Verdict and section 2.3.4 ruling (API Shape, specs/PRODUCT.md section 5 item 4): The stream endpoint uses the error map below. Every pre-header error is ProblemDetails with no filesystem path and no exception text. The framework-owned 416 is exempt from ProblemDetails. Once response headers or body have started, no second error response is attempted.

Frozen error map:
- Malformed movie id: 400.
- Absent movie, missing folder, missing primary file, and a file that disappears before open: 404.
- No eligible primary video in an existing folder: 409.
- Containment or link denial (outside-root or reparse/symlink chain ruled in Q4): 403.
- Unexpected filesystem access denial or I/O fault before headers: sanitized 500, using the current global default with no OS detail.
- Existing validation exceptions: 422 only where a ValidationException actually applies to this route; do not manufacture 422 for the cases above.
- Unsatisfiable valid range: framework-owned 416 with the Q2/Q3 frozen Content-Range bytes */length; do not force a ProblemDetails body onto the 416.

Cited basis (recon-DEV-456 section 9, section 4, section 7, and recon-DEV-456-2 section 2):
- Current global mapping is the substrate: GlobalExceptionHandler maps ValidationException to 422, BadHttpRequestException to 400, NotFoundException to 404, InvalidTransitionException to 409, FeatureDisabledException to 403, and default to 500 with no path or detail leakage; recon-DEV-456 section 9 also records that no mapped exception for outside library root or unsatisfiable range exists yet, which is why this ruling freezes the new 403 containment mapping and leaves 416 to the framework instead of inventing handler entries for them.
- Resolver failures already carry two of these meanings: recon-DEV-456 section 4 — ResolvePrimaryVideo throws NotFoundException for a missing folder and InvalidTransitionException when a folder contains no eligible top-level video file, which the global handler already turns into 404 and 409.
- Endpoint test precedent asserts the same family: recon-DEV-456 section 7 — PlaybackEndpointTests asserts problem-details for 404/409/422/403/500 and RescanEndpointTests covers missing folder 404, no eligible video 409, and malformed id 400. The 403 here is a new safety-denial mapping under section 2.3.4, deliberately distinct from the FeatureDisabledException 403; it distinguishes a containment denial from an operational failure, and unreadability faults stay 500.
- ProblemDetails sanitization is the existing default behaviour, not a new promise: recon-DEV-456 section 9 — default 500 has no path/detail leakage, and section 7 notes ThumbnailEndpointTests asserts sanitized problems. That bar applies to every pre-header stream error, including the new 403.
- Framework 416 ownership: recon-DEV-456-2 section 3 confirms 416 from the file-result machinery and section 2 confirms the result alias supports Status416RangeNotSatisfiable; Q3 already froze its Content-Range. A ProblemDetails override would fight the framework result that owns that response.

Lifetime and cancellation acceptance (frozen):
- Ownership of the open stream transfers to the file result; the endpoint must not wrap the transferred stream in a using that closes it before send. Basis: recon-DEV-456-2 section 2 — the Results.Stream overload disposes the stream after the response is sent, so endpoint lifetime ends at result execution and the framework owns completion disposal.
- A failure before the result is returned disposes the stream in the endpoint; a failure or cancellation during framework execution is disposed by framework completion/cancellation cleanup. No path leaves the handle undisposed, and no path disposes it twice as an endpoint responsibility.
- Once headers or body have started, client cancellation or a mid-body read failure aborts or finishes through framework cleanup only: no second JSON response, no ProblemDetails rewrite, and no success claim for a truncated transfer. This follows from the same disposal-after-send ownership in recon-DEV-456-2 section 2 plus the section 9 fact that the global handler can only shape a response that has not started.
- No history or catalog mutation on any path in this question; Q1 read-only ruling stands, and recon-DEV-456 section 10.4 records playback history as a play-command side effect, not a stream read effect.

Cost accepted: 403 makes safety denials distinguishable from missing content (404) and from operational unreadability (500); operators lose OS detail in the response by design and rely on server-side logs, never on response bodies.

Not blocked: structural — no ticket-text contradiction and no constitution departure; recon-DEV-456 section 10.4 expressly reserved error statuses for outside-root and missing file to a Patron ruling during spec drafting, and this is that ruling.

---

## Q6 — Compose nginx and seeking acceptance

Keel asked: Extend existing web/e2e/run.mjs harness through http://localhost:3000 web nginx /api proxy, not direct API, using sample-library import. Assert byte-exact full body/length; prefix, suffix and open-ended 206 ranges with exact spans/headers; beyond-EOF valid range 416 with bytes */length; disjoint and backwards seeks against source slices. Characterize malformed/unit/multiple ranges without status promises. Test LocalPlay=false with no history/process calls. Include browser video seek smoke on a browser-decodable sample MP4, verifying playback advances after seek via stream URL; no product player UI or all-format codec guarantee. Record source sample identity/length, ranges, statuses/headers, bytes and seek result against delivery SHA. Docker unavailable means Could not run for required live acceptance, blocking acceptance closure; script exit-2 remains SKIPPED, never PASS. No nginx tuning absent concrete failed proxy evidence and further ruling. Cost: live Compose/browser media required. Approve/amend and clarify browser proof requirement.

Patron answered: APPROVED; browser video seek is required acceptance, not optional. Full ruling follows.

[from Patron] DEV-456 Q6 ruling: APPROVED as recommended; browser video seek proof is REQUIRED acceptance, not optional smoke — record in specs/DEV-456/CONCLUSIONS.md (Keel-owned; Patron does not edit concurrently).

Verdict: Live acceptance runs through the existing web/e2e/run.mjs harness against http://localhost:3000, i.e. through the web nginx /api proxy and never directly against the API, using the sample-library import. The matrix below, including the browser seek, must pass on the same delivery SHA for acceptance closure. Docker unavailable is Could not run for this required live acceptance and blocks closure; a script exit 2 remains SKIPPED and is never reported as PASS.

Required matrix (frozen):
- Full response: byte-exact body against the source sample and exact length.
- Ranges: prefix, suffix, and open-ended single ranges each return 206 with the exact requested source slice and exact Content-Range/Content-Length; Accept-Ranges: bytes asserted as frozen in Q2/Q3.
- Beyond-EOF valid range: 416 with Content-Range bytes */length, as frozen in Q2/Q3.
- Seeking pattern: sequential disjoint ranges and a backwards seek each return the requested source slices (this is the server-side seek proof).
- Characterization only, no promised statuses: malformed Range, unsupported unit, and multiple ranges are exercised and their observed framework behaviour recorded, per Q3; the record must not be written as a guaranteed contract.
- LocalPlay: with Features:LocalPlay=false the stream succeeds, and the test asserts no playback-history write and no process/launcher call occurred (Q1 ruling made testable).
- Browser seek (required): a browser-decodable sample MP4 played through the stream URL in a real browser, seek performed, and playback verified to advance after the seek. No product player UI is built for this, and no all-format browser codec guarantee is made; the proof covers the decodable sample only.
- Evidence recorded per run: source sample identity and length, requested ranges, observed statuses and headers, byte comparisons, and seek result, all against the same delivery SHA.

Cited basis (recon-DEV-456 section 6, section 7, and harness gate rules):
- Topology is already the ticket-named path: recon section 6 — nginx is the web runtime image, location /api proxy_pass to the API preserves the prefix and does not override the Range header, the API has no host port in Compose, and scripts/run-e2e-smoke.ps1 plus web/e2e/run.mjs with BASE_URL http://localhost:3000 are the existing harness through that nginx. Testing direct-to-API would bypass the proxy the ticket names.
- Corpus exists: recon section 6 — docker/sample-library ships four MP4s (Big Buck Bunny, Elephants Dream, Sintel, Tears of Steel) as the natural full/partial/seek corpus, so no new fixture format is needed and the browser-decodable sample is chosen from this set.
- Byte and header assertions are the frozen contract made observable: Q2/Q3 froze 200/206/416, exact spans, and headers, and recon-DEV-456-2 section 9 states the Compose verification matrix is the place to prove the doc-unproven headers. Q6 is that matrix; a failed frozen assertion blocks delivery per Q3.
- Gate honesty: exit 0 pass, 1 fail, 2 SKIPPED is never a pass, and a gate that could not run has not passed (repo verification rules). Applied here: no Docker means Could not run, reported as such, and acceptance closure waits for a real run; SKIPPED is never folded into a green verdict.
- Browser scope limit: recon-DEV-456 section 11 — no video player or stream consumer exists in web/src, and the ticket names no frontend work (recon section 8: frontend work is not named by this ticket). The browser seek therefore uses a test harness page/context against the stream URL, builds no product UI, and proves only the decodable sample, not every supported extension in a browser.

Section 2.3 rulings (specs/PRODUCT.md section 5):
- Item 6 File Scope: extending web/e2e/run.mjs and the existing smoke harness is approved as the verification surface. No nginx tuning is approved: recon section 6 records no proxy_buffering or temp-file tuning exists today, and changing docker/nginx.conf is a further Patron ruling only if a concrete failed proxy acceptance demonstrates the need; it is not pre-authorized on suspicion.
- Item 4 API Shape: no new route, status, or header is created by this question; it verifies the Q1-Q5 frozen shape.
- Item 5: LocalPlay=false success with no history or process call is an acceptance assertion of the Q1 section 2.3.5 ruling, not a change to the flag.

Cost accepted: acceptance depends on live Compose plus a browser with a decodable MP4 codec, so closure is slower and environment-bound; that is the price of proving seeking where users actually play, and it is why Could not run blocks rather than substitutes a unit test.

Not blocked: structural — the ticket itself names full/partial/range/seeking verified via Compose nginx (recon-DEV-456 section 2), so this ruling fixes the matrix and the required browser proof; it adds no product scope.

---

## Q7 — Approach, architecture, and file boundaries

Keel asked: Recommend new Api/Endpoints/StreamEndpoints.cs orchestrating existing movie-details query and IMediaLibraryScanner.ResolvePrimaryVideo; narrow Core/Ports/ILibraryFileReader.cs and Infrastructure/FileSystem/LibraryFileReader.cs for folder authorization and authorized read-open. No general service layer, new project, schema, package, or Core handler. API owns result, MIME and route-local containment 403 mapping; Infrastructure owns paths/links/file IO. New focused endpoint/file-reader tests; amend ApiEndpoints registration, existing Infrastructure DI registration as identified from recon, OpenApiContractTests, LibraryEndpointsTests, web/e2e/run.mjs, and regenerate openapi.json/types.ts/mocks/handlers.ts. No product UI, launcher, global error remapping, selector rewrite, thumbnail extraction, nginx/config rewrite, deletion or unrelated rewrite. Exact existing DI file is not evidenced: constrain plan to existing composition seam and have Wisp identify file before implementation rather than guess. Freeze responsibilities under 2.3.1/2/3/5/6? Cost: one narrow filesystem port for independent real IO and HTTP lifetime tests.

Patron answered: APPROVED and frozen; Wisp must identify exact DI file before implementation. Full ruling follows.

[from Patron] DEV-456 Q7 ruling: APPROVED as recommended; approach and bounded file-responsibility set frozen — record in specs/DEV-456/CONCLUSIONS.md (Keel-owned; Patron does not edit concurrently).

Verdict: Build one new StreamEndpoints.cs in the existing API endpoints folder orchestrating the existing movie-details query and IMediaLibraryScanner.ResolvePrimaryVideo; add one narrow Core port ILibraryFileReader with its Infrastructure FileSystem implementation for folder authorization and authorized read-open. The API owns the framework stream result, MIME lookup, and the route-local containment 403 mapping. Infrastructure owns real paths, link/reparse checks, and file I/O. No general streaming-service layer, no new project, no schema, no package, no new Core handler.

Responsibility split (frozen):
- API (StreamEndpoints.cs): route, movie-details query, ResolvePrimaryVideo call, FileExtensionContentTypeProvider read-only MIME lookup per Q3, Results.Stream/TypedResults.Stream with enableRangeProcessing per Q3, route-local 403 for containment denial per Q5, and the Q5 pre-header error shaping that the existing global handler does not already supply.
- Core (Ports/ILibraryFileReader.cs): the narrow authorization-plus-open contract only; no business handler, no HTTP type, no framework result type crosses this port.
- Infrastructure (FileSystem/LibraryFileReader.cs): canonical full-path root/candidate handling, separator-boundary containment, symlink/reparse-chain rejection per Q4, regular-file check, and the single authorized read-open returning the response-lifetime stream per Q2/Q5.
- Tests: new focused endpoint tests and file-reader tests; characterization of unspecified range edges per Q3 lives here, not in the spec contract.
- Amended existing files, bounded to: ApiEndpoints.cs registration line; the existing Infrastructure DI registration at its current composition seam (exact file not evidenced — see below); OpenApiContractTests operation metadata; LibraryEndpointsTests route inventory; web/e2e/run.mjs for the Q6 matrix; and generated web/src/api/openapi.json, types.ts, and mocks/handlers.ts produced by the generation tools only.
- Explicitly out: production UI, launcher or playback-handler changes, global error-handler remapping, scanner selector rewrite, thumbnail/discovery extraction, nginx or Compose config rewrite, any file deletion, and any unrelated rewrite.

Cited basis (recon-DEV-456 sections 3, 4, 7, 8, 10; recon-DEV-456-2 sections 2, 6, 7):
- Endpoint shape follows the existing composition: recon section 3 — Program.cs maps app.MapApiEndpoints through ApiEndpoints.cs chaining per-feature Map methods on /api, and a stream endpoint means a new Endpoints file plus one registration line. The thumbnail endpoint is the read-then-serve precedent: recon section 4 — resolve details via the movie-details query, then open bytes (ThumbnailEndpoints.cs pattern).
- Resolver reuse is a port call, not new selection logic: recon section 4 — IMediaLibraryScanner.ResolvePrimaryVideo is the existing Core port with three production callers; the stream endpoint becomes the fourth and the PrimaryVideoSelector rule is untouched.
- The narrow port mirrors the existing port/implementation split (Core Ports interface, Infrastructure FileSystem implementation, as with IMediaLibraryScanner and DirectoryMediaLibraryScanner in recon section 4), so real I/O and HTTP lifetime can be tested independently: endpoint tests double the ports as PlaybackEndpointTests already doubles IMediaLibraryScanner and friends (recon section 7), while file-reader tests exercise real paths and links like the containment tests cited in recon section 5.
- Contract amendments are the known drift points: recon section 8 — adding the route requires OpenApiContractTests ExpectedOperations, the LibraryEndpointsTests endpoint-name list, and regenerated openapi.json/types.ts/handlers.ts via the generation script; generated files are never hand-edited. Recon-DEV-456-2 section 7 supplies the Produces metadata pattern the endpoint declares so regeneration carries 200/206 binary Stream and explicit 416.

Section 2.3 rulings (specs/PRODUCT.md section 5):
- Item 1 Dependencies: no new NuGet or npm package is approved; framework range processing and the framework MIME provider are already the ruled machinery (recon section 10.1, recon-DEV-456-2 sections 2 and 6).
- Item 2 Architecture: no new project, top-level folder, or general layer is approved. The single narrow ILibraryFileReader port is ruled a port in the existing Core Ports seam, not a repository, mediator, or services abstraction; recon section 10.2 reserves a streaming-service layer or project for a Patron ruling, and this ruling declines that shape in favour of the named narrow port.
- Item 3 Database Schema: no schema or migration is approved or implied; streaming stays read-only over existing movie data (recon section 10.3).
- Item 5 Security and Local Execution: this approach touches no Features:LocalPlay, no launcher, no Process.Start, and no secrets; Q1 ruling stands, and Q6 asserts it in acceptance.
- Item 6 File Scope: the new files and the amended-file list above are the approved scope. Deletions, the out-list above, and any DI file beyond the existing composition seam are not approved. The exact existing Infrastructure DI registration file is not evidenced in recon: the plan must constrain itself to the existing composition seam, and Wisp must identify that file from the worktree before implementation; nobody guesses or creates a new registration location.

Cost accepted: one extra narrow port and its tests, bought back as independent testability of real filesystem authorization/open against HTTP result lifetime, without mocking the filesystem behaviour the containment ruling depends on.

Not blocked: structural — no ticket-text contradiction and no constitution departure; recon section 10.6 already enumerates these expected new and amended files, and this ruling freezes that set.

---

## Q8 — Test strategy, gates, and task ordering

Keel asked: Phase B Wisp drift/dependency and exact-DI-file recon first; real-filesystem tests for relative/contained legacy rooted paths, outside/sibling-prefix escapes, links, missing files and regular-file enforcement; endpoint integration covers 200/206/416 bytes/headers, 400/403/404/409/500 sanitization, access parity, LocalPlay=false/no side effects, cancellation/disposal. Use existing xUnit conventions, real disk for filesystem behavior, existing Testcontainers patterns for EF, never driver mocks. Then C# OpenAPI metadata and contract regeneration/inventory; then Compose nginx/browser acceptance; then normal build-workspace Roslyn, complexity, diff-scoped InspectCode, eligible-C# mutation, property, vulnerable-package, format, dotnet test and applicable web gates. Retain selector properties; add containment properties if useful; no property opt-out without explicit task-note basis. No runtime gates in spec workspace. Preserve exact gate statuses and live acceptance blocking. Approve/amend as one delivery plan with cited basis?

Patron answered: APPROVED as one delivery plan. Full ruling follows.

[from Patron] DEV-456 Q8 ruling: APPROVED as recommended; test strategy and task ordering frozen as one delivery plan — record in specs/DEV-456/CONCLUSIONS.md (Keel-owned; Patron does not edit concurrently).

Verdict: Delivery proceeds in this order and no later phase substitutes for an earlier one: (1) Phase B Wisp drift/dependency recon and exact DI-file identification; (2) focused real-filesystem file-reader tests; (3) endpoint integration tests; (4) C# OpenAPI metadata, contract regeneration, and inventory tests; (5) Compose nginx plus browser acceptance; (6) full normal build-workspace gates. No runtime gates run in this spec workspace; gates belong to the implementation workspace and are reported with their true statuses.

Phase detail (frozen):
- Phase B first: Wisp reconfirms drift and dependencies at implementation base and names the exact existing Infrastructure DI registration file left unidentified in Q7. Implementation does not guess that file and does not start before this recon lands.
- Real-filesystem tests before endpoint integration: relative contained paths, contained legacy rooted paths, outside-root escapes, sibling-prefix escapes, symlink/reparse links, missing files, and regular-file enforcement, all on real disk. Filesystem translation is never mocked; EF behaviour, where touched at all, uses only the existing Testcontainers patterns with no data-access driver mocks.
- Endpoint tests next, in current xUnit conventions: 200/206/416 with byte and header assertions, 400/403/404/409/500 with ProblemDetails sanitization, library caller access-policy parity, LocalPlay=false success with no history or process side effects, and cancellation/disposal ownership per Q5.
- Contract after endpoint behaviour: C# Produces metadata for 200/206 binary Stream and explicit 416, regeneration of openapi.json/types.ts/handlers.ts by the generation tools only, and the OpenApiContractTests plus LibraryEndpointsTests inventory updates.
- Live acceptance after contract: the Q6 Compose nginx matrix through http://localhost:3000 including the required browser seek, on the same delivery SHA.
- Gates last, in the build workspace: Roslyn analyzers, cyclomatic complexity, and InspectCode diff-scoped; mutation on eligible changed C#; property tests; vulnerable packages; format; dotnet test; and web gates for the web-side changes. PrimaryVideoSelector property tests are retained untouched; containment boundary properties may be added if useful, and a property-test opt-out is never claimed without an explicit task-note basis.

Cited basis (recon-DEV-456 sections 5, 6, 7, 8, 10 and repo gate rules):
- Real-disk containment proof has precedent and a known gap: recon section 5 — the three containment precedents carry real tests (ThumbnailConsumerTests, ThumbnailCacheTests, FileSystemLibraryFolderDiscoveryTests), while no test today rejects an outside-root rooted path in the resolver; Q8 closes that gap at the file-reader level before any endpoint work depends on it. Repo rule: never mock the data-access driver for behaviour depending on real query translation or serialisation — use Testcontainers; the same principle keeps filesystem authorization on real disk.
- Endpoint test shape is the existing one: recon section 7 — PlaybackEndpointTests doubles the ports via ApiHostFactory and asserts the 404/409/422/403/500 problem-details family, ThumbnailEndpointTests asserts byte-exact serving, sanitized problems, concurrent reads, and access-policy parity, and RescanEndpointTests covers the 404/409/400 cases Q5 maps onto. Q8 reuses those conventions instead of inventing a new test architecture, matching the repo rule to keep the existing assertion and mocking libraries.
- Selector properties already exist and stay: recon section 7 — PrimaryVideoSelectorPropertyTests runs FsCheck permutation and exclusion invariants; streaming does not change selection (Q4/Q7), so those tests are retained as regression proof, and new containment properties are additive only.
- Contract ordering follows the drift machinery: recon section 8 — OpenApiContractTests compares ExpectedOperations both ways against the committed openapi.json and regeneration is gated by the environment flag and generation script; metadata must exist in C# before regeneration can carry it, and inventory tests close the loop.
- Live acceptance ordering follows Q6: recon section 6 — nginx proxy preserves Range, the sample-library corpus exists, and the e2e harness targets localhost:3000; unit and endpoint tests cannot prove proxy or browser seek behaviour, so Compose plus browser runs after the contract is stable and before gates close the delivery.
- Gate status honesty is a repo rule, not a preference: exit 0 pass, 1 fail, 2 SKIPPED or opt-out is never PASS, NOT APPLICABLE is mutation-only, and a gate that could not run is reported Could not run. Q6 already ruled Docker-unavailable live acceptance as Could not run blocking acceptance closure; Q8 preserves every one of those distinctions verbatim in delivery reporting, including that mutation is minutes-expensive and pre-PR scoped to eligible changed projects.

Section 2.3 note: this plan introduces no dependency, project or layer, schema, API shape, LocalPlay/secret/Process.Start change, or file beyond the Q7 frozen set; it orders and proves that set. Wisp drift recon in Phase B is the check that this statement still holds at implementation base; if it does not, the delta returns to Patron before code.

Not blocked: structural — ordering and test strategy are Patron-decidable under the ticket named verification matrix (recon-DEV-456 section 2) and change nothing the ticket delivers.

---

## Q9 — Loop terms and shared understanding

Keel asked: Recommend Critical/High block code-review/ship-review closure. Medium/Low receive explicit evidence-based disposition, never waiving failed gates or frozen acceptance. Frozen scope is primary-file HTTP stream, existing resolver reuse, contained authorization/open, range/error contract, generated API chain, regression tests and required Compose nginx/browser seek in Q1-Q8; anything else is a follow-up issue, not a finding in this round. Cap at two remediation/review rounds; remaining above-bar findings block and report to Bernstein, never silently waived. M requires all Sentry/Ledger/Compass reports and PR publication. This freezes grill/brief only, not plan challenge, Gate 1, runtime acceptance or merge. No taste assumptions; ASSUMPTIONS.md says none; trusted mount is explicit Q4 ruling. Approve loop terms and shared understanding to freeze Q1-Q9, leaving exact DI-file identification to Phase B Wisp before implementation?

Patron answered: APPROVED and brief freeze authorized from Q1-Q9. Full ruling follows.

[from Patron] DEV-456 Q9 ruling: APPROVED; loop terms frozen and brief freeze authorized on Q1-Q9 — record in specs/DEV-456/CONCLUSIONS.md (Keel-owned; Patron does not edit concurrently).

Verdict: Yes, shared understanding is confirmed. Keel is authorized to freeze the grill/brief from Q1-Q9 exactly as persisted in CONCLUSIONS.md. This freeze covers the brief only. It does not approve plan challenge adjudication, Gate 1, runtime acceptance, implementation, or merge; those remain their own later steps, and merge authority stays with the user.

Loop terms (frozen):
- Critical and High findings block code-review and ship-review closure. Medium and Low findings each receive an explicit evidence-based disposition in the review record; no disposition, at any severity, waives a failed gate or a frozen acceptance item.
- Frozen scope for findings is the Q1-Q8 set: the primary-file HTTP stream on the ticket-named route, existing resolver reuse, contained authorization and authorized open, the frozen range and error contract, the generated API contract chain, regression tests, and the required Compose nginx plus browser seek evidence. Anything outside that set is a follow-up issue routed out of this round, not a finding expanded inside it.
- Remediation and review are capped at two rounds. Above-bar (Critical/High) findings still open after the cap block closure and are reported to Bernstein; they are never silently waived and the loop never iterates unbounded.
- Size M closure requires all three axis reports (Sentry, Ledger, Compass) and publication of the review outcome on the PR. A review round is not finished until it is on the PR.

Assumptions: APPROVED as recommended — no taste assumptions are made in Q1-Q9. ASSUMPTIONS.md for DEV-456 records none, in those words. The trusted-library-mount boundary is an explicit Q4 ruling in CONCLUSIONS.md, not an assumption, and must not be moved into ASSUMPTIONS.md.

Open delegation (sole remaining unknown): the exact existing Infrastructure DI registration file, not evidenced in either recon, is delegated to Phase B Wisp drift/dependency recon before implementation, per Q7/Q8. Implementation must not guess it, create a new registration location, or start before that identification lands. Everything else in Q1-Q9 is decided; no other open question is carried into the brief freeze.

Cited basis:
- Scope and authority: the ticket names the route, resolver reuse, containment, and the Compose verification matrix (recon-DEV-456 section 2), and recon section 10 closes that nothing forces a ticket-text contradiction or a constitution departure; Q1-Q8 therefore decided only what the ticket and specs left open, under specs/PRODUCT.md section 5 items 1-6 as ruled question by question.
- Freeze limits: specs/PRODUCT.md section 3 makes the ticket Scope and Technical Design authoritative and already-decided; Q9 freeze asserts the brief matches that authority, while Gate 1, plan challenge, runtime acceptance, and merge are separate authorities no grill ruling can grant — merge in particular is never Patron or agent authority.
- Taste rule correctly yields none: specs/PRODUCT.md section 4 requires taste assumptions to be logged tagged [assumed] in ASSUMPTIONS.md; Q1-Q9 contains no label wording, sort order, empty-state copy, or UI decision (Q6 expressly builds no product player UI), so the honest record is none rather than an invented entry.
- Gate and acceptance honesty carries into review: repo gate rules keep PASS, FAIL, SKIPPED, NOT APPLICABLE, and Could not run distinct, and Q3/Q6 already ruled that failed frozen-header tests and unavailable live acceptance block delivery; review dispositions inherit that bar and cannot waive it.

Not blocked: structural — no owner checkbox arises from Q1-Q9; the two escalation classes in specs/PRODUCT.md section 5 (ticket-text contradiction, constitution departure) were checked at each question and neither occurred.

---

## Grill closure receipt — 2026-11-07

Patron Q9 confirmed shared understanding and authorized brief freeze from Q1-Q9. Keel wrote brief.md and ASSUMPTIONS.md. The earlier paused status is historical and superseded by Q3 and this closure. Full Q1-Q9 exchanges and cited rulings are persisted above. No taste assumptions, structural owner checkbox, plan freeze, Gate 1, implementation approval, runtime acceptance or merge clearance is claimed. The exact existing DI file remains a mandatory Phase B Wisp identification before implementation, as explicitly ruled in Q7-Q9.

---

## Q10 — New-port ADR and constitution compliance correction

Keel asked: Q7 adds ILibraryFileReader; brief.md:148 and plan.md:22 prohibit ADR. Constitution Principle I requires an ADR for every added port; Governance requires compliance. Recommend retain approved narrow port, supersede no-ADR constraint, authorize a scoped constitution named-port enumeration extension with required governance record and ArchitectureTests whitelist update preserving existing entries, plus ADR before port implementation. This follows the constitution existing permitted enumeration-extension procedure (:65-82), without waiving any principle or changing ticket deliverables, so no structural owner checkbox is implied. Exact whitelist facts absent from recon must go to Wisp; Keel will not infer them or write an ADR without Bernstein assigned number. Approve this compliant correction with cited basis, or rule another compliant shape?

Patron answered (verbatim):

> [from Patron] DEV-456 Q10 approved: retain Q7 narrow ILibraryFileReader port and supersede the no-ADR constraint in brief.md:148 and plan.md:22. Basis: Constitution Principle I requires an ADR for every added port and Governance requires compliance; constitution:65-82 provides the permitted named-port enumeration-extension procedure, so use that scoped extension with its required governance record, not a waiver. Conditions: ADR before port implementation once Bernstein assigns its number; ArchitectureTests whitelist update preserves existing entries and adds only the exact new port, with exact whitelist facts routed to Wisp as needs-recon if absent, never inferred. No principle is waived and no ticket deliverable changes, so no structural owner checkbox. Update brief first, CONCLUSIONS second, then give Quill one numbered analysis fix list.

Disposition: APPROVED compliant correction. Brief Q10 was recorded first. Q10 supersedes the earlier no-ADR constraint and sole-unknown statements only; Q1-Q9 streaming behaviour, required live acceptance, review cap, and authority boundaries remain operative. Constitution Principle I (:299-305), permitted extension procedure (:65-82), and Governance (:712-734) are the cited basis. Compliance requires the scoped named-port extension/governance record, port ADR before implementation, and exact new-port architecture enforcement preserving existing entries. These bounded surfaces supplement Q7 file scope; no principle or gate is waived, and no structural owner checkbox arises.

Accepted cost: ADR and scoped governance/enforcement work before port implementation. Open facts: Bernstein must assign the ADR number; Wisp must supply the exact whitelist facts absent from both recon notes before the architecture edit. Existing Phase B exact-DI-file recon remains mandatory. No ADR number or whitelist pattern is inferred, no ADR or constitution is edited in this analyze turn, and no production or test code is changed. Quill receives one numbered analysis fix list to propagate the decision into its own artifacts. Analysis closure, plan challenge/freeze, ordinary Gate 1, implementation, runtime acceptance, and merge clearance remain separate and unclaimed.

---

## Q10 evidence completion receipt — Phase 2 step 3.3 resume

Bernstein instructed: Claimed ADR number is 0037; read recon-DEV-456-3 and author docs/adr/0037-library-file-reader-port.md, then independently reanalyze Quill repair round 1. Keel recorded the assigned number and recon evidence in brief Q10 prerequisite evidence update first, then this receipt. ADR 0037 was authored and read back; it records the already-approved decision, not a new ruling or Gate 1 approval.

Wisp recon-DEV-456-3 section 3 supplies the exact whitelist location and additive edit: ArchitectureTests.cs:30-31, PortWhitelistPattern, append |ILibraryFileReader before )$, preserving the 16 existing entries and order, count floor, namespace selection and test body. Port plus whitelist insertion must form one buildable slice, with snapshot facts refreshed at Phase B pickup. This closes the previously missing number/whitelist facts only. Constitution enumeration/governance compliance remains a required prerequisite; no constitution or production/test code is changed by Keel. The pre-existing constitution omissions reported by Wisp are not silently added to this scoped port extension. Exact DI-file identification remains delegated to Phase B Wisp. Plan challenge, plan freeze and ordinary Gate 1 remain pending.

### Q10 bounded baseline disposition

Keel asked: recon-DEV-456-3 section 3.4 reports pre-existing constitution named-port list omits IEnrichmentOutbox and IRecommendationProfileSource while code whitelist has all 16 existing ports. Recommend keep Q10 extension limited to ILibraryFileReader and preserve both existing surfaces as found except that addition; classify the pre-existing omission as out-of-scope follow-up via Bernstein/Rigger, with no blanket constitution-compliance claim. This does not relax any rule for the new port. Approve bounded disposition with cited Q9 frozen-scope/Q10 basis?

Patron answered (verbatim):

> [from Patron] DEV-456 Q10 bounded disposition approved: keep the constitution named-port extension limited to ILibraryFileReader only. Basis: Q9 frozen scope and Q10 as recorded, with recon-DEV-456-3 section 3.4 showing IEnrichmentOutbox and IRecommendationProfileSource already in the code whitelist of 16 existing ports but omitted from the pre-existing constitution list. Preserve both existing surfaces exactly as found apart from the single ILibraryFileReader addition; classify that pre-existing constitution omission as an out-of-scope follow-up routed via Bernstein to Rigger, and make no blanket constitution-compliance claim. This relaxes nothing for the new port: ADR 0037 as assigned by Bernstein, now authored, still precedes port implementation. Do not expand Keel artifacts beyond recording this bounded disposition.

Disposition: record only this bounded ruling; no additional scope or principle waiver. Brief was updated first. Bernstein must route the pre-existing omission to Rigger for follow-up recording; Keel does not write YouTrack.

## Q11 — Plan challenge ruling (2026-11-07)

Keel asked Patron to settle seven bounded implementation/governance choices arising from Sentry F1-F3, Ledger M1-M4/L1-L2, and Compass F1-F5: explicit narrow port signature and policy exception; authorized async shared open and link checks; route-local nosniff; structured movie-id errors; explicit Build-PR governance ownership; buildable red-test sequence/link-proof precondition; inherited-only 422. Recommended choices are recorded first in brief Q11. Patron returned:

> [from Patron] DEV-456 Q11 ruled item by item, no Patron file edits. 1 AMENDED to Keel preference: signature void AuthorizeFolder(LibraryPath folder) and Stream OpenRead(LibraryPath file), BCL types in Core only, same canonicalization and link denial in both, OpenRead authorizes the returned file then opens once; introduce LibraryFileAccessDeniedException in Core Ports used only for policy and safety denial including non-regular path, route-local maps only that type to 403, missing folder or file or disappearance maps existing NotFoundException to 404, OS UnauthorizedAccessException and genuine IO or permission or read failure are not blanket-caught and remain sanitized 500 with no raw path or detail exposed (basis Q7 narrow port, recon-456 sections 5 and 7, Principle VI). 2 APPROVED: per-component link and reparse rejection on the authorized canonical path only, one async FileStream with FileShare.Read plus Asynchronous and SequentialScan, with focused concurrent-read proof (recon sections 5 and 9). 3 APPROVED: route-local X-Content-Type-Options nosniff on successful binary 200 and 206 stream responses only, tested and documented, no global header or config edit (recon section 10). 4 APPROVED: Principle VI applies, structured errors carry the requested movie id without filesystem paths including route-local 403 and post-header failure, add tests and task rather than claiming existing pipeline evidence, no new telemetry names or framework types in Core (Constitution Principle VI). 5 APPROVED: preserve Q10 extension in the Build PR, Keel verifies ADR and prior governance after pickup, Keel-owned constitution task adds only ILibraryFileReader plus Sync Impact Report and Last Amended with appropriate MINOR bump (1.11.0 if current remains 1.10.0, refresh at pickup), no backfill of prior omitted ports, ADR 0037 stays Proposed until extension and governance exist (Q10, Constitution Principle I and Governance, recon-3 section 3.4). 6 APPROVED: buildable type and whitelist slice before real-disk tests compile, behavior-red tests only after referenced surfaces exist, no uncompiled-red commits, drop provisional P markers, Windows link proof fails loud if no actual link fixture exists and is never SKIPPED as PASS (Q9 frozen scope, recon sections 7 and 9). 7 APPROVED: 422 stays inherited mapping only, no synthetic stream validation scenario, correct-status assertions cover mapped negatives (Q3-Q10). Overall these refine implementation and required compliance without ticket contradiction or principle departure, so no structural owner checkbox.

Disposition: approved as amended. Adds only the explicit policy-denial exception within the existing Core Ports seam, alongside Q7 narrow reader. All gate and acceptance requirements remain. Ledger L4 is answered by recon-DEV-456-3 section 2: 0036 occupied on feature/458-spec, 0037 assigned by Bernstein; no numbering repair or search needed. Sentry F4 is accepted personal-scale residual per Q7/Q9; observed exhaustion routes to follow-up through Bernstein/Rigger, no throttle work in DEV-456. Ledger L3 warrants bounded removal of stale drafting banners and reference-based Q10 wording without erasing historical decisions. Full 17-item adjudication and repair verification belong in adjudication-DEV-456-Keel. No PLAN FROZEN or Gate 1 approval claimed by this append.

## Plan challenge freeze receipt — 2026-11-07

**PLAN FROZEN — ready for ordinary owner Gate 1 review.** This append supersedes earlier pending-plan-challenge/freeze statuses only. Authoritative plan content is the Q1-Q11 brief plus the independently verified Quill repair-round-1 artifacts; drafting banners describe authorship history, not an outstanding Keel decision.

Keel adjudicated all 17 findings from Sentry (4), Ledger (8), Compass (5): 0 Critical/High, 7 Medium, 10 Low. Fifteen document corrections are independently verified, Sentry F4 is an explicitly accepted personal-scale residual, Ledger L4 is resolved by recon-DEV-456-3 section 2. Patron Q11 settled all required decisions with cited basis. One bounded plan-challenge repair round used; no second round required. ADR 0037 status/format/contract repaired and independently read back.

Read-only cross-artifact reanalysis and brief-to-plan/tasks comparison: 0 unresolved planning findings, 13 FR + 5 SC = 18/18 requirements with task coverage; 28 unique unchecked tasks; no unmapped feature tasks. No provisional parallel markers on T002 or endpoint test triad/new logging tests. Ordering is T000 Gate 1 receipt -> T001 Wisp facts -> T001b Keel analyze -> T001c Keel verify-only -> T001d Keel constitution/governance -> T003 atomic buildable type/whitelist -> T002 behavioral tests -> T004 reader -> T005 DI -> endpoint -> contracts -> live Compose/browser -> gates/review/delivery. Existing brief Q8 wording is refined by Q11, not permission for noncompiling-red commits.

Receipt: Maestri note adjudication-DEV-456-Keel in LamuFlix notes, with individual dispositions and file:line verification. Worktree F:/Dev/LamuFlix.worktrees/feature-456-spec; branch feature/456-spec; current Git base/HEAD 659a2e9b739c092dc8565ee31ed6bc5bcc4b68e3. The reviewed documents are working-tree artifacts, not committed content attributed to that base SHA.

Ordinary Gate 1 remains pending; no structural owner checkbox arises. No implementation authorization, constitution amendment completion, Principle I/VI implementation PASS, runtime/build gate result, live acceptance, PR publication, or merge clearance is claimed. ADR 0037 remains Proposed; T001d in the Build PR still requires the scoped extension/governance, and Phase B Wisp must supply exact DI-file identification and dependency/drift facts before Keel pickup analysis. Required nginx/browser acceptance remains mandatory; Could not run/SKIPPED never PASS. Bernstein owns next routing/publication; the user owns merge.

---

## Gate 1 ruling — Phase 2 step 7 (2026-11-07)

[from Patron] DEV-456 Gate 1 ruling: `gate1: provisional` set on `specs/DEV-456/spec.md` (`**Status**: gate1: provisional`).

- Preconditions met: Keel plan-challenge freeze receipt above records 0 unresolved planning findings, 18/18 requirements with task coverage, and all 17 findings dispositioned (0 Critical/High) in `adjudication-DEV-456-Keel`; `checklists/requirements.md` User-Visible Outcome items are ticked (Nothing visible in Compose SPA, with reason).
- Boundary: this is provisional only — ordinary Gate 1 remains pending owner merge of the spec PR, no structural owner checkbox arises (Q10/Q11; no ticket contradiction or constitution departure), and no implementation, Principle I/VI PASS, runtime gate, or merge clearance is claimed.
- ADR 0037 is authored and remains Proposed until the Build-PR scoped constitution extension/governance (T001d) exists.


## T000 — Gate 1 spec-PR merge receipt (2026-11-08)

Evidence: Conductor pickup instruction and Maestri task note `DEV-456`, Intake lines 5-7, record spec PR #338 merged at `cfbb7ae4`, with `specs/DEV-456/` on main. The task note Rigger intake receipt lines 12-14 names worktree `F:/Dev/LamuFlix.worktrees/DEV-456`, branch `feature/DEV-456`, pickup HEAD `17d9545ced1d0cd811403a0ece4c4ecec3a1f3b6`; recon-DEV-456 lines 4-7 independently records that pickup provenance and the landed spec files. This is a receipt of the recorded owner spec merge, not a new Patron ruling.

T000 is complete. This receipt supersedes historical pending-owner-Gate-1 banners only. T001/T001b remain incomplete: recon-DEV-456 identifies the existing API registration seam at `src/LamuFlix.Api/HandlerRegistration.cs:32-34` (lines 11 and 23), conflicting with the Infrastructure-only DI wording in plan/tasks, but does not establish DEV-438/DEV-459 implementation dependency state or DEV-459 PR #343 compatibility. No feature implementation clearance, completed governance, build/runtime PASS, or dependency waiver is claimed. Missing recon must be supplied before the pickup verdict and seam disposition.


## Q12 — Phase B dependency and DI-seam pickup (2026-11-08)

Keel asked whether DEV-459 PR #343 must merge before DEV-456 implementation and whether the evidenced API registration seam supersedes the Infrastructure-only wording. Recommended: retain DEV-459 as an integration/delivery dependency, implement against the unchanged scanner contract after T001b/c/d, verify after #343 lands before closure, and register the reader beside the scanner in the existing API composition file.

Patron answered:

[from Patron] DEV-456 pickup ruling (Q12): APPROVED as recommended on both bounded pickup questions — Keel to record in brief.md, specs/DEV-456/CONCLUSIONS.md, and authorized task text.

Verdict: (1) DEV-459 (#343 at 22404dfc, awaiting-merge) remains a delivery/integration dependency only and does not block DEV-456 Build implementation against the unchanged IMediaLibraryScanner contract on main (17d9545c, with DEV-438 landed through 86f9aeb5), with T001b/T001c/T001d still gating T003 and live/delivery verification required after #343 lands before closure; (2) register ILibraryFileReader -> LibraryFileReader in src/LamuFlix.Api/HandlerRegistration.cs:32-34 beside IMediaLibraryScanner while keeping LibraryFileReader in src/LamuFlix.Infrastructure/FileSystem/LibraryFileReader.cs, and amend Infrastructure-only DI wording in plan.md/tasks.md (and spec.md where referenced) as factual seam drift with no new layer, file, or scope change.

- Dependency boundary & section 2.3 (items 1-5 unchanged; no ticket contradiction or constitution departure): recon-DEV-456:8-16 and task note DEV-456:31-35 verify DEV-438 is on origin/main through 86f9aeb5 and IMediaLibraryScanner (Scan / ResolvePrimaryVideo at src/LamuFlix.Core/Ports/IMediaLibraryScanner.cs:5-10) is identical across main (17d9545c) and DEV-459 PR #343 (22404dfc). Because spec.md:24,73-75,99,107-110 and Q4 already require accepting both post-DEV-459 relative folders and contained legacy rooted folders with independent AuthorizeFolder-before-resolve and OpenRead-after-resolve physical containment, DEV-456 must not import or duplicate #343 schema/LibraryPathKey/lexical-helper changes and must verify integration after #343 lands before live acceptance/delivery closure (T001b/T001c/T001d still gate T003 per spec.md:201 and tasks.md:20,47-49,59).
- DI composition seam & section 2.3.2 / 2.3.6 ruling (existing seam, named file scope): task note DEV-456:21, recon-DEV-456, and src/LamuFlix.Api/HandlerRegistration.cs:32-36 verify the existing filesystem/port DI composition seam is AddLamuFlixHandlers in src/LamuFlix.Api/HandlerRegistration.cs (registering IFileSystem, IMediaLibraryScanner, ILibraryFolderDiscovery, IMovieFolderImporter), not a file under src/LamuFlix.Infrastructure. Under section 2.3.2 (no new layer/project) and section 2.3.6 (named existing DI seam per Q7/Q8, CONCLUSIONS.md:212,226,245), registering services.AddScoped<ILibraryFileReader, LibraryFileReader>() in src/LamuFlix.Api/HandlerRegistration.cs beside IMediaLibraryScanner while keeping LibraryFileReader in src/LamuFlix.Infrastructure/FileSystem/LibraryFileReader.cs is approved.
- Authorized task/plan drift correction (no structural blocker): correcting Infrastructure DI wording in specs/DEV-456/plan.md, specs/DEV-456/tasks.md (T001, T005), and specs/DEV-456/spec.md (Scope 7, FR-011) to name src/LamuFlix.Api/HandlerRegistration.cs is factual Phase B seam alignment already anticipated by Q7/Q8 and tasks.md T001/T001b (tasks.md:46-47), creating no owner Gate 1 checkbox.

### T001 pickup disposition

- **T001 complete; DEV-459 does not block implementation as a dependency.** DEV-438 is on main through `86f9aeb5`; no separate DEV-438 PR merge receipt is claimed (updated recon-DEV-456 line 8). DEV-459 #343 remains awaiting-merge at `22404dfcedcbccfe8f4af1c7d1d22d0d9ebf2567`, not landed (line 9). Pickup base is `17d9545ced1d0cd811403a0ece4c4ecec3a1f3b6` (lines 4-5).
- **Dependency finding disposed by Patron Q12:** identical scanner contract (line 12), changed relative identity/lexical canonicalization/persistence (lines 13-14), and absent stream-reader authorization in #343 (line 15) are compatible with spec Scope 2-3 / US1. Keep folder authorization before resolution and file authorization before one open; relative normal-case and contained legacy-rooted coverage remain mandatory. Do not import or duplicate DEV-459 schema/key/helper work. Revalidate against landed DEV-459 before live acceptance/delivery closure; record that later landed SHA and actual test/runtime results.
- **DI finding disposed by Patron Q12:** task note DEV-456 line 21 preserves Wisp registration evidence `src/LamuFlix.Api/HandlerRegistration.cs:32-34`. Current recon replaced the original intake note; the earlier receipt remains historical evidence, not current recon line numbering. Register `ILibraryFileReader` to `LibraryFileReader` there beside the scanner; keep reader implementation in Infrastructure. Brief, spec, plan, and tasks now name that seam; no new composition layer/file.
- T000 receipt remains valid. Its earlier T001-blocked paragraph is historical and superseded by this disposition. **T001b/T001c/T001d remain unchecked and still block T003**; this bounded dependency/seam reanalysis does not assert full pickup analyze completion, refreshed whitelist/governance proof, constitution amendment, Principle I implementation PASS, runtime acceptance, or merge clearance. No production files changed.

## T001b / T001c / T001d — Pickup execution receipt (2026-11-08)

Authority: Conductor Phase 3 step 2 execution ask; brief Q10/Q11/Q12; T000/T001 receipts above.
Worktree: `F:/Dev/LamuFlix.worktrees/DEV-456`, verified branch `feature/DEV-456`.
Evidence base: Wisp `recon-DEV-456` lines 4-16 at `17d9545ced1d0cd811403a0ece4c4ecec3a1f3b6`;
task note `DEV-456` lines 17-23 (pickup drift receipt) and 29-43 (dependency/seam receipt).
This append supersedes the preceding incomplete-task statuses only; no new product decision.

### T001b — Read-only specification analysis report

Automated prerequisite command `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks`
returned exit 1: `Not on a feature branch. Current branch: feature/DEV-456` (numeric-branch convention).
**Automated analysis: Could not run.** Completed the explicitly requested manual equivalent across
brief, spec, plan, tasks, CONCLUSIONS, current constitution and Wisp pickup facts. No analysis hooks apply.

| ID | Category | Severity | Evidence / disposition |
| --- | --- | --- | --- |
| P1 | Dependency drift | Disposed | recon lines 8-16; Q12 in brief/spec/plan/tasks retains DEV-459 as integration/delivery dependency, unchanged resolver contract, independent physical authorization and post-merge verification. No dependency merge invented. |
| P2 | DI seam drift | Disposed | Task note line 21; brief Q7/Q12, spec Scope 7/FR-011, plan structure and tasks T001/T005 consistently name Api/HandlerRegistration.cs beside the scanner. Adapter stays in Infrastructure. |
| P3 | Constitution prerequisite | Required separate execution | Principle I lacked ILibraryFileReader at pickup; T001d explicitly delivers the amendment before T003. No current feature port/code is claimed compliant by planning alone. Amendment execution is recorded below, outside read-only analysis. |

| Requirement | Task coverage |
| --- | --- |
| FR-001 | T006, T007, T008, T010, T014, T015 |
| FR-002 | T006b, T007, T010 |
| FR-003 | T003, T002, T004, T005, T006b |
| FR-004 | T006, T007, T010 |
| FR-005 | T006, T007, T010, T014, T015 |
| FR-006 | T006a, T007, T010 |
| FR-007 | T002, T006, T006c, T007, T010 |
| FR-008 | T006, T007, T010 |
| FR-009 | T009, T011, T012, T013 |
| FR-010 | T014, T015 |
| FR-011 | T001c, T001d, T003, T005, T016 |
| FR-012 | T001, T001b, T001c, T001d, T003, T002, T017, T018, T019 |
| FR-013 | T000, T016, T020, T021 |
| SC-001 | T006, T014, T015 |
| SC-002 | T006, T014, T015 |
| SC-003 | T014, T015 |
| SC-004 | T002, T006, T010, T014, T015 |
| SC-005 | T013, T017, T018, T019, T020, T021 |

Coverage: 13 FR + 5 SC = 18/18 (100%); 28 unique tasks, no unmapped tasks.
No unresolved ambiguity, duplication, critical issue or additional pickup task drift found.
Historical drafting/Gate 1 banners are superseded by the explicit freeze/T000 receipts, rather
than new open gates. Q11 ordering remains T003 buildable port/whitelist slice before T002
behavioral reds; no same-file parallelism. Principle VI errors and real link/concurrent-reader
proofs remain explicit implementation requirements. T001b complete; no further Quill fix round.

### T001c — Verify-only ADR/governance/whitelist-plan pickup

Read existing `docs/adr/0037-library-file-reader-port.md`: present, Proposed, exact Q11 port,
policy exception, file-open flags, scoped amendment and atomic whitelist obligation. No ADR edit
or status transition. Brief Q10 prerequisite evidence update, Q11, plan Constitution Check and
T003 agree on the recon-DEV-456-3 section 3 plan: append only `|ILibraryFileReader` before `)$`
at ArchitectureTests.cs:31, preserving all 16 entries/order, selector, floor and method body.
Wisp pickup drift receipt in task note lines 19-20 reports nine added spec files and zero
src/test/web drift, so the recorded whitelist plan requires no refreshed pattern fact. This is
verification of the plan from supplied recon, not an independent production-code measurement.
Prior governance authority is Q10/Q11, constitution Governance, and owner spec-merge T000.
T001c complete; whitelist code remains exclusively T003 and omitted-port backfill stays excluded.

### T001d — Constitution amendment delivered in working tree

Updated `.specify/memory/constitution.md` from 1.10.0 to **1.11.0**, Last Amended **2026-11-08**,
retaining Ratified **2026-09-21**. Principle I gains only `ILibraryFileReader` beside the scanner;
all prior entries/rules remain. Prepended Sync Impact Report records authority, unchanged titles,
no added/removed sections, template/runtime-guidance review, migration and follow-up obligations.
Plan/spec/tasks templates need no edit; command-template directory and docs/quickstart.md absent.
README/AGENTS carry no duplicated named-port inventory needing synchronization. No schema/data
migration; atomic T003 port/whitelist implementation remains required. DEV-467 remains separate.

T001b, T001c and T001d marked **[X]** in tasks.md. Their pickup prerequisites for T003 are
satisfied by these working-tree deliverables, ready for Conductor routing/publication. No commit,
push, ADR acceptance, production/whitelist edit, implementation compliance PASS, build/runtime
gate, live acceptance, dependency delivery closure or merge clearance is claimed. Required
post-DEV-459 integration and nginx/browser acceptance still block delivery closure.


---

## Q13 — Phase 5 sample-library acceptance corpus ruling (2026-11-08)

Verdict: Adopt Option 1 — under Charter §2.3.6 (`specs/PRODUCT.md` §5 item 6), authorize replacing the tracked 0-byte placeholder MP4s in `docker/sample-library/` (`Big Buck Bunny (2008).mp4`, `Elephants Dream (2006).mp4`, `Sintel (2010).mp4`, `Tears of Steel (2012).mp4`) with small, real, browser-decodable H.264 MP4 clips (`moov` atom present, seekable duration) as a forced acceptance-corpus edit in scope for Phase 5–6 (T014–T016); reject Options 2 and 3.

- **Forced acceptance edit & §2.3.6 File Scope (`specs/PRODUCT.md` §5 item 6; `CONCLUSIONS.md:46,181`; `spec.md` Scope 6–7, FR-010–FR-011, SC-001–SC-003)**: The ticket and Q6 require full (`200`), partial (`206`), beyond-EOF (`416`), sequential/backwards seek, and real-browser seek verification through Compose nginx against `docker/sample-library` copied by `web/e2e/fixtures.mjs:78` and `web/e2e/run.mjs:80,233`, whereas all four tracked `docker/sample-library/*.mp4` files are 0-byte empty blobs (`e69de29b` since `3f17c9b0`; `specs/DEV-17/research.md:38`). Because the ticket acceptance criteria and Q6 gate force decodable non-empty MP4 bytes in `docker/sample-library`, populating `docker/sample-library/*.mp4` is approved in scope under §2.3.6 and supplements the Q7 / FR-011 / T016 bounded file footprint without changing ticket deliverables or creating an owner Gate 1 checkbox.
- **Option 2 rejected under §2.3.1 Dependencies (`specs/PRODUCT.md` §5 item 1; `.github/workflows/ci.yml:31-32,106-132`; `CONCLUSIONS.md:48,181,222`)**: Synthesizing clips at e2e runtime with `ffmpeg` adds an unapproved runtime tool dependency to the e2e harness and CI `e2e` job (which installs no `ffmpeg`), leaves default `LIBRARY_PATH=./docker/sample-library` Compose runs with 0-byte files, and contradicts Q6 (`CONCLUSIONS.md:181`).
- **Option 3 rejected under Q6 corpus contract (`CONCLUSIONS.md:181`; `spec.md` US4, FR-010, SC-003)**: Reusing `tests/LamuFlix.IntegrationTests/Fixtures/Thumbnails/short-video.mp4` (1.0s, 10 frames, 1,914 bytes) as the e2e runtime source couples `web/e2e` to a `.NET` integration-test thumbnail fixture, departs from the Q6 ruling that the browser-decodable sample comes from `docker/sample-library`, leaves `docker/sample-library` broken at 0 bytes, and provides an overly thin 1-second clip for browser seek advancement.


---

## Q14 — Phase 4 Compass F2 dangling-symlink precedence ruling (2026-11-08)

Verdict: Link policy takes precedence (`LibraryFileAccessDeniedException` → `403 Forbidden`) whenever any existing component in the candidate chain under the library root is a symlink or reparse point, even if its target is missing (dangling link); `NotFoundException` (`404 Not Found`) applies only when no symlink/reparse component exists in the chain and the requested folder or file is absent.

- **Per-component link rejection precedes target existence (`spec.md` Scope 3 `:24`, US1 `:99,110`, Scope 5 `:40,42`, FR-003 `:192`, FR-007 `:196`; `CONCLUSIONS.md` Q4 `:100`, Q5 `:134-135,153`, Q11 `:336`)**: Frozen containment requires rejecting any symlink/reparse component throughout the candidate chain as a policy/safety denial (`LibraryFileAccessDeniedException` → `403`) and deliberately distinguishes safety denial (`403`) from missing regular content (`404`).
- **No target-existence dereference before link check (`src/LamuFlix.Infrastructure/FileSystem/LibraryFileReader.cs:46-55`; `src/LamuFlix.Infrastructure/FileSystem/FileSystemLibraryFolderDiscovery.cs:175-176`; `CONCLUSIONS.md` Q4 `:106,115`)**: Evaluating `Directory.Exists`/`File.Exists` before `HasReparsePointInChain` follows the link to probe whether its target exists, turning a broken link into `404` and creating a target-existence oracle across `403` vs `404`; link/reparse inspection on the entry itself (`LinkTarget` / `FileAttributes.ReparsePoint`) must run before target-existence checks.
- **Scope & Charter §2.3 (`specs/PRODUCT.md` §5; `CONCLUSIONS.md` Q7 `:207-210`, Q9 `:277-278`)**: Enforcing link-first ordering in `src/LamuFlix.Infrastructure/FileSystem/LibraryFileReader.cs` with real-disk unit coverage in `tests/LamuFlix.UnitTests/FileSystem/LibraryFileReaderTests.cs` stays strictly within the frozen Q7/FR-011 file footprint and changes no API shape, ticket deliverable, or constitutional principle.
