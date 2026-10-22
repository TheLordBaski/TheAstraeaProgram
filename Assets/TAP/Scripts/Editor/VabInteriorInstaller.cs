using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace TAP.EditorTools
{
    /// <summary>Rebuilds only the authored VAB prefabs and their placement preview.</summary>
    public static class VabInteriorInstaller
    {
        const string Folder = "Assets/TAP/Art/SpaceCenter";

        [MenuItem("TAP/Space Centre/Install VAB Interior")]
        public static void Install()
        {
            var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var manifest = JObject.Parse(File.ReadAllText(Path.Combine(project, "Art/SpaceCenter/vab-interior-manifest.json")));
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/SpaceCenter_PBR.mat");
            if (material == null) throw new InvalidOperationException("Space centre material is missing");
            var modelPath = Folder + "/Models/01_VehicleAssembly.fbx";
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "SpaceCenter_PBR"), material);
            importer.importAnimation = false;
            importer.isReadable = false;
            importer.preserveHierarchy = true;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.generateSecondaryUV = true;
            importer.SaveAndReimport();

            // Batch startup has an unnamed scene; create the preview before adding any objects to it.
            var testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(testScene);
            var root = new GameObject("01_VehicleAssembly");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
                model.transform.SetParent(root.transform, false);
                Transform Marker(string name) => model.GetComponentsInChildren<Transform>().Single(t => t.name == name);
                var origin = Marker("RocketAssemblyOrigin").position;
                // FBX axis reflection varies with the importer; derive its signs from the authored marker.
                float sx = origin.x / -4f, sz = origin.z / 6f;
                if (Mathf.Abs(Mathf.Abs(sx) - 1) > .001f || Mathf.Abs(Mathf.Abs(sz) - 1) > .001f || Mathf.Abs(origin.y - .31f) > .001f)
                    throw new InvalidOperationException("Unexpected FBX coordinate conversion: " + origin);
                Vector3 Point(JToken p) => new Vector3((float)p[0] * sx, (float)p[2], (float)p[1] * sz);

                var collisions = new GameObject("Collision / simplified static");
                collisions.transform.SetParent(root.transform, false);
                foreach (var item in manifest["colliders"])
                {
                    var go = new GameObject((string)item["name"]);
                    go.transform.SetParent(collisions.transform, false);
                    go.transform.localPosition = Point(item["center"]);
                    if (item["rotation_x"] != null) go.transform.localRotation = Quaternion.Euler(-(float)item["rotation_x"] * sz, 0, 0);
                    var size = item["size"];
                    go.AddComponent<BoxCollider>().size = new Vector3((float)size[0], (float)size[2], (float)size[1]);
                }
                foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
                {
                    // Exact rails and stair treads supplement the broad, inexpensive deck/ramp colliders.
                    if (filter.name.Contains("gallery") || filter.name.StartsWith("Interior Stair tower") || filter.name.StartsWith("VAB Door"))
                        filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                }

                var lights = new GameObject("Interior lighting / neutral");
                lights.transform.SetParent(root.transform, false);
                foreach (float x in new[] { -20f, 12f })
                    foreach (float z in new[] { 14f, 34f, 54f })
                        foreach (float y in new[] { -5f, 17f })
                        {
                            var go = new GameObject("Service luminaire");
                            go.transform.SetParent(lights.transform, false);
                            go.transform.localPosition = new Vector3(x * sx, z, y * sz);
                            var light = go.AddComponent<Light>();
                            light.type = LightType.Point;
                            light.color = Color.white;
                            light.intensity = 7f;
                            light.range = 24f;
                            light.shadows = LightShadows.None;
                        }

                foreach (var t in root.GetComponentsInChildren<Transform>())
                {
                    t.gameObject.layer = TAP.Core.Layers.Terrain;
                    if (t.GetComponent<MeshRenderer>() != null)
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                }
                var renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Any(r => r.sharedMaterials.Any(m => m != material))) throw new InvalidOperationException("VAB material remap failed");
                int triangles = model.GetComponentsInChildren<MeshFilter>().Sum(f => (int)f.sharedMesh.GetIndexCount(0) / 3);
                if (triangles != (int)manifest["triangles"])
                    throw new InvalidOperationException("VAB triangle count changed on import: " + triangles + " vs " + manifest["triangles"] + "; " + string.Join("; ", model.GetComponentsInChildren<MeshFilter>().Select(f => f.name + "=" + f.sharedMesh.GetIndexCount(0)/3)));
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                if (Mathf.Abs(bounds.size.y - 77.7f) > .01f) throw new InvalidOperationException("VAB scale is incorrect");

                // Query only this asset's colliders, ignoring objects in other open editor scenes.
                var min = Marker("ClearVolumeMin").position;
                var max = Marker("ClearVolumeMax").position;
                var center = (min + max) * .5f;
                var half = new Vector3(Mathf.Abs(max.x-min.x), Mathf.Abs(max.y-min.y), Mathf.Abs(max.z-min.z)) * .5f - Vector3.one * .01f;
                Physics.SyncTransforms();
                var hits = Physics.OverlapBox(center, half, Quaternion.identity, 1 << TAP.Core.Layers.Terrain, QueryTriggerInteraction.Ignore)
                    .Where(c => c.transform.IsChildOf(root.transform)).ToArray();
                if (hits.Length != 0) throw new InvalidOperationException("VAB central envelope obstructed by: " + string.Join(", ", hits.Select(c => c.name)));
                var floor = collisions.GetComponentsInChildren<BoxCollider>().Single(c => c.name == "High bay floor");
                if (!floor.Raycast(new Ray(new Vector3(origin.x,1,origin.z),Vector3.down),out var floorHit,2f) || Mathf.Abs(floorHit.point.y-.31f) > .002f)
                    throw new InvalidOperationException("VAB assembly floor collision is missing");
                int colliderCount = root.GetComponentsInChildren<Collider>().Length;

                var fullPath = Folder + "/Prefabs/01_VehicleAssembly.prefab";
                if (PrefabUtility.SaveAsPrefabAsset(root, fullPath) == null) throw new IOException("VAB prefab save failed");
                // Both prefabs share the same FBX meshes. This one places the rocket's floor origin at (0,0,0).
                model.transform.localPosition -= origin;
                collisions.transform.localPosition -= origin;
                lights.transform.localPosition -= origin;
                root.name = "VAB_AssemblySpace";
                var assemblyPath = Folder + "/Prefabs/VAB_AssemblySpace.prefab";
                if (PrefabUtility.SaveAsPrefabAsset(root, assemblyPath) == null) throw new IOException("Assembly-space prefab save failed");
                if (Marker("RocketAssemblyOrigin").position.sqrMagnitude > .000001f) throw new InvalidOperationException("Assembly origin is not centred");

                var cameraGo = new GameObject("Interior preview camera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, testScene);
                cameraGo.transform.position = new Vector3(0, 27.69f, -32f * sz);
                cameraGo.transform.LookAt(new Vector3(0, 32.69f, 6f * sz));
                var camera = cameraGo.AddComponent<Camera>();
                camera.fieldOfView = 92;
                camera.nearClipPlane = .05f;
                camera.farClipPlane = 250;
                camera.backgroundColor = new Color(.08f, .08f, .08f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                cameraGo.AddComponent<UniversalAdditionalCameraData>();
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.6f,.6f,.6f);
                RenderSettings.fog = false;
                var sceneFolder = Folder + "/Scenes";
                if (!AssetDatabase.IsValidFolder(sceneFolder)) AssetDatabase.CreateFolder(Folder, "Scenes");
                EditorSceneManager.SaveScene(testScene, sceneFolder + "/VAB_Interior_Preview.unity");

                var report = new { id="01_VehicleAssembly", result="PASS", triangles, renderers=renderers.Length,
                    size=new[] {bounds.size.x,bounds.size.y,bounds.size.z}, colliders=colliderCount, clearVolumeUnobstructed=true,
                    clearSize=new[] {24f,67.9f,30f}, assemblyOrigin=new[] {0f,0f,0f}, assemblyFloorRaycast=true,
                    fbxAxisSigns=new[] {sx,sz}, material=Folder+"/Materials/SpaceCenter_PBR.mat", prefab=fullPath, assemblyPrefab=assemblyPath };
                File.WriteAllText(Path.Combine(project,"Art/SpaceCenter/vab-interior-unity-check.json"), Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
                var checkPath = Path.Combine(project,"Art/SpaceCenter/unity-check.json");
                var checks = JArray.Parse(File.ReadAllText(checkPath));
                for(int i=0;i<checks.Count;i++) if((string)checks[i]["id"]=="01_VehicleAssembly") checks[i]=JObject.FromObject(report);
                File.WriteAllText(checkPath,checks.ToString());
                AssetDatabase.SaveAssets();
                InstallRuntimePrefabs();
                Debug.Log("VAB_INTERIOR_UNITY_PASS " + triangles + " triangles; " + colliderCount + " colliders; centre empty");
                if (!Application.isBatchMode) EditorSceneManager.CloseScene(testScene, true);
            }
            finally { if(root != null) Object.DestroyImmediate(root); }
        }

        [MenuItem("TAP/Space Centre/Install Runtime VAB Prefabs")]
        public static void InstallRuntimePrefabs()
        {
            const string runtimeFolder = "Assets/TAP/Resources/SpaceCenter";
            if (!AssetDatabase.IsValidFolder(runtimeFolder)) AssetDatabase.CreateFolder("Assets/TAP/Resources", "SpaceCenter");
            foreach (var id in new[] { "01_VehicleAssembly", "VAB_AssemblySpace" })
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Prefabs/" + id + ".prefab");
                if (source == null) throw new InvalidOperationException("Missing authored VAB prefab: " + id);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                try
                {
                    instance.name = id;
                    if (PrefabUtility.SaveAsPrefabAsset(instance, runtimeFolder + "/" + id + ".prefab") == null)
                        throw new IOException("Runtime VAB prefab save failed: " + id);
                }
                finally { Object.DestroyImmediate(instance); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("VAB_RUNTIME_PREFABS_INSTALLED");
        }
    }
}
