using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using HootLib.Objects;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Always)]
    internal static class PdaPatcher
    {
        /// <summary>
        /// Jump in after PDA data has loaded but before Nautilus modifies it and cache the vanilla values where
        /// necessary.
        /// </summary>
        [HarmonyBefore("com.snmodding.nautilus")]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PDAScanner), nameof(PDAScanner.Initialize))]
        private static void CaptureVanillaFragmentNumbers(PDAData pdaData)
        {
            DeathrunInit._Log.Debug("Overriding cached fragment numbers with vanilla state.");
            
            NautilusShell<TechType, int> scanCache = DeathrunInit._recipeChanges.GetFragmentScanCache();
            if (scanCache == null)
            {
                DeathrunInit._Log.Warn("Tried to record vanilla fragment scan numbers, but cache is null.");
                return;
            }

            foreach (PDAScanner.EntryData entryData in pdaData.scanner)
            {
                // Don't consider values we didn't even overwrite.
                if (!scanCache.TryGetOriginalValue(entryData.key, out int scanNum))
                    continue;
                // Don't consider values that are already valid (likely due to another mod).
                if (scanNum > 0)
                    continue;
                
                scanCache.OverrideCachedValue(entryData.key, entryData.totalFragments);
            }
        }
    }
}