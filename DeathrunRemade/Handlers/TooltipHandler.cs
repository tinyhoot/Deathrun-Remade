using System.Text;
using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
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
            
            // Set this hint here so that nitrogen hints can then add on to it.
            if (config.SurfaceAir != Difficulty3.Normal)
                AddToTooltip(TechType.PipeSurfaceFloater, "Makes surface air breathable.");
            if (config.NitrogenBends != Difficulty3.Normal)
                AddNitrogenHints();
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
                return "Unlimited";
            return $"{depth}m";
        }

        private static string GetTooltipFormat()
        {
            return "\n<size=20><color=#DDDEDEFF>{0}</color></size>";
        }
        
        /// <summary>
        /// Write the tooltip displaying crush depth values on an item in the inventory.
        /// </summary>
        public static void WriteCrushDepthTooltip(StringBuilder sb, TechType techType)
        {
            ConfigSave config = SaveData.Main.Config;
            // Don't do anything if the setting isn't on or this isn't even a suit.
            if (config.PersonalCrushDepth == Difficulty3.Normal)
                return;
            float crushDepth = CrushDepthHandler.GetCrushDepth(techType, config);
            if (crushDepth <= CrushDepthHandler.SuitlessCrushDepth)
                return;
            sb.AppendFormat(GetTooltipFormat(), $"CRUSH DEPTH: {GetDepthToolTip(crushDepth)}");
        }
        
        /// <summary>
        /// Write the tooltip displaying nitrogen values on an item in the inventory.
        /// </summary>
        public static void WriteNitrogenTooltip(StringBuilder sb, Eatable eatable)
        {
            if (SaveData.Main.Config.NitrogenBends == Difficulty3.Normal)
                return;
            if (!SurvivalPatcher.TryGetNitrogenValue(eatable, out float nitrogen))
                return;
            sb.AppendFormat(GetTooltipFormat(), $"NITROGEN: {nitrogen:F0}");
        }
    }
}