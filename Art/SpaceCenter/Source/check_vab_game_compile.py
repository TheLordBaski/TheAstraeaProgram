"""Compile against Unity's generated references without touching its running Editor/cache."""
from pathlib import Path
import json
import subprocess

PROJECT=Path(__file__).resolve().parents[3]
OUTPUT=PROJECT/'Temp/VabCompileCheck'
OUTPUT.mkdir(parents=True,exist_ok=True)
BEE=PROJECT/'Library/Bee/artifacts/1900b0aEDbg.dag'
UNITY=Path('D:/Programs/Unity/6000.5.7f1/Editor/Data')
DOTNET=UNITY/'DotNetSdk/dotnet.exe'
CSC=UNITY/'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
extra={
    'TAP.Game':['Assets/TAP/Scripts/Game/SpaceCenter/VabModel.cs'],
    'TAP.Editor':['Assets/TAP/Scripts/Editor/VabInteriorInstaller.cs'],
    'TAP.Tests.EditMode':['Assets/TAP/Tests/EditMode/VabIntegrationTests.cs'],
    'TAP.Tests.PlayMode':['Assets/TAP/Tests/PlayMode/VabSceneTests.cs'],
}
results=[]
for assembly,sources in extra.items():
    template=BEE/(assembly+'.rsp')
    if not template.exists():
        assert assembly=='TAP.Tests.PlayMode'
        # The formerly empty PlayMode assembly has no generated response file yet.
        template=BEE/'TAP.Tests.EditMode.rsp'
        lines=[line for line in template.read_text().splitlines() if not line.strip('"').endswith('.cs') and 'UNITY_EDITOR' not in line and 'TAP.Tests.EditMode.UnityAdditionalFile.txt' not in line]
    else:lines=template.read_text().splitlines()
    content='\n'.join(lines)
    content=content.replace('-out:"Library/Bee/artifacts/1900b0aEDbg.dag/'+assembly+'.dll"','-out:"'+(OUTPUT/(assembly+'.dll')).as_posix()+'"')
    if assembly=='TAP.Tests.PlayMode':
        content=content.replace('-out:"Library/Bee/artifacts/1900b0aEDbg.dag/TAP.Tests.EditMode.dll"','-out:"'+(OUTPUT/(assembly+'.dll')).as_posix()+'"')
        content=content.replace('-refout:"Library/Bee/artifacts/1900b0aEDbg.dag/TAP.Tests.EditMode.ref.dll"','-refout:"'+(OUTPUT/(assembly+'.ref.dll')).as_posix()+'"')
    content=content.replace('-refout:"Library/Bee/artifacts/1900b0aEDbg.dag/'+assembly+'.ref.dll"','-refout:"'+(OUTPUT/(assembly+'.ref.dll')).as_posix()+'"')
    if assembly!='TAP.Game':content=content.replace('Library/Bee/artifacts/1900b0aEDbg.dag/TAP.Game.ref.dll',(OUTPUT/'TAP.Game.ref.dll').as_posix())
    for source in sources:
        if source not in content:content+='\n"'+source+'"'
    response=OUTPUT/(assembly+'.rsp');response.write_text(content)
    run=subprocess.run([str(DOTNET),str(CSC),'@'+str(response)],cwd=PROJECT,capture_output=True,text=True)
    (OUTPUT/(assembly+'.log')).write_text(run.stdout+run.stderr)
    assert run.returncode==0,(assembly,run.stdout,run.stderr)
    print(assembly+' COMPILE_PASS',flush=True)
    results.append(dict(assembly=assembly,result='PASS'))
(PROJECT/'Art/SpaceCenter/vab-game-compile-check.json').write_text(json.dumps(dict(result='PASS',compiler='Unity Roslyn with generated project response files',assemblies=results,unity_editor_test_run=False),indent=2))
