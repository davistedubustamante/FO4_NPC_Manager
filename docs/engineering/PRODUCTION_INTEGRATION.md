# Headless production integration — experimental

The priority is a real editor pipeline. The offline inspector is useful preparation for real collection validation, not a prerequisite for implementing this adapter.

## Reuse audit

| Phase | Existing implementation reused | Integration |
|---|---|---|
| Input/winning overrides | PluginReader, PluginManager, RecordParsers.ParseNPC | Explicit load order; no implicit Plugins.txt |
| Edited sex and template categories | NpcRecordOverrideApplier | Real scalar applier plus original frozen Traits-only materialization; other inherited categories remain blocked |
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

Check the actual `AgeEvidence` enum before authoring a profile; numeric 1 denotes Adult. Race identity is explicitly approved by the profile author, not inferred by the tool. Object IDs are decimal JSON integers in their originating plugin: 24 bits for full plugins, 12 bits for ESL. Oversized IDs are rejected before the library can mask them to another NPC/race. No fixture master belongs in a real load order. Never upload proprietary inputs or private profiles. The adapter requires complete ordered masters and reviewed canonical NPC input. Localized sources need the explicit selected-language loose-table context; Traits-only inheritance uses the original frozen materializer. Other inherited categories and unresolved resources remain blocked. See LOCALIZED_AND_TRAITS.md.

## Result and recovery semantics

1–25 explicit targets. Each target first produces a uniquely named isolated ESP shard. When every target has a verified checkpoint, the original multientry writer consolidates them into `bundle-<id>/EXPERIMENTAL-NPC-Batch.esp`, reopens it, verifies all global identities/content/headers and copies only checkpointed generated resources into `bundle-<id>/Data`. Conflicting generated files fail export; no originals are copied or modified. If any target is blocked, no aggregate is exported. Failure for one target is recorded and the next runs. Each attempt uses a fresh directory; failed output is retained, never installed and never checkpointed. Resume checks the profile, every input plugin/preset/selected loose-table hash and prior output hashes, then skips verified checkpoints. A changed profile/input/output fails closed. Pipeline version is part of the identity (`production-batch-v7-reviewed-canonical-input`); checkpoints from before the ESL/live-adult-race/localization/Traits/canonical admission guards cannot bypass them on resume and are not migrated silently. Profile hashes do not prove adulthood, licensing or runtime compatibility. The original files must remain stable while a run is active. Plugin/preset identity is checked again before checkpointing and between targets; changed or unreadable inputs quarantine the batch. Mesh/texture/archive resource contents are not yet hash-sealed for resume; freeze the separate profile while validating and do not treat the plugin/preset fingerprint as an asset manifest.

`RequireFaceGen=false` is an explicit diagnostic mode. `RECORD-ONLY-VERIFIED` and `APPEARANCE-RECORD-VERIFIED-NO-FACEGEN` are incomplete conversions. FaceGen requires success, no skip, retained geometry, zero failed texture slots, nonempty emitted NIF/DDS, every required declared output from the original packer specs, and isolated paths before checkpointing. Even `FACEGEN-GENERATED-NOT-GAME-VALIDATED` is not proof of in-game appearance or collection compatibility. The original generator is integrated but cannot be exercised with real meshes/textures on the cloud machine.

`BatchJournal.Quarantine(reason)` provides logical rollback: preserve staging and mark it unusable. There is no installer and nothing to uninstall from the original game. Interrupted snapshots fail closed; recovery repair is deliberately not automatic. Hashes catch accidental changes, not an attacker able to rewrite the whole journal.

## Evidence boundaries and next work

CI uses generated binary plugins through production reader/overlay/writer and tests mixed-batch continuation/resume. The standalone journal tests are infrastructure tests, not conversion evidence. The resource index is initialized once per batch process with the explicit load order and a staging-only cache. An index error is retained as a blocker for later targets; it is not retried unchanged for every NPC.

Real human adult NPC → NIF/DDS → separate game profile remains blocked on locally available licensed resources. Body/skin delivery, armor model validation, voice/dialogue regressions and actual CRU/WoD compatibility are pending. Do not rename or distribute staged bundles as validated mods.

## CRU/WoD interoperability contract

Collection identity/version is still unknown; this is an integration contract, not a compatibility certification. The adapter consumes the winning plugin order supplied by the private profile and the resources visible under its Data root. It does not import a mod-manager collection, reorder plugins or resolve an entire collection from mod names.

For mod-manager deployments, multiple mod-storage directories alone are not a resolved Data view. Local evaluation needs an already authorized separate profile whose virtualized or staged Data exposes the actual winning loose files and active archives. The original resource index receives the explicit plugin list, not an inferred system Plugins.txt. Do not point it at an incomplete physical Data folder and assume all virtual mods were seen. Inspect the local resource-index errors and verify representative winning mesh/material/texture origins before accepting FaceGen.

FaceGen paths retain the originating NPC plugin identity using the original generator; changing the aggregate ESP filename does not change that identity. The aggregate overrides only selected NPC records, keeps their voice/script identities and ships generated resources separately. This reduces the edit surface but does not prove scene conditions, outfit models, body morph delivery or quest compatibility. Validation must record collection versions, runtime/F4SE/LooksMenu versions, exact load order, preset provenance and the locally frozen resource view. No collection installation, profile activation or save mutation is performed by this tool.
