---
status: Accepted
---

# Deploy the home cluster from a release tag gated on the mutation job

ADR 0039 publishes images from every `main` push that runs CI and deliberately does not wait for `stryker-core-gate`, the whole-Core mutation job, so a `main` commit can have images before its mutation result is known. ADR 0040 point 5 let `promote-home.yml` run on a `v*` tag, but the tag run only checked that images existed. This ADR makes the tag a release and says what it must prove.

## Decisions

1. **A `v*` tag is the release.** The tag must look like `vX.Y.Z`. Pushing it runs `promote-home.yml`, which deploys the commit and records the tag.
2. **The tag deploys only if `stryker-core-gate` passed for the promoted commit.** The run finds the newest `ci.yml` run on `main` for that sha and reads the job's conclusion. A missing run, a running job, a skipped job or a failure stops the run before `env/home` changes. The check judges the commit whose images are promoted, which can be an earlier commit than the tagged one (see below).
3. **Promotion still walks back to the newest commit with published images.** CI runs on the tip of each push and skips docs-only pushes, so the tagged commit may have no images. The run promotes the newest commit at or before the tag that has both images, and the gate is judged on that commit. A tag on a spec-only commit therefore releases the last code commit.
4. **The tag is written to `RELEASE.json`** as `release`, next to the commit and image digests, and appears in the `env/home` commit message. Manual runs write `release: null`.
5. **A GitHub Release is created with generated notes** after `env/home` is pushed. It lists the commit and both image digests. If the release already exists the step leaves it alone, so re-running a tag run to roll back does not rewrite history.
6. **`workflow_dispatch` with a sha stays for hotfixes.** It skips the gate and keeps `dry_run`, and records no release. Using it is a deliberate decision to deploy before the mutation job has judged the commit.
7. **Rollback is a re-run.** Re-running the promote-home run of an earlier tag deploys that tag's commit again. It uses the sha stored in the original run, so the result does not depend on where `main` has moved since.

## Consequences

- ADR 0040 point 5 changes: a tag deploys only after the mutation gate, instead of after the image check alone.
- A tag run has no dry run. A tag push deploys at once, so the gate is the only safety on that path. Check the preview with a `workflow_dispatch` dry run first when the change is risky.
- A tag run uses the workflow file at the tagged commit, so this change must be on `main` before the first release tag is pushed.
- The run needs `actions: read` to read the CI jobs and `contents: write` to create the release.
- Pushing a tag before `stryker-core-gate` finishes fails the run. Wait for the job, then re-run the failed run; no new tag is needed.
- The release tag shows in `RELEASE.json`, in the `env/home` history and in the GitHub Releases list. The cluster's deployed version is answered by the tag, not only by a sha.
