# Persistent execution plan

Branch: dev/npc-transformation-experimental. Baseline preserved: d09860b.
Latest runtime-verified code: a0bf328. No recurring scheduler is active.

## Completed engineering iteration (fixture scope)
- R01 verified: AGENTS, requirement DAG, blockers, evidence and resume instructions in Git; lightweight records CI passes.
- R02 verified on generated binary fixtures: load an NPC_ with the pinned library, write an isolated record-only ESP, reopen, resolve cleaned-master indexes, compare canonical global-FormID content, preserve headers/subrecord structure and input hashes. 21 assertions passed in Windows CI.
- R03 verified on generated fixtures: offline Windows inspector, bounded BA2 name indexes, private local root bindings, output protection and reparse handling. Windows: 20 tests passed including PowerShell; Linux: 19 passed, 1 Windows-only skipped.
- R04 prepared and inactive: reusable prompt, proposed schedule and advisory budgets; no callable Codex scheduler in this session and no activation performed.
- Full native/GUI Publish|x64/CLI Release|x64/integration-gate build passed at https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39193615511 (a0bf328).

## Next selected requirement: R05 — real collection adapter
Blocked by B01/B02: needs reviewed collection URLs/versions and a local inspection of the actual Windows profile. User runs Tools/WoDInspector/Inspect-WoD.ps1 locally, reviews summary.json and selected metadata, and keeps all plugins/assets/private root bindings local. Do not upload 300 GB or request copyrighted resource bundles.

When metadata is available: establish winning plugin/resource order and runtime versions; inspect representative NPC records read-only; require explicit adulthood evidence and materialize inherited categories before authoring. Extend binary-fixture coverage for actual script properties and load-order variations before admitting those inputs. Then one real NPC staged patch -> complete FaceGen NIF/DDS -> body/equipment -> voice/dialogue/quest regressions -> small batch. Every stage needs its own evidence; no original installations or saves are modified.

## Resume procedure
Read AGENTS.md, requirements.json, BLOCKERS.md and EVIDENCE.md. Inspect git status and actual CI before advancing state. Keep user work; no resets or automatic merges. Start from the first feasible pending requirement, not from a clean infrastructure rebuild. Use pinned reader/writer/editor functions. Run meaningful local and Windows checks; initial execution plus at most three diagnosis-driven retries (four total). Keep one active Windows run per code revision. At the cap record the diagnosis, checkpoint and switch to independent work; no unchanged retries or a fifth full build in that cycle.

Historical failed runs are retained in EVIDENCE.md. The final fix added global-space semantic verification after observing legitimate CleanMasters remapping; it did not remove content-preservation checks.

## Activation boundary
The automation proposal is disabled and is not a supported Codex import schema. Confirm actual scheduler capability, execution location, usage allowance, concurrency and pause controls, and obtain explicit approval before activation. No background coding continues merely because this plan exists.
