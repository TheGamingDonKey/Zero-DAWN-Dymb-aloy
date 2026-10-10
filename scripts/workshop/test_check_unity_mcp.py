"""Run the real checker with an in-memory MCP transport and isolated evidence."""
from contextlib import asynccontextmanager
import json
from pathlib import Path
import runpy
import shutil
import sys
import tempfile
from types import ModuleType, SimpleNamespace
import unittest
from unittest.mock import patch


class Result:
    def __init__(self, data):
        self.data = data

    def model_dump(self, **_kwargs):
        return self.data


class BridgeReports(unittest.TestCase):
    def run_checker(self, scenario, diagnostic=True):
        with tempfile.TemporaryDirectory(prefix="workshop-mcp-test-") as directory:
            root = Path(directory)
            script = root / "scripts/workshop/check_unity_mcp.py"
            script.parent.mkdir(parents=True)
            shutil.copyfile(Path(__file__).with_name("check_unity_mcp.py"), script)
            artifacts = root / ".artifacts/workshop"
            artifacts.mkdir(parents=True)
            latest = artifacts / ("unity-mcp-diagnostic-input.json" if diagnostic else "unity-mcp-protocol.json")
            latest.write_text(json.dumps({"passed": True, "old": True}))

            class FakeSession:
                def __init__(self, *_args):
                    pass

                async def __aenter__(self):
                    return self

                async def __aexit__(self, *_args):
                    pass

                async def initialize(self):
                    if scenario == "startup_error":
                        raise RuntimeError("injected startup failure")
                    return Result({"serverInfo": {"name": "synthetic MCP"}})

                async def list_tools(self):
                    return SimpleNamespace(tools=[])

                async def list_resources(self):
                    return SimpleNamespace(resources=[SimpleNamespace(uri="mcpforunity://instances")])

                async def read_resource(self, _uri):
                    data = {"instances": [{"id": "test", "path": str(root / "MRWorkshop/Assets")}]}
                    text = "incomplete{" if scenario == "registry_error" else json.dumps(data)
                    return SimpleNamespace(contents=[SimpleNamespace(text=text)])

                async def call_tool(self, name, _arguments):
                    if name == "execute_menu_item":
                        if scenario == "menu_error":
                            return Result({"structuredContent": {"success": False}})
                        if scenario != "timeout":
                            evidence = artifacts / "input-proof-synthetic"
                            evidence.mkdir()
                            (evidence / "result.json").write_text(json.dumps({"passed": scenario != "diagnostic_failure"}))
                    if name == "read_console":
                        rows = [{"type": "Error", "message": "injected diagnostic error"}] if scenario == 'console_rows' else []
                        return Result({"structuredContent": {"success": scenario != "console_error", "data": rows}})
                    return Result({"structuredContent": {"success": True}})

            @asynccontextmanager
            async def transport(_params):
                yield object(), object()

            mcp = ModuleType("mcp")
            mcp.ClientSession = FakeSession
            mcp.StdioServerParameters = lambda **kwargs: kwargs
            if scenario == 'dependency_error':
                mcp = None  # Force import failure without accessing installed dependencies.
            client = ModuleType("mcp.client")
            stdio = ModuleType("mcp.client.stdio")
            stdio.stdio_client = transport
            arguments = [str(script)]
            if diagnostic:
                arguments += ["--diagnostic", "input", "--wait-seconds", "0"]
            error = None
            with patch.dict(sys.modules, {"mcp": mcp, "mcp.client": client, "mcp.client.stdio": stdio}), \
                    patch.dict("os.environ", {"FOCUS_PYTHON": "synthetic-python.exe"}), \
                    patch.object(sys, "argv", arguments), patch("builtins.print"):
                try:
                    runpy.run_path(str(script), run_name="__main__")
                except Exception as caught:
                    error = caught
            report = json.loads(latest.read_text())
            snapshots = [json.loads(file.read_text()) for file in artifacts.glob("unity-mcp-*.json") if file != latest]
            self.assertFalse(list(artifacts.glob("*.part-*")))
            return report, snapshots, error

    def test_failed_menu_replaces_old_passing_report(self):
        report, snapshots, error = self.run_checker("menu_error")
        self.assertIsNotNone(error)
        self.assertFalse(report.get("passed", True), "A failed request left the prior passing latest report")
        self.assertFalse(report.get("old", False))
        self.assertTrue(report["invocation_id"])
        self.assertEqual(snapshots, [report])

    def test_timeout_saves_the_requested_job_failure(self):
        report, snapshots, error = self.run_checker("timeout")
        self.assertIsNotNone(error)
        self.assertFalse(report["passed"])
        self.assertIn("bounded", report["error"])
        self.assertTrue(report["diagnostic_request"]["success"])
        self.assertEqual(snapshots, [report])

    def test_malformed_registry_saves_fresh_failure(self):
        report, snapshots, error = self.run_checker("registry_error")
        self.assertIsNotNone(error)
        self.assertFalse(report["passed"])
        self.assertFalse(report["editor_round_trip_verified"])
        self.assertEqual(snapshots, [report])

    def test_server_initialization_error_saves_fresh_failure(self):
        report, snapshots, error = self.run_checker("startup_error")
        self.assertIsNotNone(error)
        self.assertFalse(report["passed"])
        self.assertIn("injected startup failure", report["error"])
        self.assertEqual(snapshots, [report])

    def test_missing_mcp_dependency_saves_fresh_failure(self):
        report, snapshots, error = self.run_checker('dependency_error')
        self.assertIsNotNone(error)
        self.assertFalse(report['passed'])
        self.assertEqual(report['error_type'], 'ModuleNotFoundError')
        self.assertEqual(snapshots, [report])

    def test_console_failure_is_not_reported_as_verified(self):
        report, snapshots, error = self.run_checker("console_error")
        self.assertIsNotNone(error)
        self.assertFalse(report["passed"])
        self.assertTrue(report["diagnostic_result"]["passed"])
        self.assertEqual(snapshots, [report])

    def test_completed_failed_diagnostic_is_preserved(self):
        report, snapshots, error = self.run_checker("diagnostic_failure")
        self.assertIsNotNone(error)
        self.assertFalse(report["passed"])
        self.assertFalse(report["diagnostic_result"]["passed"])
        self.assertEqual(snapshots, [report])

    def test_console_error_rows_fail_and_preserve_evidence(self):
        report, snapshots, error = self.run_checker('console_rows')
        self.assertIsNotNone(error)
        self.assertFalse(report['passed'])
        self.assertTrue(report['diagnostic_result']['passed'])
        self.assertEqual(report['console']['data'][0]['message'], 'injected diagnostic error')
        self.assertEqual(snapshots, [report])

    def test_success_has_its_own_snapshot_and_fresh_latest(self):
        report, snapshots, error = self.run_checker("success")
        self.assertIsNone(error)
        self.assertTrue(report["passed"])
        self.assertTrue(report["editor_round_trip_verified"])
        self.assertTrue(report["completed_at"])
        self.assertEqual(snapshots, [report])

    def test_connection_only_succeeds_without_diagnostic(self):
        report, snapshots, error = self.run_checker("success", diagnostic=False)
        self.assertIsNone(error)
        self.assertTrue(report["passed"])
        self.assertEqual(snapshots, [report])


if __name__ == "__main__":
    unittest.main()
