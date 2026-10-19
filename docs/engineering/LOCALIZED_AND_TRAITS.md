# Localized records, Traits and safe preparation

Experimental branch only. This is an engineering route for original-reader/writer fixtures; it does not certify GOTY/DLC/WoD gameplay or global female conversion. See EVIDENCE.md for exact tested commits and CI outcomes.

## Explicit language resources (R15)

`CampaignInput` and `NpcBatchProfile` accept `Localization`. Example private fragment:

```json
{
  "Localization": {
    "Language": "spanish",
    "Tables": [
      {
        "Plugin": "Fallout4.esm",
        "Kind": "strings",
        "FilePath": "C:\\NpcResources\\Strings\\Fallout4_spanish.STRINGS",
        "Sha256": "<64 hexadecimal SHA256 characters from the existing file>"
      }
    ]
  }
}
```

`Kind` is `strings`, `dlstrings` or `ilstrings`. Plugin must belong to the explicit load order; basename must match plugin/language/kind. The Windows/VFS profile helper discovers existing selected loose tables and hashes them without copying them. Hashes prove selected bytes, not authorship; resource selection must represent the actual frozen active resource view.

`LocalizedExportScope` reuses the pinned original resolver through its original resource dictionary. Only selected tables are visible, so INI/UI/English alternatives cannot silently select another language. Table handles deny concurrent modification on Windows; input hashes are checked again. The scope restores prior dictionary, resource root and transient output encoding. This is a dedicated headless context; do not invoke alongside the GUI renderer in the same process.

The existing writer produces a self-contained **nonlocalized ESP with selected-language text inline**. Original masters, their external numeric string IDs and voice/script records remain untouched. Verification records source string ID, source plugin, field/table kind and resolved-text hash. It never writes an external string ID into a file declaring inline strings. A different output language requires regeneration; this is not a multilingual localized ESP writer.

Boundaries:

- No explicit resource context: localized input remains blocked.
- Wrong/missing table, ID, hash, owner, language or masters: fail closed, before creating the ESP for admission failures.
- Tables: 1..4096 selected files, each at most 32 MiB, pool at most 128 MiB; no reparse paths.
- Unsealed `.cpoverride` and persistent global localization/general encoding overrides are rejected. UTF8 output is scoped and restored.
- A nonzero ID resolving to empty text still produces an original-writer finding and remains blocked; zero is the legitimate empty sentinel.
- BA2-only strings are not yet admitted by this explicit manifest route. Reuse original archive/index APIs next; do not add a BA2 or string parser. Archive selection, bounded extraction and resource provenance need controlled positive fixtures.
- Mixed per-plugin inline encoding overrides and real GOTY/DLC resource combinations need additional explicit regression evidence. No broad collection certification from current fixtures.

## Strict original Traits materialization (R11)

`StrictTraitsMaterialization` delegates to the existing `ProbeCategoryOwn`, `CongelarResolucion` and `MakeCategoryOwn`. It never implements a second chain resolver or materializer. Work occurs on a shadow; effective race/voice are copied through the original complete Traits bucket, the Traits bit is cleared, then the existing edit/preset/save path runs.

Only Traits-only deterministic NPC-to-NPC chains are admitted in this iteration. Missing/deleted sources, cycles, duplicate pointer blocks, incomplete terminal race/voice, non-NPC/leveled-list sources and other inherited categories remain blocked. The editor's permissive dead-pointer fallback is not automatic export evidence. Child race is checked on the materialized effective NPC; unknown adulthood still requires explicit reviewed evidence.

Fixtures preserve target nonempty VMAD, factions, inventory and packages while comparing canonical global content after master cleanup. NPC identities and original plugin hashes are checked. These are record-preservation tests, not proof of FaceGen, quests or game behavior.

All exported NPCs and Traits sources must have a reviewed canonical parse. Unconsumed/opaque fields can hide FormIDs that master cleanup cannot remap. Reopening a file or comparing uninterpreted bytes cannot prove those references safe; admission rejects original canonical findings. Production/global admission fingerprints v7 reject older checkpoints/plans without deleting prior staging.

## Static narrative impact (R14)

`--analyze-narrative <campaign.private.json> <new-report.private.json>` uses `CanonBridge.Tree` and `WbFormIdWalker`, including quests/aliases, scenes, relationships, voices, packages, dialogue and placed references under CELL/WRLD. Report edges carry readable canonical `Field` and explicit `Subrecord` identity; readable field names do not necessarily contain their signatures.

Reverse transitive references require individual review of linked NPCs. Existing category/adult policy distinguishes generic, unique, companion and special routes. Effective voice is recorded, not replaced. Parser findings and references outside scanned signatures remain visible; an outside-view reference is not certified missing. Record, edge, transitive impact and report byte budgets fail closed.

`ReviewRequired` is static narrative triage, not admission for conversion. Absence of a static edge is not quest compatibility. Runtime aliases/conditions, dynamic Papyrus lookups, audio/dialogue delivery and actual quest/scene execution remain unvalidated. The graph is not yet a complete campaign execution-admission policy.

## Quota-bound sequential preparation (R13)

CLI commands:

```text
--start-preparation <campaign> <saved-plan> <new-stage> <byte-budget>
--next-preparation <campaign> <saved-plan> <stage>
--pause-preparation <campaign> <stage>
--resume-preparation <campaign> <stage>
```

Start binds the current input and plan hashes. Next prepares exactly one page through the existing production profile DTO. It validates fresh inputs, skips verified page files, retries partial/corrupt files in new charged attempts and retains earlier files. Pause/resume validate protected roots before opening state. No unattended process or recurring task is started.

`ReservedWriteBudget` enforces a hard **logical file-byte ceiling for its own gateway writes**: checksummed ledger and full payload reservation are flushed before a new payload file is created. Ledger bytes count too. Exhaustion writes no new payload. Partial attempts retain their full charge; torn reservation logs fail closed. Exclusive locking prevents concurrent writers; recovery rehashes completed files. Limits: at most 2000 reservations, individual controlled payload at most 32 MiB, recovery ledger at most 1 MiB, immutable workspace ceiling 4 KiB..1 GiB.

This does **not** impose an OS allocation quota, account for filesystem overhead, or constrain arbitrary/native file handles. The original ESP/FaceGen writers use paths directly and do not yet honor this gateway. Therefore **no global mass ESP/FaceGen worker is enabled**. A verified boundary covering all writer/cache/temp/native paths is still required, including exhaustion/interruption tests with real generated resources. Estimates and post-write checks do not satisfy that requirement.

## Local Windows bridge

See Tools/WoDInspector/README.md. `prepare_local_profile.py` reuses the existing TES4 header inspector; `Invoke-GlobalNpc.ps1` fingerprints and invokes an already built experimental CLI. Both remain separate from `D:\CRU-Female-Lite`. Use an existing isolated active resource view, retain private paths/reports locally and never upload or duplicate the 300 GB collection. Neither tool installs software, configures a mod manager/VFS, modifies saves or enables services.
