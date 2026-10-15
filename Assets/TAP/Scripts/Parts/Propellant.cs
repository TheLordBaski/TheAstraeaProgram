using System;
using System.Collections.Generic;

namespace TAP.Parts
{
    /// <summary>
    /// What an engine burns: one resource, or a mix of resources drawn together at the mix's ratio (D-01). An amount of
    /// propellant is a mass in kg; the components are drawn in units, at their share of that mass.
    /// </summary>
    public sealed class Propellant
    {
        public readonly struct Component
        {
            public readonly ResourceDefinition Resource;
            /// <summary>Share of the propellant's mass (the shares add up to 1).</summary>
            public readonly double MassShare;

            public Component(ResourceDefinition resource, double massShare) { Resource = resource; MassShare = massShare; }

            /// <summary>Units of this resource in one kg of the propellant.</summary>
            public double UnitsPerKg => Resource.density > 0 ? MassShare / Resource.density : 0;
        }

        public readonly string Id;
        public readonly string Name;
        public readonly bool IsMix;
        public readonly Component[] Components;

        private Propellant(string id, string name, bool isMix, Component[] components)
        {
            Id = id; Name = name; IsMix = isMix; Components = components;
        }

        /// <summary>The most restrictive flow of the components: a mix flows as far as all its parts do.</summary>
        public ResourceFlow Flow
        {
            get
            {
                var flow = ResourceFlow.Vessel;
                foreach (var c in Components) if (c.Resource.flow < flow) flow = c.Resource.flow;
                return flow;
            }
        }

        public static Propellant Of(ResourceDefinition resource) =>
            new Propellant(resource.id, resource.name, false, new[] { new Component(resource, 1) });

        /// <summary>A mix by its volume ratios; null when a component is unknown or has no mass.</summary>
        public static Propellant Of(MixDefinition mix, Func<string, ResourceDefinition> resource)
        {
            if (mix?.components == null || mix.components.Count == 0) return null;
            double total = 0;
            var weights = new double[mix.components.Count];
            var defs = new ResourceDefinition[weights.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                var def = resource(mix.components[i].resource);
                if (def == null || def.density <= 0 || mix.components[i].ratio <= 0) return null;
                defs[i] = def;
                weights[i] = mix.components[i].ratio * def.density;
                total += weights[i];
            }
            var parts = new Component[weights.Length];
            for (int i = 0; i < parts.Length; i++) parts[i] = new Component(defs[i], weights[i] / total);
            return new Propellant(mix.id, mix.name, true, parts);
        }

        /// <summary>The mass (kg) of this propellant that can be burned from these amounts of its components: the scarcest one decides.</summary>
        public double BurnableMass(Func<string, double> unitsOf)
        {
            double mass = double.MaxValue;
            foreach (var c in Components)
            {
                double perKg = c.UnitsPerKg;
                if (perKg <= 0) continue;
                mass = Math.Min(mass, unitsOf(c.Resource.id) / perKg);
            }
            return mass == double.MaxValue ? 0 : mass;
        }

        /// <summary>The components' units that make up this mass of the propellant.</summary>
        public IEnumerable<(ResourceDefinition resource, double units)> UnitsFor(double mass)
        {
            foreach (var c in Components) yield return (c.Resource, mass * c.UnitsPerKg);
        }
    }
}
