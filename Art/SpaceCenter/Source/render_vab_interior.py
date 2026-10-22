"""Save a lit, editable preview rig and render the actual enclosed VAB interior."""
import bpy
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Source/01_VehicleAssembly.blend'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene=bpy.context.scene
old=bpy.data.collections.get('VAB interior preview rig')
if old:
    for obj in list(old.objects):bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.collections.remove(old)
rig=bpy.data.collections.new('VAB interior preview rig');scene.collection.children.link(rig)

def area(name,p,target,power,size):
    light=bpy.data.lights.new(name,'AREA');light.energy=power;light.shape='DISK';light.size=size
    light.color=(1,1,1)
    obj=bpy.data.objects.new(name,light);rig.objects.link(obj);obj.location=p
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

for z,power in ((8,1300),(28,2300),(48,2300),(68,18000)):
    for x in (-21,13):
        for y in (-7,6,18):area('Interior service luminaire',(x,y,z),(x+(-4-x)*.35,y,z-6),power,3)
for y in (-6,6,18):area('High bay luminaire',(-4,y,74.4),(-4,y,0),42000,8)
area('Open door daylight',(-4,-15,30),(-4,24,22),42000,22)

cameras=[]
for name,p,target,lens in (
    ('VAB_Interior_Assembly',(-4,-26,28),(-4,12,33),15),
    ('VAB_Interior_Galleries',(-16,-9,34),(3,17,32),17),
    ('VAB_Interior_Workshop',(-12,-10,3.1),(16,6,7),24),
):
    data=bpy.data.cameras.new(name);data.lens=lens;data.clip_start=.05;data.clip_end=400
    camera=bpy.data.objects.new(name,data);rig.objects.link(camera);camera.location=p
    camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    cameras.append(camera)
prefs=bpy.context.preferences.addons['cycles'].preferences
try:
    prefs.compute_device_type='OPTIX';prefs.get_devices()
    for device in prefs.devices:device.use=device.type=='OPTIX'
    scene.cycles.device='GPU'
except Exception:scene.cycles.device='CPU'
scene.cycles.samples=64;scene.cycles.use_denoising=True
scene.view_settings.exposure=-1.5
scene.render.resolution_x=1600;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
scene.camera=cameras[0]
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
for camera in cameras:
    scene.camera=camera;scene.render.filepath=str(ROOT/'Previews'/(camera.name+'.png'))
    bpy.ops.render.render(write_still=True)
    print('VAB_INTERIOR_RENDER_PASS',camera.name,flush=True)
