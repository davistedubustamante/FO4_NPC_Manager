# Persistent execution plan

Branch: dev/npc-transformation-experimental. Baseline: d09860b. No automatic scheduler is active.

## Current iteration
1. R01: establish instructions, requirement state, blockers, evidence and recovery. Verified by engineering-records CI at 3ac23fa.
2. R02: use pinned FO4_Base_Library to read a valid generated FO4 NPC_ plugin, write a separate override ESP, reopen and verify identity, masters and preserved fields. Binary fixture evidence only; no claim of in-game conversion. In progress.
3. R03: prepare a Windows read-only WoD inspector with metadata/resource index and isolated output, no collection upload. Implemented; first 18 tests pass on Linux/Windows, private binding and PowerShell entry-point coverage under validation.
4. R04: investigate available Codex recurring automation, prepare disabled configuration and approval instructions. Prepared and disabled; no callable scheduler in session. Do not activate.
5. Run local checks + Windows CI, diagnose up to three attempts per failing step, record exact results and commit. Continue next viable item.

## Next milestones
Real authorized plugin/collection manifests → winning overrides and explicit adult classification → template materialization → one NPC patch verified by existing reader → FaceGen staged bundle → body/equipment → dialogue/voice and quest checks → collection regression on a separate Windows test profile. Expand batch size only after the preceding stage passes.

## Resume procedure
Read AGENTS.md and requirements.json; inspect git status, HEAD and CI evidence. Keep user work. Fetch only as needed; no resets or automatic merges. Check actual artifacts/tests before advancing a requirement. After a failure record diagnosis and attempt count, fix the identified cause or mark the precise external prerequisite and continue independently. End each session with a pushed recoverable commit, remaining blockers and next viable requirement. Do not promise background execution without an active authorized scheduler.
