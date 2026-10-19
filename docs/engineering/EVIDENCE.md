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

### Diagnosed master remapping and explicit bounded extension
- Attempt 3: https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39192996110 at 1d6c59d. GUI/CLI/native builds succeeded; public annotation finally exposed `System.IO.InvalidDataException: Master remapping needs a separate verification stage.` The identity check before that guard passed.
- The strict original master-prefix check was an explicitly unsupported feature boundary, not evidence that the existing writer corrupted identity. Implemented common-global-FormID readback using PluginManager.MergeOverridePlugin, RecordParsers.ParseNPC and SaveNpcEspWriter.CuerpoDeNpcSobrescrito. Header and signature structure checks retained; added explicit identity checks for NPC, race and voice across removed unused masters.
- Reasoned retry policy amendment: initial run + three retries = four total executions, with this final targeted verification after obtaining an actual diagnostic. No fifth full build in this session. No user approval needed for this routine bounded technical decision; no recurring automation activated.
- Latest inspector Windows matrix at 1d6c59d passed 20 tests including PowerShell; Linux passed 19 and skipped the one Windows-only test. Engineering records CI passed at https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39192996109.

## Final verified checkpoint
- Runtime-tested commit: a0bf328. Full Windows workflow **Success**: https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39193615511. DirectXTex, GUI Publish|x64, CLI Release|x64, integration gate, artifact collection and license/provenance checks passed.
- ESP gate: 21 explicit assertions; generated NPC_ fixture read by PluginReader, separate ESP written by SaveNpcEspWriter, reopened and remapped by PluginManager, canonical global-space body comparison and header/subrecord structure/source-hash checks passed. Published `EXPERIMENTAL-NPC-ESP-roundtrip-NOT-GAME-VALIDATED`; generated inputs/Fallout4.esm is an empty test placeholder, not a real game master.
- Synthetic motor: 31 assertions passed on both Linux and Windows. Inspector: Windows 20 tests passed (including PowerShell entry point and junctions); Linux 19 passed, 1 Windows-only skipped. No zero-test or skipped-as-passed claim.
- Engineering records workflow passed at https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39193615502 for a0bf328; subsequent documentation updates are checked separately by the lightweight records workflow.
- Original six dependency commits, native source provenance, x64 configurations and license contents retained. No merges, releases, installation, original game/save edits, paid-service activation or recurring task activation.
- Limits: fixture has a minimal empty VMAD, not real quest-script properties. No actual collection resources, FaceGen, body/equipment, voiced dialogues, quests or in-game runtime were validated. Next requirement R05 depends on actual local metadata/collection versions.

## Production integration iteration — work under validation
- Reused original editor APIs after source audit; see PRODUCTION_INTEGRATION.md. Added production preset/save-shadow adapter, per-NPC batch coordinator, staged FaceGen call and dedicated Windows CLI. No replacement serializer/generator.
- Local portable gate exit 0: original 31 assertions plus 18 journal assertions, including profile/output tampering, path overlap/escape, symlinks and logical quarantine.
- Initial Roslyn syntax check found reserved VB identifier `overrides`; corrected to `edits`. Syntax parsing is not Windows semantic compilation.
- Windows validation pending pushed code revision; real collection assets and in-game evidence unavailable.
