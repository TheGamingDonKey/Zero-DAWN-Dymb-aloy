"""Small headless checks; does not import bpy or create files."""
import ast
from copy import deepcopy
import json
from pathlib import Path

from geometry_plan import build_plan, validate_plan


def must_reject(plan, reason):
    try:
        validate_plan(plan)
    except ValueError:
        return reason
    raise AssertionError("Validation accepted: " + reason)


def main():
    folder = Path(__file__).resolve().parent
    for path in sorted(folder.glob("*.py")):
        ast.parse(path.read_text(encoding="utf-8"), filename=str(path))
    original = build_plan()
    report = validate_plan(original)
    assert report == validate_plan(build_plan()), "Plan must be deterministic"
    checks = ["AST syntax", "deterministic plan", "requested dimensions", "centred bounds",
              "closed edges", "nondegenerate triangles", "positive signed volume", "material slots", "triangle budget"]
    changed = deepcopy(original)
    changed[0]["requested_unity_dimensions_metres"][0] = .5
    checks.append(must_reject(changed, "reject wrong dimensions"))
    changed = deepcopy(original)
    changed[0]["materials"].append("Fourth")
    checks.append(must_reject(changed, "reject fourth material"))
    changed = deepcopy(original)
    changed[1]["faces"][0] = (0, 0, 1)
    checks.append(must_reject(changed, "reject duplicate triangle vertex"))
    changed = deepcopy(original)
    changed[2]["face_materials"][0] = 3
    checks.append(must_reject(changed, "reject out-of-range material index"))
    changed = deepcopy(original)
    changed[0]["faces"].pop()
    changed[0]["face_materials"].pop()
    checks.append(must_reject(changed, "reject open shell"))
    # Repeat otherwise-valid disconnected assets to exercise the combined budget.
    checks.append(must_reject(deepcopy(original) * 4, "reject 10000+ triangles"))
    print(json.dumps({"status": "passed_pure_python_only", "checks": checks,
                      "total_planned_triangles": report["total_triangles"],
                      "blender_executed": False, "export_verified": False}, indent=2))


if __name__ == "__main__":
    main()
