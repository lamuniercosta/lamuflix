# scripts/sync_youtrack_board.py
"""
Synchronizes the YouTrack board with the LamuFlix architecture plan.
Can be run standalone or by a subagent.
Idempotent and resilient with caching and progress reporting.
"""

import argparse
import os
import sys
import json
import time
import urllib.request
import urllib.parse
import urllib.error

def get_config():
    base_url = os.environ.get("YOUTRACK_URL") or os.environ.get("YOUTRACK_API_URL")
    token = os.environ.get("YOUTRACK_TOKEN") or os.environ.get("YOUTRACK_API_KEY")

    if not base_url or not token:
        # Check Windows User environment via powershell or registry if running on Windows
        try:
            import subprocess
            res = subprocess.run(
                ["powershell", "-NoProfile", "-Command", 
                 "$u = [Environment]::GetEnvironmentVariable('YOUTRACK_URL', 'User'); "
                 "if (-not $u) { $u = [Environment]::GetEnvironmentVariable('YOUTRACK_API_URL', 'User') }; "
                 "$t = [Environment]::GetEnvironmentVariable('YOUTRACK_TOKEN', 'User'); "
                 "if (-not $t) { $t = [Environment]::GetEnvironmentVariable('YOUTRACK_API_KEY', 'User') }; "
                 "Write-Output \"$u|$t\""],
                capture_output=True, text=True, check=True
            )
            parts = res.stdout.strip().split("|")
            if len(parts) == 2:
                if not base_url:
                    base_url = parts[0]
                if not token:
                    token = parts[1]
        except Exception as e:
            print(f"Warning: could not fetch Windows user env vars: {e}")

    if not base_url or not token:
        raise ValueError("Missing YOUTRACK_URL and/or YOUTRACK_TOKEN.")

    return base_url.rstrip("/"), token

def make_request(base_url, token, path, method="GET", body=None, retries=3):
    url = f"{base_url}{path}"
    headers = {
        "Authorization": f"Bearer {token}",
        "Accept": "application/json",
        "Content-Type": "application/json"
    }

    data = None
    if body is not None:
        data = json.dumps(body).encode("utf-8")

    for attempt in range(retries):
        try:
            req = urllib.request.Request(url, data=data, headers=headers, method=method)
            with urllib.request.urlopen(req) as resp:
                resp_text = resp.read().decode("utf-8")
                if resp_text:
                    return json.loads(resp_text)
                return {}
        except urllib.error.HTTPError as e:
            err_msg = e.read().decode("utf-8", errors="replace")
            print(f"HTTPError {e.code} on {method} {url}: {err_msg}")
            if e.code in (429, 500, 502, 503, 504) and attempt < retries - 1:
                time.sleep(2 * (attempt + 1))
                continue
            raise RuntimeError(f"YouTrack API error {e.code}: {err_msg}")
        except Exception as e:
            if attempt < retries - 1:
                time.sleep(2 * (attempt + 1))
                continue
            raise

def run_command(base_url, token, issue_id, query):
    body = {
        "query": query,
        "issues": [{"idReadable": issue_id}]
    }
    return make_request(base_url, token, "/api/commands", method="POST", body=body)

def update_issue_text(base_url, token, issue_id, summary=None, description=None):
    body = {}
    if summary:
        body["summary"] = summary
    if description:
        body["description"] = description
    return make_request(base_url, token, f"/api/issues/{issue_id}", method="POST", body=body)

def create_issue(base_url, token, summary, description):
    body = {
        "project": {"id": "0-1"},
        "summary": summary,
        "description": description
    }
    resp = make_request(base_url, token, "/api/issues?fields=idReadable,id", method="POST", body=body)
    return resp["idReadable"]

def parse_args(argv):
    # The script takes no options. Without a parser, arguments meant for a
    # single ticket were ignored: the run did a full board sync, exited 0,
    # and read as success. Unknown arguments now exit 2.
    parser = argparse.ArgumentParser(
        description="Sync the YouTrack board with scripts/youtrack-plan.json. Takes no arguments.",
        epilog="To change one ticket's state use: pwsh scripts/local/Set-YouTrackState.ps1 -Ticket DEV-### -State Done",
    )
    return parser.parse_args(argv)

def main():
    parse_args(sys.argv[1:])
    base_url, token = get_config()
    print(f"Connected to YouTrack: {base_url}")

    plan_path = os.path.join(os.path.dirname(__file__), "youtrack-plan.json")
    with open(plan_path, "r", encoding="utf-8") as f:
        plan = json.load(f)

    epic_map_file = os.path.join(os.path.dirname(__file__), "epic-map.json")
    epic_map = {}
    if os.path.exists(epic_map_file):
        with open(epic_map_file, "r", encoding="utf-8") as f:
            epic_map = json.load(f)

    created_tasks_file = os.path.join(os.path.dirname(__file__), "created-tasks.json")
    created_tasks = {}
    if os.path.exists(created_tasks_file):
        with open(created_tasks_file, "r", encoding="utf-8") as f:
            created_tasks = json.load(f)

    # -------------------------------------------------------------
    # Step 1 & 2: Process Epics
    # -------------------------------------------------------------
    print("\n=== STEP 1 & 2: Processing Epics ===")
    for epic in plan["epics"]:
        key = epic["key"]
        action = epic["action"]
        summary = epic["summary"]
        desc = epic["description"]
        parent = epic.get("parent", "DEV-93")
        priority = epic.get("priority", "Major")

        if action == "modify":
            id_readable = epic["idReadable"]
            print(f"Modifying existing epic {id_readable}: {summary}")
            update_issue_text(base_url, token, id_readable, summary, desc)
            run_command(base_url, token, id_readable, f"Type Epic Repository lamuflix Priority {priority} subtask of {parent}")
            epic_map[key] = id_readable
        elif action == "create":
            if key in epic_map:
                id_readable = epic_map[key]
                print(f"Epic {key} already created as {id_readable}. Ensuring fields...")
                update_issue_text(base_url, token, id_readable, summary, desc)
                run_command(base_url, token, id_readable, f"Type Epic Repository lamuflix Priority {priority} subtask of {parent}")
            else:
                print(f"Creating new epic: {summary}")
                id_readable = create_issue(base_url, token, summary, desc)
                run_command(base_url, token, id_readable, f"Type Epic Repository lamuflix Priority {priority} subtask of {parent}")
                epic_map[key] = id_readable
                print(f"  -> Created as {id_readable}")

        # Save epic map checkpoint
        with open(epic_map_file, "w", encoding="utf-8") as f:
            json.dump(epic_map, f, indent=2)

    print("\nEpic Mapping established:")
    for k, v in epic_map.items():
        print(f"  {k} -> {v}")

    # -------------------------------------------------------------
    # Step 3 & 4: Process Tasks
    # -------------------------------------------------------------
    print("\n=== STEP 3 & 4: Processing Tasks ===")
    total_tasks = len(plan["tasks"])
    for idx, task in enumerate(plan["tasks"], 1):
        key = task["key"]
        action = task["action"]
        summary = task["summary"]
        desc = task["description"]
        est = task.get("estimatedTime")
        repo = task.get("repository", "lamuflix")
        priority = task.get("priority", "Major")
        epic_key = task["epicKey"]
        parent_id = epic_map[epic_key]

        est_cmd = f"Estimated Time {est}" if est else ""
        cmd = f"Type Task Repository {repo} Priority {priority} {est_cmd} subtask of {parent_id}".strip()

        if action == "modify":
            id_readable = task["idReadable"]
            print(f"[{idx}/{total_tasks}] Modifying task {id_readable} ({key}): {summary}")
            update_issue_text(base_url, token, id_readable, summary, desc)
            run_command(base_url, token, id_readable, cmd)
            created_tasks[key] = id_readable
        elif action == "create":
            if key in created_tasks:
                id_readable = created_tasks[key]
                print(f"[{idx}/{total_tasks}] Task {key} already created as {id_readable}. Ensuring fields...")
                update_issue_text(base_url, token, id_readable, summary, desc)
                run_command(base_url, token, id_readable, cmd)
            else:
                print(f"[{idx}/{total_tasks}] Creating new task ({key}): {summary}")
                id_readable = create_issue(base_url, token, summary, desc)
                run_command(base_url, token, id_readable, cmd)
                created_tasks[key] = id_readable
                print(f"  -> Created as {id_readable} under {parent_id} (Est: {est})")

        # Save checkpoint periodically
        if idx % 5 == 0 or idx == total_tasks:
            with open(created_tasks_file, "w", encoding="utf-8") as f:
                json.dump(created_tasks, f, indent=2)

    # -------------------------------------------------------------
    # Step 5: Verification
    # -------------------------------------------------------------
    print("\n=== STEP 5: Verifying YouTrack State ===")
    q = urllib.parse.quote("project: DEV Repository: lamuflix")
    fields = urllib.parse.quote("idReadable,summary,customFields(name,value(name,presentation)),links(direction,linkType(name),issues(idReadable))")
    issues = make_request(base_url, token, f"/api/issues?fields={fields}&query={q}&$top=200")

    print(f"Total issues found for Repository: lamuflix = {len(issues)}")
    missing_parent = []
    missing_est = []

    for issue in issues:
        id_r = issue["idReadable"]
        summary = issue.get("summary", "")
        cfields = {f["name"]: f.get("value") for f in issue.get("customFields", [])}
        
        itype = cfields.get("Type", {}).get("name") if cfields.get("Type") else None
        iest = cfields.get("Estimated Time", {}).get("presentation") if cfields.get("Estimated Time") else None
        
        parent_link = [l for l in issue.get("links", []) if l.get("direction") == "INWARD" and l.get("linkType", {}).get("name") == "Subtask"]
        parents = [p["idReadable"] for l in parent_link for p in l.get("issues", [])]

        if itype == "Task":
            if not parents:
                missing_parent.append(id_r)
            if not iest:
                missing_est.append(id_r)

    print(f"Tasks without parent: {len(missing_parent)} ({missing_parent})")
    print(f"Tasks without estimate: {len(missing_est)} ({missing_est})")

    if not missing_parent and not missing_est:
        print("\nALL VERIFICATIONS PASSED SUCCESSFULLY!")
    else:
        print("\nVerification flagged issues to check.")

if __name__ == "__main__":
    main()
