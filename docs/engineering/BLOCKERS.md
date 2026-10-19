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
