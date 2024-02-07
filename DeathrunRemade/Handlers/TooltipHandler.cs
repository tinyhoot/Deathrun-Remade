using System.Text;
using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using DeathrunRemade.Patches;
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
            LocalisationHandler.SetTechTypeName(TechType.Battery, LocalisationHandler.Get("deathrunremade_battery"));
            LocalisationHandler.SetTechTypeName(TechType.PowerCell, LocalisationHandler.Get("deathrunremade_powercell"));
            LocalisationHandler.SetTooltip(TechType.Battery, LocalisationHandler.Get("Tooltip_deathrunremade_battery"));
            LocalisationHandler.SetTooltip(TechType.PowerCell, LocalisationHandler.Get("Tooltip_deathrunremade_powercell"));
            
            // Set this hint here so that nitrogen hints can then add on to it.
            if (config.SurfaceAir != Difficulty3.Normal)
                AddToTooltip(TechType.PipeSurfaceFloater, LocalisationHandler.Get("Tooltip_deathrunremade_pipesurfacefloater1"));
            if (config.NitrogenBends != Difficulty3.Normal)
                AddNitrogenHints();
        }

        /// <summary>
        /// Add extra lines to items which affect nitrogen levels in some way.
        /// </summary>
        private static void AddNitrogenHints()
        {
            AddToTooltip(TechType.FirstAidKit, LocalisationHandler.Get("Tooltip_deathrunremade_firstaidkit"));
            AddToTooltip(TechType.Boomerang, LocalisationHandler.Get("Tooltip_deathrunremade_boomerang"));
            AddToTooltip(TechType.LavaBoomerang, LocalisationHandler.Get("Tooltip_deathrunremade_boomerang"));

            string pipeText = LocalisationHandler.Get("Tooltip_deathrunremade_pipesurfacefloater2");
            AddToTooltip(TechType.PipeSurfaceFloater, pipeText);
            AddToTooltip(TechType.BasePipeConnector, pipeText);
            AddToTooltip(TechType.Pipe, $"{pipeText} {LocalisationHandler.Get("Tooltip_deathrunremade_pipe")}");
        }

        private static void AddToTooltip(TechType techType, string textToAdd)
        {
            string old = Language.main.Get($"Tooltip_{techType}");
            LocalisationHandler.SetTooltip(techType, $"{old} {textToAdd}");
        }

        /// <summary>
        /// Provide a more readable tooltip for "infinity" crush depths.
        /// </summary>
        private static string GetDepthToolTip(float depth)
        {
            if (Mathf.Approximately(depth, CrushDepthHandler.InfiniteCrushDepth))
                return LocalisationHandler.Get("dr_crushdepth_infinite");
            return $"{depth}{LocalisationHandler.Get("MeterSuffix")}";
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
            sb.AppendFormat(GetTooltipFormat(), LocalisationHandler.GetFormatted("dr_crushdepthaddon", GetDepthToolTip(crushDepth)));
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
            sb.AppendFormat(GetTooltipFormat(), LocalisationHandler.GetFormatted("dr_nitrogenaddon", $"{nitrogen:F0}"));
        }
    }
}