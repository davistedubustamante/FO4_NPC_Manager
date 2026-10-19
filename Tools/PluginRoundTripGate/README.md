# Isolated NPC ESP integration gate — Windows only

Uses FO4 NPC Manager and the exact FO4_Base_Library revision/layout in the existing Windows workflow.
No game installation or proprietary assets required. The generated `inputs/Fallout4.esm` is an empty test placeholder; never install it over the real game master. Test inputs are generated binary fixtures.
The fixture NPC includes the required FO4 scalar subrecords; companion race/class/voice records are
minimal reference fixtures and are not a playable world or proof of in-game legality.

From the CI workspace (parent of the FO4_NPC_Manager checkout), use the existing VS MSBuild:

```powershell
msbuild FO4_NPC_Manager/Tools/PluginRoundTripGate/PluginRoundTripGate.vbproj -t:Restore,Build -p:Configuration=Release -p:Platform=x64
```

Run `PluginRoundTripGate.exe <new-staging-directory>` from the compiled win-x64 output. The gate writes
fixtures under `inputs/` and an isolated override under `patch/`, reopens using PluginReader, and verifies
identity/masters, female bit, unrelated fields and input hashes. Every existing output or output within
input is refused. Age evidence is explicit test-fixture metadata, not a real classifier. No resource
installation happens. `verification.json` only exists after verification succeeds; a failed staging
folder must not be used as a patch. All artifacts remain EXPERIMENTAL and NOT GAME VALIDATED.

Production implementation is `FO4_NPC_Manager/NPC/IsolatedNpcPatch.vb`, which reuses RecordParsers,
PluginManager and SaveNpcEspWriter. It refuses localized input, all template flags, missing/out-of-order
masters, unknown age and an existing female actor. Master cleanup is verified by reparsing the patch through the same PluginManager and emitting both trees
in a common global-FormID space with the existing canonical serializer. Header and subrecord structure
remain checked. This is fixture evidence, not a real-collection or full-script regression test.

## Integrated production and batch coverage

The gate also loads a female LooksMenu JSON preset through the original parser/category filter,
uses the original override/save-shadow/overlay functions, writes appearance morphs to ESP and reopens them.
It exercises mixed-batch continuation, profile/preset hash rejection and resumable checkpoints. Missing
real FaceGen resources must produce a blocked attempt with no checkpoint.

A separate two-NPC fixture exercises the original multientry writer through aggregate export, verifies
both NPC identities/content/appearance after readback, and tests aggregate hash tampering on resume.
The Windows workflow then invokes NpcConversionCli.exe against the generated two-NPC profile. These
are actual production API calls on generated records; they do not demonstrate a playable collection,
real NIF/DDS generation or quest/voice/body compatibility. The FaceGen generator requires licensed
local resources for positive integration evidence.
