# Recurring execution: prepared, inactive

Discovery of callable tools in this session found no Codex automation/scheduler CRUD capability.
This is a limitation of this session, not evidence that the Codex product lacks automations.
No recurring task, cron entry, GitHub schedule, self-hosted worker or paid service was created.
The existing `workflow_dispatch` is a manually launched CI build/test, not an autonomous coding agent.

`config/automation-proposal.json` is a versioned project proposal, **not a supported Codex import file**.
`AUTOMATION_PROMPT.md` is the reusable task prompt. The proposed cadence is weekdays at 09:00
America/Lima, with one project task at a time and an advisory 25-minute agent allowance. These
limits are instructions until the actual scheduling interface confirms enforceable usage caps;
GitHub Actions has independent job timeouts and can consume account minutes. No pricing or quota
is assumed. Three failure attempts require new diagnoses, not unchanged retries.

Before activation, confirm in the actual Codex automation UI/tool: repository and branch support,
cloud versus local execution, whether a local app must remain running, credentials, persistent
workspace behavior, available usage allowance, notification destination, scheduler timezone,
concurrency and pause/cancel controls. Do not invent any of these capabilities from a schedule label.
If the current client exposes no scheduler, retain the proposal and start manually using the prompt.

Activation is a separate action that needs explicit user authorization for the concrete schedule,
execution location and budget. Do not activate a recurring GitHub workflow as a workaround.
Review the first run's evidence before extending cadence. Pausing must prevent future launches;
record any task already running and checkpoint it separately. Avoid schedules that trigger themselves
through their own commits.
