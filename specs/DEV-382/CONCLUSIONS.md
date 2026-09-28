# DEV-382 conclusions

## Q1 — Which runner fixes the mutant-to-test linkage?

### Evidence before the question

The xunit v3 test project is an `Exe` run by VSTest out of process: `testhost.exe` launches `LamuFlix.Test.exe @@ <rsp>` as a child. Stryker's VSTest coverage collector sets `MutantControl.ActiveMutant` by reflection inside `testhost.exe`, not in the child process that runs the tests. The environment-variable fallback (`STRYKER_MUTANT_ID_CONTROL_VAR`) is fixed when `vstest.console` starts, so it cannot change per mutant. The child therefore sees `ActiveMutant = -1` and every mutant survives. Source: Stryker 4.16 `InjectedHelpers/MutantControl.cs` `IsActive`, `Stryker.DataCollector/CoverageCollector.cs` lines 158-175.

The MTP runner (`"test-runner": "mtp"`) uses a memory-mapped mutant file (`STRYKER_MUTANT_FILE`) that the test process reads per mutant. It needs no csproj change.

All experiments mutate `src/LamuFlix.Core` files `Domain/ReleaseYear.cs` and `Features/Enrichment/EnrichmentFailureClassifier.cs` (63 tested mutants), `LamuFlix.Test` only, concurrency 1, since disabled. Configs, logs, reports and process samples are in the session scratchpad (`dev382/`, `dev382-v5/`).

| Run | Stryker | Runner / coverage | Score | Statuses | Killer names in report |
|---|---|---|---|---|---|
| E1 | 4.16.0 | solution-wide VSTest, off | 100% | 63 killed, all by `ArchitectureTests.Core_features_must_depend_only_on_ports_domain_or_pipeline` | yes |
| E2 | 4.16.0 | VSTest, off | 0% | 63 survived | – |
| E3 | 4.16.0 | MTP, off | 96.83% | 60 killed, 2 survived, 1 timeout | no: ids only, `testFiles` empty |
| E4 | 5.0.0 | VSTest, off | 0% | 63 survived | – |
| E5 | 5.0.0 | MTP, off | 96.83% | 60 killed, 2 survived, 1 timeout | yes (143 named tests) |
| E6 | 5.0.0 | MTP, `perTest` | 100% (spurious) | 25 killed, 38 timeout | yes |

E6's timeouts are runner failures, not mutant behavior: the coverage capture phase took 28 minutes, then `AssemblyTestServer.RunTestsAsync` threw `TimeoutException` and the test server restarted 45 times. `ReleaseYear.cs:12` String mutation survived in E5 but is Timeout in E6. Stryker counts Timeout as detected, so the score is inflated to 100%. Stryker's default `coverage-analysis` is `perTest`, so an MTP config must set `off` explicitly.

Process evidence: in E4 each test runs in `LamuFlix.Test.exe` whose parent is `testhost.exe`; in E5/E6 the test process is `dotnet.exe <wt>\tests\LamuFlix.Test\bin\Debug\net10.0\LamuFlix.Test.dll --server --client-port ...` launched by `dotnet-stryker.exe`. In every run, including the failing E4, the test process loaded `tests\LamuFlix.Test\bin\Debug\net10.0\LamuFlix.Core.dll`, so a module path alone does not prove the mutated assembly executed.

Confound noted: test processes from the separate DEV-296 worktree ran during E5 and E6. This does not explain E6's pattern (runner-level server timeouts and restarts; a known survivor turned Timeout).

### First exchange

Question: which runner fixes the linkage? Options were 4.16 MTP (works, killer ids without names, "preview" warning), testing Stryker 5.0.0 first, or other approaches.

User's answer: **"Test 5.0.0"**. That produced E4-E6 above.

### Second exchange

Question (re-posed with E4-E6): Which runner fixes the linkage?

Recommendation: Stryker 5.0.0 with `"test-runner": "mtp"` and `"coverage-analysis": "off"` set explicitly, because it is the only configuration with real kills and named `killedBy`, which the DEV-382 receipts require. Cost: the tool pin in `.config/dotnet-tools.json` moves 4.16.0 → 5.0.0 for every mutation run in the repo (including DEV-296's Core gate once merged); 5.0.0 targets the .NET 10 runtime, which the repo already uses; coverage off runs every test per mutant (about 17 minutes for 63 mutants here). Alternative: stay on 4.16 MTP with no pin change and id-only killers.

User's answer: **"5"**.

### Decision

Use Stryker **5.0.0**, `"test-runner": "mtp"`, `"coverage-analysis": "off"` (explicit).

### Rationale

- VSTest cannot link mutants to tests for these xunit v3 `Exe` test projects in either 4.16 or 5.0.0 (E2, E4 both 0%).
- MTP kills real mutants with the same result on both versions (E3, E5 96.83%), and 5.0.0 reports the killing tests by name.
- `perTest` coverage under MTP on 5.0.0 produces spurious timeouts that inflate the score (E6), so it is excluded.

### Implications

- `.config/dotnet-tools.json`: `dotnet-stryker` 4.16.0 → 5.0.0.
- Tracked `stryker-config.json` (and the temp configs `scripts/run-mutation.ps1` derives from it) gain `test-runner: mtp` and `coverage-analysis: off`; thresholds stay 90/80/80.
- `scripts/run-mutation.ps1` comments and workarounds written for 4.16 (the `since` worktree workaround) need re-checking against 5.0.0.
- Runtime grows with coverage off; acceptable for Infrastructure (40 mutants) and Api (9) per DEV-296 D7d, slow for full Core.
- A receipt needs stronger assembly evidence than a module path (to be decided).

---

## Q2 — Where do the Infrastructure and Api receipts run?

### Evidence before the question

On main (`1d7efa1`), `src/LamuFlix.Infrastructure` has no `.cs` files and `src/LamuFlix.Api` has only `Program.cs`. `LamuFlix.Test.csproj` references neither project; only `LamuFlix.ArchitectureTests` does. The code DEV-296 D7d mutated (Infrastructure 40 mutants, Api 9) exists only on `feature/DEV-296`: `Infrastructure/Pipeline/{HandlerLog,LoggingDecorator,ServiceCollectionExtensions,TracingDecorator,ValidationDecorator}.cs` and `Api/ValidationExceptionHandler.cs`, plus the `LamuFlix.Test` project references and tests that reach them. `feature/DEV-296` is pushed at `eefbbd9` and contains main `1d7efa1`; the DEV-296 worktree is 3 commits ahead at `f66d8c2` with uncommitted files.

### Exchange

Question: Where should DEV-382's Infrastructure and Api receipts run?

Recommendation: keep DEV-382 on main as a tooling-only change (Stryker pin, config, `run-mutation.ps1`) and produce the receipts by running the fixed tooling against a pushed DEV-296 commit in a temporary detached checkout outside the repo, never touching the DEV-296 worktree. Cost: receipts are pinned to a commit not on main and go stale if DEV-296 changes; they use pushed `eefbbd9`, not local `f66d8c2`, unless DEV-296 pushes first. Alternatives: stack DEV-382 on `feature/DEV-296` (measures exactly what merges, but cannot merge before DEV-296), or wait for DEV-296 to merge (blocks DEV-382).

User's answer: **"Keep it on main"**.

### Decision

DEV-382 branches from main and changes tooling only. Receipts are produced against the pushed `feature/DEV-296` head (by SHA, currently `eefbbd9`) in a temporary detached checkout outside the repo, with DEV-382's tooling applied there. The DEV-296 worktree is not modified.

### Rationale

- The ticket keeps DEV-382 separate from DEV-296's Core gate; the linkage fix is tooling and does not need DEV-296's code to merge.
- Main has nothing in Infrastructure or Api to mutate, so the receipts must run where that code exists.

### Implications

- Every receipt records the DEV-296 SHA it measured and the DEV-382 tooling SHA applied.
- If DEV-296 changes Infrastructure, Api or their tests after the receipts, they are stale and must be re-run; the receipt procedure must be scripted so this is one command.
- A pushed commit is required; local-only DEV-296 commits are not measured.

---

## Q3 — How do mutation runs handle `LamuFlix.ArchitectureTests`?

### Evidence before the question

`scripts/run-mutation.ps1` runs `dotnet stryker -f <temp cfg> -p <proj>` from the repo root and sets no `test-projects`, so Stryker runs every test project that references the mutated project. On `feature/DEV-296` that includes `LamuFlix.ArchitectureTests` for both Infrastructure and Api (`tests/LamuFlix.ArchitectureTests/LamuFlix.ArchitectureTests.csproj` references Core, Infrastructure, Api, ServiceDefaults). Stryker injects helper types (namespace `Stryker`, `MutantControl`) into the mutated assembly, so a NetArchTest dependency rule fails for every mutant regardless of which mutant is active. E1 reproduced this: 63/63 killed, all by `LamuFlix.ArchitectureTests.ArchitectureTests.Core_features_must_depend_only_on_ports_domain_or_pipeline`. The script's only guard is the zero-kill check (`scripts/run-mutation.ps1` line 216, `tested > 0 and killed == 0`), which an all-architecture-test kill set passes.

### Exchange

Question: How should mutation runs handle `LamuFlix.ArchitectureTests`?

Recommendation: (1) exclude it from mutation runs: each generated config sets `test-projects` explicitly, derived at run time as every test project under `tests/` that references the mutated project, minus `*.ArchitectureTests`; (2) add a tripwire in report evaluation that fails the gate if any `killedBy` test name belongs to an ArchitectureTests project. Cost: the script owns the rule for which tests count instead of Stryker's discovery; a future structure-only test project would also need excluding. Alternatives: keep architecture tests and discount mutants killed only by them (runtime cost per mutant, more complex score math); or make arch rules ignore the `Stryker` namespace (changes test code for tooling; other rules such as sealed-type checks may still trip).

User's answer: **"Recomendation"** (accepts the recommendation).

### Decision

1. Mutation runs exclude `*.ArchitectureTests` projects. Each generated Stryker config sets `test-projects` explicitly to the test projects under `tests/` that reference the mutated project, minus `*.ArchitectureTests`.
2. Report evaluation fails the gate when any mutant's `killedBy` resolves to a test in an ArchitectureTests project.

### Rationale

- Architecture tests assert structure, not behavior; the only mutants they kill are artifacts of Stryker's injected types, which make every mutant "killed" (E1).
- Named `killedBy` (Q1, Stryker 5.0.0) makes the tripwire a direct check, catching a silent regression of the exclusion.

### Implications

- `run-mutation.ps1` gains test-project discovery (project references into the mutated project) and the ArchitectureTests exclusion; the discovered list is printed and recorded in receipts as part of the effective config.
- If discovery finds no eligible test project for a mutated project, the gate must fail rather than run Stryker's default discovery.
- Report evaluation resolves `killedBy` ids to names via `testFiles` and checks the tripwire; `-EvaluateReport` standalone applies it too.
- The E1 finding stands as evidence that DEV-296 D7d Core "2/2 Killed", D7f's Core-scope PASS basis, and DEV-294 D7a's "119 tested, 119 killed" are likely artifact kills; how to handle that is a separate question.

---

## Q4 — What counts as proof that the test process ran the mutated assembly?

### Evidence before the question

The ticket asks for "test-host assembly-loading evidence". In E4 (5.0.0 VSTest, 0% killed) the test process `LamuFlix.Test.exe` (parent `testhost.exe`) loaded `tests\LamuFlix.Test\bin\Debug\net10.0\LamuFlix.Core.dll`, the same path loaded in the working MTP runs E5/E6 (`dotnet.exe ...LamuFlix.Test.dll --server --client-port`, parent `dotnet-stryker.exe`). A loaded-module path therefore does not distinguish a working linkage from a broken one.

### Exchange

Question: What counts as proof that the test process ran the mutated assembly?

Recommendation: a separate receipt script (not the gate) records per run: (1) process sample — each test process's pid, parent, command line and loaded `LamuFlix.*` modules (the E4-E6 sampler); (2) assembly identity — for the mutated project's DLL at the loaded path, SHA256 during the run and whether its metadata defines types in the `Stryker` namespace, next to the SHA256 of a clean build of the same DLL; (3) behavioral proof — per-mutant `killedBy` test names. Cost: the sampler is Windows-only (`Get-Process .Modules`) and timing-based (300 ms poll), so very short-lived processes can be missed — acceptable for a diagnostic receipt, which is why it stays out of `run-mutation.ps1`; one more script to maintain. Alternatives: behavioral proof only (simpler, does not literally answer the ticket's assembly-loading request); or sampling built into `run-mutation.ps1` (evidence on every gate run, but Windows-only timing-based logic in the gate).

User's answer: **"Recomendation"** (accepts the recommendation).

### Decision

A dedicated receipt script, separate from `scripts/run-mutation.ps1`, captures for each run:

1. Process sample: pid, parent pid/name, command line, and loaded `LamuFlix.*` module paths of every test process.
2. Assembly identity: SHA256 of the mutated project's DLL at the loaded path during the run, whether its metadata defines types in the `Stryker` namespace, and the SHA256 of a clean build of the same DLL.
3. Behavioral proof: per-mutant status and `killedBy` test names from the report.

### Rationale

- Module path alone is identical in broken (E4) and working (E5) runs.
- Hash difference plus injected `Stryker` types proves the loaded file is the mutated build; named kills prove mutants executed in that process.
- Keeping timing-based, Windows-only sampling out of the gate keeps the gate deterministic.

### Implications

- The receipt script drives `run-mutation.ps1` (or the same generated config) so receipts measure the gate's actual configuration, and records its native exit.
- Metadata inspection uses `System.Reflection.Metadata` (no assembly load into the sampler process).
- A run where the sampler misses every test process records that explicitly; it is not reported as evidence.

---

## Q5 — Where do the receipts live, and in what form?

### Exchange

Question: Where do the receipts live and in what form?

Recommendation: commit a compact `specs/DEV-382/receipts/<project>/receipt.json` plus a human-readable `receipt.md` per mutated project (Infrastructure, Api). `receipt.json` holds both SHAs (measured DEV-296 head, DEV-382 tooling), the Stryker version, the effective config including the discovered test projects, the mutated file list, per-mutant rows (file:line, mutator, status, `killedBy` names), native exits, and the Q4 process/assembly evidence. Full `mutation-report.json`, HTML reports and logs stay outside the repo (StrykerOutput must not be committed); the full JSON report is attached to the DEV-382 YouTrack issue only after the owner confirms that outward action. Alternatives: commit the full reports (large, generated, violates the StrykerOutput rule); keep everything outside the repo (receipts not reviewable in the PR).

User's answer: **"Go with your recomendation. Don't ask me any more questions"** (accepts the recommendation and delegates every remaining alignment decision to the recommender).

### Decision

As recommended. Receipts are compact, committed, and derived by the receipt script; bulky output stays out of the repo.

---

## Delegated decisions (no further questions, per the Q5 answer)

Recorded as recommender rulings under the owner's delegation. Each is the recommendation that would have been asked.

### D1 — DEV-296 / DEV-294 implications of the E1 artifact

DEV-382 does not re-run, reclassify, or edit DEV-296 D7d/D7f or DEV-294 D7a. The finding (ArchitectureTests kill every mutant because Stryker injects `Stryker`-namespace types) is filed as a follow-up issue, with E1 as evidence. Basis: ticket scope is Infrastructure and Api linkage; "separate from DEV-296's Core gate".

### D2 — Closing bar for review loops

A finding blocks only when it is Critical or High **and** carries a concrete failure scenario (inputs/state → wrong gate verdict, wrong receipt, or crash). Everything else is logged, not fixed in-round.

### D3 — Frozen scope

The pin bump, the config and gate-script changes (MTP, coverage off, test-project discovery with the ArchitectureTests exclusion, the ArchitectureTests `killedBy` tripwire, fail-on-no-eligible-test-project), the receipt script, receipts for Infrastructure and Api against the pushed DEV-296 SHA, and updated comments/docs. Anything else is a follow-up issue, not a finding in this round.

### D4 — Round cap

Two review rounds; at most two fix commits per round.

### D5 — Human gate 1

The owner delegated all remaining decisions and asked not to be questioned. Gate 1 is therefore not held as a blocking question: the spec, plan and tasks are presented in the hand-off for the owner to veto, and implementation proceeds. Commit, push, and any YouTrack post remain owner actions, confirmed separately.

---

## Results — receipts at `f66d8c2` (2026-09-28)

Measured `origin/feature/DEV-296` at `f66d8c2` with `scripts/new-mutation-receipt.ps1`. The setup was Stryker 5.0.0 with the MTP runner and coverage off. `test-projects` was `tests/LamuFlix.Test/LamuFlix.Test.csproj` only. Stryker 5 honoured that list: it logged "Analyzing 1 test project(s)" and never built ArchitectureTests. The committed receipts are from run 2 (20:57Z). Run 1 (20:42Z) used the first sampler, which was replaced because it checked each process only once.

### Linkage: fixed

The following holds in both projects and both runs:
- Every Killed mutant's `killedBy` resolves to a `LamuFlix.Test` test.
- No killer is an ArchitectureTests test.
- No killer id is unresolved.

In run 2, the test servers loaded the mutated assembly:
- **Infrastructure:** 23 of 24 sampled loads of `LamuFlix.Infrastructure.dll` defined `Stryker` types, and their SHA256 differed from the clean rebuild at the same path.
- **Api:** 13 of 14 sampled loads of `LamuFlix.Api.dll` did the same.
- **The remaining load in each project** matched the clean rebuild.

The E2/E4 zero-kill result was a runner linkage failure, not a test-quality verdict (ADR-0016).

### Scores

| Project | Run | Killed | Survived | Timeout | Score | Gate |
|---|---|---|---|---|---|---|
| Infrastructure | 1 | 27 | 0 | 12 | 100% of 39 | PASS |
| Infrastructure | 2 | 19 | 1 | 19 | 97.44% of 39 | PASS |
| Api | 1 | 7 | 3 | 3 | 76.92% of 13 | FAIL (Stryker exit 2) |
| Api | 2 | 7 | 0 | 6 | 100% of 13 | PASS |

### Finding F1: Survived/Timeout is not stable between identical runs

Runs 1 and 2 used the same commit, tooling and machine. Even so, 13 Infrastructure mutants and 3 Api mutants changed status between them:
- **Api:** `Program.cs:9`, `:11` and `:12` statement mutations went from Survived to Timeout. That flipped the Api gate from FAIL to PASS.
- **Infrastructure:** changes went both ways:
  - 10 mutants went Killed → Timeout;
  - 2 went Timeout → Killed;
  - `LoggingDecorator.cs:17` went Timeout → Survived.

Stryker counts Timeout as detected, so these flips move the score.

Working hypothesis (not verified):
1. With coverage off, every mutant runs all 163 tests, so a mutant's run time sits close to Stryker's timeout.
2. Machine load then moves mutants across that line.
3. Run 2's sampler polls module lists every 500 ms, which adds load. Timeouts rose from 12 to 19 (Infrastructure) and from 3 to 6 (Api).

The 80 threshold and the gate are unchanged (SC-005). Per D3, this is a follow-up, not a DEV-382 fix. The follow-up would:
- measure repeat-run stability with a larger `additional-timeout`;
- consider whether per-test coverage can come back under MTP once the linkage is proven.

### Finding F2 (D1, for DEV-296): Api `Program.cs` mutants are not killed

Neither run killed any of the five `Program.cs` statement mutants. They either survived or timed out, so the Api pass in run 2 depends on timeouts. With them counted as Survived, Api would be below 80. This is a DEV-296 test finding, not a DEV-382 one. Two things stay open, and neither is filed here:
- the DEV-296 follow-up for this finding;
- the Core re-measurement.

### Artifacts outside the repo

Work root `<temp>/LamuFlix-receipt/20260928-205740` (run 2) and `<temp>/LamuFlix-receipt/20260928-204253` (run 1) contain:
- the gate logs;
- `*.processes.jsonl`;
- the full `mutation-report.json` per project.

Attaching them to DEV-382 is an owner action (Q5).
