# Headless production integration — experimental

The priority is a real editor pipeline. The offline inspector is useful preparation for real collection validation, not a prerequisite for implementing this adapter.

## Reuse audit

| Phase | Existing implementation reused | Integration |
|---|---|---|
| Input/winning overrides | PluginReader, PluginManager, RecordParsers.ParseNPC | Explicit load order; no implicit Plugins.txt |
| Edited sex and template categories | NpcRecordOverrideApplier | Real scalar applier; inherited categories still rejected until frozen materialization resolutions are supplied |
| Preset parser/category selection | LooksmenuLoader.ParseFile, PresetCategoryFilter.BuildFiltered | Female LooksMenu preset; face categories selected, body/runtime channels refused |
| Save shadow | NpcOverrideSaver.ComposeSaveShadow, NpcRecordOverlay.AplicarOverlay | Original materialize → overlay → scalar order; edited session sex reflected before overlay |
| ESP export/readback | SaveNpcEspWriter.SaveOverridePlugin, PluginReader, canonical body emitter | Same serializer as GUI; semantic verification across master cleanup |
| FaceGen | FaceGenBuilder.BuildCharGen, NpcRenderContext, NpcMaterialResolver | Reopen/merge saved patch before bake, empty preset overlay to avoid double application, explicit staging output |
| Recovery | BatchJournal | Durable exclusive snapshots, profile/input SHA256, output hashes, partial attempt retention |

`NpcOverrideSaver.ExecuteAsync` also exists as an original batch orchestrator. It couples export to `SaveEsp_Form.SaveTarget` and can install scripts in Data. This adapter reuses its production shadow and writer phases directly to avoid those installation/UI side effects. It does not add a second plugin serializer or FaceGen generator.

## Windows invocation

Use `EXPERIMENTAL-Conversion-CLI/NpcConversionCli.exe` from the Windows build artifact. Run it as a dedicated process, with a private profile and an existing staging parent outside every game/mod/save root:

```powershell
.\NpcConversionCli.exe C:\Private\npc-profile.private.json D:\NPC-Staging
# Resume using the directory printed by the first invocation:
.\NpcConversionCli.exe C:\Private\npc-profile.private.json D:\NPC-Staging D:\NPC-Staging\npc-experimental-<id>
```

Save the private profile outside the repository. `*.private.json` and `npc-experimental-*` staging directories are ignored by Git.

Profile shape (paths and identities are examples; do not run against an original installation without a separate authorized profile):

```json
{
  "DataRoot": "D:\\SeparateProfile\\Data",
  "LoadOrder": ["Fallout4.esm", "AuthorizedSource.esp"],
  "ProtectedRoots": ["D:\\SeparateProfile", "D:\\OriginalGame", "D:\\ModStorage", "C:\\PrivateSaves"],
  "RequireFaceGen": true,
  "Targets": [{
    "SourcePlugin": "AuthorizedSource.esp", "ObjectId": 2048,
    "Age": 1, "AdultEvidence": "Replace with reviewed source evidence of adulthood",
    "AppearancePreset": "C:\\Private\\adult-female-preset.json",
    "HumanRacePlugin": "Fallout4.esm", "HumanRaceObjectId": 88046
  }]
}
```

Check the actual `AgeEvidence` enum before authoring a profile; numeric 1 denotes Adult. Race identity is explicitly approved by the profile author, not inferred by the tool. Object IDs are decimal JSON integers. No fixture master belongs in a real load order. Never upload proprietary inputs or private profiles. The adapter currently requires readable nonlocalized plugins, complete ordered masters and noninherited NPC records.

## Result and recovery semantics

1–25 explicit targets; one isolated ESP shard per NPC, not an aggregate ESP yet. Failure for one target is recorded and the next runs. Each attempt uses a fresh directory; failed output is retained, never installed and never checkpointed. Resume checks the profile, every input plugin/preset hash and prior output hashes, then skips verified checkpoints. A changed profile/input/output fails closed. Profile hashes do not prove adulthood, licensing or runtime compatibility. The original files must remain stable while a run is active. Plugin/preset identity is checked again before checkpointing and between targets; changed or unreadable inputs quarantine the batch. Mesh/texture/archive resource contents are not yet hash-sealed for resume; freeze the separate profile while validating and do not treat the plugin/preset fingerprint as an asset manifest.

`RequireFaceGen=false` is an explicit diagnostic mode. `RECORD-ONLY-VERIFIED` and `APPEARANCE-RECORD-VERIFIED-NO-FACEGEN` are incomplete conversions. FaceGen requires success, no skip, retained geometry, zero failed texture slots, nonempty emitted NIF/DDS, every required declared output from the original packer specs, and isolated paths before checkpointing. Even `FACEGEN-GENERATED-NOT-GAME-VALIDATED` is not proof of in-game appearance or collection compatibility. The original generator is integrated but cannot be exercised with real meshes/textures on the cloud machine.

`BatchJournal.Quarantine(reason)` provides logical rollback: preserve staging and mark it unusable. There is no installer and nothing to uninstall from the original game. Interrupted snapshots fail closed; recovery repair is deliberately not automatic. Hashes catch accidental changes, not an attacker able to rewrite the whole journal.

## Evidence boundaries and next work

CI uses generated binary plugins through production reader/overlay/writer and tests mixed-batch continuation/resume. The standalone journal tests are infrastructure tests, not conversion evidence. Real human adult NPC → NIF/DDS → separate game profile remains blocked on locally available licensed resources. Body/skin delivery, armor model validation, voice/dialogue regressions and actual CRU/WoD compatibility are pending. Do not rename or distribute staged bundles as validated mods.
