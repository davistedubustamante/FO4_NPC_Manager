# Windows: isolated authentic resource preflight

Experimental; no complete conversion is certified. Keep this work outside `D:\CRU-Female-Lite`.
Use an already authorized isolated profile/resource view. No installation, VFS registration,
service, game/save modification, collection copy or upload is performed by these commands.
All private manifests and reports stay on the PC. Do not upload plugins, archives or resources.

## Explicit localized winners, including BA2

Use the existing campaign profile with its real ordered `LoadOrder`, `DataRoot`, all original
mod/game/save directories in `ProtectedRoots`, reviewed adult/race evidence and female presets.
Select the actual winning source for each plugin/language/kind in that isolated profile.
This is an explicit selection, not automatic inference of MO2/Vortex priority.
Do not list both a loose source and an archive source for the same table. No English fallback.

The `Localization` section can initially omit hashes:

```json
{
  "Language": "spanish",
  "Tables": [
    {
      "Plugin": "Fallout4.esm",
      "Kind": "strings",
      "FilePath": "D:\\AuthorizedProfile\\Data\\Fallout4 - Interface.ba2",
      "ResourcePath": "Strings\\Fallout4_spanish.strings"
    }
  ]
}
```

Names above illustrate syntax, not a claim that this archive contains that member in your game
version. Inspect the actual local archive index with the existing inspector. Include DLC/mod
owners and `dlstrings` / `ilstrings` when used. Loose tables instead have their absolute table
`FilePath` and an empty `ResourcePath`. Leave all original bytes unchanged.

With the verified Windows CLI from the experimental CI artifact:

```powershell
$cli = 'D:\FO4GlobalTools\NpcConversionCli.exe'
& $cli --seal-localization 'D:\FO4GlobalTrial\selected.private.json' 'D:\FO4GlobalTrial\sealed.private.json'
if ($LASTEXITCODE -ne 0) { throw 'Localization selection failed; do not export' }
& $cli --discover-reviewed 'D:\FO4GlobalTrial\sealed.private.json' 'D:\FO4GlobalTrial\reviewed-plan.private.json'
if ($LASTEXITCODE -ne 0) { throw 'Reviewed discovery failed' }
```

Alternatively use the existing fingerprinted `Invoke-GlobalNpc.ps1` with tasks `SealLocalization`
and `DiscoverReviewed`, `-CliSha256`, `-ProfilePath` and a new `-OutputPath`.
The sealer reads individual selected BA2 members in memory through the original library;
no whole archive is extracted. `Sha256` seals the source file/archive; `ResourceSha256` seals
that BA2 member. Original parser/export operation checks them again. It disables archive index
cache writes and restores resource context. A dedicated headless resource context is required
for BA2; no active archive dictionary from another GUI profile may be reused.

`--discover-reviewed` reuses winning inventory and the original canonical reference graph.
Findings/missing analysis block admission; known quest/scene/relationship dependencies require
individual review. A static candidate is never a runtime compatibility certificate. Existing
`--discover` remains inventory/planning only. Reviewed pages revalidate the same narrative route
on preparation/resume. No global asset executor is enabled.

## One real adult: read-only asset validation

Construct `trial.private.json` with:

- `CollectionIdentity`: exact WoD/CRU collection identity/version (not an assumed acronym).
- `IsolatedProfileEvidence`: identification of the authorized isolated profile.
- `Profile`: existing `NpcBatchProfile`, with `RequireFaceGen: true`, exactly one reviewed adult
  male target, actual `SourcePlugin`/local `ObjectId`, compatible female `AppearancePreset`,
  explicit `HumanRacePlugin`/`HumanRaceObjectId`, sealed localization and protected roots.
- `Resources`: explicit existing local loose/VFS resources, each `Role`, `FilePath`, `Sha256`.
  Required roles: `facegen-nif`, `facegen-dds`, `body-nif`, `equipment-nif`. Multiple DDS/equipment
  entries are allowed; maximum 64 files, each <=32 MiB, aggregate <=128 MiB. Use the isolated
  local resource view. Do not extract or duplicate the collection to satisfy this diagnostic.

Calculate selected-file hashes locally with `Get-FileHash -Algorithm SHA256`.

```powershell
& $cli --preflight-real 'D:\FO4GlobalTrial\trial.private.json' 'D:\FO4GlobalTrial\new-preflight-001'
if ($LASTEXITCODE -ne 0) { throw 'Asset/profile preflight failed' }
```

The report is written through the reserved-write gateway with a 1 MiB ceiling. Original
NIF reader checks FO4 version/root and nonempty geometry; native DirectXTex decodes bounded
2D DDS. Exact source hashes, sizes, geometry counts, dimensions, target input seal and narrative
state are recorded. Header/geometry/decode success does not establish female rig, skin weights,
morph/body/armor slots, preset identity, FaceGen correspondence, material/texture dependencies,
voice delivery or gameplay. These remain explicit report blockers. This command generates no ESP,
FaceGen, body or equipment files and installs nothing.

## Remaining gate before a genuine export trial

Do not start the mass worker or treat the diagnostic batch as hard-quota protected. The current
stream capability enforces pre-write reservations, seek/length/write bounds, durable completion
and recovery for controlled serializers. Original `SaveOverridePlugin` opens its own path through
`EscrituraEnElLugar`; FaceGen additionally calls `Save_As_Manolo`, DDS/TGA path writers and resource
index/material cache paths. These still need stream adapters or a verified pre-existing OS storage
isolation mechanism. The original NIF library exposes `Save_To_Bytes_Manolo`, a viable future
adapter; it is not silently substituted here. No OS quota/VHD was installed, mounted or activated.

After all those writes are constrained and tested, the next real trial must export one adult using
the original preset/record/FaceGen pipelines, reopen ESP in the frozen load order, validate every
produced NIF/DDS and resource reference, verify female body/equipment and preserve script/voice/
faction/package/narrative evidence. Then run isolated in-game tests using test saves and explicit
user authorization for any installation/activation. Missing resources or runtime evidence remain
blocked; no placeholder resource can satisfy authentic conversion acceptance.
