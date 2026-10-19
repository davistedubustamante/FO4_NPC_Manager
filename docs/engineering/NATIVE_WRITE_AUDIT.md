# R13 native / auxiliary write audit

Experimental; completeHardQuotaVerified=false. No full FaceGen worker is admitted.
The byte ceiling covers payload/ledger lengths, not physical filesystem allocation overhead.

| Entry point | Actual writer | Controlled policy / remaining proof |
| --- | --- | --- |
| SaveNpcEspWriter.SaveOverridePlugin | original TES4/GRUP serializer | SHA-verified optional stream interface; bounded sink then gateway; no parallel serializer |
| Journal / aggregate / shard reports | ControlledFileOutput | bounded serialization, exact reservation, durable completion; partial charges retained |
| Aggregate resources / retained shards | ControlledFileOutput.CopyNew | source length/hash, exact stream limit, create-new only; source never moved/deleted |
| FaceGen DDS | original native byte encoders then FaceGenOutputAdapter | seven byte call sites; existing DDS fixture; full genuine generator still missing |
| FaceGen NIF | original Save_To_Bytes_Manolo then adapter | two call sites; positive genuine output still missing; Save_As_Manolo not admitted |
| Material copies | NpcMaterialResolver adapter | two byte call sites; genuine material dependency trial missing |
| Stale FO4/SSE FaceGen cleanup | two File.Delete sites in FaceGenBuilder | admission helper retains charged files; validates root; closed controlled scope rejects deletion |
| Morph debug dump | NpcMorphPoseResolver File.WriteAllBytes | disabled under controlled record/FaceGen scopes even if Logger is enabled |
| Diagnostic TGA | original FaceTintCompositor file helper | rejected before invocation in controlled FaceGen scope |
| Archive index cache | FilesDictionary_class SaveArchiveIndex / stale pruning | original empty CacheDirectory disables persistence; current controlled worker blocks before indexing; full-generation scope must explicitly disable and restore cache |
| Material file save / transactions | original FO4UnifiedMaterial_Class and EscrituraEnElLugar | Save_To_Shader edits in-memory NIF; separate file/transaction APIs not admitted; prove full reachable call tree before generation |
| Texture helpers accepting filenames | original DirectXTextureConversionHelper | byte-returning methods are used by adapted builder; filename save methods not admitted |
| Packages / BA2 / arbitrary native handles | original library path APIs | not admitted; no package writer or global worker under this quota |

The dedicated record trial is single-NPC and excludes RequireFaceGen=true before workspace
creation. Existing GUI / legacy positional CLI are not certified aggregate-quota executors.
No fallback through them is part of the trial procedure. Full native generation must use one
fresh attempt root, the same gateway for every admitted sink, disabled disk caches/logging/debug
outputs, and a verified input resource set. Static call-site review alone is not runtime proof.

Next acceptance: a genuine NIF/DDS/material generation run under the shared quota; inject
exhaustion/interruption at its actual writers; validate all emitted assets and references with
original readers; prove no auxiliary path escapes before removing the FaceGen admission guard.
Authentic inputs stay on the authorized isolated Windows PC; never upload the collection.
