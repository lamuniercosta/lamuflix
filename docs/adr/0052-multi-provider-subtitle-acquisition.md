---
status: proposed
---

# Multi-provider subtitle acquisition

DEV-551 amends [ADR 0041](0041-explicit-subtitle-acquisition.md) to support simultaneous search across named subtitle providers and to expose provider identity with each Subtitle Candidate. The owner's 2026-11-18 ruling recorded in DEV-551 replaces DEV-501 ruling 6. The current movie search route accepts one language and concurrently asks every enabled, usable provider, then returns ranked candidates and independent outcomes after the bounded searches settle. OpenSubtitles is the sole production adapter delivered by DEV-551. Each adapter implements the existing port and runs the shared contract suite through its typed HTTP client.

A Candidate ID binds one provider's opaque file reference to the movie and canonical language. A download always originates from the viewer's selection and goes only to the bound provider. DEV-552 owns the picker that lets the viewer select and download as many candidates as desired. Quotas, deadlines, call spacing and trusted search and download destinations belong to each provider. Credentials are confined to that provider's HTTPS search origin. The host and existing-track serving remain available when subtitle acquisition is misconfigured.

The amendment replaces ADR 0041's single-provider and provider-neutral response decisions. ADR 0041 continues to govern existing-track independence, read-only precedence, opaque candidate selection, local file validation and safe publication. One aggregate call preserves the current request contract and gives the caller a complete ordered result. Its latency is bounded by the slowest provider deadline. The DTO additions travel through OpenAPI to generated TypeScript. DEV-551 requires this amendment to become Accepted before any deliverable merges.

Principle VII evaluates the acquisition configuration needed for each request. Search returns 409 when no enabled provider is usable, and a usable sibling lets search proceed with provider-local outcomes. Download validates the bound provider after read-only library policy. The subtitle acquisition startup-validation exception applies to every adapter.

Each adapter's trusted download destinations come from adapter declarations or explicit typed configuration, independently of provider responses. The initial destination and each redirect hop are checked before I/O. Plain HTTP requires a credential-free adapter declaration and explicit operator opt-in. Search and health share process-local provider observation state on the Core side of the existing seam, with typed-client composition outside Core. Call spacing also shares provider-local state across concurrent requests.

Basis: DEV-551 Goal, Wanted 1–10 and Done when, Constitution I, V and VII, and [Patron's DEV-551 rulings](../../specs/DEV-551/CONCLUSIONS.md).
