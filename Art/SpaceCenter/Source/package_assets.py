"""Render each source, create editable ArmorPaint files, and assemble a preview sheet."""
import json
import subprocess
import sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[1]
HERE=ROOT/'Source'
BLENDER=Path(r'C:\Program Files\Steam\steamapps\common\Blender\blender.exe')
ARMOR=Path(r'C:\Users\jakub\AppData\Local\Programs\ArmorPaint\ArmorPaint.exe')
facilities=json.loads((ROOT/'asset-manifest.json').read_text())['facilities']

def literal(path):return json.dumps(str(path))

def paint(name):
    project=HERE/(name+'.arm')
    source_time=max((HERE/(name+'_Paint.obj')).stat().st_mtime,
        max(p.stat().st_mtime for p in (ROOT/'Textures').glob('SpaceCenter_*.png')))
    if project.exists() and project.stat().st_mtime>source_time:return
    channels=[('BaseColor',0),('Metallic',4),('Roughness',3),('Normal',5),('AO',2)]
    imports='\n'.join('script_import_asset('+literal(ROOT/'Textures'/('SpaceCenter_'+ch+'.png'))+',false);' for ch,_ in channels)
    nodes=[]
    for i,(channel,socket) in enumerate(channels):
        nodes.append(f'ui_node_t *n{i}=script_material_create_node_at("TEX_IMAGE",-500,{i*270-400});'
            f'\nscript_material_set_button(n{i},0,{i});script_material_set_button(n{i},1,{2 if i==0 else 1});'
            f'\nscript_material_connect(n{i},0,output,{socket});')
    code='''#include "global.h"
int frames=0;
void save_ready(){
    frames=frames+1;
    if(frames<16){script_notify_on_next_frame(save_ready);}
    else {project_filepath_set(PROJECT);project_save(true);}
}
void load_maps(){
    IMPORTS
    slot_material_t *material=script_material_create("TAP Space Centre / shared trim PBR");
    script_material_set(material);
    ui_node_t *output=script_material_get_node("OUTPUT_MATERIAL_PBR");
    NODES
    script_material_update();
    slot_layer_t *initial=script_layer_at(0);
    slot_layer_t *base=script_layer_new_fill();
    script_layer_set_name(base,"01 / Shared 2K trim atlas / PBR finish");
    if(initial!=NULL){script_layer_delete(initial);}
    script_layer_set(base);script_fill_layer();
    slot_layer_t *paint=script_layer_new();
    script_layer_set_name(paint,"02 / Shared trim touchups (repeating UVs)");
    script_notify_on_next_frame(save_ready);
}
void main(){
    script_get_config()->layer_res=0;
    script_import_asset(MESH,false);
    script_notify_on_next_frame(load_maps);
}
'''.replace('PROJECT',literal(project)).replace('IMPORTS',imports).replace('NODES','\n'.join(nodes)).replace('MESH',literal(HERE/(name+'_Paint.obj')))
    script=HERE/(name+'_ArmorPaint.c');script.write_text(code)
    run=subprocess.run([str(ARMOR),'--background','--script',str(script)],cwd=ARMOR.parent,capture_output=True,text=True,timeout=120)
    (HERE/(name+'_ArmorPaint.log')).write_text(run.stdout+'\n'+run.stderr)
    assert run.returncode==0 and 'Project saved' in run.stdout and project.stat().st_size>5000,(name,run.stdout[-1500:],run.stderr)
    print('ARMORPAINT_PASS '+name,flush=True)

for rec in facilities:
    name=rec['id']
    command=[str(BLENDER),'--factory-startup','--background','--python',str(HERE/'render_and_check.py'),'--',name]
    with (HERE/(name+'_Render.log')).open('w') as log:
        run=subprocess.run(command,stdout=log,stderr=subprocess.STDOUT)
    assert run.returncode==0 and (ROOT/'Previews'/(name+'.png')).exists(),name+' render failed'
    paint(name)
    print('PACKAGED '+name,flush=True)

names={'01_VehicleAssembly':'VEHICLE ASSEMBLY BUILDING','02_LaunchComplex':'LAUNCH COMPLEX',
 '03_ScienceCentre':'SCIENCE & RESEARCH','04_TrackingStation':'TRACKING STATION',
 '05_TrainingFacility':'CREW TRAINING','06_TestingFacility':'PROPULSION TESTING','07_MissionControl':'MISSION CONTROL'}
sheet=Image.new('RGB',(2400,1940),(15,16,18));draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype('C:/Windows/Fonts/bahnschrift.ttf',22)
title=ImageFont.truetype('C:/Windows/Fonts/bahnschrift.ttf',46)
draw.text((45,20),'THE ASTRAEA PROGRAM',font=title,fill=(244,244,244))
draw.text((45,78),'SPACE CENTRE  /  MODERN INDUSTRIAL REVISION',font=font,fill=(255,196,0))
for i,rec in enumerate(facilities):
    x=(i%4)*600;y=132+(i//4)*600
    im=Image.open(ROOT/'Previews'/(rec['id']+'.png')).convert('RGB').resize((576,480),Image.Resampling.LANCZOS)
    sheet.paste(im,(x+12,y))
    draw.text((x+20,y+497),names[rec['id']],font=font,fill=(236,236,236))
    draw.text((x+20,y+528),f"{rec['triangles']:,} triangles / PBR / metres",font=font,fill=(160,164,168))
campus=Image.open(ROOT/'Previews/00_SpaceCentre_Campus.png').convert('RGB')
campus.thumbnail((1130,590),Image.Resampling.LANCZOS)
sheet.paste(campus,(1230,1320))
draw.text((45,1370),'EDITABLE BLENDER SOURCES',font=title,fill=(244,244,244))
draw.text((45,1440),'FBX + GLB + OBJ + ARMORPAINT',font=font,fill=(255,196,0))
draw.text((45,1490),'White / charcoal / stainless steel / smoked glass',font=font,fill=(160,164,168))
draw.text((45,1540),'Deep concrete frames / heavy tower / paired catch-style arms',font=font,fill=(160,164,168))
sheet.save(ROOT/'Previews/SpaceCentre_ContactSheet.png')
assert all((HERE/(r['id']+ext)).exists() for r in facilities for ext in ('.blend','.glb','.arm','_Paint.obj'))
print('PACKAGE_PASS 7/7',flush=True)
