import hashlib
import json
import os
from pathlib import Path
import struct
import subprocess
import tempfile
import unittest

from test_inspect_wod import plugin
import prepare_local_profile as local


class LocalProfileTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.data = self.root / 'Data'
        self.tables = self.data / 'Strings'
        self.tables.mkdir(parents=True)
        contents = bytearray(plugin())
        struct.pack_into('<I', contents, 8, 0x80)
        (self.data / 'Fixture.esp').write_bytes(contents)
        self.table = self.tables / 'Fixture_spanish.strings'
        self.table.write_bytes(b'controlled table bytes; parser validation belongs to original VB resolver')
        self.profile = self.root / 'profile.private.json'
        self.profile.write_text(json.dumps({'DataRoot': str(self.data), 'LoadOrder': ['Fixture.esp'], 'ProtectedRoots': [str(self.data)], 'Policy': {}}))
        self.output = self.root / 'prepared.private.json'

    def test_private_profile_hashes_selected_language_without_copying(self):
        before = self.table.read_bytes()
        result = local.prepare(self.profile, 'spanish', self.output)
        prepared = json.loads(self.output.read_text())
        self.assertEqual(result['tables'], 1)
        self.assertEqual(prepared['Localization']['Tables'][0]['Sha256'], hashlib.sha256(before).hexdigest().upper())
        self.assertEqual(self.table.read_bytes(), before)
        self.assertNotIn('Localization', json.loads(self.profile.read_text()))
        self.assertEqual(prepared['Localization']['Tables'][0]['FilePath'], str(self.table))

    def test_wrong_language_never_falls_back(self):
        with self.assertRaises(ValueError):
            local.prepare(self.profile, 'english', self.output)
        self.assertFalse(self.output.exists())

    def test_encoding_sidecar_refused(self):
        self.table.with_suffix('.cpoverride').write_text('1252')
        with self.assertRaises(ValueError):
            local.prepare(self.profile, 'spanish', self.output)

    def test_original_tree_cannot_receive_profile(self):
        with self.assertRaises(ValueError):
            local.prepare(self.profile, 'spanish', self.data / 'new.json')

    def test_existing_evidence_not_overwritten(self):
        self.output.write_text('preserve')
        with self.assertRaises(FileExistsError):
            local.prepare(self.profile, 'spanish', self.output)
        self.assertEqual(self.output.read_text(), 'preserve')

    def test_lite_project_is_separate(self):
        with self.assertRaises(ValueError):
            local.reject_lite(r'D:\CRU-Female-Lite\Data')
        with self.assertRaises(ValueError):
            local.reject_lite(r'd:\cru-female-lite\profile.json')

    def test_symlink_table_refused(self):
        destination = self.root / 'other-table'
        self.table.rename(destination)
        try:
            self.table.symlink_to(destination)
        except OSError:
            self.skipTest('Symlink privilege unavailable')
        with self.assertRaises(ValueError):
            local.prepare(self.profile, 'spanish', self.output)

    def test_missing_master_is_not_silently_added(self):
        contents = bytearray(plugin())
        extra = b'MAST' + struct.pack('<H', 12) + b'Missing.esm\0'
        struct.pack_into('<I', contents, 4, len(contents) - 24 + len(extra))
        contents.extend(extra)
        (self.data / 'Fixture.esp').write_bytes(contents)
        with self.assertRaises(ValueError):
            local.prepare(self.profile, 'spanish', self.output)

    @unittest.skipUnless(os.name == 'nt', 'PowerShell launcher requires Windows')
    def test_powershell_refuses_lite_before_executable_access(self):
        wrapper = Path(__file__).with_name('Invoke-GlobalNpc.ps1')
        result = subprocess.run(['powershell', '-NoProfile', '-File', str(wrapper), '-Task', 'Discover', '-CliPath', r'D:\CRU-Female-Lite\tool.exe', '-CliSha256', '0' * 64, '-ProfilePath', str(self.profile), '-OutputPath', str(self.output)], capture_output=True, text=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('separate from CRU-Female-Lite', result.stdout + result.stderr)
        self.assertFalse(self.output.exists())
