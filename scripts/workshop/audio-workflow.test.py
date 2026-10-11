"""Small subprocess checks; never regenerate the working Unity asset."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
GENERATOR = ROOT / 'scripts/reference/build_audio.py'
WORKFLOW = ROOT / 'scripts/workshop/audio-workflow.py'
SOURCE = ROOT / 'FocusCore/Assets/FocusCore/Art/FocusChirp.wav'


class AudioWorkflowTests(unittest.TestCase):
    def test_staging_and_existing_directory_refusal(self):
        self.assertTrue(WORKFLOW.exists(), 'Fresh-directory workflow is missing')
        before = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
        with tempfile.TemporaryDirectory() as temporary:
            stage = Path(temporary) / 'new-stage'
            command = [sys.executable, str(WORKFLOW), '--stage', str(stage)]
            result = subprocess.run(command, capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            report = json.loads((stage / 'audio-report.json').read_text())
            self.assertTrue(report['metrics']['passed'])
            self.assertEqual(report['metrics']['sample_rate'], 48000)
            self.assertEqual(report['source_sha256'], before)
            self.assertEqual(report['generator_sha256'],
                             hashlib.sha256(GENERATOR.read_bytes()).hexdigest())
            staged_before = (stage / 'FocusChirp.wav').read_bytes()
            repeated = subprocess.run(command, capture_output=True, text=True)
            self.assertNotEqual(repeated.returncode, 0)
            self.assertIn('already exists', repeated.stderr)
            self.assertEqual((stage / 'FocusChirp.wav').read_bytes(), staged_before)
        self.assertEqual(hashlib.sha256(SOURCE.read_bytes()).hexdigest(), before)

    def test_import_and_default_cli_in_disposable_copy(self):
        # Copy only the generator into a fake checkout. Its historical default
        # then points at this disposable sandbox, never the real asset.
        with tempfile.TemporaryDirectory() as temporary:
            sandbox = Path(temporary)
            copy = sandbox / 'scripts/reference/build_audio.py'
            copy.parent.mkdir(parents=True)
            shutil.copyfile(GENERATOR, copy)
            default = sandbox / 'FocusCore/Assets/FocusCore/Art/FocusChirp.wav'
            default.parent.mkdir(parents=True)
            imported = subprocess.run([sys.executable, '-B', '-c',
                'import runpy, sys; runpy.run_path(sys.argv[1])', str(copy)],
                capture_output=True, text=True)
            self.assertEqual(imported.returncode, 0, imported.stderr)
            self.assertFalse(default.exists(), 'Import generated an asset')
            explicit = sandbox / 'explicit.wav'
            custom = subprocess.run([sys.executable, str(copy), '--output', str(explicit)],
                                    capture_output=True, text=True)
            self.assertEqual(custom.returncode, 0, custom.stderr)
            self.assertTrue(explicit.exists())
            self.assertFalse(default.exists(), 'Explicit output wrote the default')
            original = subprocess.run([sys.executable, str(copy)],
                                      capture_output=True, text=True)
            self.assertEqual(original.returncode, 0, original.stderr)
            self.assertEqual(default.read_bytes(), explicit.read_bytes())


if __name__ == '__main__':
    unittest.main()
