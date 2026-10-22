"""Render the current VAB with the retained comparison camera and lighting."""
import bpy
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Source/01_VehicleAssembly.blend'))
scene=bpy.context.scene
scene.camera=next(o for o in scene.objects if o.type=='CAMERA' and o.data.type=='ORTHO')
scene.view_settings.exposure=0
if bpy.data.collections.get('VAB interior preview rig'):bpy.data.collections['VAB interior preview rig'].hide_render=True
scene.render.resolution_x=1440;scene.render.resolution_y=1200
prefs=bpy.context.preferences.addons['cycles'].preferences
try:
    prefs.compute_device_type='OPTIX';prefs.get_devices()
    for device in prefs.devices:device.use=device.type=='OPTIX'
    scene.cycles.device='GPU'
except Exception:scene.cycles.device='CPU'
scene.cycles.samples=48
scene.cycles.use_denoising=True
bpy.ops.mesh.primitive_plane_add(size=2000,location=(0,0,-.01))
ground=bpy.context.object;ground.name='Presentation backdrop'
material=bpy.data.materials.new('Presentation ground');material.use_nodes=True
material.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.065,.065,.065,1)
ground.data.materials.append(material)
scene.render.filepath=str(ROOT/'Previews/01_VehicleAssembly.png')
bpy.ops.render.render(write_still=True)
assert scene.camera.data.type=='ORTHO'
print('VAB_SCOPED_RENDER_PASS original camera and lighting',flush=True)
