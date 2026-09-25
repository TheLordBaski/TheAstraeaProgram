using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace TAP.Parts
{
    /// <summary>Loads and indexes part/resource definitions from Resources/Data/parts.json.</summary>
    public sealed class PartDatabase
    {
        public readonly PartCatalog Catalog;
        private readonly Dictionary<string, PartDefinition> _parts = new Dictionary<string, PartDefinition>();
        private readonly Dictionary<string, ResourceDefinition> _resources = new Dictionary<string, ResourceDefinition>();

        private static PartDatabase _instance;
        public static PartDatabase Instance
        {
            get
            {
                if (_instance == null) _instance = LoadFromResources();
                return _instance;
            }
        }

        public static JsonSerializerSettings JsonSettings => new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore,
        };

        public static PartDatabase LoadFromResources(string path = "Data/parts")
        {
            var ta = UnityEngine.Resources.Load<TextAsset>(path);
            if (ta == null) throw new Exception("Missing part catalog at Resources/" + path);
            return FromJson(ta.text);
        }

        public static PartDatabase FromJson(string json)
        {
            var cat = JsonConvert.DeserializeObject<PartCatalog>(json, JsonSettings);
            return new PartDatabase(cat);
        }

        public PartDatabase(PartCatalog catalog)
        {
            Catalog = catalog;
            foreach (var r in catalog.resources) _resources[r.id] = r;
            foreach (var p in catalog.parts)
            {
                if (_parts.ContainsKey(p.id)) Debug.LogWarning("Duplicate part id " + p.id);
                _parts[p.id] = p;
            }
        }

        public IReadOnlyList<PartDefinition> Parts => Catalog.parts;
        public IReadOnlyList<ResourceDefinition> ResourceDefinitions => Catalog.resources;

        public PartDefinition Get(string id)
        {
            if (id != null && _parts.TryGetValue(id, out var p)) return p;
            return null;
        }

        public ResourceDefinition GetResource(string id)
        {
            if (id != null && _resources.TryGetValue(id, out var r)) return r;
            return null;
        }

        public double ResourceDensity(string id) => GetResource(id)?.density ?? 0;
    }
}
