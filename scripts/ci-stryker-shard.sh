#!/usr/bin/env bash
# CI helper that splits the whole-Core Stryker run across parallel jobs and puts the
# reports back together, so the mutation gate fits a two hour budget per job.
#
#   ci-stryker-shard.sh run   <shard> <shard-count> <output-dir>
#   ci-stryker-shard.sh merge <merged-report.json> <shard-report.json>...
#
# Why this exists: one whole-Core run executes every mutant against the whole eligible unit
# suite (coverage-analysis=off is required by the MTP runner, ADR 0016), so wall time is
# mutants x suite time / concurrency and a single job ran past its 2 h limit.
#
# `run` mutates one slice of Core (whole files, plus spans of the largest files) and does NOT
# gate: it writes the report and sets thresholds.break to 0. The verdict still comes from one
# place, scripts/run-mutation.ps1 -EvaluateReport on the merged report, so the score threshold,
# artifact-kill share and file-span rules apply to the whole of Core, exactly as they do for an
# unsharded run.
#
# `run` mirrors what run-mutation.ps1 -WholeProject generates (the tracked stryker-config.json
# with test-projects, mutate, concurrency and since replaced; mutate here may hold spans). Keep
# the two in step.
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
#
# A file longer than half a shard's share is cut into pieces at member boundaries (a blank line
# followed by a line at the file's member indent), because one large file would otherwise pin
# its whole mutant count on one shard (MovieQueryString.cs alone is about 190 mutants). A piece
# is a Stryker mutate span, path{start..end}, in UTF-16 offsets of the file as Roslyn reads it
# (BOM dropped, line endings kept). Stryker tests a mutant only when a span fully contains it and
# marks the rest Ignored "Removed by mutate filter"; cmd_merge fails if a mutant is tested by
# no shard or by two, so a bad cut cannot silently shrink the gate.
shard_files() { # shard_files <project-dir> <shard> <count>
  python3 - "$1" "$2" "$3" <<'PY'
import math, os, re, sys

root, shard, count = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])

def sources():
    for d, subdirs, names in os.walk(root):
        subdirs[:] = [s for s in subdirs if s not in ("bin", "obj")]
        rel = os.path.relpath(d, root).replace(os.sep, "/")
        if rel == "Persistence/Migrations" or rel.startswith("Persistence/Migrations/"):
            continue
        for n in names:
            if n.endswith(".cs"):
                yield (n if rel == "." else f"{rel}/{n}")

def cut_lines(lines, pieces):
    """Line indexes where a member starts after a blank line, nearest to even split points."""
    indent = "    " if any(re.match(r"\s*namespace\s+[\w.]+\s*;", l) for l in lines) else "        "
    starts = [i for i in range(1, len(lines))
              if not lines[i - 1].strip() and lines[i].startswith(indent)
              and not lines[i][len(indent)].isspace() and lines[i].strip() not in ("}", "};")]
    cuts = []
    for j in range(1, pieces):
        ideal = j * len(lines) / pieces
        best = min((s for s in starts if not cuts or s > cuts[-1]),
                   key=lambda s: abs(s - ideal), default=None)
        if best is not None and best not in cuts:
            cuts.append(best)
    return cuts

files = []
for path in sorted(sources()):
    with open(os.path.join(root, path), encoding="utf-8-sig", newline="") as f:
        lines = f.read().splitlines(keepends=True)
    files.append((path, lines))

total = sum(len(lines) for _, lines in files)
piece_limit = max(1, total // count // 2)
items = []  # (line count, path, span start, span end or None for the whole file)
for path, lines in files:
    cuts = cut_lines(lines, math.ceil(len(lines) / piece_limit)) if len(lines) > piece_limit else []
    if not cuts:
        items.append((len(lines), path, 0, None))
        continue
    offsets = [0]
    for l in lines:
        offsets.append(offsets[-1] + len(l.encode("utf-16-le")) // 2)
    bounds = [0] + cuts + [len(lines)]
    for a, b in zip(bounds, bounds[1:]):
        items.append((b - a, path, offsets[a], offsets[b]))

items.sort(key=lambda t: (-t[0], t[1], t[2]))
load = [0] * count
mine = {}
for n, path, start, end in items:
    m = min(range(count), key=lambda i: load[i])
    load[m] += n
    if m == shard - 1:
        mine.setdefault(path, []).append((start, end))

for path in sorted(mine):
    spans = sorted(mine[path])
    if spans[0][1] is None:
        print(path)
    else:
        print(path + "".join(f"{{{a}..{b}}}" for a, b in spans))
PY
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
  # Restore first, one project at a time. On a fresh checkout Stryker's analysis restores the test
  # projects in parallel, and a lost race drops a project with only a warning: one CI shard ran
  # 68 Evaluation tests instead of 2105 and reported 91 survivors.
  local test
  for test in "${tests[@]}"; do dotnet restore "$test" --nologo -v q; done
  cd "$project_dir"
  dotnet stryker -f "$cfg" -p "$(basename "$project_file")" -O "$out" | tee "$out/stryker.log"

  if grep -E 'Analysis of project .* failed' "$out/stryker.log"; then
    echo "Stryker dropped a test project; the shard report would undercount kills" >&2
    exit 1
  fi
  # Stamp the whole-Core mutant count (the same in every shard) so the merge can prove that the
  # shards together listed every mutant, a file no shard mutated included.
  local created report="$out/reports/mutation-report.json"
  created="$(sed -nE 's/.*INF\] ([0-9]+) mutants created.*/\1/p' "$out/stryker.log" | head -n1)"
  [[ -n "$created" ]] || { echo "no 'mutants created' line in the Stryker output" >&2; exit 1; }
  jq --argjson created "$created" '. + { lamuflixMutantsCreated: $created }' "$report" > "$report.tmp"
  mv "$report.tmp" "$report"
}

# Merge shard reports into one schema-2 report. Test ids are only unique within a run, so each
# killedBy/coveredBy id is rewritten to "<test file>|<test name>" before the shards are unioned.
# One test then stays one test across shards, which keeps the kill-share and file-span rules
# whole-run rules instead of per-shard ones.
#
# Every report lists every Core file, but a file outside the shard's mutate list has no mutants
# there. A file cut into spans has mutants in several reports; each lists every mutant of the
# file, and the ones outside that shard's spans are Ignored "Removed by mutate filter". The merge
# keeps the other copies and fails unless every mutant (location, mutator, replacement) is kept
# exactly as many times as one listing report lists it: once per mutant, never zero (lost at a
# cut) or twice. CompileError mutants are the exception: Stryker lists them unfiltered in every
# report of the file whatever the spans (they are never tested), so they are taken from the first
# listing report only. Finally the files' mutant counts must add up to the mutants Stryker created
# (stamped by cmd_run, equal in every shard), so a file no shard mutated cannot drop out unseen.
# Mutant ids are prefixed with the shard's position so kept copies cannot collide.
cmd_merge() {
  local merged="$1"; shift
  (( $# > 0 )) || { echo "no shard reports given" >&2; exit 1; }
  jq -s '
    def canon($f; $t): $f + "|" + $t.name;
    def remap($m): map(. as $id | $m[($id | tostring)] // ($id | tostring));
    def mkey: [.location.start.line, .location.start.column, .location.end.line,
               .location.end.column, .mutatorName, (.replacement // "")] | tostring;
    def filtered: .status == "Ignored" and .statusReason == "Removed by mutate filter";
    def counts: group_by(mkey) | map({ key: (.[0] | mkey), value: length }) | from_entries;
    def parts:
      to_entries | map(
        (.key + 1) as $shard
        | .value as $r
        | ([ ($r.testFiles // {}) | to_entries[] | .key as $f | .value.tests[]?
             | { key: (.id | tostring), value: canon($f; .) } ] | from_entries) as $m
        | {
            created: $r.lamuflixMutantsCreated,
            meta: ({ schemaVersion: $r.schemaVersion, thresholds: $r.thresholds, projectRoot: $r.projectRoot }
                   | with_entries(select(.value != null))),
            files: (($r.files // {}) | map_values(.mutants |= map(
                .id = "\($shard)-\(.id)"
              | (if has("killedBy")  then .killedBy  |= remap($m) else . end)
              | (if has("coveredBy") then .coveredBy |= remap($m) else . end)))),
            testFiles: (($r.testFiles // {}) | with_entries(
                .key as $f | .value.tests |= map(.id = canon($f; .))))
          });
    def compileError: .status == "CompileError";
    parts as $p
    | [ $p[].files | to_entries[] ] | group_by(.key)
    | map(
        .[0].key as $f
        | . as $all
        | (map(select(.value.mutants | length > 0)) | if length > 0 then . else [ $all[0] ] end)
        | (.[0].value.mutants | counts) as $listed
        | (.[0].value.mutants | map(select(compileError))) as $broken
        | ($broken | counts) as $brokenCount
        | [ .[].value.mutants[] | select((filtered or compileError) | not) ] as $kept
        | ($kept | counts) as $tested
        | {
            file: { key: $f, value: (.[0].value + { mutants: ($kept + $broken) }) },
            errors: ([ $listed | to_entries[]
                       | select(($tested[.key] // 0) + ($brokenCount[.key] // 0) != .value)
                       | "\($f) \(.key): kept \(($tested[.key] // 0) + ($brokenCount[.key] // 0)), expected \(.value)" ]
                   + [ $tested | keys[] | select($listed[.] == null)
                       | "\($f) \(.): missing from the first report" ]
                   + ([ .[].value.mutants[] | select(compileError) | mkey
                        | select($brokenCount[.] == null) ] | unique
                       | map("\($f) \(.): CompileError in a later report but not the first")))
          }) as $merged
    | ([ $p[].created ] | unique) as $created
    | ([ $merged[].file.value.mutants | length ] | add // 0) as $total
    | ([ $merged[].errors[] ]
       + (if ($created | length) != 1 or ($created[0] | type) != "number"
          then [ "shards disagree on (or lack) the mutants-created count: \($created | tostring)" ]
          elif $total != $created[0]
          then [ "files list \($total) mutants, Stryker created \($created[0])" ]
          else [] end)) as $errors
    | if ($errors | length) > 0
      then error("mutants not tested exactly once across shards:\n" + ($errors | join("\n")))
      else
        $p[0].meta + {
          files: ([ $merged[].file ] | from_entries),
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
