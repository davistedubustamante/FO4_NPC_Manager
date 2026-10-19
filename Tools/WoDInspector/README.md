# WoD / CRU local inspector — offline, read-only

Requires Python 3.10+ and Windows PowerShell/PowerShell, already installed by the user. No installer,
network request, extraction, game launch, save access or upload mechanism is included.
Run against explicit directories while the game and mod manager are idle. Example (replace paths):

```powershell
.\Tools\WoDInspector\Inspect-WoD.ps1 -GameRoot 'D:\Games\Fallout4\Data' -ModsRoot 'D:\MO2\mods' -ProfileRoot 'D:\MO2\profiles\Test' -OutputRoot 'E:\FO4-inspection\run-001' -ArchiveIndex
```

Output must be a **new** directory outside all input roots. The scanner refuses symlink/junction roots
and skips linked descendants, marking the result partial. It does not change originals or saved games.
Do not point it at save/profile roots containing saves unless you want filename metadata of those files;
no save contents are read, but use a profile configuration-only directory when possible.

`inventory.jsonl` contains relative filenames, sizes, modification times, plugin TES4 masters/flags and
load-order/profile list lines. `resources.jsonl` optionally lists BA2 member names/categories by reading
only bounded name tables, without reading/decompressing archive payloads. BA2 GNRL/DX10 versions
1/2/3/7/8 are recognized; other versions/types are reported as unsupported. BSA extraction/indexing,
virtual filesystem resolution, plugin NPC analysis and winning resource resolution are not implemented.
`summary.json` reports completeness/errors/skips and explicitly denies game compatibility validation.
`local-roots.private.json` binds root labels to actual local Windows directories for future resource
reading. It contains absolute private paths: keep it local and never share or commit it.
Exit codes: 0 complete inventory, 2 partial inventory, 1 invalid invocation/output or fatal error.
A partial result must never be treated as a full load-order or resource compatibility audit.

No 300 GB copy is necessary. Keep all outputs local. Review mod names, profile lines and paths for
privacy before sharing a small summary; never commit generated inventories, original plugins or assets.
The tool does not infer the meaning/version of WoD or collection compatibility from folder names.
Provide the collection URLs/versions separately. MO2's physical mod tree is not its winning virtual Data
view; a staged profile export or confirmed manager rules are needed for winner analysis.

Concurrent replacement of filesystem objects cannot be prevented by this offline script: inspect a
quiescent installation. Reparse checks and before/after metadata detect common problems, not a hostile
filesystem race. Large inventories stream JSONL; BA2 count/header limits make corrupt inputs fail closed.

Tests: `python -B -m unittest discover -s Tools/WoDInspector -v`. Fixtures are generated, not game assets;
Windows tests use real directory junctions to check reparse handling.
