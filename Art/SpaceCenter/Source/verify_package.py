"""Check the delivered package and Unity/FBX evidence without rebuilding assets."""
import json
import struct
from io import BytesIO
from pathlib import Path
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
PROJECT=ROOT.parents[1]
manifest=json.loads((ROOT/'asset-manifest.json').read_text())
exports=json.loads((ROOT/'export-check.json').read_text())
unity=json.loads((ROOT/'unity-check.json').read_text(encoding='utf-8-sig'))
assert len(exports)==len(unity)==len(manifest['facilities'])==7
expected_base=np.array(Image.open(ROOT/'Textures/SpaceCenter_BaseColor.png').convert('RGB'))
texture_time=max(p.stat().st_mtime for p in (ROOT/'Textures').glob('SpaceCenter_*.png'))
for rec in manifest['facilities']:
    name=rec['id']
    for ext in ('.blend','.glb','.arm','_Paint.obj'):
        path=ROOT/'Source'/(name+ext)
        assert path.stat().st_size>5000,path
        if ext in ('.blend','.glb','.arm'):assert path.stat().st_mtime>=texture_time,str(path)+' has old surfaces'
    blob=(ROOT/'Source'/(name+'.glb')).read_bytes()
    magic,version,length=struct.unpack_from('<III',blob)
    assert magic==0x46546c67 and version==2 and length==len(blob),name+' GLB header'
    json_length,chunk_type=struct.unpack_from('<II',blob,12)
    assert chunk_type==0x4e4f534a
    data=json.loads(blob[20:20+json_length])
    binary_start=20+json_length+8
    for material in data['materials']:
        texture=material['pbrMetallicRoughness']['baseColorTexture']['index']
        image=data['images'][data['textures'][texture]['source']]
        view=data['bufferViews'][image['bufferView']]
        start=binary_start+view.get('byteOffset',0)
        pixels=np.array(Image.open(BytesIO(blob[start:start+view['byteLength']])).convert('RGB'))
        assert np.array_equal(pixels,expected_base),name+' stale GLB palette'
    assert len(data['meshes'])>0 and all('bufferView' in image for image in data['images']),name+' embedded GLB images'
    assert all('TEXCOORD_0' in primitive['attributes'] for mesh in data['meshes'] for primitive in mesh['primitives']),name+' GLB UV'
    assert next(x for x in exports if x['id']==name)['result']=='PASS'
    imported=next(x for x in unity if x['id']==name)
    assert imported['result']=='PASS' and imported['triangles']==rec['triangles'],name+' Unity mesh mismatch'
    assert (PROJECT/'Assets/TAP/Art/SpaceCenter/Prefabs'/(name+'.prefab')).exists()
for channel in ('BaseColor','Metallic','Roughness','Normal','AO','ORM','MOS'):
    with Image.open(ROOT/'Textures'/('SpaceCenter_'+channel+'.png')) as image:assert image.size==(2048,2048)
rough=np.array(Image.open(ROOT/'Textures/SpaceCenter_Roughness.png'))
mos=np.array(Image.open(ROOT/'Textures/SpaceCenter_MOS.png'))
assert np.array_equal(mos[:,:,3],255-rough)
surfaces=json.loads((ROOT/'Textures/surface-index.json').read_text())
assert all(max(surfaces[i][1])-min(surfaces[i][1])<=8 for i in (0,1,2,3,5,7,8,9,12))
assert surfaces[4][1]==[13,23,34] and surfaces[6][1]==[255,196,0]
runtime=PROJECT/'Assets/TAP/Art/SpaceCenter/Textures'
for channel in ('BaseColor','Normal','MOS'):
    filename='SpaceCenter_'+channel+'.png'
    assert (runtime/filename).read_bytes()==(ROOT/'Textures'/filename).read_bytes(),filename+' stale Unity palette'
assert (ROOT/'Previews/SpaceCentre_ContactSheet.png').exists()
report=dict(result='PASS',facilities=7,triangles=manifest['total_triangles'],
    checks=['source files','embedded GLB textures','GLB palette pixel parity','GLB UVs','FBX re-import','Unity triangle parity','URP prefabs','PBR map size and packing','neutral architectural palette','Unity texture byte parity'])
(ROOT/'package-check.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
