# Persistent execution plan

Branch: dev/npc-transformation-experimental. Preserve 74289ec, d1e483f and all prior progress. No merge, release, installation, original game/save edits or recurring automation activation.

## Goal and verified checkpoints

Final target: female conversion across Fallout 4 GOTY, DLC and every active WoD plugin, with separate verified redesign paths for special categories. GLOBAL_CONVERTER.md defines the architecture. R16 is final certification and remains pending; inventory/plans/flag edits are never global conversion proof. No dependence on the historical three Lite mods.

Latest runtime-verified code: e71248f at https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39272442946 (Success). Native, GUI, original/new CLI, 72 integration assertions, actual discovery and automatic page preparation passed. Earlier verified checkpoint d1e483f remains preserved.

- R01: persistent recovery records, DAG, retry and authorization boundaries verified.
- R02/R09: original reader, overrides, preset parser/filter/save shadow, writer, semantic readback and staged FaceGen entry point reused. Positive real FaceGen is not certified.
- R03: local read-only inspector verified on fixtures; actual installation data remains local/unavailable in cloud.
- R04: recurring proposal prepared, disabled; no callable scheduler was available.
- R10: aggregate ESP and controlled checkpointed file-copy/dedup/conflict/tamper tests passed. Controlled .bin files prove copy semantics, not NIF/DDS or in-game compatibility.
- R12: winning NPC discovery/lineage, reviewed category/adult policies, child/unknown/deleted/ESL/stale/localized admission and deterministic pages verified on generated binary fixtures. Portable planner exercises 10,000 entries. This is not 10,000 game conversions.

## Current verification

Verified e71248f: original loader sigFilter, loaded-record count, shared preset admission and distinct actual-inventory/policy bounds. Windows assertions passed: 72; portable baseline 31 + journal 21 + campaign 23. Local pinned SDK rebuild/runtime and inspector checks pass. No new dependency installation or pin changes.

Global iteration attempts: 1 failed harness compile (sr/Sr shadowing); 2 failed harness compile (copyJournal/CopyJournal shadowing); 3 fully passed; 4 final refinement fully passed. Four-execution limit respected; no fifth full build. Both compilation failures and their causes remain in EVIDENCE.md. Documentation-only changes use engineering-records CI.

## Next feasible implementation

1. R15 localization: remove the current verified export boundary only after explicit isolated language selection, table/resource provenance and missing-table/ID failures are tested. Reuse PluginManager.TryResolveLocalizedString, CanonResolver.TryText and canonical writer findings. Do not add string parsers or rely silently on INI/UI-culture/English fallback. Generate controlled string fixtures; actual game tables remain local.
2. R11 inherited Traits: reuse the existing chain resolver and frozen materializer. Include nonempty VMAD and preserve voice/other inherited categories. Unknown/cyclic/unresolved chains remain blocked.
3. R14 narrative/special routes: original record APIs for quests, aliases, scenes, relationships and voices. Companion faction/key and Unique/Essential/VMAD signals are triage, not complete dependency analysis. Robots/creatures/child redesign need independent proof.
4. R13 campaign executor: campaign/page identity and output hashes, one sequential worker, pause/resume and hard output quota before enabling writer/FaceGen. Estimates/post-write checks are not quotas. Depends on actual asset/body and admission evidence.
5. R16: reconcile actual complete GOTY/DLC/WoD profile and runtime-only coverage; certify real bundles, missions/scenes/voices and category-specific redesign. No unverified target silently counted converted.

The inspector is a helper, not a development gate. Continue independent code work when local game resources are missing. Actual positive FaceGen/body/equipment/runtime tests require a frozen authorized separate Windows profile with licensed resources. Never upload or duplicate the 300 GB collection.

## Storage and resume

Reuse SDK/pinned caches; cloud working-set ceiling 2 GiB (observed tools 1.2 GiB + repository 22 MiB). Bounded new Windows artifacts at most 256 MiB plus portable matrix evidence at most 1 MiB/job; retention two days; do not delete existing user artifacts. Discovery writes bounded metadata only. Production v4/global plan v4 reject older admission fingerprints without deleting prior staging.

On resume read AGENTS.md, requirements.json, BLOCKERS.md and EVIDENCE.md; inspect Git and actual CI, then select first viable pending requirement. Preserve user work. Initial attempt plus at most three diagnosis-driven retries per failing step, one active Windows revision. Do not rebuild unchanged passing code. No background work is implied by this plan or the disabled automation proposal.
