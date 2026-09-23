# DEV-361 assumptions

- [assumed] For a proven external MVC binder false positive in `FilmesFilterViewModel`, the local suppression may use a `// ReSharper disable <InspectionId>` / `// ReSharper restore <InspectionId>` bracket with a one-line reason such as "Written by the ASP.NET Core MVC model binder". The expected writer is the controller's `[FromQuery]` parameter (`FilmesController.cs:25`). No JSON deserializer is established for these three hits. The ticket fixes the per-type/member scope and the reason requirement. This entry records only the exact wording and syntax (`task-DEV-361:16-18,28`; Patron Q4).
- [assumed] Replacement wording for the stale rationale comments at `.editorconfig:67,69,72,74` (T013 Sentry F3; B1 answer). Each comment says DEV-361 restored the key to `warning` and names the DEV-361 fix or member-scoped suppression, not a follow-up. For `:67`, it says the owner reversed B1 and that legacy `null!` sites are suppressed per type or member. The exact text is the implementer's.
- [assumed] B1 suppression reasons use this one-line English wording per writer:
  - "EF Core initializes DbSet properties";
  - "Materialized by EF Core";
  - "Deserialized by Newtonsoft.Json";
  - "Written by the ASP.NET Core MVC model binder";
  - "Set by the Razor tag-helper activator";
  - "Assigned by <Type.Member> at <file:line>";
  - "Deliberate null to assert ArgumentNullException";
  - "Reflection lookup of a known member".

  Exact phrasing is taste. The scope, and the requirement to give a reason, are FR-008.

No structural decision is assumed. The B1 reversal is the owner's answer [x] on PR #3 (`CONCLUSIONS.md` B1).
