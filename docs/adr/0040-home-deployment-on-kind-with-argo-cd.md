---
status: Accepted
---

# Run the home deployment on a local kind cluster managed by Argo CD

The home deployment (DEV-496) runs LamuFlix on one machine. It replaces the Compose stack with a single-node kind cluster, where Argo CD reconciles Kubernetes manifests from Git and the images are the exact digests published from `main` by DEV-495 (ADR 0039).

## Decisions

1. **Kustomize, not Helm.** `deploy/k8s/base` holds plain manifests and `deploy/k8s/overlays/home` pins the images by digest. One overlay exists, so a chart's templating would only add indirection. Render with `kubectl kustomize --load-restrictor LoadRestrictionsNone`; the restrictor is needed because the ollama init script is read from `docker/`.
2. **Images pinned by digest in the overlay.** Base manifests carry bare image names. The overlay sets the API and Web digests from the publish run and the third-party digests from Compose. RabbitMQ is the exception: Compose has no digest, so the digest resolved on 2026-11-11 is pinned and recorded.
3. **Argo CD v3.6.1, pinned, trimmed.** The upstream `install.yaml` is applied at a fixed version. Dex, notifications and the ApplicationSet controller are removed because the home cluster has no SSO, no notifications and no ApplicationSets. The server runs with `server.insecure` on the loopback NodePort 3180.
4. **Git branch `env/home` is the desired state.** The `lamuflix-home` Application tracks `env/home` and path `manifests`. Automated sync with prune and self-heal is on. Nothing else writes that branch except `promote-home.yml`.
5. **Promotion is a deliberate act.** `promote-home.yml` runs on `workflow_dispatch` with an explicit sha, or on a `v*` tag. Its `dry_run` input defaults to true and only prints the rendered diff. A run with no changes is a no-op. This keeps the ADR 0039 trade-off visible: an image published from a `main` commit reaches the home cluster only when someone promotes it.
6. **Single API replica with the Recreate strategy.** The API hosts background services in-process and serves a SignalR hub without a backplane, so two replicas or an overlapping rolling update could run two copies of those services. Recreate costs downtime during a rollout and on a bad image. That cost is accepted for a single-user home instance.
7. **Stateful services never prune.** Postgres, RabbitMQ and ollama StatefulSets carry `Prune=false`. Removing a manifest must not delete a PersistentVolumeClaim and its data.
8. **Backups before any sync that could damage data.** A PreSync Job dumps Postgres to `/backups` with `pg_dump -Fc`, checks the dump with `pg_restore -l` and keeps the 10 newest. If Postgres is not reachable yet (first install), it logs and exits 0. A nightly CronJob at 04:00 keeps the 14 newest. Both write only files matching their own name pattern. The host folder `F:\Dev\lamuflix-home\backups` holds the operator's pre-migration dump and is never written by a cleanup.
9. **Host access is read-only where it can be.** The library (`E:\Filmes`) reaches the API pod through a read-only node mount and a read-only pod mount. The kind cluster is created with a loopback port map only, so nothing listens on a LAN interface.

## Consequences

- The cluster is rebuilt from Git plus the operator's secrets and a dump. `scripts/home/New-HomeCluster.ps1` creates it; `Restore-HomeCatalog.ps1` restores a named dump only.
- Secrets never enter Git. The API settings, Postgres, RabbitMQ and TMDb credentials are created in the cluster from a scratch folder outside the repository.
- kind uses API server port 3080 on loopback, which was free when checked. Changing it touches only `deploy/kind/cluster.template.yaml`.
- The parity check (`scripts/Test-DeployParity.ps1`, run in CI) fails when the Compose file and the rendered manifests disagree on digests, service names, ports, probe endpoints or required env names. Differences that are intended are listed in its fixtures, not allowed silently.
- Compose stays in the repository for development. Its `README.md` guidance is corrected in this change.
