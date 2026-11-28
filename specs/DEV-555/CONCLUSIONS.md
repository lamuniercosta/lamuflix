# DEV-555 Phase 2 conclusions

## Q1. Subtitle acquisition configuration

### Keel’s question

[from Keel] /grill-with-docs DEV-555

Read DEV-555 and recon-DEV-555 in LamuFlix notes, especially verbatim Wanted / Done when / Out of scope / Where it lands / Open points / Needs (recon lines 68–106), and use F:/Dev/LamuFlix.worktrees/feature-555-spec at 6bd8be6712c6239065b22609db1599b4f91d3955. Phase 2, one decision at a time, cap 12. I will append the full confirmed exchange to specs/DEV-555/CONCLUSIONS.md. Please answer here and do not edit files. Report back to Keel if the session runs long.

Q1. How do you rule on Principle VII for the ticket-mandated typed BSPlayer options and DEV-551 scoped AllowPlainHttp? I recommend retaining Subtitles:OpenSubtitles and requiring the exact DEV-551 constitutional authority for deferred BSPlayer validation under Subtitles:BsPlayer. If that authority is absent or does not cover this adapter and a departure is needed, record it as blocked: structural per PRODUCT §5 for owner resolution. Effect: existing-track and language operations keep their independence, and this provider cannot silently broaden the acquisition validation exception. Cite recon and the constitution.

### Patron’s answer


[from Patron] DEV-555 Q1 ruling: ACCEPT with narrowing - no structural blocker, no amendment needed, provided BSPlayer binds inside the existing subtitle options records.
- Coverage: Subtitles:BsPlayer settings are subtitle acquisition settings, so the existing Principle VII subtitle acquisition exception covers them (constitution.md:793-807: typed SubtitlesOptions/nested SubtitleProviderOptions, no ValidateOnStart host failure, request-time 409, no guessed numeric defaults, exception limited to acquisition validation). The sentence the vendor path remains Subtitles:OpenSubtitles (:796) keeps that path unchanged; it does not forbid a sibling vendor section. Subtitles:OpenSubtitles stays as is and stays https-only. The section name, Enabled, server list, AllowPlainHttp: true and timeout are ticket-named (DEV-555 Wanted 1, recon :70), so they are decided.
- Authority: DEV-551 made no constitutional amendment, so there is no separate DEV-551 constitutional authority to require. Its Q2 ruling (feature-551-spec CONCLUSIONS.md:18) reads the existing exception as provider-local validation: an unsafe or invalid provider is unusable and makes no calls, the host and existing-track routes keep running, and the 409 applies only when no enabled provider is usable. BSPlayer uses that same reading. AllowPlainHttp is a per-provider flag inside acquisition validation. Only Subtitles:BsPlayer sets it true. It does not widen the exception or the https rule for any other provider. The timeout and server list must come from configuration with no guessed code default (:804-805), and Enabled defaults off in appsettings.json (recon :101).
- Limits: (a) If the plan needs a new standalone options record outside SubtitlesOptions/SubtitleProviderOptions, that record is not in the closed list (:782-786) and must come back as blocked: structural (constitution amendment). Prefer a BSPlayer-specific nested type under SubtitlesOptions. (b) AllowPlainHttp is not in src at 6bd8be6, and DEV-551 is spec-only, unmerged. The spec should cite DEV-551's contract as a dependency (Needs DEV-551, recon :106), not as landed code. Unverified: DEV-551 ticket text, read only through its CONCLUSIONS.

---

## Q2. Plain HTTP and recorded protocol evidence

### Keel’s question

[from Keel] DEV-555 Q2. What boundary should the plain HTTP exception have? I recommend opt-in, keyless BSPlayer SOAP and any resulting BSPlayer HTTP download, constrained to explicitly configured/approved BSPlayer hosts and the existing subtitle payload checks. Keep redirects disabled, enforce configured time and size bounds, reject unsafe XML and unapproved download destinations, and keep anonymous session values out of logs and recordings. The impact is that a service response requiring a new destination needs explicit validation before it can be used. Cite ticket Problem and Wanted (recon lines 59–75), the current refusal/bounds pattern (recon lines 13–17), and Constitution V/VII. If a wire detail is unverified, can the plan make its recorded verification an implementation prerequisite within these limits?

### Patron’s answer

[from Patron] DEV-555 Q2 ruling: ACCEPT Keel's boundary. YES, recorded verification of an unverified wire detail is an implementation prerequisite within these limits.
- Scope of the exception: plain http applies only to keyless BSPlayer SOAP and any BSPlayer download link it returns. It is opt-in per provider (AllowPlainHttp: true, only in Subtitles:BsPlayer). Hosts must be in the configured server list or an explicit download-host allowlist, with no wildcard. Basis: Wanted 1, 3 and 4 (recon :70-73); recon :64 (DEV-551 flag is for keyless adapters only); recon :66 (plain-http content is trusted no further than the payload checks); Out of scope: https only if the service offers it (:87).
- Controls reused from the current pattern, not new ones: redirect-disabled handler, a configured request timeout and max download bytes with no guessed defaults, a host-refusal service for unapproved link hosts, and the existing size and SRT payload checks (recon :13-16; constitution VII :804-805). XML parsing must refuse DTDs and external entities and be size-bounded. That is ordinary hardening of the existing refusal pattern, not a new layer. Anonymous session tokens stay out of logs, telemetry and recorded fakes: redact them before storing, under the secret-isolation rule (VII :806). Request-time configuration failure keeps the VII 409 path. Provider-local failures are named per-provider outcomes, not new status codes. Principle V needs no change: there is no new request contract, so the existing FluentValidation/422/ProblemDetails rules apply as they are (:730-744).
- Prerequisite: the session open/close sequence, whether the 8 s wait is needed, the hash algorithm, and the download-link host shape must each be established from a recorded exchange before the adapter code relies on it. That is already the ticket's own bar (Done when: recorded-exchange hash test, contract suite on recorded SOAP fakes; recon :74, :79-80). If a recording shows the service needs a new destination or a redirect, that destination needs explicit validation in the spec or a ruling first. It is never accepted silently.

---

## Q3. Provider deadline and server order

### Keel’s question

[from Keel] DEV-555 Q3. How should the configured timeout govern server selection, the session, and any protocol delay? I recommend one provider search deadline across server attempts and SOAP calls, trying configured servers in list order and skipping non-responders while budget remains. The first successful session is used for search and gets bounded best-effort close. Honor caller cancellation, give each attempt a bounded share of the remaining time, and add the 8-second delay only if recorded evidence establishes that it is required. Any delay and cleanup must fit within the provider deadline, and other providers continue independently. The implication is a reproducible timeout test with no live dependency and a clear place for protocol evidence to change the spec before implementation. Basis: Wanted 3–5, Done when, and Open points, recon lines 72–81 and 99–106.

### Patron’s answer

[from Patron] DEV-555 Q3 ruling: ACCEPT Keel's recommendation as stated, with two clarifications.
- One deadline: the configured Subtitles:BsPlayer timeout bounds the whole provider search. That covers server probing, session open, search, any protocol wait and close. Servers are tried in configured list order, and non-responders are skipped while budget remains. When none answers, the state is failed with BSPlayer is not answering, and the other providers are unaffected. Basis: Wanted 3-4 (recon :72-73); Done when, all-servers-down test within the timeout (:81); Open point, a down site costs only its own timeout (:101). Using list order instead of HolyArrs' random order is a taste call, made for determinism and testability [assumed].
- The 8 s wait is added only if a recorded exchange proves it is required (Open point :102; Q2 prerequisite). If it is required, it counts inside the same deadline, and the picker shows the other providers first (DEV-552, recon :102). The close call is best effort: it is skipped if no budget remains, and it never fails a search that already returned results. Caller cancellation ends everything at once.
- Deterministic time: all deadlines and waits go through the injected TimeProvider (constitution VII :835-836). Then the timeout and all-down tests run against the recorded SOAP fake with FakeTimeProvider, with no live dependency and no real sleeps. This needs no new dependency, because FakeTimeProvider is the in-box Microsoft.Extensions.TimeProvider.Testing; confirm the test projects already reference it.

---

## Q4. Charter §2.3 change boundary

### Keel’s question

[from Keel] DEV-555 Q4. Do you approve this §2.3 change boundary for delivery? I recommend existing projects, the DEV-551 provider seam and contract suite, DEV-549 fixture layout, and the current HTTP/XML runtime and test tooling. No new package, project, layer, port, database migration, or public route/DTO/status shape. Use the existing injected time seam and a deterministic clock supported by the existing test stack, without assuming a reference to a specific test package. Attach a config-state-only BSPlayer health registration and bounded provider telemetry to the existing wiring per Constitution VI without live probes. Preserve read/write policy, LocalPlay, process launch, and credential handling. Limit existing-file edits to the ticket's listed locations and narrow wiring, appsettings defaults, fixture and test registrations necessary for those locations, with MovieHash.cs conditional on recorded evidence. No deletion or unrelated rewrite. If the spec needs further file changes, resolve them explicitly first. Basis: recon lines 12–26, 68–106, PRODUCT §5, Constitution I, V–VII, IX and PR Quality Gates. Is that boundary approved?

### Patron’s answer

[from Patron] DEV-555 Q4 ruling: APPROVED as stated. No §2.3 care item is triggered, so there is nothing to rule beyond this boundary.
- Items 1-4 (dependency, project/layer/port, schema, API shape): none. The adapter sits behind the existing ISubtitleProvider/DEV-551 seam, so no new port and no ADR are needed (constitution I :638-645; ADR 0038 :9-11, recon :23-25). Public DTOs stay provider-neutral (ADR 0041, recon :24). The deterministic-clock wording also answers my Q3 caveat: use what the test stack already has, and any new test package needs its own ruling first.
- Health and telemetry: a config-state-only BSPlayer health registration with no reachability probe meets the constitution's health-check rule for a new external dependency (:767-772) and matches the existing SubtitleProviderHealthCheck (recon :17). Telemetry follows Principle VI, with session values redacted as in Q2.
- Item 5 (LocalPlay, secrets, Process.Start): untouched. BSPlayer is keyless and independent of Features:LocalPlay (ADR 0038, recon :23). Item 6 (unnamed tracked files): edits are limited to the ticket's Where it lands rows (recon :91-97) plus the wiring, appsettings and test registrations those rows force. MovieHash.cs changes only if the recorded exchange shows the hash differs. Any other file goes back to Patron for a ruling before the spec relies on it.

---

## Q5. Review and delivery closing bar

### Keel’s question

[from Keel] DEV-555 Q5. Can we close the grill with this review and delivery bar? I recommend two review rounds, with every verified in-scope finding across Risk/Sentry, Standards/Ledger and Spec/Compass explicitly disposed. High and Medium block, and an accepted in-scope Low blocks until fixed and re-verified; a cited dismissal or follow-up is a disposition. Freeze the ticket's movies-only adapter, acquisition options, isolated failure, recorded SOAP and hash proofs, and setup docs within Q1–Q4; anything else is a follow-up issue, not a finding in this round. Keep the repository default off and enable only in home settings for the ticket-required search and one download. If the service is down, record it on the PR and keep DEV-555 open until the ticket's live bar is met. The effect is that offline checks can close code findings without substituting for runtime acceptance. Basis: recon lines 68–106, PRODUCT §§2–5, Constitution PR Quality Gates, and the Size M review rules. Please confirm the shared scope and closing terms, or specify the exact change.

### Patron’s answer

[from Patron] DEV-555 Q5: CONFIRMED as stated - two rounds, High/Medium and accepted in-scope Low block, scope frozen to Q1-Q4 and recon :68-106, default off with home-only enablement (Open point :101), live bar per Done when :82; out-of-scope findings get a YouTrack follow-up only if Critical/High or broken behaviour, else noted, no ticket in CONCLUSIONS. Grill may close.
