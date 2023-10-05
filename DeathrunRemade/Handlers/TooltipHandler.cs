using DeathrunRemade.Configuration;
using DeathrunRemade.Objects.Enums;
using DeathrunRemade.Patches;
using Nautilus.Handlers;
using UnityEngine;

namespace DeathrunRemade.Handlers
{
    internal static class TooltipHandler
    {
        /// <summary>
        /// Replace or add extra information to vanilla items.
        /// </summary>
        /// <param name="config">The config active in the current game.</param>
        public static void OverrideVanillaTooltips(ConfigSave config)
        {
            LanguageHandler.SetTechTypeName(TechType.Battery, "Lithium Battery");
            LanguageHandler.SetTechTypeName(TechType.PowerCell, "Lithium Power Cell");
            LanguageHandler.SetTechTypeTooltip(TechType.Battery, "Advanced rechargeable mobile power source.");
            LanguageHandler.SetTechTypeTooltip(TechType.PowerCell, "High-capacity rechargeable mobile power source.");
            
            if (config.PersonalCrushDepth != Difficulty3.Normal)
                AddCrushDepthHints(config);
            // Set this hint here so that nitrogen hints can then add on to it.
            if (config.SurfaceAir != Difficulty3.Normal)
                AddToTooltip(TechType.PipeSurfaceFloater, "Makes surface air breathable.");
            if (config.NitrogenBends != Difficulty3.Normal)
                AddNitrogenHints();
        }

        /// <summary>
        /// Add extra lines to suits to indicate their level of protection.
        /// </summary>
        private static void AddCrushDepthHints(ConfigSave config)
        {
            string radDepth = GetDepthToolTip(CrushDepthHandler.GetCrushDepth(TechType.RadiationSuit, config));
            string reinforcedDepth = GetDepthToolTip(CrushDepthHandler.GetCrushDepth(TechType.ReinforcedDiveSuit, config));
            float reinforcedTemp = SuitPatcher.GetTemperatureLimit(TechType.ReinforcedDiveSuit);
            string filterDepth = GetDepthToolTip(CrushDepthHandler.GetCrushDepth(TechType.WaterFiltrationSuit, config));

            AddToTooltip(TechType.RadiationSuit, $"Protects the user at {radDepth}.");
            AddToTooltip(TechType.ReinforcedDiveSuit,
                $"Protects the user at {reinforcedDepth} and temperatures up to {reinforcedTemp}C.");
            AddToTooltip(TechType.WaterFiltrationSuit, $"Protects the user at {filterDepth}.");
        }

        /// <summary>
        /// Add extra lines to items which affect nitrogen levels in some way.
        /// </summary>
        private static void AddNitrogenHints()
        {
            AddToTooltip(TechType.FirstAidKit, "Also purges nitrogen from the bloodstream.");
            AddToTooltip(TechType.Boomerang, "Seems to have unusual nitrogen-neutralising blood chemistry.");
            AddToTooltip(TechType.LavaBoomerang, "Seems to have unusual nitrogen-neutralising blood chemistry.");

            string pipeText = "Supplies diving gas mixtures to help purge nitrogen from the bloodstream.";
            AddToTooltip(TechType.PipeSurfaceFloater, pipeText);
            AddToTooltip(TechType.BasePipeConnector, pipeText);
            AddToTooltip(TechType.Pipe,
                $"{pipeText} Your Safe Depth decreases more quickly while breathing at a pipe.");
        }

        private static void AddToTooltip(TechType techType, string textToAdd)
        {
            string old = Language.main.Get($"Tooltip_{techType}");
            LanguageHandler.SetTechTypeTooltip(techType, $"{old} {textToAdd}");
        }

        /// <summary>
        /// Provide a more readable tooltip for "infinity" crush depths.
        /// </summary>
        private static string GetDepthToolTip(float depth)
        {
            if (Mathf.Approximately(depth, CrushDepthHandler.InfiniteCrushDepth))
                return "all depths";
            return $"depths up to {depth}m";
        }
    }
}