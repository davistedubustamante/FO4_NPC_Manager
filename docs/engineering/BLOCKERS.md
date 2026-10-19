# Blockers and requested interventions

| ID | Blocker | Independent work | User action when needed |
|---|---|---|---|
| B01 | CRU/WoD identities, versions and actual winning load order unavailable | Generated plugin fixtures, writer integration, local inspector | Run inspector locally; provide collection URLs/versions and a reviewed metadata-only summary, never 300 GB of resources |
| B02 | Cloud machine is Linux; no local Fallout 4 or Windows runtime/assets | Pinned Windows CI, portable tests and inspector self-tests | Later run staged integration in a separate Windows profile with licensed game resources |
| B03 | GitHub API previously blocked by proxy; native Git and public HTML work | Push branch; inspect public run/job status and annotations | Public error annotations now expose the runtime failure. Only if detailed logs remain necessary, supply a reviewed failing log or permit api.github.com in environment settings; no token in chat |
| B04 | No callable recurring Codex automation tool discovered in this session | Prepare disabled task proposal and manual resume prompt | Authorize activation only after capability, schedule, usage allowance and scope are confirmed |
| B05 | No evidence of in-game quests/dialogue/FaceGen compatibility | Binary round-trip and deterministic patch verification | Later validate one staged NPC in a separate profile; never original saves |

A missing collection does not block binary-fixture engineering. Retry only after diagnosis or a prerequisite changes. Track individual attempt details in EVIDENCE.md.

## Resolved internal verification boundary
The original raw master-prefix guard rejected a legitimate CleanMasters remap. Resolved at a0bf328
using existing canonical reader/emitter verification in global FormID space. Windows gate passed
at https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39193615511.
No user source-code change or credential intervention is needed for this resolved issue.

## What the user actually needs to provide next
Run the offline inspector on the real Windows installation/profile; share only a reviewed small
summary and the collection URLs/versions (including what WoD identifies). Keep original plugins,
archives, saves and local-roots.private.json local. Later, actual game behavior must be tested in a
separate profile with test saves. Recurring task activation is a different action requiring explicit
approval and an available scheduling interface; none was activated.

## Integration-specific limits
- Original FaceGen generator is now connected to staged output in code; real NIF/DDS generation still requires local licensed meshes/textures. Inspector completion is not a prerequisite for developing this adapter.
- Coordinator stages uniquely named per-NPC shards and has aggregate ESP/file-copy validation on controlled fixtures, with no automatic installation. Traits-only frozen resolution is verified; additional inherited categories and asset-content fingerprints remain implementation work, not requests to upload the collection.
- Plugin/preset hashes are sealed; assets must remain frozen during local evaluation because a full resource-content manifest is not yet attached to resume. Body/outfit/runtime overlays and voice replacement are rejected or deferred rather than silently claimed.

## Global scope blockers (new)
- B06 / R15: selected-language loose tables now pass original resolver/export/readback at 71d329a. Localized inputs without an admitted explicit resource manifest remain blocked; BA2-only strings, encoding exceptions and real GOTY/DLC/WoD trials remain pending. Use existing local tables/archives; never upload them.
- B07 / R13: writer and FaceGen own their file handles; no verified hard output quota yet. Global discovery/page preparation only emit bounded metadata; no global asset-writing worker is enabled. Preflight estimates and post-write scans are not quota enforcement.
- B08 / R14: companion faction/key rules and Unique/Essential/VMAD signals do not replace full quest/alias/scene/relationship analysis. Coverage is inventory/planning, never converted count. Robots/creatures/child redesign stay separate pending evidence.

### Current session refinements (binary/Windows fixtures verified at 71d329a)
- R15 selected-language loose-table path implemented using original resolver/writer. No-context localized export stays blocked. BA2-only string resources, nonzero IDs resolving to empty strings (original writer reports a finding), encoding sidecars and real collection evidence remain blocked; do not remove these guards. Output is inline chosen-language ESP; original external ID provenance is audited, numeric IDs are not copied into a nonlocalized ESP.
- R11 strict Traits-only deterministic NPC chains implemented with original frozen materializer. Other inherited categories, leveled-list choices and malformed/ambiguous chains remain blocked; real face/body evidence is separate.
- R14 static canonical reference graph implemented; parser findings/out-of-filter references and dynamic/runtime behavior prevent compatibility certification. Individual narrative review remains required.
- R13 durable hard pre-write ceiling implemented only for files written through ReservedWriteBudget and sequential campaign profile preparation. The original path-based ESP writer and FaceGen cannot be constrained by that gateway; no mass asset writer enabled. OS allocation overhead and unrelated writers are outside this logical-file byte ceiling.
- Initial R15 revision was superseded/cancelled after queue delay. Final 71d329a passed Windows CI; earlier diagnosed failures are preserved in EVIDENCE.md. No dependency/toolchain changes were used to address runner availability.

- Final selected loose-table, Traits-only, static graph and controlled preparation subroutes passed all 125 Windows assertions at 71d329a; remaining blockers above are real scope limits, not an unexplained build failure. Complete R15/R11/R14/R13 remain in_progress until their broader acceptance evidence exists.

### Continued BA2 / inherited-bucket iteration

- R15 BA2 GNRL selected STRINGS/DLSTRINGS/ILSTRINGS, per-member/source hashes, explicit winner, original resolver/ESP reopen and local sealer passed the integrated Windows gate at 033045f /39334233343. This removes the synthetic BA2-only admission blocker, not missing real GOTY/DLC/WoD evidence. Encoding exceptions and actual profile priority review remain pending.
- R11 Traits+Factions+Inventory+AIData combinations now pass original materializer/writer fixtures. Other categories, leveled choices and actual asset/runtime behavior stay blocked.
- R14 reviewed discovery now integrates parser findings and static dependencies with classifier/pages. No static-to-runtime compatibility claim.
- R13 gateway adds bounded streams and durable completion, including failure after full bytes and completion-quota exhaustion. Optional FaceGen NIF/DDS/material output adapter is in validation; full ESP/cache/temp/auxiliary coverage and real generation are not verified. Global mass worker remains disabled.
- Local authentic resource preflight uses original NIF/native DDS readers, frozen target/preset inventory and source hashes. No real resources exist in Cloud. Follow LOCAL_AUTHENTIC_TRIAL.md on an authorized isolated Windows profile; do not upload the collection. No immediate game installation/activation is required for this read-only diagnostic.

205fb47 /39335263895 passed169 Windows checks and117 portable assertions. Controlled FaceGen DDS output admission is verified in fixture scope; NIF generation and all ESP/cache/temp/auxiliary paths still do not have complete hard-quota certification. Read-only BA2 asset-member preflight (single-chunk GNRL/DX10) is in final Windows validation; real resources and multi-chunk routes remain blocked.
