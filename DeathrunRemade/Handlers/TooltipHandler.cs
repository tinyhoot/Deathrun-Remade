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
        private static NautilusShell<TechType, string> _nameCache;
        private static NautilusShell<TechType, string> _tooltipCache;
        
        /// <summary>
        /// Replace or add extra information to vanilla items.
        /// </summary>
        /// <param name="config">The config active in the current game.</param>
        public static void OverrideVanillaTooltips(ConfigSave config)
        {
            _nameCache = new NautilusShell<TechType, string>(
                (techType, str) => LanguageHandler.SetTechTypeName(techType, str),
                techType => Language.main.Get(techType.AsString()));
            _tooltipCache = new NautilusShell<TechType, string>(
                (techType, str) => LanguageHandler.SetTechTypeTooltip(techType, str),
                techType => Language.main.Get($"Tooltip_{techType}"));
            
            _nameCache.SendChanges(TechType.Battery, GetLocalised("deathrunremade_battery"));
            _nameCache.SendChanges(TechType.PowerCell, GetLocalised("deathrunremade_powercell"));
            _tooltipCache.SendChanges(TechType.Battery, GetLocalised("Tooltip_deathrunremade_battery"));
            _tooltipCache.SendChanges(TechType.PowerCell, GetLocalised("Tooltip_deathrunremade_powercell"));
            
            // Set this hint here so that nitrogen hints can then add on to it.
            if (config.SurfaceAir != Difficulty3.Normal)
                AddToTooltip(TechType.PipeSurfaceFloater, GetLocalised("Tooltip_deathrunremade_pipesurfacefloater1"));
            if (config.NitrogenBends != Difficulty3.Normal)
                AddNitrogenHints();
        }

        public static void OnReset()
        {
            _nameCache.UndoChanges();
            _tooltipCache.UndoChanges();
        }

        private static string GetLocalised(string key)
        {
            return Language.main.Get(key);
        }

        private static string GetLocalisedFormat(string key, params object[] formatArgs)
        {
            return Language.main.GetFormat(key, formatArgs);
        }

        /// <summary>
        /// Add extra lines to items which affect nitrogen levels in some way.
        /// </summary>
        private static void AddNitrogenHints()
        {
            _tooltipCache.SendChanges(TechType.FirstAidKit, "");
            
            AddToTooltip(TechType.FirstAidKit, GetLocalised("Tooltip_deathrunremade_firstaidkit"));
            AddToTooltip(TechType.Boomerang, GetLocalised("Tooltip_deathrunremade_boomerang"));
            AddToTooltip(TechType.LavaBoomerang, GetLocalised("Tooltip_deathrunremade_boomerang"));

            string pipeText = GetLocalised("Tooltip_deathrunremade_pipesurfacefloater2");
            AddToTooltip(TechType.PipeSurfaceFloater, pipeText);
            AddToTooltip(TechType.BasePipeConnector, pipeText);
            AddToTooltip(TechType.Pipe, $"{pipeText} {GetLocalised("Tooltip_deathrunremade_pipe")}");
        }

        private static void AddToTooltip(TechType techType, string textToAdd)
        {
            string old = Language.main.Get($"Tooltip_{techType}");
            _tooltipCache.SendChanges(techType, $"{old} {textToAdd}");
        }

        /// <summary>
        /// Provide a more readable tooltip for "infinity" crush depths.
        /// </summary>
        private static string GetDepthToolTip(float depth)
        {
            if (Mathf.Approximately(depth, CrushDepthHandler.InfiniteCrushDepth))
                return GetLocalised("dr_crushdepth_infinite");
            return $"{depth}{GetLocalised("MeterSuffix")}";
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
            sb.AppendFormat(GetTooltipFormat(), GetLocalisedFormat("dr_crushdepthaddon", GetDepthToolTip(crushDepth)));
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
            sb.AppendFormat(GetTooltipFormat(), GetLocalisedFormat("dr_nitrogenaddon", $"{nitrogen:F0}"));
        }
    }
}