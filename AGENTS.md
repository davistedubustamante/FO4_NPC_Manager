# FO4 NPC Manager — autonomous experimental engineering

Work from `dev/npc-transformation-experimental`; preserve existing work. Read `docs/engineering/EXECUTION_PLAN.md`, `requirements.json`, `BLOCKERS.md` and `EVIDENCE.md` on every resumed task. Each cloud task is already isolated; do not create a worktree unless requested.

## Iteration contract
1. Select the highest-priority feasible pending requirement; investigate existing library/editor functions first. Record selection and acceptance checks in the plan.
2. Implement real behavior, with tests that exercise the actual implementation and meaningful negative cases. Never implement a second production ESP serializer to bypass the existing reader/writer.
3. Run local tests and the existing pinned Windows GitHub Actions pipeline. Preserve pins, TLS/checksum verification, licenses and original build configurations. Maximum four total executions (initial plus three diagnosis-driven retries) per failing step; record failures and switch to independent work if blocked.
4. Track command, exit status, test counts, CI URL and exact tested commit. Synthetic, binary round-trip, generated FaceGen and in-game evidence are distinct levels. Do not claim game compatibility from compilation.
5. Commit verified code on the experimental branch; push for CI. Document any unverified work and remaining blocker precisely. Continue with another viable requirement within the session.

## Boundaries
No merge, release, original game/save edits, installation, paid service or activation of recurring automation without explicit authorization. Stage only in a new directory outside every input/game/mod/save root; fail closed on existing output, symlinks/reparse points and incomplete inputs. Never upload proprietary plugins, archives, saves, credentials or a mod collection. Local inspection defaults to metadata; archive resource indexes stay local. Do not infer adulthood from appearance or level; unknown age is excluded. Unresolved templates, dialogue/voice constraints and assets must remain blockers, not silent fallbacks.

## Recovery and budget
Keep recoverable commits and machine-readable requirement state. On interruption inspect Git status and latest CI, resume the first unverified step rather than restarting. One active Windows run per code iteration; do not repeatedly rebuild unchanged passing code. Session work is bounded by available execution time and usage; there is no unattended worker beyond actual authorized platform capabilities. Proposed recurring runs must have an explicit task/time budget, retry limit and stop conditions, and remain disabled pending approval.

## Commands
Synthetic gate: `cd Tools/AdultTransformationGate && dotnet run -c Release -- <new-report.json>` (SDK 8.0.408). Cloud SDK: `/workspace/.tools/dotnet/dotnet`; set DOTNET_CLI_HOME and NUGET_PACKAGES under /workspace/.tools. NuGet may need the approved execution mode for its system mutex. See the plan for Windows integration and local WoD inspector checks. Don't print environment or credential values.

## Global campaign objective
Target Fallout 4 GOTY, DLC and all active WoD plugins, not a fixed Lite mod list. Read GLOBAL_CONVERTER.md. Prioritize generic adult humans; unique/companions require review; robots/creatures separate redesign. Child/unknown adulthood stays blocked. Plans and prepared pages are not converted NPCs. No global FaceGen executor without verified hard write-budget enforcement. Reuse SDK/dependency caches; cloud session working-set ceiling 2 GiB and new Windows artifact payload ceiling 256 MiB plus portable evidence at most 1 MiB per matrix job, retention two days; do not delete existing user artifacts.
