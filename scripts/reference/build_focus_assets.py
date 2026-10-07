"""Run inside Blender through MCP. Original procedural study; preserves other scenes."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'FocusCore/Assets/FocusCore/Art'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE = ROOT / 'art/source'
SOURCE.mkdir(parents=True, exist_ok=True)
if bpy.data.scenes.get('FocusVisualStudy'):
    raise RuntimeError('FocusVisualStudy already exists; inspect it instead of rebuilding blindly.')
scene = bpy.data.scenes.new('FocusVisualStudy')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
collection = bpy.data.collections.new('Focus Original Assets')
scene.collection.children.link(collection)

def material(name, rgb, emission=0):
    m=bpy.data.materials.new(name)
    m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*rgb,1)
    p.inputs['Roughness'].default_value=.3
    if emission:
        p.inputs['Emission Color'].default_value=(*rgb,1)
        p.inputs['Emission Strength'].default_value=emission
    m.diffuse_color=(*rgb,1)
    return m

shell=material('Focus Shell',(.27,.32,.39))
white=material('Focus Pearl',(.78,.83,.88))
blue=material('Focus Blue',(.05,.40,.80),2)
cyan=material('Focus Cyan',(.10,.80,.88),2)
violet=material('Focus Violet',(.48,.18,.84),2)
pink=material('Focus Tag',(.93,.24,.82),2)
dark=material('Focus Panel',(.025,.035,.075))

def mesh(name, vertices, faces, mat, parent=None):
    data=bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces); data.update()
    obj=bpy.data.objects.new(name,data); collection.objects.link(obj)
    obj.data.materials.append(mat); obj.parent=parent
    return obj

def empty(name):
    obj=bpy.data.objects.new(name,None); collection.objects.link(obj)
    return obj

device=empty('FocusDevice')
tri=[(-.022,0),(.017,.028),(.021,-.022)]
def prism(name, scale, front, back, mat):
    v=[(x*scale,y,z*scale) for y in (front,back) for x,z in tri]
    return mesh(name,v,[(2,1,0),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)],mat,device)
body=prism('Device shell',1,0,.008,shell)
insert=prism('Device luminous inset',.82,-.0004,0,blue)
face=prism('Device pearl cap',.65,-.001, -.0004,white)
for obj in (body,face):
    bevel=obj.modifiers.new('Tiny machined bevel','BEVEL'); bevel.width=.0004; bevel.segments=2
for i in range(4):
    x=.006+i*.0024
    mesh('Device etched channel '+str(i),[(x,-.0015,-.007),(x+.0005,-.0015,-.007),(x+.0005,-.0015,.011),(x,-.0015,.011)],[(0,1,2,3)],shell,device)

reticle=empty('FocusReticle')
def arc(name, inner, outer, start, end, mat, parent, segments=24):
    v=[]; f=[]
    for i in range(segments+1):
        t=math.radians(start+(end-start)*i/segments)
        for r in (inner,outer): v.append((r*math.cos(t),0,r*math.sin(t)))
    for i in range(segments): f.append((2*i,2*i+1,2*i+3,2*i+2))
    return mesh(name,v,f,mat,parent)
arc('Reticle violet foundation',.270,.277,0,360,violet,reticle,96)
for i in range(72):
    arc('Reticle index %02d'%i,.282,.287,i*5,i*5+1.35,cyan if i%3 else blue,reticle,1)
for i in range(18):
    arc('Reticle telemetry %02d'%i,.299,.307,i*20+3,i*20+7,violet if i%3==0 else cyan,reticle,1)
for i in range(4):
    arc('Component wedge '+str(i),.319,.404,218+i*31,245+i*31,dark,reticle,8)
    arc('Component border '+str(i),.402,.405,218+i*31,245+i*31,pink if i==1 else blue,reticle,8)
    t=math.radians(231.5+i*31); x=.360*math.cos(t); z=.360*math.sin(t); s=.012
    mesh('Component diamond '+str(i),[(x-s,-.001,z),(x,-.001,z+s),(x+s,-.001,z),(x,-.001,z-s)],[(0,1,2,3)],white,reticle)
mesh('Reticle centre square',[(-.004,0,-.004),(-.004,0,.004),(.004,0,.004),(.004,0,-.004)],[(0,1,2,3)],white,reticle)
for obj in bpy.context.selected_objects: obj.select_set(False)
parts=list(reticle.children)
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
parts[0].name='Focus reticle combined mesh'
reticle.rotation_euler.y=0; reticle.keyframe_insert(data_path='rotation_euler',frame=1)
reticle.rotation_euler.y=math.radians(5); reticle.keyframe_insert(data_path='rotation_euler',frame=60)
reticle.rotation_euler.y=0; reticle.keyframe_insert(data_path='rotation_euler',frame=120)
scene.frame_start=1; scene.frame_end=120; scene.frame_set(1)

# An enlarged presentation instance; exported original remains fifty millimetres tall.
display=empty('Device presentation')
display.instance_type='COLLECTION'
device_collection=bpy.data.collections.new('Focus Device Export')
scene.collection.children.link(device_collection)
for obj in [device]+list(device.children):
    collection.objects.unlink(obj); device_collection.objects.link(obj)
display.instance_collection=device_collection
display.location=(-.70,0,.08); display.scale=(6,6,6)
reticle.location=(.26,0,.02)

camera_data=bpy.data.cameras.new('Focus Study Camera')
camera=bpy.data.objects.new('Focus Study Camera',camera_data); scene.collection.objects.link(camera)
camera.location=(0,-2.6,.12)
camera.rotation_euler=(Vector((-.1,0,0))-camera.location).to_track_quat('-Z','Y').to_euler()
camera_data.type='ORTHO'; camera_data.ortho_scale=1.8; scene.camera=camera
world=bpy.data.worlds.new('Focus Study World'); world.use_nodes=True
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.012,.017,.035,1)
scene.world=world
light_data=bpy.data.lights.new('Focus Study Softbox','AREA'); light_data.energy=200; light_data.shape='DISK'; light_data.size=3
light=bpy.data.objects.new('Focus Study Softbox',light_data); scene.collection.objects.link(light)
light.location=(-1,-2,2); light.rotation_euler=(Vector((0,0,0))-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1200; scene.render.resolution_y=650; scene.render.resolution_percentage=100
try: scene.render.engine='BLENDER_EEVEE'
except TypeError: pass # Keep the existing supported engine; no guessed fallbacks.

fbx_props=bpy.ops.export_scene.fbx.get_rna_type().properties
axis_forward=[e.identifier for e in fbx_props['axis_forward'].enum_items]
axis_up=[e.identifier for e in fbx_props['axis_up'].enum_items]
assert '-Z' in axis_forward and 'Y' in axis_up
for root,name in ((device,'FocusDevice'),(reticle,'FocusReticle')):
    for o in bpy.context.selected_objects: o.select_set(False)
    objects=[root]+list(root.children)
    for o in objects: o.select_set(True)
    old=root.location.copy(); root.location=(0,0,0)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    root.location=old
scene.render.filepath=str(ROOT/'.artifacts/research/focus-asset-study.png')
bpy.context.view_layer.layer_collection.children['Focus Device Export'].exclude=True
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'FocusVisualStudy.blend'))
report={'blender':bpy.app.version_string,'scene':scene.name,'device_height_metres':.05,'mesh_count':sum(o.type=='MESH' for o in scene.objects),'polygons':sum(len(o.data.polygons) for o in scene.objects if o.type=='MESH'),'animation_keyframe_frames':[1,60,120],'exports':[str(OUT/'FocusDevice.fbx'),str(OUT/'FocusReticle.fbx')]}
(ROOT/'.artifacts/research/blender-assets-evidence.json').write_text(json.dumps(report,indent=2))
assert report['polygons'] < 1500
assert all(Path(p).stat().st_size>1000 for p in report['exports'])
print(json.dumps(report))
