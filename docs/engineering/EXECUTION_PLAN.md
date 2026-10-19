# Persistent execution plan

Branch: dev/npc-transformation-experimental. Baseline preserved: d09860b.
Latest runtime-verified code: 9adb4a6 (production batch/preset/aggregate/CLI fixture scope). No recurring scheduler is active.

## Completed engineering iteration (fixture scope)
- R01 verified: AGENTS, requirement DAG, blockers, evidence and resume instructions in Git; lightweight records CI passes.
- R02 verified on generated binary fixtures: load an NPC_ with the pinned library, write an isolated record-only ESP, reopen, resolve cleaned-master indexes, compare canonical global-FormID content, preserve headers/subrecord structure and input hashes. 21 assertions passed in Windows CI.
- R03 verified on generated fixtures: offline Windows inspector, bounded BA2 name indexes, private local root bindings, output protection and reparse handling. Windows: 20 tests passed including PowerShell; Linux: 19 passed, 1 Windows-only skipped.
- R04 prepared and inactive: reusable prompt, proposed schedule and advisory budgets; no callable Codex scheduler in this session and no activation performed.
- Full native/GUI Publish|x64/CLI Release|x64/integration-gate build passed at https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39193615511 (a0bf328).

## Completed production integration: R09 and R10 (binary-fixture scope)
9adb4a6 passed full Windows CI at https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39259948641: 37 integration assertions plus actual two-NPC CLI execution. Original reader, override applier, preset parser/filter, save shadow, ESP writer and FaceGen entry point are reused. Mixed failures, partial attempts, hash-sealed resume and one aggregate ESP are implemented. See PRODUCTION_INTEGRATION.md for APIs and evidence boundaries. Positive real FaceGen remains blocked, not certified by these checks.

## Current selected hardening: ESL identity validation
The original library masks light-plugin object IDs to 12 bits. Reject oversized NPC/race IDs before lookup; add positive ESL appearance readback and negative alias tests. Final bounded Windows execution pending the next code revision. Portable baseline 31 and journal 21 assertions pass locally. Preserve the successful 9adb4a6 checkpoint if the final validation exposes a new issue.

## Next feasible work and real-input acceptance
The inspector is not a development gate. Next implementation work includes frozen inherited-category resolutions through the existing materializer, candidate/profile discovery using the existing reader, and content fingerprints for referenced resources. Real acceptance needs one authorized adult human NPC and licensed local resources in a frozen separate Windows profile: staged ESP + NIF/DDS, then body/equipment and quest/dialogue regressions. R05–R08 remain blocked on their actual inputs/evidence, not on further code development. No collection upload or original installation/save mutation is required.

## Resume procedure
Read AGENTS.md, requirements.json, BLOCKERS.md and EVIDENCE.md. Inspect git status and actual CI before advancing state. Keep user work; no resets or automatic merges. Start from the first feasible pending requirement, not from a clean infrastructure rebuild. Use pinned reader/writer/editor functions. Run meaningful local and Windows checks; initial execution plus at most three diagnosis-driven retries (four total). Keep one active Windows run per code revision. At the cap record the diagnosis, checkpoint and switch to independent work; no unchanged retries or a fifth full build in that cycle.

Historical failed runs are retained in EVIDENCE.md. The final fix added global-space semantic verification after observing legitimate CleanMasters remapping; it did not remove content-preservation checks.

## Activation boundary
The automation proposal is disabled and is not a supported Codex import schema. Confirm actual scheduler capability, execution location, usage allowance, concurrency and pause controls, and obtain explicit approval before activation. No background coding continues merely because this plan exists.
