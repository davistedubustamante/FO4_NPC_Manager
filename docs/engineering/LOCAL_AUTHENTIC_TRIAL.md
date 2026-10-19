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
  male target (`Age: 1`, reviewed `AdultEvidence`), actual `SourcePlugin`/local `ObjectId`, compatible female `AppearancePreset`,
  explicit `HumanRacePlugin`/`HumanRaceObjectId`, sealed localization and protected roots.
- `Resources`: explicit existing local resources, each `Role`, `FilePath`, `Sha256` and optional `ResourcePath`.
  Required roles: `facegen-nif`, `facegen-dds`, `body-nif`, `equipment-nif`. Multiple DDS/equipment
  entries are allowed; maximum 64 selected resources, each <=32 MiB, aggregate <=128 MiB.
  `FilePath` points to a loose NIF/DDS or the original BA2 (<=16 GiB); `Sha256` seals that source
  file/archive. For BA2, `ResourcePath` selects the exact existing member; no member is extracted
  to disk. Original single-chunk GNRL/DX10 APIs are used, with pre-extraction stored/decompressed
  byte bounds and 2D DDS limits. Multi-chunk/unsupported layouts remain blocked. Do not extract
  or duplicate the collection to satisfy this diagnostic.

Calculate selected-source hashes locally with `Get-FileHash -Algorithm SHA256`. For an archive
member, hash the archive, not a separately extracted copy. Example resource entry (replace the
path/member with one actually present in the local index):

```json
{
  "Role": "facegen-dds",
  "FilePath": "D:\\AuthorizedProfile\\Data\\SelectedResources.ba2",
  "Sha256": "<SHA256 of the original archive>",
  "ResourcePath": "Textures/Actors/Character/FaceCustomization/Fallout4.esm/<actual-member>.dds"
}
```

The report records both archive/source and logical NIF/DDS SHA256 plus member identity. A
matching source hash seals the bytes used for extraction; it does not prove that they match the
female preset or body. Keep that distinction when supplying resources.

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
`EscrituraEnElLugar`; FaceGen has an opt-in dedicated output adapter: original `Save_To_Bytes_Manolo` serializes NIF
in memory; DDS/material bytes use exact reservations, directory creation is deferred, and TGA
diagnostics fail closed. It is not enabled as a batch-wide quota: original ESP/cache/temp and
other auxiliary paths still need verified stream adapters or pre-existing OS storage isolation.
Actual NIF/FaceGen generation under this adapter remains unverified without local resources. No OS quota/VHD was installed, mounted or activated.

After all those writes are constrained and tested, the next real trial must export one adult using
the original preset/record/FaceGen pipelines, reopen ESP in the frozen load order, validate every
produced NIF/DDS and resource reference, verify female body/equipment and preserve script/voice/
faction/package/narrative evidence. Then run isolated in-game tests using test saves and explicit
user authorization for any installation/activation. Missing resources or runtime evidence remain
blocked; no placeholder resource can satisfy authentic conversion acceptance.

## Quota-controlled record trial (not full FaceGen conversion)

The new `--controlled-record` command accepts the existing **NpcBatchProfile**, not the campaign
or AuthenticTrialInput document. Prepare exactly one reviewed adult human target using the
existing prepared-page profile; set `RequireFaceGen` to `false`. Preserve its frozen load order,
preset hashes, localization selection and protected input roots. Never remove an admission
failure to force a record through. Use a fresh output directory outside every source root:

```powershell
& $cli --controlled-record 'D:\FO4GlobalTrial\one-npc.private.json' 'D:\FO4GlobalTrial\record-attempt-01' 67108864
if ($LASTEXITCODE -ne 0) { throw 'Trial blocked; retain its journal for diagnosis' }
& $cli --controlled-record 'D:\FO4GlobalTrial\one-npc.private.json' 'D:\FO4GlobalTrial\record-attempt-01' 67108864 --resume
if ($LASTEXITCODE -ne 0) { throw 'Recovery failed; do not install output' }
```

The numeric ceiling includes payload files and the reservation ledger as logical bytes, not NTFS
allocation overhead. The original ESP serializer emits through a bounded sink; shards, consolidated
ESP, report/checkpoint snapshots and copied resources share the same ledger. Failed/partial writes
retain their reservation. Existing files cannot be overwritten or reclaimed to bypass the ceiling.
Do not manually repair/delete a ledger or retry in the same workspace without `--resume`.

`RequireFaceGen=true` is rejected before workspace creation in this command. The legacy positional
CLI path does **not** provide this aggregate quota and must not be used to bypass that restriction.
Neither command installs anything. A successful controlled record trial is only ESP/preset evidence:
it does not establish generated NIF/DDS, body/equipment fit, dialogue or in-game correctness.
The complete authentic conversion remains blocked until the native/cache/auxiliary write audit and
full-generation validation are complete. Keep real-resource reports local.
