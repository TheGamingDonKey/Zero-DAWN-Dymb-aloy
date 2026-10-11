"""Deferred batch export. Run only in a NEW background factory-startup Blender.

Regular Python supports --plan-only without importing bpy or writing output.
No live MCP use, current-scene deletion, overwrite, Unity import, or collider work.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import sys
import uuid

SCRIPT_DIR = Path(__file__).resolve().parent
ROOT = SCRIPT_DIR.parents[2]
sys.path.insert(0, str(SCRIPT_DIR))
from geometry_plan import PALETTE, build_plan, validate_plan  # noqa: E402


def arguments():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--plan-only", action="store_true")
    parser.add_argument("--run-id", help="Optional unique label; existing output is refused")
    parser.add_argument("--no-preview", action="store_true", help="Skip optional 512px Workbench PNGs")
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    return parser.parse_args(argv)


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def export_batch(options, assets, planned_report):
    import bpy
    if not bpy.app.background or bpy.data.filepath:
        raise RuntimeError("Use a NEW --background --factory-startup process without a .blend argument; live/saved sessions are refused.")
    # Stable mesh/material/FBX APIs common to 4.5 LTS and 5.2. No newer-only operators.
    if bpy.app.version < (4, 5, 0):
        raise RuntimeError("This recipe targets Blender 4.5+; earlier versions are outside its scope.")
    try:
        bpy.ops.export_scene.fbx.get_rna_type()
    except Exception as error:
        raise RuntimeError("FBX exporter unavailable. No output created; enable/install the bundled FBX exporter in this batch installation.") from error
    if not bpy.context.window:
        raise RuntimeError("Batch context has no window/scene selection context; no output created.")
    run_id = options.run_id or (datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S") + "-" + uuid.uuid4().hex[:8])
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_-]{0,63}", run_id):
        raise ValueError("Run ID must be 1-64 letters/digits/underscores/hyphens")
    folder_name = "OriginalProps-" + run_id
    delivery = ROOT / "MRWorkshop/Assets/Workshop/Art/External" / folder_name
    source = ROOT / "art/source/workshop" / folder_name
    if delivery.exists() or source.exists():
        raise FileExistsError("Run folder already exists; choose a new run ID. Existing files are never replaced.")
    delivery.mkdir(parents=True, exist_ok=False)
    source.mkdir(parents=True, exist_ok=False)
    manifest_path = delivery / "manifest.json"
    report = {
        "schema_version": 1, "status": "building", "run_id": run_id,
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "blender_version": bpy.app.version_string,
        "generator": str(Path(__file__).relative_to(ROOT)),
        "generator_sha256": sha256(Path(__file__)),
        "geometry_plan_sha256": sha256(SCRIPT_DIR / "geometry_plan.py"),
        "delivery_folder": str(delivery), "editable_source_folder": str(source),
        "units": "metres", "fbx_axes": {"forward": "-Z", "up": "Y"},
        "provenance": "Original procedural geometry authored for this workshop; no imported meshes, textures, rigs or game assets.",
        "asset_license": "Follow repository terms; this generator adds no new license grant.",
        "materials": {name: {"base_color_rgba": color, "opaque": True, "textures": []}
                      for name, color in PALETTE.items()},
        "plan": planned_report, "assets": [], "unity_import_verified": False,
        "quest_verified": False, "limitations": [
            "Combined closed intersecting primitive shells; not a Boolean-unioned fabrication mesh.",
            "No colliders, animation, textures or runtime integration.",
            "Blender/FBX output does not establish Unity import scale, shader appearance or Quest performance."]}

    def save_manifest():
        manifest_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

    save_manifest()
    original_scene = bpy.context.window.scene
    owned_scenes = set()
    try:
        materials = {}
        for name, color in PALETTE.items():
            mat = bpy.data.materials.new(folder_name + "_" + name)
            mat.use_nodes = True
            mat.diffuse_color = color
            shader = next(node for node in mat.node_tree.nodes if node.type == "BSDF_PRINCIPLED")
            shader.inputs["Base Color"].default_value = color
            shader.inputs["Roughness"].default_value = .42
            shader.inputs["Metallic"].default_value = .35 if name == "Workshop_PaleSteel" else .05
            materials[name] = mat

        for asset in assets:
            scene = bpy.data.scenes.new(folder_name + "_" + asset["name"])
            owned_scenes.add(scene)
            scene.unit_settings.system = "METRIC"
            scene.unit_settings.scale_length = 1.0
            mesh = bpy.data.meshes.new(folder_name + "_" + asset["name"])
            mesh.from_pydata(asset["vertices"], [], asset["faces"])
            mesh.update()
            obj = bpy.data.objects.new(asset["name"], mesh)
            scene.collection.objects.link(obj)
            for material_name in asset["materials"]:
                mesh.materials.append(materials[material_name])
            for polygon, slot in zip(mesh.polygons, asset["face_materials"]):
                polygon.material_index = slot
            mesh.calc_loop_triangles()
            actual_triangles = len(mesh.loop_triangles)
            expected = next(item for item in planned_report["assets"] if item["name"] == asset["name"])
            if actual_triangles != expected["triangle_count"]:
                raise RuntimeError("Blender tessellation differs from plan: " + asset["name"])
            bpy.context.window.scene = scene
            bpy.context.view_layer.update()
            actual_size = list(obj.dimensions)
            if any(abs(a - b) > 1e-6 for a, b in zip(actual_size, expected["blender_dimensions_metres"])):
                raise RuntimeError("Blender dimensions differ from plan: " + asset["name"])
            if tuple(obj.scale) != (1.0, 1.0, 1.0) or any(obj.location) or any(obj.rotation_euler):
                raise RuntimeError("Export object transform must remain identity")
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            fbx = delivery / (asset["name"] + ".fbx")
            result = bpy.ops.export_scene.fbx(
                filepath=str(fbx), use_selection=True, object_types={"MESH"},
                global_scale=1.0, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                axis_forward="-Z", axis_up="Y", use_space_transform=True,
                bake_space_transform=False, use_mesh_modifiers=False,
                mesh_smooth_type="OFF", use_triangles=True,
                add_leaf_bones=False, bake_anim=False, path_mode="AUTO")
            if "FINISHED" not in result or not fbx.is_file() or fbx.stat().st_size < 1024:
                raise RuntimeError("FBX export did not produce a valid-size file: " + asset["name"])
            entry = {**expected, "actual_blender_triangle_count": actual_triangles,
                     "actual_blender_dimensions_metres": actual_size,
                     "actual_blender_material_names": [mat.name for mat in mesh.materials],
                     "transform": {"location": list(obj.location), "rotation_euler": list(obj.rotation_euler), "scale": list(obj.scale)},
                     "fbx": {"path": str(fbx), "bytes": fbx.stat().st_size, "sha256": sha256(fbx)},
                     "preview": None}
            report["assets"].append(entry)
            save_manifest()
            if not options.no_preview:
                scene.render.engine = "BLENDER_WORKBENCH"
                scene.render.resolution_x = scene.render.resolution_y = 512
                scene.render.resolution_percentage = 100
                scene.render.image_settings.file_format = "PNG"
                scene.display.shading.light = "STUDIO"
                scene.display.shading.color_type = "MATERIAL"
                scene.display.shading.show_shadows = True
                scene.display.shading.show_cavity = True
                scene.display.shading.background_type = "WORLD"
                scene.world = bpy.data.worlds.new(folder_name + "_" + asset["name"])
                scene.world.color = (.025, .030, .045)
                camera_data = bpy.data.cameras.new(folder_name + "_" + asset["name"])
                camera = bpy.data.objects.new(asset["name"] + "_PreviewCamera", camera_data)
                scene.collection.objects.link(camera)
                span = max(actual_size)
                camera.location = (span * 1.1, -span * 2.2, span * 1.4)
                camera.rotation_euler = (-camera.location).to_track_quat("-Z", "Y").to_euler()
                camera_data.type = "ORTHO"
                camera_data.ortho_scale = span * 1.5
                camera_data.clip_start = .001
                scene.camera = camera
                preview = delivery / (asset["name"] + ".png")
                scene.render.filepath = str(preview)
                render_result = bpy.ops.render.render(write_still=True)
                if "FINISHED" not in render_result or not preview.is_file():
                    raise RuntimeError("PNG render did not finish")
                entry["preview"] = {"path": str(preview), "bytes": preview.stat().st_size,
                                    "sha256": sha256(preview), "size_pixels": [512, 512]}
                save_manifest()

        blend = source / "WorkshopOptionalProps.blend"
        # Writes only owned scenes and their dependencies; does not Save As the caller's file.
        bpy.data.libraries.write(str(blend), owned_scenes, fake_user=True, compress=True)
        if not blend.is_file() or blend.stat().st_size < 1024:
            raise RuntimeError("Editable source write did not produce a valid-size file")
        report["editable_source"] = {"path": str(blend), "bytes": blend.stat().st_size, "sha256": sha256(blend)}
        report["actual_total_triangles"] = sum(item["actual_blender_triangle_count"] for item in report["assets"])
        report["status"] = "blender_exported_unity_import_pending"
        save_manifest()
        print(json.dumps({"status": report["status"], "manifest": str(manifest_path),
                          "total_triangles": report["actual_total_triangles"]}, indent=2))
    except Exception as error:
        report["status"] = "failed_partial_output_retained"
        report["error"] = str(error)
        save_manifest()
        raise
    finally:
        bpy.context.window.scene = original_scene


def main():
    options = arguments()
    assets = build_plan()
    report = validate_plan(assets)
    if options.plan_only:
        print(json.dumps(report, indent=2))
        return
    export_batch(options, assets, report)


if __name__ == "__main__":
    main()
