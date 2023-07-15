using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using DeathrunRemade.Components;
using DeathrunRemade.Configuration;
using DeathrunRemade.Handlers;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using HarmonyLib;
using HootLib;
using HootLib.Components;
using Nautilus.Handlers;
using Nautilus.Utility;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

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
        internal static NitrogenHandler _Nitrogen;
        internal static NotificationHandler _Notifications;

        // Run Update() once per second.
        private const float UpdateInterval = 1f;
        private float _nextUpdate;

        private void Awake()
        {
            _Log = new HootLogger(NAME);
            _Log.Info($"{NAME} v{VERSION} starting up.");
            
            // Registering config.
            _Config = new Config(Path.Combine(Paths.ConfigPath, Hootils.GetConfigFileName(NAME)),
                Info.Metadata);
            _Config.RegisterModOptions(NAME, transform);
            
            InitHandlers();
            SetupCraftTree();
            RegisterItems();
            RegisterCommands();
            RegisterGameEvents();
            
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(Hootils.GetAssembly());

            // Register the save game and ensure Nautilus marks it as ready as soon as the player enters the game.
            SaveData.Main = SaveDataHandler.RegisterSaveDataCache<SaveData>();
            SaveUtils.RegisterOnLoadEvent(() => SaveData.Main.Ready = true);

            _Log.Info("Finished loading.");
        }

        private void Update()
        {
            // Only run this method every so often.
            if (Time.time < _nextUpdate)
                return;
            _nextUpdate = Time.time + UpdateInterval;
            
            // Putting these things here prevents having to run them as MonoBehaviours too.
            _Nitrogen.Update();
            _Notifications.Update();
        }

        private void InitHandlers()
        {
            _Nitrogen = new NitrogenHandler();
            _Notifications = new NotificationHandler(_Log);
        }

        private void RegisterCommands()
        {
            ConsoleCommandsHandler.RegisterConsoleCommand<Action>("test", TestMe);
        }

        private void RegisterGameEvents()
        {
            GameEventHandler.OnPlayerAwake += _ =>
            {
                HootHudBar.Create<NitrogenBar>("NitrogenBar", -45, out GameObject _);
                SafeDepthHud.Create(out GameObject _); 
            };
        }

        private void TestMe()
        {
            _Notifications.SetupSlots();
            _Notifications.AddMessage(NotificationHandler.TopMiddle, "This is a great test for testing purposes. Your oxygen is gone btw")
                .SetDuration(5, 3);
            _Notifications.AddMessage(NotificationHandler.LeftMiddle, "N2 is a whole lot here oh god oh no");
        }

        /// <summary>
        /// Register all custom items added by this mod.
        /// </summary>
        private void RegisterItems()
        {
            // Not convinced I'm keeping this list but let's have it ready for now.
            List<DeathrunPrefabBase> prefabs = new List<DeathrunPrefabBase>();
            // Very basic items first, so later items can rely on them for recipes.
            prefabs.Add(new MobDrop(MobDrop.Variant.LavaLizardScale));
            prefabs.Add(new MobDrop(MobDrop.Variant.SpineEelScale));
            prefabs.Add(new MobDrop(MobDrop.Variant.ThermophileSample));
            
            prefabs.Add(new AcidBattery(_Config.BatteryCapacity.Value));
            prefabs.Add(new AcidPowerCell(_Config.BatteryCapacity.Value));
            prefabs.Add(new DecompressionModule());
            prefabs.Add(new FilterChip());
            prefabs.Add(new Suit(Suit.Variant.ReinforcedFiltrationSuit));
            prefabs.Add(new Suit(Suit.Variant.ReinforcedSuitMk2));
            prefabs.Add(new Suit(Suit.Variant.ReinforcedSuitMk3));
            prefabs.Add(new Tank(Tank.Variant.ChemosynthesisTank));
            prefabs.Add(new Tank(Tank.Variant.PhotosynthesisTank));
            prefabs.Add(new Tank(Tank.Variant.PhotosynthesisTankSmall));
        }

        /// <summary>
        /// Add all the new nodes to the craft tree.
        /// </summary>
        private void SetupCraftTree()
        {
            Atlas.Sprite suitIcon = Hootils.LoadSprite("SuitTabIcon.png", true);
            Atlas.Sprite tankIcon = Hootils.LoadSprite("TankTabIcon.png", true);

            CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, Constants.WorkbenchSuitTab,
                "Dive Suit Upgrades", suitIcon);
            CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, Constants.WorkbenchTankTab,
                "Specialty O2 Tanks", tankIcon);
        }
    }
}