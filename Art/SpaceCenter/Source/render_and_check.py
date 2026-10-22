"""Isolate, save and render the authored facilities; re-import every FBX."""
import bpy
import json
import sys
import math
from mathutils import Vector
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
PROJECT=ROOT.parents[1]
manifest=json.loads((ROOT/'asset-manifest.json').read_text())
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
target=args[0] if args else 'campus'
rear=len(args)>1 and args[1]=='rear'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'SpaceCentre_Campus.blend'))
scene=bpy.context.scene
prefs=bpy.context.preferences.addons['cycles'].preferences
try:
    prefs.compute_device_type='OPTIX';prefs.get_devices()
    for device in prefs.devices:device.use=device.type=='OPTIX'
    scene.cycles.device='GPU'
except Exception:scene.cycles.device='CPU'
scene.cycles.samples=48
scene.cycles.use_denoising=True
if target=='campus':
    scene.render.filepath=str(ROOT/'Previews/00_SpaceCentre_Campus.png')
else:
    rec=next(r for r in manifest['facilities'] if r['id']==target)
    col=bpy.data.collections[target]
    for other in list(scene.collection.children):
        if other!=col:
            for obj in list(other.all_objects):bpy.data.objects.remove(obj,do_unlink=True)
            bpy.data.collections.remove(other)
    offset=Vector(rec['campus_offset'])
    for obj in col.all_objects:obj.location-=offset
    bpy.context.view_layer.update()
    points=[o.matrix_world@Vector(v) for o in col.all_objects if o.type=='MESH' for v in o.bound_box]
    lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center=(lo+hi)/2
    cam=scene.camera;cam.location=center+Vector((-105,170,88) if rear else (105,-170,88))
    cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
    camera_inverse=cam.rotation_euler.to_matrix().transposed()
    projected=[camera_inverse@(p-center) for p in points]
    width=max(p.x for p in projected)-min(p.x for p in projected)
    height=max(p.y for p in projected)-min(p.y for p in projected)
    cam.data.ortho_scale=max(width,height*1.2)*1.16
    scene.render.resolution_x=1440;scene.render.resolution_y=1200
    scene.render.filepath=str(ROOT/'Previews'/(target+('_Rear' if rear else '')+'.png'))
    # Sources contain only their own facility plus a camera and daylight rig.
    for other_scene in list(bpy.data.scenes):
        if other_scene!=scene:bpy.data.scenes.remove(other_scene)
    scene.name=target
    if not rear:bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Source'/(target+'.blend')))
    # A neutral ground catcher is presentation-only and excluded from saved sources.
    bpy.ops.mesh.primitive_plane_add(size=2000,location=(0,0,-.01))
    ground=bpy.context.object;ground.name='Preview backdrop'
    material=bpy.data.materials.new('Preview background');material.use_nodes=True
    material.diffuse_color=(.065,.065,.065,1)
    material.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.065,.065,.065,1)
    ground.data.materials.append(material)
bpy.ops.render.render(write_still=True)
print('RENDER_PASS '+target,flush=True)

if target=='campus':
    checks=[]
    for rec in manifest['facilities']:
        name=rec['id'];temp=bpy.data.scenes.new('Validate '+name);bpy.context.window.scene=temp
        bpy.ops.import_scene.fbx(filepath=str(PROJECT/'Assets/TAP/Art/SpaceCenter/Models'/(name+'.fbx')))
        meshes=[o for o in temp.objects if o.type=='MESH']
        assert meshes and all(o.data.uv_layers for o in meshes),name+' UV missing'
        triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
        assert triangles>=rec['triangles']*.98,(name,triangles,rec['triangles'])
        assert all(len(o.data.materials)==1 for o in meshes),name+' material mismatch'
        assert all(math.isfinite(c) for o in meshes for v in o.data.vertices for c in v.co),name+' invalid vertex'
        bpy.context.view_layer.update()
        bounds=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
        extent=[max(p[i] for p in bounds)-min(p[i] for p in bounds) for i in range(3)]
        assert all(abs(a-b)<.1 for a,b in zip(extent,rec['bounds_blender_xyz'])),(name,extent,rec['bounds_blender_xyz'])
        checks.append(dict(id=name,result='PASS',triangles=triangles,meshes=len(meshes),bounds_blender_xyz=extent,uv=True))
        for obj in list(temp.objects):bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.scenes.remove(temp)
    (ROOT/'export-check.json').write_text(json.dumps(checks,indent=2))
    print('FBX_ROUNDTRIP_PASS 7/7',flush=True)
