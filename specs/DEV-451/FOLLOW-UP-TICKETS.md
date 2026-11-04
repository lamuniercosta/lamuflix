# DEV-451 tracked follow-ups (Rigger)

## Q4 — CRLF / mutation scope span gap (PR #297, MmrSelector.cs)

**Patron ruling:** ACCEPT split (CONCLUSIONS.md Q4; recon-DEV-451 §2 line 39, §6 line 104, §7 line 117).

**YouTrack search (2026-11-04):** No open or closed lamuflix issue in DEV-430..DEV-460 (and spot checks through DEV-470) matches this defect. DEV-442 (*.sh LF in `.gitattributes`) is a different file class and scope; do not fold Q4 into DEV-442.

**Follow-up filed:**

- **Summary:** Fix mutation scope span gap on CRLF Windows checkout (MmrSelector.cs, PR #297)
- **Parent:** DEV-100
- **Type:** Task | **Estimate:** 1d | **Tag:** size:S
- **Description:** `C:/Users/lamun/AppData/Local/Temp/dev-451-q4-followup-desc.md`

**Verified follow-up id:** DEV-460

**DEV-451 delivery constraint:** Leave `.gitattributes` and `src/**/*.cs` unchanged; diff-only `Test-RunMutation.ps1` case must not represent this defect as fixed.
