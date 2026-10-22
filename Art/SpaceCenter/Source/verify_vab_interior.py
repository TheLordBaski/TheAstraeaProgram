"""Inspect the saved VAB, not just the generation recipe, including its empty envelope."""
import bpy
import json
import math
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from vab_interior import CLEAR_MIN,CLEAR_MAX

ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Source/01_VehicleAssembly.blend'))
deps=bpy.context.evaluated_depsgraph_get()
report=[]
for obj in bpy.data.collections['01_VehicleAssembly'].objects:
    if obj.type!='MESH':continue
    evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
    assert mesh.uv_layers,obj.name
    zero=0
    for tri in mesh.loop_triangles:
        points=[obj.matrix_world@mesh.vertices[i].co for i in tri.vertices]
        assert all(math.isfinite(v) for p in points for v in p),obj.name
        if (points[1]-points[0]).cross(points[2]-points[0]).length<1e-10:zero+=1
        lo=[min(p[i] for p in points) for i in range(3)]
        hi=[max(p[i] for p in points) for i in range(3)]
        assert not all(hi[i]>CLEAR_MIN[i]+.001 and lo[i]<CLEAR_MAX[i]-.001 for i in range(3)),obj.name
    report.append(dict(mesh=obj.name,triangles=len(mesh.loop_triangles),zero_area_triangles=zero))
    evaluated.to_mesh_clear()
result=dict(result='PASS',triangles=sum(x['triangles'] for x in report),
    zero_area_triangles=sum(x['zero_area_triangles'] for x in report),meshes=report,
    clear_envelope_unobstructed=True,all_meshes_have_uvs=True,finite_vertices=True)
(ROOT/'vab-interior-geometry-check.json').write_text(json.dumps(result,indent=2))
print('VAB_INTERIOR_GEOMETRY_PASS',result['triangles'],'triangles;',result['zero_area_triangles'],'zero area',flush=True)
