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

### Global iteration Windows attempt 1 — diagnosed compile failure
- 9a87508: https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39267896851. Native/GUI/original CLI compiled. Gate compile failed at Program.vb(178,75), BC30690: SubrecordData cannot be indexed. VB lambda variable `sr` hid fixture helper function `Sr`; renamed variable to `part`. No production parser or serializer defect inferred.
- Next validation adds automatic page preparation using a generic NPC/race created through the original canonical race writer; deleted/ESL/stale/localized admission checks; controlled resource copy/dedup/conflict/tamper tests through the existing aggregate exporter. The copy fixtures are .bin bytes, explicitly not fabricated FaceGen evidence.
- Found actual existing localized-input export boundary; added R15 and a plan blocker. Global execution still disabled for hard-quota and asset/narrative acceptance. Policy/preset/input metadata limits tightened; no new installations or collection copies.

### Global attempt 2 preflight
- Portable project explicit rebuild: exit 0, zero warnings/errors. Rebuilt runtime gate: baseline 31 + journal 21 + global planner 21 assertions passed, including 10,000-row deterministic paging and localized-export blocking.
- Windows source now contains 63 mandatory integration assertions, including original 40, global winning/deleted/ESL identity, actual automatic page preparation, stale/localized admission and aggregate resource-copy semantics. Syntax gate passes; Windows semantic/runtime evidence pending.
- Global campaign policy/saved-plan CLI inputs capped at 16/64 MiB. Preset pool limited to 256 files / 64 MiB total; plugin budget maximum 8 GiB. Artifact guards reserve diagnostics space and bound portable payloads; no old artifacts removed.

### Global Windows attempt 2 — second harness name collision
- c1300e2: https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39269638774. Native, GUI and original CLI compiled. Gate compile failed BC30980 at Program.vb(198,31): local `copyJournal` hid helper `CopyJournal`, so initializer referenced itself. Renamed helper CreateCopyJournal and scanned method/local name overlaps in the affected modules. No production parser/writer defect inferred; runtime assertions not claimed passed.
- Additional admission hardening: global discovery rejects deleted race winners; direct record/preset export rejects deleted and known child races regardless of supplied adult evidence. Production pipeline seal v4 and global plan v3 prevent older checkpoints/plans bypassing the change. Added real generated race/child override tests; shared race metadata reused within a scan. Source remains experimental pending attempt 3.

### Global attempt 3 — full success
- Exact tested commit d1e483f. Full Windows workflow **Success**: https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39271103795. Native/GUI/original CLI/new CLI, 69 mandatory integration assertions, actual global discovery and automatic page preparation passed. Source pins, licenses, payload guards and publication of experimental artifacts passed.
- Controlled .bin staging resources exercise actual aggregate copy/dedup/conflict/tamper branches; no fake NIF/DDS claim. R10 is verified only for that file-copy/ESP scope; real FaceGen remains R06. Global fixture exercises actual canonical race creation, child/deleted/ESL/winner identity, explicit adult generic scheduling, stale/localized admission and no original-output writes.
- Portable matrix: baseline 31 + journal 21 + campaign 21 assertions passed; campaign cases include 10000 rows. Windows inspector 20 passed; Linux 19 passed and one Windows-only skipped.
- Last feasible optimization selected: reuse existing loader sigFilter to omit unrelated groups, expose loaded record count, share existing preset guard rather than duplicate admission checks, and add distinct policy-vs-actual-inventory limit tests. Final bounded run will validate this code revision; do not restart infrastructure or install anything.

### Final local verification for e71248f
- SDK 8.0.408 selected from Tools/AdultTransformationGate/global.json: explicit --no-restore Release rebuild exit 0, zero warnings/errors. Executed rebuilt DLL: baseline 31 + journal 21 + campaign 23 assertions passed. Distinct checks now exercise actual inventory overflow separately from oversized adult-policy evidence.
- Inspector: exit 0, 20 discovered, 19 passed and one Windows-only skipped. Records validator: exit 0, 16 requirements, acyclic DAG, automation disabled.
- Cloud storage rechecked: cached tools 1.2 GiB, repository 22 MiB, no collection/assets downloaded, no new installations. Final Windows validation pending.

## Final Windows verification: e71248f

https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39272442946 completed Success. All Windows compilation, 72 mandatory integration assertions, actual export/discovery/prepare-page CLI commands, license/provenance checks and bounded experimental artifact publication passed. Five workflow artifacts reported. Portable baseline 31, journal 21 and campaign 23 assertions passed; campaign uses 10,000 synthetic entries. Inspector Windows 20 tests; Linux 19 passed and one Windows-only skip. Original dependency pins preserved.

Four full Windows attempts in this global iteration: two diagnosed VB harness compilation failures, one successful checkpoint d1e483f, one successful final refinement e71248f. No fifth execution. Neither planner volume nor controlled aggregate copy tests certify real FaceGen, localized GOTY/DLC export, narrative behavior or global conversion. R13–R16 remain pending with explicit boundaries.

## Active R15/R11/R14/R13 session (Windows validation pending)

- Preserved e71248f/c1c771f and previous evidence. R15 initial commit 8967297 pushed; Windows run https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39278508513 remained queued for at least 14 minutes, with no executed Windows assertions reported. Do not call it passed or failed. A superseding changed-code revision may cancel this queued revision under existing concurrency policy; no completed full build is being repeated.
- Original LocalizedStringResolver/PluginManager and canonical ESP writer reused. Explicit language + selected loose tables only, SHA256/byte limits/reparse/encoding checks, resource context restored on failure. Original external IDs remain unchanged in inputs; independent nonlocalized output contains inline selected-language text. Source-ID audit added. No second strings parser. BA2 resource provenance and real GOTY/DLC verification remain pending.
- Original NpcTemplateMaterializer.ProbeCategoryOwn/CongelarResolucion/MakeCategoryOwn integrated for strict Traits-only NPC-to-NPC chains. Original editor dead-pointer fallback intentionally rejected by autonomous admission wrapper. Cycles, dead/missing/deleted/non-NPC and duplicate pointer blocks fail closed. Other inherited categories and leveled-list pinning remain blocked. Fixtures include nonempty VMAD and target faction/inventory/package preservation; runtime Windows result pending.
- R14 uses original CanonBridge.Tree/WbFormIdWalker for QUST/aliases/SCEN/RELA/VTYP/NPC and related signatures, with reverse transitive impact and bounded resources. CELL/WRLD included to reach nested placed references. Filtered-out references are marked outside scanned view, not falsely declared missing. Static graph is never quest/runtime compatibility certification.
- R13 controlled-write gateway reserves and fsyncs ledger+payload bytes BEFORE creating payload files; interrupted/partial attempts retain their full charge. Portable runtime passed 16 assertions, plus unchanged baseline31/journal21/campaign23. JSON report /tmp/r15-r13-local-verified.json. This proves only writes through the gateway: it is NOT an OS/FaceGen quota, and no global ESP/FaceGen worker was enabled. Sequential campaign preparation uses the gateway and existing page DTOs; Windows integration pending.
- Local quota development failures: an initial command used the gate working directory with repository-relative source paths (missing test file); another omitted DOTNET_CLI_HOME and failed before compilation on read-only /home/agent; first actual source compile found VB reserved keyword `protected`, renamed protectedRoot. First runtime reached torn-ledger negative case but the test helper omitted InvalidDataException in its catch filter; corrected. Subsequent pinned build exit0/no warnings and runtime exit0, all16 assertions passed. No assertions disabled. SDK/dependency pins unchanged, no installation.
- Local inspector/profile helper: 29 tests discovered, 27 passed, two Windows-only skips. Prepares private explicit-language manifest from existing read-only loose/VFS view; hashes paths without copying tables/plugins/assets. Uses existing TES4 header inspector, not a second production reader. Rejects CRU-Female-Lite paths, symlinks, incorrect masters/language and existing output. Windows launcher fingerprints existing CLI, no install/download or VFS registration.

### Integrated Windows attempt 1: diagnosed compile failure

95bb070 / https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39280537225 completed Failure. Portable matrix passed before native build; native DirectXTex compiled. GUI compile failed BC30183/BC30201 in LocalizedExportScope: `handles` is VB's reserved Handles keyword. No Windows runtime integration assertions executed. Renamed the field to tableHandles. A local Roslyn syntax-only tool using assemblies already bundled in SDK8.0.408 reproduced the errors, then parsed all seven new/changed adapter/CLI/gate files with zero syntax errors after correction. Syntax evidence does not replace Windows binding/runtime checks. Added winning-localized-override owner case and sealed string-resource roots in production staging protection; changes justify the next validation.

### Integrated Windows attempt 2: fixture order diagnosed

bf055b2 / https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39319590456: portable matrix, native, GUI, original FaceTint CLI and integration gate compilation passed. Runtime gate then failed with `Incomplete Traits chain; dead pointers cannot fall back to an intermediate NPC`. Source inspection found the positive Traits fixture appended TPLT/SNAM/CNTO/PKID after the end of the NPC schema. The original WbReader cursor is explicitly monotonic and correctly retains out-of-order fields as unconsumed, so its TPLT property is zero. Corrected factory ordering (TPLT after VTCK, faction after ACBS, inventory before AIDT, packages after AIDT) and 5-byte FO4 faction payload. Cycle fixture also places its pointer in schema order; negative tests now assert distinct rejection reasons. No production parser change or guard suppression. Public failure diagnosis now includes last passed check and stack lines.

Additional changed-code validation required: explicit scoped UTF8 output/encoding restoration and accented text; reject unsealed global encoding overrides; apply same resource scope to aggregation; protected-root checks on preparation pause/resume. Original localized block still applies without admitted explicit resource context. These remain experimental pending the next Windows runtime result.

### Integrated Windows attempt 3: R15/Traits/preparation passed; narrative field identity corrected

38c7f2b / https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39320528288: native/GUI/original CLI/gate compilation passed. Runtime reached `LAST-CHECK: invalid template chains write no output directories`, then failed the first narrative assertion (Program.NarrativeChecks): it incorrectly expected ALUA inside WbNode.Path. All preceding mandatory preparation, localized export (including accented UTF8/scoped encoding and winning-owner cases), production localized aggregate/resume and Traits preservation/cycle/incomplete/duplicate checks completed. No complete Windows gate success or artifact certification claimed for this revision.

Original WbSubrecordDef.Name uses the value's human-readable name, so WbNode.Path contains Actor rather than the ALUA signature. The production report now records explicit nearest ancestor subrecord signature separately from the readable field path; test checks Subrecord=ALUA. No parser substitution or assertion removal. Final integrated Windows validation will also exercise the newly added fail-closed canonical input guard: opaque/unconsumed NPC bytes can hide FormIDs and cannot be certified by canonical readback after master cleanup. Production/global fingerprints v7 preserve old staging/plans while rejecting their admission seals. Do not start a fifth full integrated execution after this final fourth attempt; record any remaining failure and preserve independent verified outcomes.


## Final integrated verification: 71d329a

https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39321569051 completed **Success** (5m34s, five experimental workflow artifacts). Exact source commit 71d329a; subsequent changes are documentation only. All 125 mandatory Windows integration checks completed, including every previous record/appearance/aggregate/discovery/ESL guard plus explicit localized resources, winning string owner, accented UTF8 and encoding restoration, localized aggregate/resume, strict Traits preservation/negative chains, narrative ALUA/scene/relationship/transitive/voice edges, quota-bound preparation, protected recovery and opaque canonical-record rejection.

Native DirectXTex, GUI, original FaceTint CLI, integration gate and conversion CLI compiled. Actual existing CLI export/discovery/prepare-page and fingerprinted PowerShell narrative/start/next preparation passed. License/provenance and both artifact storage/diagnostic guards passed; retention remains two days. No dependency/source pin changes, new packages, installation, merge, release, game/save modification or recurring activation.

Portable prerequisites passed: baseline31 + journal21 + campaign23 + controlled write-budget16 = 91 assertions. Planner volume is 10,000 synthetic entries, not game conversions. Inspector/profile suite has 29 discovered tests; local Linux 27 passed and two Windows-only skipped; Windows suite step passed (platform privilege-dependent skips remain separate from executed tests). Local final report /tmp/r15-r13-v7-portable.json; local pinned build exit0, zero warnings/errors. Original pinned dependencies emitted existing Windows warnings during build; no suppression added.

Four integrated executions total: 95bb070 compile failure, bf055b2 ordered-fixture runtime failure, 38c7f2b readable-path/signature assertion failure, 71d329a full success. Initial 8967297 was superseded/cancelled, not certified. Every failure remains recorded with cause/fix; no fifth integrated execution and no assertion disabled. e71248f/c1c771f remain ancestors; code worktree clean before documentation closeout.

Verified subroutes do not close complete R15/R11/R14/R13: BA2 language resource provenance/extraction, encoding exceptions and real collection evidence remain; inherited categories beyond Traits-only and leveled routes remain blocked; static narrative graph does not prove quests/runtime; gateway quota does not constrain direct-path ESP/FaceGen writers, so global mass asset execution remains disabled. Real FaceGen/body/equipment/audio and GOTY/DLC/WoD runtime coverage remain unvalidated. Detailed operating limits: LOCALIZED_AND_TRAITS.md.

## BA2 and streaming reservation iteration (Windows pending)

Original BSA_BA2 source inspected at pinned 1bfbeb59ca54219e3f208fbee52bc904ef39f743 (small source checkout, no installation/pin change). Original BethesdaReader/GetPayloadSource/FilesDictionary live generation entries and original localized resolver/writer reused. Only declared GNRL tables are extracted in memory, each stored/decompressed payload <=32 MiB, aggregate selected tables <=128 MiB. Archive and resource hashes are distinct; manifest explicitly selects one winner per plugin/kind/language, competing selections fail rather than silently override. Archive index caches disabled/restored. New Windows fixtures pending: three table types, missing IDs, UTF8 text/ESP reopen, ambiguous winners, wrong language/archive/resource hashes, invalid table and unchanged archives.

R13: new bounded seekable Stream capability reserves its full length before payload creation, rejects Write/WriteByte/Seek/SetLength beyond reservation, never exposes a path/handle, and requires a durable completion event after exact length/hash and flush. Interrupted matching bytes are not checkpoints. New portable total: 30 quota assertions (was16), baseline31+journal21+campaign23=105. Pinned local build exit0, zero warnings/errors; /tmp/ba2-stream-local.json exit0. Covers stream writes only, not direct path/native ESP/NIF/DDS writers. Mass execution remains disabled and complete R13 remains unverified.

### BA2 Windows attempt 1: resource lifetime diagnosis

dc669ba / https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39333204616 failed in the integration runtime, after `LAST-CHECK: failed BA2 operations restore original context`. All preceding new BA2 three-kind resolver, missing IDs, export/reopen, original-byte/no-extraction, conflict/language/integrity tests passed. Portable prerequisites passed. Native, GUI, original CLI and integration compilation passed. The subsequent corruption fixture could not replace its BA2 because the metadata-reader caller leaked its FileStream: original BethesdaReader does not own that stream. Fixed both metadata inspection and local sealing with explicit nested Using for source stream and reader. No production guard/test disabled. Next changed-code validation additionally includes original Factions/Inventory/AIData materialization, narrative admission and bounded local authentic asset preflight.

Local R11/R14 additions: Roslyn syntax-only pass (six VB files); pinned portable build zero warnings/errors; /tmp/r11-r14-stream-local.json exit0, baseline31+journal21+campaign28+quota30=110 assertions. Inspector/profile suite29 discovered,27 passed,two Windows-only skips. Full Windows evidence pending. Metadata WriteNew now uses the same durable stream completion route; ledger limit checked before appending so newly written ledgers cannot grow beyond recovery's1MiB limit.

## BA2/inheritance/narrative Windows checkpoint: 033045f

https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39334233343 completed Success. All mandatory integration checks, original/new CLI compilation, real CLI BA2 sealing/reviewed narrative discovery/export/preparation, fingerprinted Windows launcher, native/GUI/FaceTint builds, license/provenance and bounded experimental artifact guards passed. Portable110 assertions passed. Controlled DDS encode/decode and malformed NIF/DDS guards passed; no authentic FaceGen/body/equipment or in-game evidence claimed. Four-category inheritance positive/negative cases passed original resolver/materializer/export/readback. Previous71d329a/57043d6 remain ancestors.

### Changed-code output-adapter validation (attempt3 pending)

Continue R13 independently: optional dedicated FaceGenOutputAdapter routes the existing builder's seven DDS byte writers, two NIF serializers and two material byte writers through exact durable reservations; directory creation is deferred. It reuses original NIF Save_To_Bytes_Manolo, preserves original unscoped editor writers, rejects TGA diagnostics and refuses unbudgeted fallback after scope closure. Added controlled actual DDS adapter/exhaustion/escape/closed-scope tests. This does not enable a batch/global worker or claim ESP/cache/temp/auxiliary coverage; real NIF/FaceGen output remains unvalidated.

More portable quota evidence: completion-event exhaustion after matching full payload bytes retains charge but no checkpoint through recovery; realistic spaced Unicode mod resource paths accepted, Windows device/trailing-dot/trailing-space aliases rejected. Pinned local build zero warnings/errors; /tmp/facegen-r13-117-local.json exit0: baseline31+journal21+campaign28+quota37=117. New category-specific provenance and explicit archive-priority selection fixture included. No dependencies/pins changed; no game assets downloaded. Third full Windows execution is justified by these real writer/interface changes; keep at most four total this iteration.

## Controlled FaceGen adapter Windows verification:205fb47

https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39335263895: Windows build job completed Success, including all169 mandatory integration checks (public notice), actual CLI/launcher cases, original/native builds, licenses and artifact guards. Portable117 prerequisites passed. Actual controlled DDS byte adapter passed reservation/checkpoint, escape, exhaustion, deferred directory, closed-scope and unsupported diagnostic tests. NIF serializer and material call-site adapters compiled; no actual FaceGen/NIF generation or complete hard-quota coverage claimed. No mass worker enabled.

### Final independent local-resource admission (fourth validation pending)

Read-only authentic preflight extended to original BA2 members without extracting to disk. Source/archive SHA and selected-member SHA/path are distinct report fields. Original GetPayloadSource bounds single-chunk GNRL/DX10 stored/decompressed bytes before ExtractToMemory; unsupported multi-chunk stays blocked. DDS cube flag explicitly rejected before native decoding. Added controlled packed DDS/provenance/unchanged-source/pool/missing-member/hash/2D-guard fixtures. This is not authentic game evidence; no real NIF/body/equipment exists in Cloud. Only two functional VB files changed since205fb47, syntax pass; existing117 portable assertions are unchanged and not redundantly rerun locally. This is the final fourth integrated execution for this session; no fifth if it fails.

## Final verified code checkpoint:e56a6db

https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39336072090 completed **Success**. Public integration notice confirms176 mandatory Windows checks. Portable117 prerequisites passed. All native/GUI/original/new CLI builds, actual BA2 sealing/reviewed discovery/export/page/Windows launcher commands, authentic-preflight rejection without output, licenses/provenance and bounded experimental artifact/diagnostic guards passed. Five artifacts reported. Exact tested source e56a6db; subsequent records-only changes do not require another full Windows build.

Read-only selected BA2 DDS/member provenance, unchanged source/no extraction, pre-extraction pool exhaustion, missing member, incorrect archive hash and single-face DX10 guard all passed controlled fixtures. Original NIF validator is compiled and negative-tested; no authentic positive NIF/FaceGen/body/equipment or in-game proof is claimed. Full R15 still requires actual GOTY/DLC/WoD and encoding-exception evidence. R11 supports only the four admitted deterministic buckets. R14 is static triage, not mission/audio compatibility.

R13 remains incomplete: stream and opt-in FaceGen outputs are constrained in their tested scope, but original ESP writer, batch snapshots/reports, aggregate copy, cache/temp/package/auxiliary paths and filesystem allocation overhead do not have complete enforced coverage. New storage-writers.json records12 boundaries, tested/compiled/uncovered routes and the next interface integration. Global mass execution stays disabled; this technical code work does not require uploading game data or user source edits. Actual asset/runtime evidence must be obtained in the authorized isolated local profile using LOCAL_AUTHENTIC_TRIAL.md.

Integrated executions this bounded iteration: dc669ba /39333204616 diagnosed source-stream ownership failure;033045f /39334233343 Success;205fb47 /39335263895 Success; e56a6db /39336072090 Success. No fifth execution or unchanged full rebuild. Source/dependency pins and license controls unchanged. Local inspector29 discovered (27 passed,two Windows-only skipped); portable117 local report /tmp/facegen-r13-117-local.json. Storage observed tools1.2GiB+repo24MiB plus small temporary syntax/HTML evidence, below2GiB cloud ceiling. 71d329a and57043d6 ancestry checked. No merges, releases, installations, game/save writes or recurring activation.

## R13 original ESP interface and controlled worker iteration (Windows pending)

Preserved e56a6db/3df9782. Added deterministic source overlay on unchanged Base Library pin8dc93e1: optional original SaveOverridePlugin output delegate, leaving all serialization/master cleanup logic intact. Original and patched canonical LF SHA256 plus pinned Git blob checked; unsupported source drift fails before modification. CI records overlay manifest and script with existing source/license provenance. No second serializer, dependency version replacement or install on the user's PC.

Shared controlled output routes NPC ESP/report, BatchJournal snapshots, aggregate ESP/report and resource copies through the existing exact-reservation stream gateway. Renaming a shard becomes a charged new copy with the original retained. Checkpoints/recovery require durable quota completion. Added one-target record diagnostic worker/CLI with exclusive quota journal; global and native FaceGen workers stay blocked. Caches/temp/package paths cannot be entered from this admitted controlled worker. Full native/physical-storage coverage remains false.

Local132 portable assertions passed (baseline31+journal21+campaign28+quota37+controlled writers15), /tmp/controlled-esp-portable.json. Pinned build exit0, zero warnings/errors. Local compiler found a globally visible helper named Relative colliding with a VB loop variable; renamed ToBudgetRelativePath. Syntax helper caught reserved Resume parameter and Partial fixture variable; renamed before Windows validation. No assertion suppressed. Original writer + adapters/CLI/gate syntax parsed successfully; actual Windows binding/runtime pending.
