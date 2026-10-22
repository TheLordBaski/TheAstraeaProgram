#include "global.h"
int frames=0;
void save_ready(){
    frames=frames+1;
    if(frames<16){script_notify_on_next_frame(save_ready);}
    else {project_filepath_set("D:\\Dev\\KSP\\Art\\SpaceCenter\\Source\\07_MissionControl.arm");project_save(true);}
}
void load_maps(){
    script_import_asset("D:\\Dev\\KSP\\Art\\SpaceCenter\\Textures\\SpaceCenter_BaseColor.png",false);
script_import_asset("D:\\Dev\\KSP\\Art\\SpaceCenter\\Textures\\SpaceCenter_Metallic.png",false);
script_import_asset("D:\\Dev\\KSP\\Art\\SpaceCenter\\Textures\\SpaceCenter_Roughness.png",false);
script_import_asset("D:\\Dev\\KSP\\Art\\SpaceCenter\\Textures\\SpaceCenter_Normal.png",false);
script_import_asset("D:\\Dev\\KSP\\Art\\SpaceCenter\\Textures\\SpaceCenter_AO.png",false);
    slot_material_t *material=script_material_create("TAP Space Centre / shared trim PBR");
    script_material_set(material);
    ui_node_t *output=script_material_get_node("OUTPUT_MATERIAL_PBR");
    ui_node_t *n0=script_material_create_node_at("TEX_IMAGE",-500,-400);
script_material_set_button(n0,0,0);script_material_set_button(n0,1,2);
script_material_connect(n0,0,output,0);
ui_node_t *n1=script_material_create_node_at("TEX_IMAGE",-500,-130);
script_material_set_button(n1,0,1);script_material_set_button(n1,1,1);
script_material_connect(n1,0,output,4);
ui_node_t *n2=script_material_create_node_at("TEX_IMAGE",-500,140);
script_material_set_button(n2,0,2);script_material_set_button(n2,1,1);
script_material_connect(n2,0,output,3);
ui_node_t *n3=script_material_create_node_at("TEX_IMAGE",-500,410);
script_material_set_button(n3,0,3);script_material_set_button(n3,1,1);
script_material_connect(n3,0,output,5);
ui_node_t *n4=script_material_create_node_at("TEX_IMAGE",-500,680);
script_material_set_button(n4,0,4);script_material_set_button(n4,1,1);
script_material_connect(n4,0,output,2);
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
    script_import_asset("D:\\Dev\\KSP\\Art\\SpaceCenter\\Source\\07_MissionControl_Paint.obj",false);
    script_notify_on_next_frame(load_maps);
}
