using System.IO;
using BepInEx;
using DeathrunRemade.Configuration;
using DeathrunRemade.Items;
using HarmonyLib;
using Nautilus.Handlers;
using SubnauticaCommons;
using SubnauticaCommons.Interfaces;

namespace DeathrunRemade
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.snmodding.nautilus", "1.0")]
    internal class DeathrunInit : BaseUnityPlugin
    {
        public const string GUID = "com.github.tinyhoot.DeathrunRemade";
        public const string NAME = "Deathrun Remade";
        public const string VERSION = "0.1";

        internal static Config _Config;
        internal static ILogHandler _Log;

        private void Awake()
        {
            _Log = new HootLogger(NAME);
            _Log.Info($"{NAME} v{VERSION} starting up.");
            
            // Registering config.
            _Config = new Config(Path.Combine(Paths.ConfigPath, Hootils.GetConfigFileName(NAME)),
                Info.Metadata);
            _Config.RegisterModOptions(NAME, transform);
            
            SetupCraftTree();
            RegisterItems();
            
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(Hootils.GetAssembly());
            
            _Log.Info("Finished loading.");
        }

        /// <summary>
        /// Register all custom items added by this mod.
        /// </summary>
        private void RegisterItems()
        {
            // Very basic items first, so later items can rely on them for recipes.
            ItemInfo.AddPrefab(new MobDrop(MobDrop.Variant.LavaLizardScale), nameof(MobDrop.Variant.LavaLizardScale));
            ItemInfo.AddPrefab(new MobDrop(MobDrop.Variant.SpineEelScale), nameof(MobDrop.Variant.SpineEelScale));
            ItemInfo.AddPrefab(new MobDrop(MobDrop.Variant.ThermophileSample), nameof(MobDrop.Variant.ThermophileSample));
            
            ItemInfo.AddPrefab(new AcidBattery(_Config.BatteryCapacity.Value));
            ItemInfo.AddPrefab(new AcidPowerCell(_Config.BatteryCapacity.Value));
            ItemInfo.AddPrefab(new DecompressionModule());
            ItemInfo.AddPrefab(new FilterChip());
            ItemInfo.AddPrefab(new Suit(Suit.Variant.ReinforcedFiltrationSuit), nameof(Suit.Variant.ReinforcedFiltrationSuit));
            ItemInfo.AddPrefab(new Suit(Suit.Variant.ReinforcedSuitMk2), nameof(Suit.Variant.ReinforcedSuitMk2));
            ItemInfo.AddPrefab(new Suit(Suit.Variant.ReinforcedSuitMk3), nameof(Suit.Variant.ReinforcedSuitMk3));
            ItemInfo.AddPrefab(new Tank(Tank.Variant.ChemosynthesisTank), nameof(Tank.Variant.ChemosynthesisTank));
            ItemInfo.AddPrefab(new Tank(Tank.Variant.PhotosynthesisTank), nameof(Tank.Variant.PhotosynthesisTank));
            ItemInfo.AddPrefab(new Tank(Tank.Variant.PhotosynthesisTankSmall), nameof(Tank.Variant.PhotosynthesisTankSmall));
        }

        /// <summary>
        /// Add all the new nodes to the craft tree.
        /// </summary>
        private void SetupCraftTree()
        {
            Atlas.Sprite suitIcon = Hootils.LoadSprite("SuitTabIcon.png", true);
            Atlas.Sprite tankIcon = Hootils.LoadSprite("TankTabIcon.png", true);

            CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, ItemInfo.GetSuitCraftTabId(),
                "Dive Suit Upgrades", suitIcon);
            CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, ItemInfo.GetTankCraftTabId(),
                "Specialty O2 Tanks", tankIcon);
        }
    }
}