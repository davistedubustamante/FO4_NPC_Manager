# Persistent execution plan

Branch: dev/npc-transformation-experimental. Preserve 74289ec, e71248f, c1c771f and every subsequent checkpoint. No merge, release, installation, original game/save edits or recurring automation activation.

## Goal and evidence boundaries

Final target: female conversion across Fallout 4 GOTY, DLC and all active WoD plugins, with separate verified special-category redesign. GLOBAL_CONVERTER.md describes the campaign architecture; LOCALIZED_AND_TRAITS.md describes the new adapters and their exact bounds. Plans, flag edits and static graphs are not complete conversion proof. No dependence on the three historical Lite mods; do not use D:\CRU-Female-Lite.

Preserved previous fully successful Windows checkpoint: e71248f / https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39272442946 (72 integration assertions, original/new tools, discovery/page CLI, license/provenance and artifact guards). c1c771f preserves its engineering records.

Current functional route evidence: 38c7f2b / https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39320528288 passed every mandatory localized, Traits and preparation check, then failed the first narrative assertion. Public LAST-CHECK and stack identify the precise boundary; see EVIDENCE.md. No full workflow success claimed for it.

Latest fully successful integrated code: 71d329a / https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39321569051. Status Success. All 125 mandatory integration checks passed. Portable baseline31 + journal21 + campaign23 (10,000 synthetic rows) + controlled budget16 passed locally and in the prerequisite matrix; inspector/profile helper locally 27 passed + two Windows-only skips. Native, GUI, original/new CLI, all CLI/launcher runtime checks, license/provenance, artifact guards and publication passed. Five experimental artifacts reported.

## Session implementations

- R15: original resolver/writer, explicit selected-language loose-table manifest, immutable byte hashes/size limits, scoped UTF8 output and context restoration, original-ID audit, winning override ownership, missing/wrong resources and masters fail closed. Output stores chosen language inline; original localized inputs remain untouched. BA2-only resources, encoding exceptions and actual collection trials remain pending.
- R11: original frozen Traits resolver/materializer, strict deterministic NPC-only chains, target VMAD/faction/inventory/package preservation. Only Traits-only inheritance is admitted. Cycles/dead/incomplete/deleted/duplicate/non-NPC/other-category chains remain blocked. Child/unknown adulthood guards remain.
- R14: original canonical reference walker, static quest/alias/scene/relationship/voice impact, transitive review, explicit subrecord identity and bounded coverage findings. No quest/runtime compatibility certification or complete campaign admission integration.
- R13: durable pre-write quota for gateway-controlled metadata files, exclusive checkpoints, pause/resume, charged partial attempts and sequential page preparation through existing DTOs. No global ESP/FaceGen writer enabled; this logical byte ceiling does not constrain their direct file handles or OS allocation.
- R03/local bridge: existing metadata inspector, private selected-language profile helper and fingerprinted Windows CLI launcher; no installation, VFS setup, collection copy/upload or original file edits.
- R01/R04: recovery records/DAG maintained; recurring proposal stays disabled. R06/R07/R08/R16 still need actual assets/runtime evidence.

## Bounded validation and recovery

Initial R15 8967297 was superseded/cancelled under existing concurrency. Integrated executions: 1 (95bb070) GUI compile failed on reserved Handles identifier; 2 (bf055b2) compiled but positive Traits fixture had out-of-order fields; 3 (38c7f2b) passed R15/R11/preparation but narrative assertion confused readable names with signatures; 4 (71d329a) final changed-code validation fully passed. Exact causes/fixes remain in EVIDENCE.md. Do not start a fifth full execution for this step. If final run fails, record the precise remaining error and retain independently verified sections; never weaken an assertion or claim full success.

Documentation-only commits trigger engineering-records CI; do not rebuild passing code unchanged. On interruption inspect current Git and actual CI before repeating anything. Production/global fingerprints v7 reject older admission seals without deleting prior staging.

## Next viable requirements

1. R15: original archive/index APIs for explicit BA2 table provenance and bounded extraction, positive/negative controlled archive fixtures; explicit handling/tests of encoding exceptions and mixed inline overrides. Real GOTY/DLC positive case requires the actual local profile/tables, never uploads.
2. R11: additional inherited categories and deterministic reviewed leveled routes only after effective flag/category/narrative semantics and preservation are tested; no arbitrary leaf pick or age inference.
3. R14: integrate static findings with campaign admission and expand referenced records/voice/audio coverage; dynamic Papyrus, aliases, conditions and quests/scenes still require real game regression.
4. R13: verified enforcement covering every ESP/FaceGen/cache/temp/native write before enabling mass execution. A gateway, estimates and post-write checks are insufficient for direct-path writers. Preserve original small explicit diagnostic batch behavior and experimental labels.
5. R06/R07/R08/R16: actual FaceGen/body/equipment bundles and complete frozen GOTY/DLC/WoD profile; isolated runtime regression including dialogue, missions, scenes, relationships and reversal. No unresolved category silently counted converted.

Use an already isolated authorized Windows resource view and the local bridge. Missing game resources do not block independent development; retry limits and unavailable runtime evidence do bound what can be certified this session. No unattended work is implied.

## Storage

Reuse pinned SDK/dependency caches. Cloud working-set ceiling 2 GiB; observed tools1.2 GiB + repository23 MiB + temporary SDK Roslyn syntax helper26 MiB. No new packages or collection resources installed/downloaded. New Windows payload <=256 MiB plus portable evidence <=1 MiB per matrix job, retention two days; existing user artifacts are not deleted. Loose language pool <=128 MiB, bounded metadata outputs only. Keep original files/saves read-only.

## Current BA2 / strict-storage iteration

Preserve 71d329a and 57043d6. R15 first: original pinned BethesdaReader metadata, bounded individual GNRL entry, original FilesDictionary registration/generation and localized resolver, explicit winner/source/resource SHA and ESP reopen. No complete archive extraction or resource cache writes. New fixtures exercise all three table types, missing IDs, conflicting selections, language/hash errors and restored resource state. Windows validation pending; maximum four changed-code attempts for this new step.

R13 next: inspect/adapt stream interfaces; never claim the metadata gateway constrains direct path/native writes. Global mass executor remains disabled. Independent R11/R14 and authentic local resource admission follow as feasible.

Attempt1 dc669ba /39333204616 compiled Windows and passed new BA2 positive/negative export assertions, then exposed a metadata FileStream ownership leak when the fixture rewrote its archive. Source lifetime fixed; attempt2 will validate changed code, not repeat an unchanged failing build. Additional routes: original Factions/Inventory/AIData buckets, reviewed narrative campaign admission, local BA2 manifest sealing and authentic NIF/DDS preflight. No direct-path quota claim, no global worker, no actual asset/runtime evidence. Local procedure: LOCAL_AUTHENTIC_TRIAL.md.

033045f /39334233343 completed full Windows Success, including the actual BA2 seal/reviewed-discovery CLI. Attempt3 now validates the new optional FaceGen NIF/DDS/material budget adapter, category-specific evidence, explicit archive source priority and additional completion-quota/name tests. Local117 portable assertions passed. Full R13, real collection/runtime remain blocked; no mass worker enabled. One diagnosis-driven final fourth execution is available if this changed-code run fails.
