# Global female conversion architecture — experimental

## Scope and evidence

Target: Fallout 4 GOTY, DLC and every active WoD plugin supplied in the actual load order. Preserve 74289ec and its production reader, overrides, LooksMenu parser, writer, FaceGen and isolated export. There is no dependency on the three historical Lite mods. A global plan is **not** a converted collection.

The first global milestone discovers winning NPC records, classifies reviewed identities, assigns a stable preset and creates deterministic small pages for the existing production pipeline. The global executor, complete resource bundle and real collection certification are pending. Production batch v4 seals invalidate old checkpoints across the stricter live/adult-race admission rules, retaining old staging without automatic migration. Existing manual CLI export remains experimental; it does not inherit a new hard filesystem quota merely because a global plan exists.

## Pipeline

1. Read explicit active load order through original PluginManager; validate masters and FileID allocation. Retain one winning NPC_ row, originating plugin/local ID and ordered override lineage. Hash original plugin and preset bytes before/after discovery. Detect localized input as an explicit current export blocker (R15): discovery still works, but page preparation refuses an order the existing writer adapter cannot accept. No asset scan, extraction, copy or upload.
2. Read sex, Unique/Essential, templates and factions through original NPC parser; Child through the generated FO4 race view, with shared-race metadata cached for the scan. Missing/deleted races are blocked. The direct production export also rejects child/deleted races even when a caller supplies contradictory adult evidence. Keep deleted/unreadable entries in coverage with blockers. Companion faction/key and race policies are profile-specific reviewed metadata. Unknown mod races stay unclassified; names/level/looks never prove age.
3. Require explicit per-NPC adult evidence, known race flags and an approved human race. Child race/key wins over contradictory adult evidence. Unknown adulthood is blocked. Script-bearing/essential/special/unique/companion NPCs get individual quest/scene/voice review, not generic conversion. Nonempty VMAD is conservatively flagged, not claimed as a complete script/quest graph analysis.
4. Generic adult males with resolved categories get a stable preset chosen from the reviewed female pool by hash of originating identity. The real LooksMenu parser rejects malformed/nonfemale presets. This is a planned route, not a claim that all preset assets or body channels are valid.
5. Sort canonical identities, segment 1–25 per page, default 10. Store plan/input fingerprints, decision/reason counts and zero converted status. 10,000-row tests cover paging, order independence, category separation and exclusions. The original pipeline still validates each submitted page; complete FaceGen stays required for page preparation.
6. Re-scan and reject stale plan/policy before exporting a page as NpcBatchProfile. Reuse ProductionNpcBatch and its journal/ESP consolidation; do not add serializers. Automatic unattended global execution is not enabled until storage writes, referenced assets and narrative regressions have verifiable controls.
7. Future execution journal: campaign identity + page identity + completed output hashes; sequential worker, bounded page/time/storage reservations, pause before next page, no retry without diagnosis. Keep partial attempts quarantined. The v3 live-adult-race plan fingerprint invalidates earlier plans when admission rules change. R13 tracks execution rather than assuming planner pages are executed.

## Category routes

| Category | Current route | Required later evidence |
|---|---|---|
| Generic adult human | Planned production record/preset/FaceGen page | Real NIF/DDS, body, armor slots/female models, resource provenance, runtime |
| Unique human | Individual review | Quest aliases, relationships, scenes, conditions and voice identity |
| Companion | Faction/key policy → individual review | Companion systems, affinity, scenes, equipment and voices |
| Special/scripted/essential human | Individual review | Nonempty VMAD and quest/scene reference graph; gameplay regressions |
| Child / adulthood unknown | Blocked | Independently reviewed adult redesign; never a flag-only transformation |
| Robot | Separate redesign pending | Rig, meshes, voices, behavior and gameplay |
| Creature | Separate redesign pending | Species/rig/animation/gameplay; never female-bit success |
| Unknown race / unresolved templates | Blocked | Reviewed race policy / original frozen materializer |
| Already female | No conversion certification | Still needs integral body/FaceGen/runtime compatibility audit |

Companion detection only covers configured faction/key evidence, not all possible quest-driven companions. Quest references without NPC scripts are not yet analyzed. Winning override lineage describes conflicts; it does not automatically resolve their intent.

## Local Windows commands

Use the experimental NpcConversionCli from CI in an authorized isolated profile exposing the winning virtual Data view. Keep private JSON outside Git; no 300 GB copy is needed.

```powershell
.\NpcConversionCli.exe --discover C:\Private\campaign.private.json D:\Reports\inventory.private.json
.\NpcConversionCli.exe --prepare-page C:\Private\campaign.private.json D:\Reports\inventory.private.json 0 D:\Reports\page-0000.private.json
```

Campaign input uses DataRoot, LoadOrder, ProtectedRoots and Policy. Policy fields: HumanRaces, RobotRaces, CreatureRaces (canonical `PLUGIN.ESM|000123` identities), AdultEvidence dictionary keyed by NPC originating identity, ChildKeys, CompanionKeys, CompanionFactions, SpecialKeys, FemalePresets, BatchSize, MaxRecords, MaxReportBytes and MaxPluginBytes. Race policy alone does not establish adulthood. Empty approval policies intentionally produce an inventory of blocked cases rather than guessed conversions. The former three-argument manual export invocation is preserved.

## Storage and resource contract

- Reuse the existing SDK and pinned sources; no new installation, collection download or archive extraction. Cloud working set observed about 1.3 GiB at start; session budget 2 GiB, monitored with `du`/`df`. Existing remote artifact usage is unknown and is not claimed to be under this new budget.
- Campaign policy input at most 16 MiB and saved plan at most 64 MiB in the CLI. Default discovery input ceiling 4 GiB (maximum 8 GiB) of plugin bytes, checked before loader allocation; at most 100,000 NPC rows; preset pool at most 256 files / 64 MiB total, at most 4 MiB each. These are input/count bounds, not a measured RAM guarantee for the original loader. Oversized profiles fail explicitly; full streaming reader optimization remains future work.
- Default report/page ceiling 16 MiB, maximum configurable 64 MiB, checked before opening a new output. Never overwrite outputs or write into protected roots. This bounds each generated metadata file, not arbitrary caller-created files.
- No global FaceGen writes are launched by discovery/preparation. A hard output quota or bounded producer API is required before enabling the global executor. Current generator/writer APIs own their file handles; preflight size estimates or post-write checks are not hard disk quotas. Local proof of quota exhaustion/recovery is pending.
- CI new binary + fixture + reserved diagnostics payload capped at 256 MiB/run; fixture capped 16 MiB and diagnostics capped 16 MiB. At most four executions per failing step; new artifact retention two days; portable evidence capped at 1 MiB per matrix job. Existing artifacts are not deleted. Diagnostic omissions are recorded if binary logs exceed budget; text logs are preferred.

## Required local tests

Verify actual GOTY/DLC/WoD completeness, winning resources in the isolated mod-manager Data view, reviewed age/race/companion policies, real presets/FaceGen, body/armor compatibility and quest/voice/scene behavior. Validate one generic adult first, then small batches, then campaign scale and special categories. No installation, merge, release, paid service or save modification is performed by this milestone.

## Existing localization API audit (R15)

The base library already exposes PluginManager.TryResolveLocalizedString, CanonResolver.TryText and the canonical writer's TextoLocalizadoSinResolver findings. SaveNpcEspWriter.CuerpoDeNpcSobrescrito forwards missing-string findings into its warning list. Reuse these rather than implementing string readers. The current isolated adapter intentionally rejects localized load orders before this stage. Removing that guard alone is insufficient: LocalizedStringResolver selects INI/UI-culture/English fallback and the active resource dictionary, so isolated language selection, winning table provenance and content fingerprints must be explicit and tested before enabling export. R15 records that work; actual game tables stay local.

R16 is the final certification boundary. The current NPC_ inventory does not observe runtime-only actors created by scripts/F4SE, placed-instance state, or quest aliases beyond the loaded base records. Those limitations must be reconciled against the actual collection rather than hidden in a global coverage percentage.
