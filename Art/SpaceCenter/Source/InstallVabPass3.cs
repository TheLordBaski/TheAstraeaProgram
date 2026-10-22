// Update only the VAB model and prefab. Shared material and six other facilities are read-only.
var root="Assets/TAP/Art/SpaceCenter";
var path=root+"/Models/01_VehicleAssembly.fbx";
var material=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/SpaceCenter_PBR.mat");
if(material==null)throw new Exception("Existing space-centre material missing");
AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
var importer=(ModelImporter)AssetImporter.GetAtPath(path);
importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"SpaceCenter_PBR"),material);
importer.importAnimation=false;importer.isReadable=false;importer.preserveHierarchy=true;
importer.importTangents=ModelImporterTangents.CalculateMikk;importer.generateSecondaryUV=true;
importer.SaveAndReimport();
var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);
object check;
try
{
    instance.name="01_VehicleAssembly";instance.transform.position=Vector3.zero;
    var renderers=instance.GetComponentsInChildren<Renderer>();
    if(renderers.Length==0||renderers.Any(r=>r.sharedMaterials.Any(m=>m!=material)))throw new Exception("VAB material/geometry check failed");
    var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
    int triangles=instance.GetComponentsInChildren<MeshFilter>().Sum(f=>(int)f.sharedMesh.GetIndexCount(0)/3);
    if(bounds.size.y<70||bounds.size.y>80)throw new Exception("VAB scale changed unexpectedly");
    var prefab=PrefabUtility.SaveAsPrefabAsset(instance,root+"/Prefabs/01_VehicleAssembly.prefab");
    if(prefab==null)throw new Exception("VAB prefab save failed");
    check=new{id="01_VehicleAssembly",result="PASS",renderers=renderers.Length,triangles,size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},material=root+"/Materials/SpaceCenter_PBR.mat"};
}
finally{UnityEngine.Object.DestroyImmediate(instance);}
AssetDatabase.SaveAssets();
var project=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,".."));
var reportPath=System.IO.Path.Combine(project,"Art/SpaceCenter/unity-check.json");
var checks=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(reportPath));
for(int i=0;i<checks.Count;i++)if((string)checks[i]["id"]=="01_VehicleAssembly")checks[i]=Newtonsoft.Json.Linq.JObject.FromObject(check);
System.IO.File.WriteAllText(reportPath,checks.ToString(Newtonsoft.Json.Formatting.Indented));
return check;
