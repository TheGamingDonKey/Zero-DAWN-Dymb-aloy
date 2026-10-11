"""Original optional props, in metres and Blender Z-up. No bpy dependency."""
import json
import math

PALETTE = {
    "Workshop_SlateNavy": [0.035, 0.055, 0.095, 1.0],
    "Workshop_PaleSteel": [0.66, 0.73, 0.78, 1.0],
    "Workshop_Cyan": [0.025, 0.66, 0.76, 1.0],
    "Workshop_Amber": [0.95, 0.43, 0.045, 1.0],
}


def lathe(profile, material, segments=32, center=(0, 0, 0)):
    """Revolve a closed radius/Z profile. Zero-radius rings become one vertex."""
    vertices, rings, faces = [], [], []
    for radius, z in profile:
        ring = []
        for i in range(1 if radius == 0 else segments):
            angle = 2 * math.pi * i / segments
            ring.append(len(vertices))
            vertices.append((center[0] + radius * math.cos(angle),
                             center[1] + radius * math.sin(angle), center[2] + z))
        rings.append(ring)
    for index, current in enumerate(rings):
        following = rings[(index + 1) % len(rings)]
        if len(current) == len(following) == 1:
            continue
        for i in range(segments):
            j = (i + 1) % segments
            if len(current) == 1:
                faces.append((current[0], following[j], following[i]))
            elif len(following) == 1:
                faces.append((current[i], current[j], following[0]))
            else:
                faces.append((current[i], current[j], following[j], following[i]))
    return {"vertices": vertices, "faces": faces, "material": material}


def cylinder(radius, height, material, z=0, segments=32):
    return lathe([(0, -height / 2), (radius, -height / 2),
                  (radius, height / 2), (0, height / 2)], material,
                 segments=segments, center=(0, 0, z))


def panel(width, height, depth, chamfer, material, center=(0, 0, 0)):
    """Closed chamfered rectangular extrusion: X width, Y depth, Z height."""
    x, z, c = width / 2, height / 2, chamfer
    outline = [(-x + c, -z), (x - c, -z), (x, -z + c), (x, z - c),
               (x - c, z), (-x + c, z), (-x, z - c), (-x, -z + c)]
    vertices = [(px + center[0], y + center[1], pz + center[2])
                for y in (-depth / 2, depth / 2) for px, pz in outline]
    faces = [tuple(range(8)), tuple(range(15, 7, -1))]
    faces.extend((i, i + 8, (i + 1) % 8 + 8, (i + 1) % 8) for i in range(8))
    return {"vertices": vertices, "faces": faces, "material": material}


def combine(name, parts, unity_dimensions):
    vertices, faces, materials, face_materials = [], [], [], []
    for part in parts:
        if part["material"] not in materials:
            materials.append(part["material"])
        offset = len(vertices)
        vertices.extend(part["vertices"])
        faces.extend(tuple(index + offset for index in face) for face in part["faces"])
        face_materials.extend([materials.index(part["material"])] * len(part["faces"]))
    return {"name": name, "vertices": vertices, "faces": faces,
            "face_materials": face_materials, "materials": materials,
            "requested_unity_dimensions_metres": unity_dimensions}


def build_plan():
    slate, steel, cyan, amber = PALETTE
    power_parts = [lathe([(0, -.052), (.029, -.052), (.034, -.047),
                         (.034, .047), (.029, .052), (0, .052)], slate)]
    for z in (-.058, .058):
        power_parts.append(cylinder(.0375, .014, steel, z))
    for z in (-.025, 0, .025):
        power_parts.append(cylinder(.0345, .008, amber, z))
    rotor_parts = [lathe([(.065, -.035), (.07, -.030), (.07, .030),
                         (.065, .035), (.054, .035), (.050, .030),
                         (.050, -.030), (.054, -.035)], slate, segments=48),
                   cylinder(.030, .040, steel, segments=32)]
    for z in (-.023, .023):
        rotor_parts.append(lathe([(.070, -.002), (.070, .002),
                                 (.068, .002), (.068, -.002)], cyan,
                                segments=48, center=(0, 0, z)))
    # Two low internal support bars; no blade geometry or animation.
    rotor_parts.extend([panel(.108, .006, .009, .001, steel, (0, 0, -.021)),
                        panel(.009, .006, .108, .001, steel, (0, 0, -.021))])
    # Front panel is flush at -15mm; keep the whole shell 30mm deep.
    console_parts = [panel(.650, .250, .030, .025, steel),
                     panel(.556, .176, .002, .014, slate, (0, -.014, 0))]
    for z in (-.086, .086):
        console_parts.append(panel(.506, .004, .001, .001, cyan, (0, -.0145, z)))
    for x in (-.302, .302):
        console_parts.append(panel(.010, .086, .001, .003, cyan, (x, -.0145, 0)))
    return [combine("WorkshopPowerCell", power_parts, [.075, .130, .075]),
            combine("WorkshopRotorCasing", rotor_parts, [.140, .070, .140]),
            combine("WorkshopConsoleShell", console_parts, [.650, .250, .030])]


def summarise(asset):
    minimum = [min(v[axis] for v in asset["vertices"]) for axis in range(3)]
    maximum = [max(v[axis] for v in asset["vertices"]) for axis in range(3)]
    size = [maximum[i] - minimum[i] for i in range(3)]
    return {"name": asset["name"], "vertex_count": len(asset["vertices"]),
            "triangle_count": sum(len(f) - 2 for f in asset["faces"]),
            "blender_dimensions_metres": size,
            "unity_dimensions_metres": [size[0], size[2], size[1]],
            "bounds_center_metres": [(minimum[i] + maximum[i]) / 2 for i in range(3)],
            "materials": asset["materials"]}


def validate_plan(assets):
    summaries = []
    for asset in assets:
        result = summarise(asset)
        if len(asset["materials"]) > 3:
            raise ValueError("Too many material slots: " + asset["name"])
        if len(asset["faces"]) != len(asset["face_materials"]):
            raise ValueError("Face material count does not match")
        if any(abs(x - y) > 1e-9 for x, y in zip(
                result["unity_dimensions_metres"], asset["requested_unity_dimensions_metres"])):
            raise ValueError("Wrong requested bounds: " + asset["name"])
        if any(abs(value) > 1e-9 for value in result["bounds_center_metres"]):
            raise ValueError("Pivot is not centred: " + asset["name"])
        for face, slot in zip(asset["faces"], asset["face_materials"]):
            if len(face) < 3 or len(set(face)) != len(face):
                raise ValueError("Invalid face")
            if any(index < 0 or index >= len(asset["vertices"]) for index in face):
                raise ValueError("Invalid vertex index")
            if not 0 <= slot < len(asset["materials"]):
                raise ValueError("Invalid material index")
        # Each closed primitive remains closed after combination; no Boolean baking.
        edges = {}
        signed_volume = 0.0
        for face in asset["faces"]:
            for i, index in enumerate(face):
                edge = tuple(sorted((index, face[(i + 1) % len(face)])))
                edges[edge] = edges.get(edge, 0) + 1
            a = asset["vertices"][face[0]]
            for i in range(1, len(face) - 1):
                b, c = asset["vertices"][face[i]], asset["vertices"][face[i + 1]]
                ab, ac = [b[j] - a[j] for j in range(3)], [c[j] - a[j] for j in range(3)]
                cross = [ab[1]*ac[2]-ab[2]*ac[1], ab[2]*ac[0]-ab[0]*ac[2], ab[0]*ac[1]-ab[1]*ac[0]]
                if sum(value * value for value in cross) < 1e-22:
                    raise ValueError("Degenerate planned triangle: " + asset["name"])
                signed_volume += (a[0]*(b[1]*c[2]-b[2]*c[1]) +
                                  a[1]*(b[2]*c[0]-b[0]*c[2]) +
                                  a[2]*(b[0]*c[1]-b[1]*c[0])) / 6
        if any(count != 2 for count in edges.values()) or signed_volume <= 0:
            raise ValueError("Unclosed or inverted planned geometry: " + asset["name"])
        summaries.append(result)
    total = sum(item["triangle_count"] for item in summaries)
    if total >= 10000:
        raise ValueError("Total triangle budget exceeded")
    return {"status": "pure_python_plan_verified_not_exported", "total_triangles": total,
            "coordinate_system": "metres, Blender Z-up; FBX -Z forward / Y up",
            "palette": PALETTE, "assets": summaries}


if __name__ == "__main__":
    print(json.dumps(validate_plan(build_plan()), indent=2))
