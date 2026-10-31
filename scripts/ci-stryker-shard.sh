#!/usr/bin/env bash
# CI helper that splits the whole-Core Stryker run across parallel jobs and puts the
# reports back together, so the mutation gate fits a one hour budget per job.
#
#   ci-stryker-shard.sh run   <shard> <shard-count> <output-dir>
#   ci-stryker-shard.sh merge <merged-report.json> <shard-report.json>...
#
# Why this exists: one whole-Core run executes every mutant against the whole eligible unit
# suite (coverage-analysis=off is required by the MTP runner, ADR 0016), so wall time is
# mutants x suite time / concurrency and a single job ran past its 2 h limit.
#
# `run` mutates one slice of the Core source files and does NOT gate: it writes the report and
# sets thresholds.break to 0. The verdict still comes from one place, scripts/run-mutation.ps1
# -EvaluateReport on the merged report, so the score threshold, artifact-kill share and
# file-span rules apply to the whole of Core, exactly as they do for an unsharded run.
#
# `run` mirrors what run-mutation.ps1 -WholeProject generates (the tracked stryker-config.json
# with test-projects, mutate, concurrency and since replaced). Keep the two in step.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

harness_value() { # harness_value <key-regex> : first scalar under a key, comments stripped
  sed -nE "s/^[[:space:]]*$1:[[:space:]]*([^#[:space:]]+).*/\1/p" "$repo_root/harness.yml" | head -n1
}

# Test projects that reference $1 directly, minus *.ArchitectureTests and *.IntegrationTests
# (Get-EligibleTestProjects in run-mutation.ps1).
eligible_test_projects() {
  local target="$1" proj dir ref
  while IFS= read -r proj; do
    case "$(basename "$proj" .csproj)" in
      *ArchitectureTests | *IntegrationTests) continue ;;
    esac
    grep -q 'Microsoft\.NET\.Test\.Sdk' "$proj" || continue
    dir="$(dirname "$proj")"
    while IFS= read -r ref; do
      if [[ "$(realpath -m "$dir/${ref//\\//}")" == "$target" ]]; then
        echo "$proj"
        break
      fi
    done < <(grep -oE '<ProjectReference[^>]*Include="[^"]+"' "$proj" | sed -E 's/.*Include="([^"]+)"/\1/')
  done < <(find "$repo_root/tests" -name '*.csproj' -not -path '*/bin/*' -not -path '*/obj/*' | LC_ALL=C sort)
}

# Longest-processing-time assignment on line counts so shards carry similar mutant counts.
# Every shard computes the same assignment, so no shared state is needed.
shard_files() { # shard_files <project-dir> <shard> <count>
  local dir="$1" shard="$2" count="$3"
  (cd "$dir" && find . -name '*.cs' -not -path '*/bin/*' -not -path '*/obj/*' \
      -not -path '*/Persistence/Migrations/*' -print0 \
    | xargs -0 wc -l | grep -v ' total$' \
    | awk '{ n=$1; $1=""; sub(/^ /,""); sub(/^\.\//,""); print n "\t" $0 }' \
    | LC_ALL=C sort -t$'\t' -k1,1nr -k2,2 \
    | awk -F'\t' -v c="$count" -v s="$shard" '
        BEGIN { for (i = 1; i <= c; i++) load[i] = 0 }
        { m = 1; for (i = 2; i <= c; i++) if (load[i] < load[m]) m = i
          load[m] += $1; if (m == s) print $2 }' \
    | LC_ALL=C sort)
}

cmd_run() {
  local shard="$1" count="$2" out="$3"
  (( shard >= 1 && shard <= count )) || { echo "shard must be 1..$count" >&2; exit 1; }

  local core_name project_dir project_file
  core_name="$(harness_value wholeProject)"
  [[ -n "$core_name" ]] || { echo "harness.yml gates.mutation.wholeProject is not set" >&2; exit 1; }
  project_file="$(find "$repo_root/src" -name "$core_name.csproj" | head -n1)"
  [[ -n "$project_file" ]] || { echo "$core_name.csproj not found under src/" >&2; exit 1; }
  project_dir="$(dirname "$project_file")"

  mapfile -t tests < <(eligible_test_projects "$(realpath -m "$project_file")")
  (( ${#tests[@]} > 0 )) || { echo "no eligible test project references $core_name" >&2; exit 1; }

  mapfile -t files < <(shard_files "$project_dir" "$shard" "$count")
  (( ${#files[@]} > 0 )) || { echo "shard $shard/$count has no files" >&2; exit 1; }

  local concurrency
  concurrency="$(harness_value concurrency)"
  : "${concurrency:=$(( $(nproc) / 2 ))}"
  (( concurrency >= 1 )) || concurrency=1

  mkdir -p "$out"
  local cfg="$out/stryker-config.shard.json"
  jq -n \
    --slurpfile base "$repo_root/stryker-config.json" \
    --argjson concurrency "$concurrency" \
    --argjson tests "$(printf '%s\n' "${tests[@]}" | jq -R . | jq -s .)" \
    --argjson mutate "$(printf '%s\n' "${files[@]}" | jq -R . | jq -s .)" '
    { "stryker-config": ($base[0]["stryker-config"] + {
        "concurrency": $concurrency,
        "test-projects": $tests,
        "mutate": $mutate,
        "since": { "enabled": false },
        "thresholds": ($base[0]["stryker-config"].thresholds + { "break": 0 })
    }) }' > "$cfg"

  echo "Shard $shard/$count: ${#files[@]} Core file(s), concurrency $concurrency, tests: ${tests[*]}"
  cd "$project_dir"
  dotnet stryker -f "$cfg" -p "$(basename "$project_file")" -O "$out"
}

# Merge shard reports into one schema-2 report. Test ids are only unique within a run, so each
# killedBy/coveredBy id is rewritten to "<test file>|<test name>" before the shards are unioned.
# One test then stays one test across shards, which keeps the kill-share and file-span rules
# whole-run rules instead of per-shard ones.
cmd_merge() {
  local merged="$1"; shift
  (( $# > 0 )) || { echo "no shard reports given" >&2; exit 1; }
  jq -s '
    def canon($f; $t): $f + "|" + $t.name;
    def remap($m): map(. as $id | $m[($id | tostring)] // ($id | tostring));
    def parts:
      map(
        . as $r
        | ([ ($r.testFiles // {}) | to_entries[] | .key as $f | .value.tests[]?
             | { key: (.id | tostring), value: canon($f; .) } ] | from_entries) as $m
        | {
            meta: ({ schemaVersion: $r.schemaVersion, thresholds: $r.thresholds, projectRoot: $r.projectRoot }
                   | with_entries(select(.value != null))),
            files: (($r.files // {}) | map_values(.mutants |= map(
                (if has("killedBy")  then .killedBy  |= remap($m) else . end)
              | (if has("coveredBy") then .coveredBy |= remap($m) else . end)))),
            testFiles: (($r.testFiles // {}) | with_entries(
                .key as $f | .value.tests |= map(.id = canon($f; .))))
          });
    parts as $p
    | ($p | map(.files | keys[])) as $names
    | if ($names | length) != ($names | unique | length)
      then error("a source file appears in more than one shard report")
      else
        $p[0].meta + {
          files: ($p | map(.files) | add),
          testFiles: (reduce ($p[].testFiles | to_entries[]) as $e ({};
              .[$e.key] = ((.[$e.key] // $e.value)
                | .tests = (((.tests // []) + ($e.value.tests // [])) | unique_by(.id)))))
        }
      end
    ' "$@" > "$merged"
  echo "Merged $# shard report(s) into $merged"
}

case "${1:-}" in
  run)   shift; (( $# == 3 )) || { echo "usage: $0 run <shard> <count> <output-dir>" >&2; exit 1; }; cmd_run "$@" ;;
  merge) shift; (( $# >= 2 )) || { echo "usage: $0 merge <merged.json> <shard-report.json>..." >&2; exit 1; }; cmd_merge "$@" ;;
  *)     echo "usage: $0 {run|merge} ..." >&2; exit 1 ;;
esac
