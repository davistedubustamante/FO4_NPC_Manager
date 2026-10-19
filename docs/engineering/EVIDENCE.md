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

## Previous verified checkpoint (before production integration)
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

### Production integration attempt 1 and diagnosed retry
- c0dca14: https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39258131448. Both portable matrix jobs passed; native DirectXTex, GUI and original CLI builds succeeded. Production binary gate failed with `System.InvalidOperationException: Runtime body/skin/overlay/outfit delivery is not integrated.`
- Source diagnosis: the existing LooksMenu parser sets HasOverlays=True unconditionally to model engine clearing behavior, even when the preset has no overlays. Adapter now checks the actual unsupported overlay count rather than presence of that execution channel. No serializer/preservation guard bypassed.
- Retry also adds declared-versus-written FaceGen checks through original packer specs, rejects resource scan errors, seals inputs again before checkpoints, preserves Windows junction coverage without requiring symbolic-link privileges, and exercises the new CLI executable against a generated binary profile.
- Local repeated checks: exit 0, 31 baseline + 18 journal assertions. Inspector exit 0: 20 discovered, 19 passed and 1 Windows-only skipped. Records checker exit 0, 9 requirements. Windows retry pending.

### Production integration attempt 2 — negative-test harness diagnosis
- d6efc55: https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39258897460. GUI and original CLI compiled; binary gate reached `InvalidDataException: Profile identity or journal format mismatch.`
- The preset-tamper negative check intentionally changes the profile seal. Its Reject helper caught IOException but not InvalidDataException (which is not an IOException subtype). Added that exact rejection type; the fail-closed production check is retained. No success claim for the failed gate.
- Next code revision also implements R10: unique per-NPC shard names, original multientry writer consolidation, two-NPC readback/appearance assertions, bundle tamper rejection, copied generated-resource conflict checks and versioned resume identities. Resource index is reused once per batch process, with no unchanged retry after an index error.

### Aggregate iteration local checkpoint
- Rebuilt portable gate explicitly (`dotnet build --no-restore -c Release -t:Rebuild`): exit 0, zero warnings/errors. Executed rebuilt DLL: exit 0, baseline 31 plus expanded journal 21 assertions. New checks cover verified output lookup and a torn durable snapshot. An earlier incremental execution still used the preceding 18-check binary; it is not evidence for these three new assertions.
- Roslyn syntax gate: exit 0, zero VB syntax errors across journal, production batch, aggregate writer adapter, fixture gate and new CLI. This is not Windows semantic/runtime evidence.
- Requirements checker: exit 0, 10 requirements, acyclic dependency graph, recurring proposal disabled. Aggregate and CLI validation pending attempt 3.

### Production integration attempt 3 — passed
- Exact runtime-tested code: 9adb4a6. Full Windows CI **Success**: https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39259948641. Native DirectXTex, GUI Publish|x64, original CLI Release|x64, production gate, new conversion CLI build and actual CLI invocation on generated two-NPC input all passed. Artifact collection, SHA256/provenance and license controls passed.
- Production gate: 37 assertions. Real LooksMenu parser/category selection → override scalar/materialization → original save shadow/overlay → original ESP writer → reader/canonical comparison; mixed-batch failure continuation; missing resources block FaceGen without a checkpoint; two-NPC multientry aggregate preserves both NPC identities/appearance/headers; modified preset and aggregate hashes reject resume. All inputs are generated fixtures.
- Portable baseline 31 + journal 21 passed in both matrix jobs. Windows inspector 20 tests passed; Linux 19 passed and one Windows-only skipped. No proprietary inputs/assets uploaded.
- Next independent hardening: library source inspection found that GlobalFormIDFromObjectID masks ESL IDs to 12 bits. Added round-trip object-ID validation before looking up an NPC/race so oversized IDs cannot alias another record. Positive ESL appearance export and two negative alias cases added for the final bounded Windows validation. Also quarantine unreadable inputs on access-denied errors.

### Final ESL validation revision replaces a run before the integration step
- b6f62ca started https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39261034700. During review, found that reusing the v2 pipeline seal would let a checkpoint created before the ESL guard skip that new guard on resume.
- Bumped the sealed identity to `production-batch-v3-esl-identity`. Old staging is retained but cannot resume across the changed admission rules. No migration or deletion.
- The replaced run had not started the production integration gate when superseded. Existing workflow concurrency cancels it on push; the next run is the fourth actual integration-gate execution, not an unchanged retry after the cap. Successful checkpoint 9adb4a6 remains preserved.

### Published artifact metadata review at 9adb4a6
The public successful run lists the following artifacts. These are GitHub's published archive digests, not a separately downloaded/rehashed archive in the cloud workspace; runner-side license/provenance/SHA256 checks passed before upload.

| Artifact | Public size | Published archive SHA256 |
|---|---|---|
| fo4-npc-manager-windows-x64-UNTESTED | 42.2 MB | 49a9aa637d198168859961cfb880b838d023d53ea87e5b2bfdc89af1f1519113 |
| EXPERIMENTAL-NPC-ESP-roundtrip-NOT-GAME-VALIDATED | 20.1 KB | d2f5ae97a18449600e9364d5153073ec187ef4c59460172358659d5cc454b963 |
| windows-build-diagnostics | 5.76 MB | 8d442ea16af7a9783f50ffb635f1fee4bdbba4a597b7ce6ad74aaf122e305f43 |

The fixture artifact contains generated placeholder masters and is never installable. The build archive includes the experimental conversion CLI with original application/dependency/license outputs; no proprietary collection resources were packaged.

### Coverage boundary clarified
R10 remains in_progress: the aggregate ESP path and resume checks passed on generated NPC records, but generated-resource copy/conflict branches only compiled because positive FaceGen resources were unavailable and the successful profile requested record-only export. They must receive their own controlled staging-copy tests and real FaceGen bundle evidence. R09's integration evidence does not certify positive FaceGen; R06 remains blocked on actual assets. No requirement is considered complete from compiling a branch alone.

## Latest verified production code — 74289ec
- Full Windows CI **Success**: https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39261465287. Native, GUI Publish|x64, original CLI Release|x64, production gate, new CLI build and two-NPC executable invocation passed. Experimental fixture/build artifacts were published and license/provenance/SHA256 checks passed.
- Production gate: 40 mandatory assertions, including positive ESL appearance serialization/global identity and rejection of oversized light-plugin NPC/race IDs. Original reader/writer/override/preset functions are exercised, not replacement implementations. v3 pipeline seal prevents pre-guard checkpoint reuse.
- Portable gates: baseline 31 + journal 21 passed on Linux and Windows. Inspector: Windows 20 passed; Linux 19 passed, one Windows-only skipped. All fixture/plugin/preset/test data generated; no licensed collection resources uploaded.
- https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39261034700 was Cancelled during GUI compilation before the production gate; the replacement run is the fourth actual gate execution. No fifth execution of that step and no unchanged rerun.
- Limits: positive FaceGen NIF/DDS, generated-resource copy/conflict branch, body/equipment, inherited templates, nonempty quest VMAD, dialogue and real collection/game regressions are not certified. R10 stays in_progress for resource-copy coverage; R06 remains blocked for real FaceGen. Next independent requirement R11 persists for original inherited-Traits materialization.
- No merge, release, game/save edits, original installation changes, paid-service enablement or recurring automation activation. Remaining input requirement: licensed locally available assets plus an authorized frozen separate Windows profile and reviewed CRU/WoD/runtime versions. Keep original plugins/archives/saves/private profiles local.

## Global scope iteration (R12), validation pending
- Reused existing loader/NPC parser/race view and batch DTOs; moved DTO declarations without changing their namespace/API. Global inventory retains original identity, winner, override chain and blockers. No replacement parser/writer.
- Local portable gate: 20 campaign assertions on 10,000 entries plus existing 31 baseline/21 journal assertions; explicit rebuild succeeds with no warnings/errors. Original Windows gate expanded for winning overrides and storage limits; CLI discovery execution added. Windows evidence pending this revision.
- Storage preflight: /workspace 1.3 GiB used (32 GiB volume), existing tool cache 1.2 GiB, repository 21 MiB. No new dependencies installed. New discovery only writes bounded metadata; global execution remains disabled pending hard quota control.
