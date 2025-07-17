using System.Collections.Generic;
using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
using DeathrunRemade.Patches;
using Nautilus.Handlers;

namespace DeathrunRemade.Handlers
{
    internal static class EncyclopediaHandler
    {
        private const string EncyCategory = "Deathrun";
        private const string EncyPrefix = "Deathrun_";
        private static readonly List<string> _encyKeys = new List<string>
        {
            EncyPrefix + "Intro",
            EncyPrefix + "Aggression",
            EncyPrefix + "Atmosphere",
            EncyPrefix + "AuroraFiltration",
            EncyPrefix + "CrushDepth",
            EncyPrefix + "Explosion",
            EncyPrefix + "Lifepod",
            EncyPrefix + "Nitrogen",
            EncyPrefix + "PowerCosts",
            EncyPrefix + "Radiation",
            EncyPrefix + "VehicleDecompression",
        };

        public static void Init()
        {
            RegisterPdaEntries();
            RegisterStoryGoals();
            // No need to register an OnReset event, the LocalisationHandler will see to it.
            WaitScreenHandler.RegisterEarlyLoadTask(DeathrunInit.NAME, _ => FormatEncyEntries(SaveData.Main.Config), 
                "Formatting PDA encyclopedia entries");
        }
        
        /// <summary>
        /// Create the encyclopedia entries based on language keys. The text for the entries is found in the
        /// localisation files.
        /// </summary>
        private static void RegisterPdaEntries()
        {
            foreach (string key in _encyKeys)
            {
                PDAHandler.AddEncyclopediaEntry(key, EncyCategory, null, null);
            }
        }

        /// <summary>
        /// Add story goals to unlock the entries not already unlocked at start at the right times.
        /// </summary>
        private static void RegisterStoryGoals()
        {
            // Add an explainer for why the Aurora is breathable after fixing the generator.
            StoryGoalHandler.RegisterCustomEvent("AuroraRadiationFixed", () => NotificationHandler.Main.AddMessage(NotificationHandler.Centre, "dr_auroraRepairedBreathable"));
            StoryGoalHandler.RegisterCompoundGoal(EncyPrefix + "AuroraFiltration", Story.GoalType.Encyclopedia, 1f, "AuroraRadiationFixed");
        }

        /// <summary>
        /// Patch any encyclopedia entries containing placeholders and replace them with values based on settings.
        /// </summary>
        public static void FormatEncyEntries(ConfigSave config)
        {
            LocalisationHandler.FormatExistingLine("EncyDesc_Deathrun_CrushDepth",
                CrushDepthHandler.SuitlessCrushDepth);
            LocalisationHandler.FormatExistingLine("EncyDesc_Deathrun_Explosion",
                ExplosionPatcher.GetExplosionDepth(config.ExplosionDepth));
            LocalisationHandler.FormatExistingLine("EncyDesc_Deathrun_PowerCosts",
                5 * PowerPatcher.GetPowerCostMult(config.PowerCosts, false),
                PowerPatcher.GetPowerCostMult(config.PowerCosts, true));
            LocalisationHandler.FormatExistingLine("EncyDesc_Deathrun_Radiation",
                RadiationPatcher.GetMaxRadiationDepth(config.RadiationDepth));
        }

        /// <summary>
        /// Encyclopedia entries are not unlocked by default. Make sure the deathrun tutorial entries are accessible.
        /// </summary>
        public static void UnlockPdaIntroEntries()
        {
            foreach (string encyKey in _encyKeys)
            {
                PDAEncyclopedia.Add(encyKey, false);
                // Create a notification icon in the encyclopedia without causing an on-screen popup.
                NotificationManager.main.Add(NotificationManager.Group.Encyclopedia, encyKey, 0f);
            }
        }
    }
}