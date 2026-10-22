"""Record the final VAB package evidence and guard the six untouched facilities."""
import hashlib
import json
import re
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
PROJECT=ROOT.parents[1]
before=json.loads((ROOT/'vab-pass3-scope-before.json').read_text(encoding='utf-8-sig'))
changed=[name for name,sha in before.items() if hashlib.sha256((PROJECT/name).read_bytes()).hexdigest().lower()!=sha.lower()]
assert not changed,changed
geometry=json.loads((ROOT/'vab-interior-geometry-check.json').read_text())
unity=json.loads((ROOT/'vab-interior-unity-check.json').read_text())
package=json.loads((ROOT/'package-check.json').read_text())
assert geometry['result']==unity['result']==package['result']=='PASS'
assert geometry['zero_area_triangles']==0
assert geometry['triangles']==unity['triangles']==132020
assert unity['clearVolumeUnobstructed'] and unity['assemblyFloorRaycast']
blob=(ROOT/'Source/01_VehicleAssembly.glb').read_bytes()
length=struct.unpack_from('<I',blob,12)[0]
data=json.loads(blob[20:20+length])
assert all(any(node.get('name')==name for node in data['nodes']) for name in ('RocketAssemblyOrigin','ClearVolumeMin','ClearVolumeMax'))
assert sum(data['accessors'][p['indices']]['count']//3 for m in data['meshes'] for p in m['primitives'])==132020
for image in ('VAB_Interior_Assembly','VAB_Interior_Galleries','VAB_Interior_Workshop','01_VehicleAssembly'):
    assert (ROOT/'Previews'/(image+'.png')).stat().st_size>100000

# Existing campus overrides still target real IDs after the variant gained its new wrapper root.
prefabs=PROJECT/'Assets/TAP/Art/SpaceCenter/Prefabs'
full=(prefabs/'01_VehicleAssembly.prefab').read_text()
campus=(prefabs/'00_SpaceCentre_Facilities.prefab').read_text()
guid=re.search(r'guid: (\w+)',(prefabs/'01_VehicleAssembly.prefab.meta').read_text()).group(1)
ids=set(re.findall(r'^--- !u!\d+ &(-?\d+)',full,re.M))
targets=re.findall(r'fileID: (-?\d+), guid: '+guid,campus)
assert targets and all(i in ids or i=='100100000' for i in targets),targets
assert 'm_Name: VAB_AssemblySpace' in (prefabs/'VAB_AssemblySpace.prefab').read_text()
scene=PROJECT/'Assets/TAP/Art/SpaceCenter/Scenes/VAB_Interior_Preview.unity'
assert scene.stat().st_size>1000
result=dict(result='PASS',triangles=132020,mesh_renderers=unity['renderers'],colliders=unity['colliders'],
    reserved_width_depth_height_m=[24,30,67.9],central_geometry_empty=True,central_collision_empty=True,
    assembly_origin_centred=True,floor_collision_raycast=True,zero_area_triangles=0,
    fbx_roundtrip=True,unity_import=True,glb_triangles_and_markers=True,shared_material=True,
    armorpaint='refreshed and saved',protected_files_unchanged=len(before),campus_prefab_references_valid=True,
    preview_scene=str(scene.relative_to(PROJECT)),
    not_checked=['built-game performance','player traversal','assembly-screen integration'],
    static_equipment=['crane','freight lift','sliding door leaves'])
(ROOT/'vab-interior-check.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
