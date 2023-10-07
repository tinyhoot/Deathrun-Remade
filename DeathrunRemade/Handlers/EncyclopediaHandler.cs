using System.Collections.Generic;
using DeathrunRemade.Items;
using DeathrunRemade.Objects.Enums;
using DeathrunRemade.Patches;
using Nautilus.Handlers;
using Story;

namespace DeathrunRemade.Handlers
{
    internal class EncyclopediaHandler
    {
        private const string EncyCategory = "Deathrun";
        private const string EncyPrefix = "Deathrun_";
        private readonly Dictionary<string, string> _encyKeys = new Dictionary<string, string>
        {
            { "Intro", EncyPrefix + "Intro" },
            { "Aggression", EncyPrefix + "Aggression" },
            { "Atmosphere", EncyPrefix + "Atmosphere" },
            { "CrushDepth", EncyPrefix + "CrushDepth" },
            { "Explosion", EncyPrefix + "Explosion" },
            { "Lifepod", EncyPrefix + "Lifepod" },
            { "Nitrogen", EncyPrefix + "Nitrogen" },
            { "PowerCosts", EncyPrefix + "PowerCosts" },
            { "Radiation", EncyPrefix + "Radiation" },
            { "VehicleDeco", EncyPrefix + "VehicleDeco" },
        };
        
        public void RegisterPdaEntries()
        {
            // Create the Deathrun category.
            LanguageHandler.SetLanguageLine("EncyPath_" + EncyCategory, "Deathrun");
            PDAHandler.AddEncyclopediaEntry(_encyKeys["Intro"], EncyCategory, "A Deathrun Introduction", 
                "Think you're a jaded Planet 4546B veteran? Well then try a Death Run! You will need all the skills"
                + " you've learned to handle the onslaught of aggressive creatures, increased damage, the unbreathable "
                + "atmosphere, nitrogen and 'The Bends', the engulfing radiation, the expensive fabrication costs, and "
                + "many other hazards!\n\nDeathrun is less about winning and more about how much you can achieve before"
                + " meeting your untimely demise. Your score improves the longer you survive and the more progress you "
                + "make in the game. Good luck!");
            PDAHandler.AddEncyclopediaEntry(_encyKeys["Aggression"], EncyCategory, "Creature Aggression",
                "Local scans show the native life on planet 4546B to be extremely aggressive. Stay low to the "
                + "ground and avoid hovering in one place for too long.");
            PDAHandler.AddEncyclopediaEntry(_encyKeys["Atmosphere"], EncyCategory, "Unbreathable Atmosphere",
                "The atmosphere of planet 4546B is poisonous to humans. Alterra provides you with blueprints for "
                + "high-tech floating pumps capable of filtering the surface air to a breathable mixture.\n\n"
                + "All purchases final. Alterra makes no guarantees for the continued operation of these pumps in "
                + "environments with high background radiation.\n\n1. Use filter pumps to breathe surface air.");
            PDAHandler.AddEncyclopediaEntry(_encyKeys["CrushDepth"], EncyCategory, "Crush Depth",
                "An unaided diver on planet 4546B should venture no deeper than 200m without either an improved "
                + "diving suit or submersible support. While many types of diving suits will extend this range, "
                + "reinforced suits, and in particular the Reinforced Dive Suit Mark 3, offer the best protection.\n\n"
                + "1. Personal safe depth 200m.\n2. Equip better dive suits for better protection.\n"
                + "3. Scan deep creatures for insight on more depth-resistant materials.");
            PDAHandler.AddEncyclopediaEntry(_encyKeys["Explosion"], EncyCategory, "Quantum Explosion",
                "Simulations show that in the unlikely event of a quantum drive explosion on planet 4546B the "
                + $"shockwave would be felt as far as {ExplosionPatcher.GetExplosionDepth(Difficulty3.Deathrun)}m below "
                + $"the surface. In line with emergency training protocols Alterra staff is advised to be prepared to "
                + $"seek shelter as deep as possible, preferably inside a reinforced structure.\n\n"
                + $"1. Explosion shockwave down to {ExplosionPatcher.GetExplosionDepth(Difficulty3.Deathrun)}m.\n"
                + $"2. Seek shelter as deep as possible.");
            PDAHandler.AddEncyclopediaEntry(_encyKeys["Lifepod"], EncyCategory, "Lifepod Flotation Failure", 
                "This lifepod has malfunctioned and its flotation devices have failed. Any repair costs incurred "
                + "from returning the pod in an inadequate state will be deducted from your pay.");
            PDAHandler.AddEncyclopediaEntry(_encyKeys["Nitrogen"], EncyCategory, "Nitrogen and The Bends", 
                "The deeper you go, the higher your blood nitrogen level will be. In Deathrun your 'Safe Depth' "
                + "will tend to settle out at about 3/4 of your current depth, so you'll want to get close to that depth "
                + "without going above it, and then wait for your safe depth to improve. You must also avoid ascending "
                + "too *quickly* (i.e. you can no longer just hold down the ascent button at all times). First Aid kits "
                + "and floating air pumps can help with making ascents -- eating certain kinds of native life may help "
                + "as well.\n\nIn real life, 'The Bends', or decompression sickness, results from nitrogen bubbles "
                + "forming in the bloodstream. The deeper a diver goes, the faster nitrogen accumulates in their "
                + "bloodstream. If they ascend slowly and make appropriate 'deco stops' and 'safety stops', the "
                + "nitrogen is removed as they exhale. But if they ascend too quickly, the nitrogen forms bubbles "
                + "which can block important blood vessels and cause death.\n\n1. Ascend slowly.\n2. Watch your 'Safe Depth'.");
            PDAHandler.AddEncyclopediaEntry(_encyKeys["PowerCosts"], EncyCategory, "Increased Power Costs",
                "The costs of fabrication (as well as battery charging, scanning, and water filtration) have "
                + "increased dramatically following the imposition of the Galactic Fair Robotic Labor Standards Act. "
                + "You may find that fabricators cost as much as 15 Standard Imperial Power Units per use. Power "
                + "likewise recharges rather slowly.\n\nIn radiated areas, power usage will be even greater, as much as "
                + "five times of what you'd normally expect.");
            PDAHandler.AddEncyclopediaEntry(_encyKeys["Radiation"], EncyCategory, "Extreme Radiation",
                "In the aftermath of a quantum drive explosion large amounts of radiation is known to seep into "
                + "the immediate area. The radiation commonly affects surface areas as well as any bodies of water up "
                + $"to a depth of {RadiationPatcher.GetMaxRadiationDepth(Difficulty4.Kharaa)}m until Alterra staff "
                + $"seals the drive core and prevents further radiation leakage.\n\n1. Radiation affects the surface.\n"
                + $"2. Radiation penetrates up to {RadiationPatcher.GetMaxRadiationDepth(Difficulty4.Kharaa)}m below "
                + $"the surface.\n3. Repair any leaks to ameliorate effects.");
            PDAHandler.AddEncyclopediaEntry(_encyKeys["VehicleDeco"], EncyCategory, "Vehicle Decompression",
                "The Seamoth and Prawn Suit provide decompression assistance for their pilots, preventing "
                + "decompression sickness while they are being used. This does however result in significant power "
                + "drain whenever the pilot exits the vehicle at depth -- the deeper the exit, the higher the cost. "
                + "Disembarking at a Moon Pool or directly into a Cyclops does not incur this cost. The Cyclops itself "
                + "has a more sophisticated decompression airlock system and can always be exited without any energy drain.");
            
            // Add PDA encyclopedia entries for custom items.
            PDAHandler.AddEncyclopediaEntry(EncyPrefix + "DecoModule", "Tech/Vehicles", "Nano Decompression Module",
                "Purges nitrogen from the pilot's bloodstream and reduces the energy costs for exiting the vehicle. "
                + "Can be stacked for additional improvement.", unlockSound: PDAHandler.UnlockImportant);
            PDAHandler.AddEncyclopediaEntry(EncyPrefix + "FilterChip", "Tech/Equipment", "Integrated Filter Chip",
                "Provides bloodstream filtering, rendering surface air breathable and purging nitrogen while "
                + "wearer is indoors. Comes with a free Compass.", unlockSound: PDAHandler.UnlockImportant);
        }

        /// <summary>
        /// Add story goals to unlock the entries not already unlocked at start at the right times.
        /// </summary>
        public void RegisterStoryGoals()
        {
            StoryGoalHandler.RegisterItemGoal(EncyPrefix + "DecoModule", Story.GoalType.Encyclopedia,
                DecompressionModule.UnlockTechType);
            // Add our own custom goal on top of the goal triggered when all leaks are fixed and use it to unlock
            // both the filterchip blueprint and encyclopedia entry.
            StoryGoalHandler.RegisterCompoundGoal(EncyPrefix + "FilterChip", Story.GoalType.Encyclopedia, 5f, 
                "AuroraRadiationFixed");
            StoryGoalHandler.RegisterOnGoalUnlockData(EncyPrefix + "FilterChip", new[]
            {
                new UnlockBlueprintData
                {
                    unlockType = UnlockBlueprintData.UnlockType.Available,
                    techType = FilterChip.TechType
                }
            });
        }

        /// <summary>
        /// Encyclopedia entries are not unlocked by default. Make sure the deathrun tutorial entries are accessible.
        /// </summary>
        public void UnlockPdaIntroEntries()
        {
            foreach (string encyKey in _encyKeys.Values)
            {
                PDAEncyclopedia.Add(encyKey, false);
            }
        }
    }
}