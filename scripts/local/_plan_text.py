"""Keeps scripts/youtrack-plan.json in step with a direct YouTrack text edit.

sync_youtrack_board.py rewrites every planned issue's summary and description
from the plan, so an edit made only in YouTrack is undone by the next sync.
Edit-YouTrackIssue.ps1 calls this after it has verified the edit in YouTrack.

  python _plan_text.py <ticket> --scripts-dir <dir>
      [--summary-file <file>] [--description-file <file>]

Exit 0: the plan entry was updated. Exit 3: the ticket is not in the plan, so
the sync never touches it. Exit 2: bad input.

The plan is rewritten with the same JSON settings it was written with, and its
newline style and trailing newline are kept, so the diff shows only the edit.
"""

import argparse
import json
import os
import sys

NOT_PLANNED = 3


def read_text(path):
    with open(path, "r", encoding="utf-8") as f:
        return f.read().replace("\r\n", "\n").rstrip()


def read_json(path, default):
    if not os.path.exists(path):
        return default
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def find_entry(plan, ticket, epic_map, created_tasks):
    """The plan entry sync_youtrack_board.py maps to this ticket, or None."""
    for entries, created in ((plan.get("epics", []), epic_map), (plan.get("tasks", []), created_tasks)):
        for entry in entries:
            if entry.get("idReadable") == ticket or created.get(entry.get("key")) == ticket:
                return entry
    return None


def write_plan(path, raw, plan):
    text = json.dumps(plan, indent=2, ensure_ascii=False)
    if raw.endswith("\n"):
        text += "\n"
    if "\r\n" in raw:
        text = text.replace("\n", "\r\n")
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)


def main(argv):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("ticket")
    parser.add_argument("--scripts-dir", required=True)
    parser.add_argument("--summary-file")
    parser.add_argument("--description-file")
    args = parser.parse_args(argv)
    if not (args.summary_file or args.description_file):
        parser.error("nothing to write: pass --summary-file and/or --description-file")

    plan_path = os.path.join(args.scripts_dir, "youtrack-plan.json")
    with open(plan_path, "r", encoding="utf-8", newline="") as f:
        raw = f.read()
    plan = json.loads(raw)
    epic_map = read_json(os.path.join(args.scripts_dir, "epic-map.json"), {})
    created_tasks = read_json(os.path.join(args.scripts_dir, "created-tasks.json"), {})

    entry = find_entry(plan, args.ticket, epic_map, created_tasks)
    if entry is None:
        print(f"plan: {args.ticket} is not in youtrack-plan.json; the sync leaves it alone")
        return NOT_PLANNED

    changed = []
    if args.summary_file:
        entry["summary"] = read_text(args.summary_file)
        changed.append("summary")
    if args.description_file:
        entry["description"] = read_text(args.description_file)
        changed.append("description")
    write_plan(plan_path, raw, plan)
    print(f"plan: youtrack-plan.json entry {entry.get('key')} updated for {args.ticket} ({', '.join(changed)}); commit it")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
