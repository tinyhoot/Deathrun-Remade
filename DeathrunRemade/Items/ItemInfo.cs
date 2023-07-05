using System.Collections.Generic;

namespace DeathrunRemade.Items
{
    internal static class ItemInfo
    {
        private const string _prefix = "deathrunremade_";
        
        public static readonly Dictionary<string, string> ClassIds = new Dictionary<string, string>
        {
            { nameof(AcidBattery), _prefix + "acidbattery" },
        };
    }
}