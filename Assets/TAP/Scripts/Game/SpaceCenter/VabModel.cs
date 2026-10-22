using System;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>The same authored VAB, with placement origins for the site and the assembly floor.</summary>
    public static class VabModel
    {
        public static GameObject Create(bool assemblySpace = false)
        {
            string path = "SpaceCenter/" + (assemblySpace ? "VAB_AssemblySpace" : "01_VehicleAssembly");
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing VAB prefab: Resources/" + path);
            var root = UnityEngine.Object.Instantiate(prefab);
            root.name = "AssemblyBuilding";
            if (!assemblySpace)
                foreach (var light in root.GetComponentsInChildren<Light>()) light.enabled = false;
            return root;
        }
    }
}
