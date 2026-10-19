# Evidence and iteration log

## Baseline
- d09860b preserves the synthetic engine and 31 assertions; Linux/Windows matrix and GUI/CLI build passed in https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39182037976 (code commit ca3b55a; later documentation only).
- No real ESP conversion or in-game compatibility claimed by that baseline.

## Current iteration
- Read runtime skill and network policy; reused existing Git authentication. Environment network enforcement observation is unknown; operations must be checked individually.
- Inspected pinned FO4_Base_Library 8dc93e189cccfe026c128eb60d06c94fc949831b, same revision as existing Windows workflow.
- Implementation and validation results will be appended before marking requirements verified.
