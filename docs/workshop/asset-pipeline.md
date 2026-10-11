# Optional original prop recipe

The working Unity primitives remain the current workshop visuals. These optional assets do not change Workshop controllers, authored colliders, scenes or the corrected APK milestone.

`scripts/workshop/assets/geometry_plan.py` builds the original mesh data with standard Python. `build_workshop_assets.py` consumes that same plan in a new Blender batch process. No external models, game meshes, textures, rigs, model-generation services or downloaded dependencies are used. Scripts and generated assets follow the repository's terms; this recipe adds no new licence grant.

The designs are a faceted power cell with amber bands and steel end caps, an open stepped rotor casing with a central hub/support bars and cyan bands (no blades), and a shallow chamfered console shell with an inset face and cyan status strips. The meshes combine closed, overlapping primitive shells; they are visual props, not Boolean-unioned fabrication models.

| Prop | Requested Unity dimensions (X/Y/Z metres) | Planned triangles | Materials |
| --- | --- | ---: | --- |
| Power cell | 0.075 / 0.130 / 0.075 | 896 | Slate navy, pale steel, amber |
| Rotor casing | 0.140 / 0.070 / 0.140 | 1,720 | Slate navy, pale steel, cyan |
| Console shell | 0.650 / 0.250 / 0.030 | 168 | Pale steel, slate navy, cyan |

The combined plan is **2,784 triangles**, below the 10,000 target. Each export is one mesh with three opaque material slots, an origin at its centred bounds, identity rotation/location and positive unit scale. Geometry is authored in metres with Blender Z up. FBX uses `-Z` forward and `Y` up with unit-scale export. Real Unity import dimensions/orientation still need inspection; exporter settings alone do not prove Unity's imported result. Material colours use Principled Base Color without textures; Unity can map those three slots to its existing simple opaque materials during a later authorised import.

## Checks available now

The Store `python.exe` alias on this host is an installer shortcut, so use the existing bundled Python. From the focus-core checkout:

```powershell
$workshopPython = 'C:\Users\mrbos\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
& $workshopPython -B 'scripts/workshop/assets/build_workshop_assets.py' --plan-only
& $workshopPython -B 'scripts/workshop/assets/verify_plan.py'
```

`--plan-only` imports no Blender API and writes no output. The check script parses Python syntax, checks deterministic mesh data, dimensions, centred pivots, closed edges, nondegenerate planned triangles, positive signed volume, material indices/limits and the combined triangle budget. Deliberately invalid dimensions, fourth materials, duplicate vertices, material indices, open shells and excessive totals must be rejected. These checks establish the mathematical plan and syntax, **not** Blender execution, rendered appearance, FBX correctness or Unity import.

## Deferred Blender command

Do not run this while the other chat's Blender sessions or the Workshop Editor/native build are using the shared applications/resources. Leave the traveller scene and those processes untouched. This recipe is designed for the primary mesh/material/FBX APIs of Blender 4.5 LTS and the historical local Blender 5.2.2 LTS authoring installation. Neither version has executed this new recipe yet. Versions before 4.5 are refused; future versions have no compatibility guarantee.

The authorised installed Store launcher is the existing alias below. Its batch argument forwarding is **unverified**, so a return from the launcher alone is not export proof. After resources are free and Workshop Unity is closed, from the checkout:

```powershell
$workshopBlender = Join-Path $env:LOCALAPPDATA 'Microsoft\WindowsApps\blender-launcher.exe'
$workshopRecipe = Join-Path (Get-Location) 'scripts\workshop\assets\build_workshop_assets.py'
& $workshopBlender --background --factory-startup --python-exit-code 1 --python $workshopRecipe --
```

If Store batch forwarding fails, supply an explicitly authorised standalone Blender executable to `$workshopBlender` and run the same arguments. Do not borrow or control the other chat's Blender process/installation, install another application, or route this through live Blender MCP. `scripts/workshop/Open-Blender.ps1` is an interactive bridge launcher and is not this batch command.

An optional `--run-id my-unique-run` after the final `--` selects a readable identifier; unsafe names or any existing run directory are refused. `--no-preview` after `--` skips optional preview rendering if a headless Workbench renderer is unavailable. Without it, each prop receives a 512×512 PNG rendered in Blender Workbench with an orthographic camera. PNG appearance and headless rendering remain untested. A failed run preserves partial output with a manifest status of `failed_partial_output_retained`; rerun only with a fresh run ID. The script refuses live Blender contexts or a loaded `.blend`, never deletes a scene, never saves over the caller's main file and restores its prior scene selection.

## Output and later acceptance

Each run creates two matching new folders:

- `MRWorkshop/Assets/Workshop/Art/External/OriginalProps-<run>`: three FBX files, optional PNG previews and `manifest.json`.
- `art/source/workshop/OriginalProps-<run>`: `WorkshopOptionalProps.blend` containing three separate centred prop scenes, materials and any preview cameras.

The editable `.blend` deliberately stays outside Unity `Assets`, where Unity would otherwise launch Blender as an automatic import dependency. The manifest records both directories, run time, Blender version, source-script hashes, requested/planned and actual Blender dimensions/counts, logical and actual material names, identity transforms, file sizes/hashes, original provenance and licence. It marks Unity import and Quest acceptance as unverified. It marks `blender_exported_unity_import_pending` only after the real FBX and editable source files exist and Blender's measured counts/dimensions match the plan. A preview/export exception records the failure instead.

After actual batch success, inspect all three real PNGs (or inspect the mesh scenes if previews were skipped), open the source in an authorised separate session, and import the FBX into Unity only when that step is authorised and resources are free. Check 75×130mm, 140×70mm and 650×250×30mm dimensions, centred pivots, Y-up orientation, opaque materials and visible surface winding. Keep semantic colliders as authored in Unity. Only then consider swapping a primitive visual for one of these optional meshes; preserve the fallback. This document reports recipe/plan completion only. Real Blender export, PNG inspection, Unity import and physical Quest testing remain pending.
