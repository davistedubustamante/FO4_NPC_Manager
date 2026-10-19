import hashlib
import json
import os
from pathlib import Path
import struct
import subprocess
import tempfile
import unittest

import inspect_wod as scanner


def plugin():
    body = b'HEDR' + struct.pack('<HfII', 12, 1.0, 0, 0x800)
    return b'TES4' + struct.pack('<IIIIHH', len(body), 0, 0, 0, 131, 0) + body


def ba2(names, version=1, kind=b'GNRL'):
    return b'BTDX' + struct.pack('<I4sIQ', version, kind, len(names), 24) + b''.join(struct.pack('<H', len(n)) + n for n in names)


class InspectorTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.data = self.root / 'Data'
        self.data.mkdir()
        (self.data / 'Fixture.esp').write_bytes(plugin())

    def run_scan(self, index=False):
        return scanner.inspect({'game': self.data}, self.root / 'report', index)

    def test_inventory_preserves_originals(self):
        (self.data / 'plugins.txt').write_text('*Fixture.esp\n', encoding='utf-8')
        before = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in self.data.iterdir()}
        report = self.run_scan()
        after = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in self.data.iterdir()}
        self.assertEqual(before, after)
        self.assertEqual(report['filesInventoried'], 2)
        self.assertEqual(report['status'], 'complete')
        rows = [json.loads(x) for x in (self.root / 'report' / 'inventory.jsonl').read_text().splitlines()]
        self.assertEqual(next(r['profileLines'] for r in rows if 'profileLines' in r), ['*Fixture.esp'])
        self.assertNotIn(str(self.data), (self.root / 'report' / 'summary.json').read_text())

    def test_new_output_only(self):
        self.run_scan()
        marker = self.root / 'report' / 'marker'
        marker.write_text('preserve')
        with self.assertRaises(FileExistsError):
            self.run_scan()
        self.assertEqual(marker.read_text(), 'preserve')

    def test_private_bindings_locate_local_resources(self):
        self.run_scan()
        bindings = json.loads((self.root / 'report' / 'local-roots.private.json').read_text())
        self.assertEqual(Path(bindings['game']), self.data)
        self.assertNotIn(str(self.data), (self.root / 'report' / 'inventory.jsonl').read_text())

    @unittest.skipUnless(os.name == 'nt', 'PowerShell wrapper requires Windows')
    def test_windows_powershell_entry_point(self):
        wrapper = Path(__file__).with_name('Inspect-WoD.ps1')
        result = subprocess.run(['powershell', '-NoProfile', '-File', str(wrapper),
                                 '-GameRoot', str(self.data), '-OutputRoot', str(self.root / 'wrapped')],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        report = json.loads((self.root / 'wrapped' / 'summary.json').read_text())
        self.assertEqual(report['filesInventoried'], 1)
        self.assertEqual(report['status'], 'complete')

    def test_output_inside_source_refused(self):
        with self.assertRaises(ValueError):
            scanner.inspect({'game': self.data}, self.data / 'report')
        self.assertFalse((self.data / 'report').exists())

    def test_output_equal_source_refused(self):
        with self.assertRaises(ValueError):
            scanner.inspect({'game': self.data}, self.data)

    def test_output_ancestor_refused(self):
        with self.assertRaises(ValueError):
            scanner.inspect({'game': self.data}, self.root)

    def test_missing_root_refused(self):
        with self.assertRaises(ValueError):
            scanner.inspect({'game': self.root / 'absent'}, self.root / 'report')

    def test_plugin_header(self):
        self.assertEqual(scanner.plugin_header(self.data / 'Fixture.esp')['masters'], [])

    def test_corrupt_plugin_is_partial(self):
        (self.data / 'broken.esp').write_bytes(b'bad')
        result = self.run_scan()
        self.assertEqual(result['status'], 'partial')
        self.assertEqual(len(result['errors']), 1)

    def test_archive_name_table_only(self):
        (self.data / 'assets.ba2').write_bytes(ba2([b'meshes/actors/character/facegendata/facegeom/a.nif', b'textures/armor/a.dds']))
        result = self.run_scan(True)
        self.assertEqual(result['archiveMembersIndexed'], 2)
        self.assertEqual(result['status'], 'complete')
        rows = [json.loads(x) for x in (self.root / 'report' / 'resources.jsonl').read_text().splitlines()]
        self.assertEqual(rows[0]['category'], 'facegen')
        self.assertFalse(any(self.data.rglob('*.nif')))

    def test_archive_index_is_opt_in(self):
        (self.data / 'assets.ba2').write_bytes(b'not-read')
        self.assertEqual(self.run_scan()['status'], 'complete')

    def test_truncated_archive_is_partial(self):
        (self.data / 'assets.ba2').write_bytes(ba2([b'mesh.nif'])[:-2])
        self.assertEqual(self.run_scan(True)['status'], 'partial')

    def test_unsafe_archive_name_refused(self):
        file = self.data / 'assets.ba2'
        for name in [b'../outside.nif', b'C:/outside.nif', b'/outside.nif']:
            file.write_bytes(ba2([name]))
            with self.assertRaises(ValueError):
                list(scanner.archive_names(file))

    def test_unknown_archive_version_refused(self):
        file = self.data / 'assets.ba2'
        file.write_bytes(ba2([b'mesh.nif'], version=999))
        with self.assertRaises(ValueError):
            list(scanner.archive_names(file))

    def test_invalid_archive_bounds_refused(self):
        file = self.data / 'assets.ba2'
        file.write_bytes(b'BTDX' + struct.pack('<I4sIQ', 1, b'GNRL', 1, 999999))
        with self.assertRaises(ValueError):
            list(scanner.archive_names(file))

    def test_directory_link_not_followed(self):
        external = self.root / 'outside'
        external.mkdir()
        (external / 'private.txt').write_text('do not scan')
        link = self.data / 'linked'
        if os.name == 'nt':
            subprocess.run(['cmd', '/c', 'mklink', '/J', str(link), str(external)], check=True, capture_output=True)
        else:
            link.symlink_to(external, target_is_directory=True)
        result = self.run_scan()
        self.assertEqual(result['status'], 'partial')
        self.assertEqual(result['filesInventoried'], 1)
        self.assertEqual(len(result['skipped']), 1)

    def test_root_link_refused(self):
        link = self.root / 'linked'
        if os.name == 'nt':
            subprocess.run(['cmd', '/c', 'mklink', '/J', str(link), str(self.data)], check=True, capture_output=True)
        else:
            link.symlink_to(self.data, target_is_directory=True)
        with self.assertRaises(ValueError):
            scanner.inspect({'game': link}, self.root / 'report')

    def test_cli_partial_exit_status(self):
        (self.data / 'broken.esp').write_bytes(b'broken')
        self.assertEqual(scanner.main(['--root', 'game=' + str(self.data), '--output', str(self.root / 'report')]), 2)

    def test_cli_duplicate_root_refused(self):
        self.assertEqual(scanner.main(['--root', 'game=' + str(self.data), '--root', 'game=' + str(self.data), '--output', str(self.root / 'report')]), 1)
        self.assertFalse((self.root / 'report').exists())


if __name__ == '__main__':
    unittest.main()
