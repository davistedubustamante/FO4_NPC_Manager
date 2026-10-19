# Evidence and iteration log

## Baseline
- d09860b preserves the synthetic engine and 31 assertions; Linux/Windows matrix and GUI/CLI build passed in https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39182037976 (code commit ca3b55a; later documentation only).
- No real ESP conversion or in-game compatibility claimed by that baseline.

## Current iteration
- Read runtime skill and network policy; reused existing Git authentication. Environment network enforcement observation is unknown; operations must be checked individually.
- Inspected pinned FO4_Base_Library 8dc93e189cccfe026c128eb60d06c94fc949831b, same revision as existing Windows workflow.
- Implementation and validation results will be appended before marking requirements verified.

### Iteration 1 — local checks and diagnosis
- Synthetic baseline: SDK 8.0.408 `dotnet run` completed, exit 0, 31 assertions.
- Offline inspector: `python3 -B -m unittest discover -s Tools/WoDInspector -v`, exit 0, 18 tests, no skipped tests on Linux.
- Source inspection of pinned `Rec_NPCx` established ACBS Template Flags offset 14 (not 18), VMAD default version 6 and required OBND/AIDT/CNAM/DATA/NAM6/NAM7/NAM8. Updated the generated fixture accordingly before claiming round-trip success.
- Reused CLI project guidance: every indirect native consumer also imports DirectXTexWrapper.targets to pin x64 correctly. Added the same import to the gate rather than bypassing native checks.
- Windows attempt 1: https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39191877221 at b6cc961; GUI compiled, remaining steps under observation. The above fixture/native configuration corrections are staged for attempt 2.
- No callable scheduling tool found among available tool names/descriptions. Versioned proposal is disabled and explicitly not an official Codex import schema.

- Attempt 1 ended Failure specifically in Build and exercise isolated NPC ESP round-trip; DirectXTex, GUI and CLI were success. Its raw web log returned 404 and GitHub API 403. The exact compiler/runtime diagnostic is unavailable, so no root-cause claim is made. Attempt 2 adds captured text logs and public job summaries, plus the independently identified fixture/schema and native-target corrections. No assertions disabled.

### Independent inspector and recovery iteration
- Engineering records CI passed: https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39192367366 at 3ac23fa.
- Attempt 2 matrix produced both synthetic artifacts: Linux and Windows passed the 31-assertion baseline and 18 inspector tests, including a Windows junction. Full build/gate remains under observation at https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39192367304.
- Added local private root bindings so indexed resources can be located on the same Windows machine without asset copies. Added an end-to-end PowerShell entry-point test. Local result: 20 tests discovered, 19 passed, 1 Windows-only test skipped. Do not report the skipped test as locally passed; Windows CI must exercise it.

### Windows attempt 2 and final bounded validation
- https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39192367304 at 3ac23fa: DirectXTex, GUI and CLI succeeded; the round-trip step failed. Captured logs exist in diagnostics, but both raw-log and summary-partial web endpoints returned 404 without session access, while api.github.com returned 403. Detailed root cause still unknown; no false diagnosis or passing round-trip claimed.
- Local Roslyn VB parser on both new VB files: exit 0, zero syntax errors. This is syntax evidence only, not semantic compilation or runtime evidence.
- Fixture HEDR count corrected to include four records plus four GRUPs (8), matching the existing writer's counting contract.
- Final bounded attempt adds public error annotations from captured diagnostic lines and includes the private-bindings/PowerShell inspector tests. No preservation check removed or downgraded. If this attempt fails, record its exact diagnostic and stop retrying the same step in this session.
