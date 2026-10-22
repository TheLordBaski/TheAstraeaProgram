// Run through Unity Pipeline eval_file. Creates assets without changing the open scene.
var project=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,".."));
var folder="Assets/TAP/Art/SpaceCenter";
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
void Folder(string path)
{
    if(AssetDatabase.IsValidFolder(path))return;
    var parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);
    var guid=AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));
    if(string.IsNullOrEmpty(guid))throw new Exception("Folder creation failed: "+path);
}
Folder(folder+"/Textures");Folder(folder+"/Materials");Folder(folder+"/Prefabs");
foreach(var channel in new[]{"BaseColor","Normal","MOS"})
{
    var name="SpaceCenter_"+channel+".png";var path=folder+"/Textures/"+name;
    System.IO.File.Copy(System.IO.Path.Combine(project,"Art/SpaceCenter/Textures",name),path,true);
    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
    importer.textureType=channel=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
    importer.sRGBTexture=channel=="BaseColor";importer.maxTextureSize=2048;
    importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;
    importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
}
Texture2D Map(string channel)=>AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/SpaceCenter_"+channel+".png");
var matPath=folder+"/Materials/SpaceCenter_PBR.mat";
var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
if(material==null)
{
    var shader=Shader.Find("Universal Render Pipeline/Lit");
    if(shader==null)throw new Exception("URP Lit shader unavailable");
    material=new Material(shader);AssetDatabase.CreateAsset(material,matPath);
}
material.SetTexture("_BaseMap",Map("BaseColor"));material.SetTexture("_MainTex",Map("BaseColor"));
material.SetColor("_BaseColor",Color.white);material.SetTexture("_MetallicGlossMap",Map("MOS"));
material.SetTexture("_OcclusionMap",Map("MOS"));material.SetTexture("_BumpMap",Map("Normal"));
material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.SetFloat("_BumpScale",.3f);
material.EnableKeyword("_METALLICSPECGLOSSMAP");material.EnableKeyword("_NORMALMAP");
EditorUtility.SetDirty(material);
var ids=new[]{"01_VehicleAssembly","02_LaunchComplex","03_ScienceCentre","04_TrackingStation","05_TrainingFacility","06_TestingFacility","07_MissionControl"};
var positions=new[]{new Vector3(-95,0,65),new Vector3(95,0,65),new Vector3(-125,0,-55),new Vector3(80,0,-60),new Vector3(-55,0,-145),new Vector3(100,0,-160),new Vector3(-25,0,-50)};
var checks=new System.Collections.Generic.List<object>();
var campus=new GameObject("TAP Space Centre / Facilities");
try
{
    for(int i=0;i<ids.Length;i++)
    {
        var id=ids[i];var path=folder+"/Models/"+id+".fbx";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        if(importer==null)throw new Exception("Missing importer: "+id);
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"SpaceCenter_PBR"),material);
        importer.importAnimation=false;importer.isReadable=false;importer.preserveHierarchy=true;
        importer.importTangents=ModelImporterTangents.CalculateMikk;importer.generateSecondaryUV=true;
        importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            instance.name=id;instance.transform.position=Vector3.zero;
            var renderers=instance.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0)throw new Exception("No geometry: "+id);
            if(renderers.Any(r=>r.sharedMaterials.Any(m=>m!=material)))throw new Exception("Material remap failed: "+id);
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            if(bounds.size.x<45||bounds.size.x>110||bounds.size.y<8||bounds.size.y>100||bounds.size.z<40||bounds.size.z>100)
                throw new Exception("Unexpected metre scale: "+id+" "+bounds.size);
            int triangles=instance.GetComponentsInChildren<MeshFilter>().Sum(f=>(int)f.sharedMesh.GetIndexCount(0)/3);
            var prefab=PrefabUtility.SaveAsPrefabAsset(instance,folder+"/Prefabs/"+id+".prefab");
            if(prefab==null)throw new Exception("Prefab save failed: "+id);
            var placed=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            placed.transform.SetParent(campus.transform,false);placed.transform.localPosition=positions[i];
            checks.Add(new{id,result="PASS",renderers=renderers.Length,triangles,size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},material=matPath});
        }
        finally{UnityEngine.Object.DestroyImmediate(instance);}
    }
    PrefabUtility.SaveAsPrefabAsset(campus,folder+"/Prefabs/00_SpaceCentre_Facilities.prefab");
}
finally{UnityEngine.Object.DestroyImmediate(campus);}
AssetDatabase.SaveAssets();
System.IO.File.WriteAllText(System.IO.Path.Combine(project,"Art/SpaceCenter/unity-check.json"),Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));
return new {result="PASS",facilities=checks.Count,material=matPath,prefab=folder+"/Prefabs/00_SpaceCentre_Facilities.prefab"};
