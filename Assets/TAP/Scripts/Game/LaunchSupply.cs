using System;
using System.Collections.Generic;
using TAP.Parts;
using TAP.Persistence;

namespace TAP.Game
{
    /// <summary>
    /// The launch site's stock of propellant and other consumables (D-01). A launch fills the vessel's tanks from it, so the
    /// player never picks a propellant. For now the stock is unlimited: free in sandbox and science mode, priced in career
    /// (<see cref="Price"/>, charged by CAR-02). Bases elsewhere will hold what they make or receive.
    /// </summary>
    public static class LaunchSupply
    {
        /// <summary>Part setting (craft file): how full the part's tanks are at launch, 0 to 1. Missing means full.</summary>
        public const string FillSetting = "fill";

        /// <summary>How full a craft part launches, from its settings.</summary>
        public static double FillOf(PartNodeRecord node)
        {
            if (node?.settings != null && node.settings.TryGetValue(FillSetting, out double f)) return Math.Max(0, Math.Min(1, f));
            return 1;
        }

        /// <summary>Whether a launch fills this resource from the site's stock: not a motor's solid fuel, ablator or charge.</summary>
        public static bool IsSupplied(ResourceDefinition r) => r != null && r.flow != ResourceFlow.Part && r.density > 0;

        /// <summary>The resources a part carries when it launches at this fill; the tanks' capacity stays as it is.</summary>
        public static List<ResourceRecord> Fill(PartDefinition def, double fill, PartDatabase db)
        {
            var records = new List<ResourceRecord>();
            if (def == null) return records;
            foreach (var r in def.resources)
                records.Add(new ResourceRecord { id = r.id, amount = IsSupplied(db.GetResource(r.id)) ? r.amount * fill : r.amount, max = r.Max });
            return records;
        }

        /// <summary>What the stock charges for the propellant a design launches with (funds; 0 while the resources have no price).</summary>
        public static double Price(CraftDesign design, PartDatabase db)
        {
            double total = 0;
            foreach (var node in design.parts)
            {
                var def = db.Get(node.partId);
                if (def == null) continue;
                double fill = FillOf(node);
                foreach (var r in def.resources)
                {
                    var rd = db.GetResource(r.id);
                    if (IsSupplied(rd)) total += r.amount * fill * rd.price;
                }
            }
            return total;
        }
    }
}
