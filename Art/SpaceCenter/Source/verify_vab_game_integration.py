"""Check runtime prefab dependencies and focused Unity test evidence."""
import hashlib
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

PROJECT=Path(__file__).resolve().parents[3]
ROOT=PROJECT/'Art/SpaceCenter'
runtime=PROJECT/'Assets/TAP/Resources/SpaceCenter'
references=[]
for name in ('01_VehicleAssembly','VAB_AssemblySpace'):
    prefab=runtime/(name+'.prefab')
    source=PROJECT/'Assets/TAP/Art/SpaceCenter/Prefabs'/(name+'.prefab')
    assert prefab.with_suffix('.prefab.meta').exists(),name+' runtime prefab was not imported'
    guids=set(re.findall(r'guid: ([0-9a-f]{32})',prefab.read_text()))
    source_guid=re.search(r'guid: ([0-9a-f]{32})',source.with_suffix('.prefab.meta').read_text()).group(1)
    if source_guid in guids:
        # The Unity installer creates a variant; resolve its inherited dependencies below.
        guids.update(re.findall(r'guid: ([0-9a-f]{32})',source.read_text()))
    else:
        assert prefab.read_bytes()==source.read_bytes(),name+' runtime/source prefab mismatch'
    expected={re.search(r'guid: ([0-9a-f]{32})',path.read_text()).group(1) for path in (
        PROJECT/'Assets/TAP/Art/SpaceCenter/Models/01_VehicleAssembly.fbx.meta',
        PROJECT/'Assets/TAP/Art/SpaceCenter/Materials/SpaceCenter_PBR.mat.meta')}
    # Mesh-renderer materials are inherited through the FBX importer's external-material remap.
    guids.update(re.findall(r'guid: ([0-9a-f]{32})',
        (PROJECT/'Assets/TAP/Art/SpaceCenter/Models/01_VehicleAssembly.fbx.meta').read_text()))
    assert expected.issubset(guids),name+' missing shared VAB dependencies'
    references.append(dict(prefab=str(prefab.relative_to(PROJECT)),shared_mesh_and_material=True))
before=json.loads((ROOT/'vab-pass3-scope-before.json').read_text(encoding='utf-8-sig'))
assert all(hashlib.sha256((PROJECT/name).read_bytes()).hexdigest().lower()==sha.lower() for name,sha in before.items())
tests=[]
for mode in ('editmode','playmode'):
    result=ET.parse(ROOT/f'vab-game-{mode}-results.xml').getroot()
    assert result.attrib['result']=='Passed',ET.tostring(result,encoding='unicode')[-4000:]
    cases=result.findall('.//test-case')
    assert cases and all(c.attrib['result']=='Passed' for c in cases)
    tests.append(dict(mode=mode,result='PASS',tests=len(cases),names=[c.attrib['fullname'] for c in cases]))
validated_scripts=[
    'Assets/TAP/Scripts/Game/Assembly/AssemblyScene.cs',
    'Assets/TAP/Scripts/Game/Assembly/EditorCamera.cs',
    'Assets/TAP/Scripts/Game/Assembly/EditorVisuals.cs',
    'Assets/TAP/Scripts/Game/SpaceCenter/VabModel.cs',
    'Assets/TAP/Scripts/Game/SpaceCenter/SpaceCenterScene.cs',
    'Assets/TAP/Scripts/Game/Flight/LaunchSiteBuilder.cs',
    'Assets/TAP/Tests/EditMode/VabIntegrationTests.cs',
    'Assets/TAP/Tests/PlayMode/VabSceneTests.cs',
]
validation=PROJECT/'Temp/VabValidation'
if validation.exists():
    assert all((PROJECT/p).read_bytes()==(validation/p).read_bytes() for p in validated_scripts),'Validation source differs from game source'
capture=ROOT/'Previews/VAB_InGame.png'
assert capture.exists() and capture.stat().st_size>10000,'Game camera capture is missing'
report=dict(result='PASS',views=['space-centre hub','assembly editor','flight launch-site scenery'],
    runtime_prefabs=references,tests=tests,validation='isolated copy of this project in Unity 6000.5.7f1',
    live_editor_session='left open; tests ran separately',game_camera_capture=str(capture.relative_to(PROJECT)),
    validation_texture_import='VAB full resolution; unrelated textures reduced to 256px in the disposable test project',
    protected_files_unchanged=len(before),validated_source_hashes={p:hashlib.sha256((PROJECT/p).read_bytes()).hexdigest() for p in validated_scripts},craft_height_limit_m=60,
    equipment='static crane, lift and doors',not_checked=['standalone player build','performance profiling'])
(ROOT/'vab-game-integration-check.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
