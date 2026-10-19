"""Validate persistent recovery records without enabling any automation."""
import json
from pathlib import Path


def validate(root):
    root = Path(root)
    required = ['AGENTS.md', 'docs/engineering/EXECUTION_PLAN.md', 'docs/engineering/BLOCKERS.md',
                'docs/engineering/EVIDENCE.md', 'docs/engineering/AUTOMATIONS.md', 'docs/engineering/AUTOMATION_PROMPT.md']
    for name in required:
        if not (root / name).is_file() or not (root / name).read_text(encoding='utf-8').strip():
            raise ValueError('Missing persistent record: ' + name)
    document = json.loads((root / 'docs/engineering/requirements.json').read_text())
    requirements = document['requirements']
    nodes = {r['id']: r for r in requirements}
    if not nodes or len(nodes) != len(requirements):
        raise ValueError('Requirements must have unique nonempty IDs')
    visiting, visited = set(), set()
    def visit(key):
        if key not in nodes:
            raise ValueError('Unknown dependency: ' + key)
        if key in visiting:
            raise ValueError('Requirement dependency cycle: ' + key)
        if key in visited:
            return
        visiting.add(key)
        item = nodes[key]
        if item['status'] not in ('pending', 'in_progress', 'verified', 'blocked', 'prepared'):
            raise ValueError('Unknown requirement state: ' + key)
        if not item['acceptance']:
            raise ValueError('Missing acceptance: ' + key)
        if item['status'] == 'verified' and not item['evidence']:
            raise ValueError('Verification without evidence: ' + key)
        for dependency in item['dependsOn']:
            visit(dependency)
        visiting.remove(key)
        visited.add(key)
    for key in nodes:
        visit(key)
    proposal = json.loads((root / 'config/automation-proposal.json').read_text())
    if proposal['enabled'] is not False or proposal['activationRequiresExplicitUserApproval'] is not True:
        raise ValueError('Recurring automation has not been authorized for activation')
    if proposal['hardBudgetEnforcementVerified'] is not False:
        raise ValueError('Budget enforcement needs actual platform evidence')
    print(f'{len(nodes)} requirements validated; dependency DAG valid; scheduler remains disabled.')


if __name__ == '__main__':
    validate(Path(__file__).resolve().parents[2])
