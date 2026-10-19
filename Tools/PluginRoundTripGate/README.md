# Isolated NPC ESP integration gate — Windows only

Uses FO4 NPC Manager and the exact FO4_Base_Library revision/layout in the existing Windows workflow.
No game installation or proprietary assets required. Test inputs are generated binary fixtures.
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
masters, unknown age and an existing female actor. Complex master remapping that changes unrelated raw
bytes is rejected rather than accepted without a semantic comparison. The next task is a typed,
load-order-aware verification adapter; don't remove preservation assertions to pass a real mod.
