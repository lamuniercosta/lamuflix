---
status: Accepted
---

# Publish images from main after the gate jobs, without waiting for stryker-core

The home deployment (DEV-496) promotes an exact published image by digest, so every green commit on `main` needs its API and Web images in GHCR before anyone decides to promote it (DEV-495, PR A).

The `publish-images` job in `.github/workflows/ci.yml` runs only on `push` to `main` and needs `build`, `web`, `e2e`, `container-build` and `property-tests`. It pushes `ghcr.io/lamuniercosta/lamuflix-api` and `ghcr.io/lamuniercosta/lamuflix-web`, each tagged `sha-<full commit sha>` and `main`, for `linux/amd64`, with OCI labels from `docker/metadata-action` and a per-image GitHub Actions build cache. The job-level `packages: write` permission is the only write scope; the workflow default stays `contents: read`.

It does not wait for `stryker-core` or `stryker-core-gate`. Whole-Core mutation runs 16 shards of up to 150 minutes each (ADR 0016), so waiting for it would delay every publish by hours. It is also not a release gate for the home deployment, which promotes only by a deliberate manual act (DEV-496 keeps promotion a deliberate operator action).

Trade-off: a `main` commit whose whole-Core mutation gate fails after merge can still have published images. The failure stays visible in the `stryker-core-gate` job. Promotion is manual, so such an image reaches the home cluster only if the operator promotes it.

Publishing only from `main` keeps fork pull requests and unreviewed branches away from package write access. Publishing on tags only was rejected, since it would need a manual tag for every change. The `main` tag moves with every publish; the `sha-` tag and the digest are the immutable references that promotion uses.
