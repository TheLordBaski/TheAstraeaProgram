"""Rebuild only the VAB and replace only its campus collection, preserving the view."""
import bpy
import json
from pathlib import Path
from mathutils import Vector

HERE=Path(__file__).resolve().parent
ROOT=HERE.parent
PROJECT=ROOT.parents[1]
NAME='01_VehicleAssembly'
source=HERE/(NAME+'.blend')
bpy.ops.wm.open_mainfile(filepath=str(source))
old=bpy.context.scene
rig=[o for o in old.objects if o.type in {'CAMERA','LIGHT'} and not any(c.name=='VAB interior preview rig' for c in o.users_collection)]
camera=next(o for o in rig if o.type=='CAMERA' and o.data.type=='ORTHO');world=old.world
resolution=(old.render.resolution_x,old.render.resolution_y)
for obj in list(old.objects):
    if obj not in rig:bpy.data.objects.remove(obj,do_unlink=True)
for col in list(bpy.data.collections):bpy.data.collections.remove(col)
for mesh in list(bpy.data.meshes):
    if mesh.users==0:bpy.data.meshes.remove(mesh)
for material in list(bpy.data.materials):
    if material.users==0:bpy.data.materials.remove(material)
# Reuse the authoring helpers without running the seven-facility export loop.
builder=HERE/'build_space_center.py'
prefix,marker,_=builder.read_text().partition('\nreport=[]')
assert marker,'Shared authoring definitions not found'
api={'__file__':str(builder)}
exec(compile(prefix,str(builder),'exec'),api)
scene=api['scene'];scene.name=NAME;scene.world=world;scene.camera=camera
for obj in rig:scene.collection.objects.link(obj)
scene.render.resolution_x,scene.render.resolution_y=resolution
scene.render.resolution_percentage=100
scene.cycles.samples=48
B=api['Builder'](NAME,(0,0,0));api['B']=B
api['vab']();B.finish()
# Sliding leaves are separate meshes, parked open for access to the finished interior.
next(o for o in B.objects if o.name=='VAB Door Left').location.x-=12.6
next(o for o in B.objects if o.name=='VAB Door Right').location.x+=12.6
for obj in B.objects:
    for modifier in obj.modifiers:
        if modifier.type=='BEVEL':modifier.width=.005
    if obj.type=='FONT':
        api['select']([obj]);bpy.ops.object.convert(target='MESH')
        obj.data.materials.clear();obj.data.materials.append(api['mat'])
        uv=obj.data.uv_layers.new(name='UVMap')
        for loop in uv.data:loop.uv=(.375,.375)
api['select'](B.objects+api['vab_markers'])
bpy.context.view_layer.update()
triangles,vertices=api['evaluated_stats'](B.objects)
deps=bpy.context.evaluated_depsgraph_get()
corners=[o.matrix_world@Vector(v) for o in B.objects for v in o.evaluated_get(deps).bound_box]
bounds=[max(p[i] for p in corners)-min(p[i] for p in corners) for i in range(3)]
assert 70<bounds[2]<80 and triangles>10000
assert all(o.type=='MESH' and o.data.uv_layers for o in B.objects)
scene['VAB revision']='Hollow high bay, perimeter galleries and stores, connected stairs, freight lift and parked overhead hoist. Clear centre 24 x 30 x 67.9 m above floor.'

# Every polygon is checked, rather than mesh bounds that can span both perimeter walls.
from vab_interior import CLEAR_MIN,CLEAR_MAX,ASSEMBLY_ORIGIN
violations=[]
for obj in B.objects:
    evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh()
    for polygon in mesh.polygons:
        points=[obj.matrix_world@mesh.vertices[i].co for i in polygon.vertices]
        lo=[min(p[i] for p in points) for i in range(3)]
        hi=[max(p[i] for p in points) for i in range(3)]
        if all(hi[i]>CLEAR_MIN[i]+.001 and lo[i]<CLEAR_MAX[i]-.001 for i in range(3)):
            violations.append(obj.name);break
    evaluated.to_mesh_clear()
assert not violations,('Geometry in reserved assembly envelope',violations)
colliders=api['vab_colliders']
colliders.extend([
    dict(name='Apron',center=(0,0,.15),size=(80,74,.3)),
    dict(name='West wall',center=(-28.89,6,38),size=(.22,42,75.4)),
    dict(name='East wall',center=(20.89,6,38),size=(.22,42,75.4)),
    dict(name='Rear wall',center=(-4,26.89,38),size=(49.56,.22,75.4)),
    dict(name='Left front wall',center=(-22.75,-14.89,38),size=(12.5,.22,75.4)),
    dict(name='Right front wall',center=(14.75,-14.89,38),size=(12.5,.22,75.4)),
    dict(name='Door header',center=(-4,-14.89,67.9),size=(25,.22,15.6)),
    dict(name='Roof',center=(-4,6,75.59),size=(49.56,41.56,.22)),
    dict(name='Low bay',center=(25,6,11),size=(10,42,21.4)),
    *[dict(name='Office annex',center=(x,-23,8.1),size=(10,15,15.6)) for x in (-24,16)],
])
assert all(not all(c['center'][i]+c['size'][i]/2>CLEAR_MIN[i] and c['center'][i]-c['size'][i]/2<CLEAR_MAX[i] for i in range(3)) for c in colliders)
interior=dict(result='GEOMETRY_PASS',units='metres',blender_z_up=True,assembly_origin=ASSEMBLY_ORIGIN,
    clear_min=CLEAR_MIN,clear_max=CLEAR_MAX,clear_size=[CLEAR_MAX[i]-CLEAR_MIN[i] for i in range(3)],
    max_rocket_height=68,colliders=colliders,triangles=triangles,meshes=len(B.objects),
    stair_riser_m=2.5/14,stair_going_m=5.2/14,stair_clear_width_m=1.65,
    door_clear_width_m=25,door_clear_height_m=59.8,
    doors={'VAB Door Left':[-12.6,0,0],'VAB Door Right':[12.6,0,0]})
(ROOT/'vab-interior-manifest.json').write_text(json.dumps(interior,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(source))
fbx=PROJECT/'Assets/TAP/Art/SpaceCenter/Models'/(NAME+'.fbx')
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,
    axis_forward='-Z',axis_up='Y',bake_anim=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(HERE/(NAME+'.glb')),use_selection=True,export_format='GLB',export_yup=True)
bpy.ops.wm.obj_export(filepath=str(HERE/(NAME+'_Paint.obj')),export_selected_objects=True,
    forward_axis='NEGATIVE_Z',up_axis='Y',export_materials=False)
manifest=json.loads((ROOT/'asset-manifest.json').read_text())
record=next(r for r in manifest['facilities'] if r['id']==NAME)
record.update(triangles=triangles,vertices=vertices,meshes=len(B.objects),bounds_blender_xyz=bounds)
manifest['total_triangles']=sum(r['triangles'] for r in manifest['facilities'])
(ROOT/'asset-manifest.json').write_text(json.dumps(manifest,indent=2))

# Round-trip only this FBX; the six other exports remain untouched.
check_scene=bpy.data.scenes.new('VAB roundtrip');bpy.context.window.scene=check_scene
bpy.ops.import_scene.fbx(filepath=str(fbx))
meshes=[o for o in check_scene.objects if o.type=='MESH']
assert meshes and all(o.data.uv_layers for o in meshes)
imported=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
assert imported==triangles,(imported,triangles)
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
extent=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
assert all(abs(a-b)<.1 for a,b in zip(extent,bounds))
checks=json.loads((ROOT/'export-check.json').read_text())
checks[next(i for i,r in enumerate(checks) if r['id']==NAME)]=dict(id=NAME,result='PASS',triangles=imported,meshes=len(meshes),bounds_blender_xyz=extent,uv=True)
(ROOT/'export-check.json').write_text(json.dumps(checks,indent=2))

# Update the VAB in the editable master without rebuilding any other facility.
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'SpaceCentre_Campus.blend'))
campus=bpy.context.scene
old_col=bpy.data.collections[NAME]
for obj in list(old_col.all_objects):bpy.data.objects.remove(obj,do_unlink=True)
bpy.data.collections.remove(old_col)
with bpy.data.libraries.load(str(source),link=False) as (available,loaded):loaded.collections=[NAME]
new_col=loaded.collections[0];campus.collection.children.link(new_col)
material=bpy.data.materials.get('SpaceCenter_PBR')
for obj in new_col.all_objects:
    obj.location+=Vector(record['campus_offset'])
    if obj.type=='MESH' and material:
        obj.data.materials.clear();obj.data.materials.append(material)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SpaceCentre_Campus.blend'))
print('VAB_SCOPED_EXPORT_PASS',triangles,bounds,flush=True)
