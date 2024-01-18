using System;
using BepInEx;
using DeathrunRemade.Components;
using DeathrunRemade.Components.RunStatsUI;
using DeathrunRemade.Configuration;
using DeathrunRemade.Handlers;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using DeathrunRemade.Patches;
using HarmonyLib;
using HootLib;
using HootLib.Components;
using HootLib.Objects;
using Nautilus.Handlers;
using Nautilus.Utility;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace DeathrunRemade
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.snmodding.nautilus", "1.0.0.27")]
    internal class DeathrunInit : BaseUnityPlugin
    {
        public const string GUID = "com.github.tinyhoot.DeathrunRemade";
        public const string NAME = "Deathrun Remade";
        public const string VERSION = "0.1.4";

        internal static Config _Config;
        internal static ILogHandler _Log;
        internal static NotificationHandler _Notifications;
        internal static RunHandler _RunHandler;
        internal static SafeDepthHud _DepthHud;
        private EncyclopediaHandler _encyclopediaHandler;
        private VanillaRecipeChanges _recipeChanges;
        
        // The base object from which the main menu highscores window is instantiated.
        private GameObject _baseStatsWindow;
        // The object that is actually active.
        internal static RunStatsWindow _RunStatsWindow;

        // Run Update() once per second.
        private const float UpdateInterval = 1f;
        private Hootimer _updateTimer;
        private Harmony _harmony;

        private void Awake()
        {
            _Log = new HootLogger(NAME);
            _Log.Info($"{NAME} v{VERSION} starting up.");

            _updateTimer = new Hootimer(() => Time.deltaTime, UpdateInterval);
            
            // Registering config.
            _Config = new Config(Hootils.GetConfigFilePath(NAME), Info.Metadata);
            _Config.RegisterModOptions(NAME, transform);

            // Register the in-game save game of the current run.
            SaveData.Main = SaveDataHandler.RegisterSaveDataCache<SaveData>();

            try {
                InitHandlers();
                LoadFiles();
                SetupCraftTree();
                RegisterItems();
                RegisterCommands();
                RegisterGameEvents();
                
                // Set up all the harmony patching.
                _harmony = new Harmony(GUID);
                HarmonyPatching(_harmony);
                SaveData.OnSaveDataLoaded += HarmonyPatchingDelayed;
            }
            catch (Exception ex)
            {
                DeathrunUtils.FatalError(ex);
            }

            _Log.Info("Finished loading.");
        }

        private void Update()
        {
            // Only run this method every so often.
            if (!_updateTimer.Tick())
                return;
            
            // Putting these things here prevents having to run them as MonoBehaviours too.
            _Notifications.Update();
        }

        /// <summary>
        /// Execute all harmony patches that need to run every time regardless of which config options were chosen.
        /// </summary>
        private void HarmonyPatching(Harmony harmony)
        {
            // Waiting for HarmonyX to update their fork with PatchCategories
            harmony.PatchAll(typeof(GameEventHandler));
            harmony.PatchAll(typeof(BatteryPatcher));
            harmony.PatchAll(typeof(CauseOfDeathPatcher));
            harmony.PatchAll(typeof(CompassPatcher));
            harmony.PatchAll(typeof(CountdownPatcher));
            harmony.PatchAll(typeof(EscapePodPatcher));
            harmony.PatchAll(typeof(ExplosionPatcher));
            harmony.PatchAll(typeof(FilterPumpPatcher));
            harmony.PatchAll(typeof(FoodChallengePatcher));
            harmony.PatchAll(typeof(RadiationPatcher));
            harmony.PatchAll(typeof(RunStatsTracker));
            harmony.PatchAll(typeof(SaveFileMenuPatcher));
            harmony.PatchAll(typeof(SuitPatcher));
            harmony.PatchAll(typeof(TooltipPatcher));
            harmony.PatchAll(typeof(WaterMurkPatcher));
        }
        
        /// <summary>
        /// Execute all harmony patches that should only be applied with the right config options enabled. For that
        /// reason, they must be delayed until the game loads and the config is locked in.
        ///
        /// Some of these could totally run at the beginning of the game too, but why patch things when there's no
        /// need to?
        /// </summary>
        private void HarmonyPatchingDelayed(SaveData save)
        {
            try
            {
                ConfigSave config = save.Config;
                if (config.CreatureAggression != Difficulty4.Normal)
                    _harmony.PatchAll(typeof(AggressionPatcher));
                if (config.DamageTaken != DamageDifficulty.Normal)
                    _harmony.PatchAll(typeof(DamageTakenPatcher));
                if (config.SurfaceAir != Difficulty3.Normal)
                    _harmony.PatchAll(typeof(BreathingPatcher));
                if (config.NitrogenBends != Difficulty3.Normal)
                {
                    _harmony.PatchAll(typeof(NitrogenPatcher));
                    _harmony.PatchAll(typeof(SurvivalPatcher));
                }

                if (config.PowerCosts != Difficulty4.Normal)
                    _harmony.PatchAll(typeof(PowerPatcher));
                if (config.PacifistChallenge)
                    _harmony.PatchAll(typeof(PacifistPatcher));
            }
            catch (Exception ex)
            {
                DeathrunUtils.FatalError(ex);
            }
        }

        private void InitHandlers()
        {
            _Notifications = new NotificationHandler(_Log);
            // Load statistics of all runs ever played.
            _RunHandler = new RunHandler(_Log);
            _ = new WarningHandler(_Config, _Log, _Notifications, SaveData.Main);
            _encyclopediaHandler = new EncyclopediaHandler();
            _encyclopediaHandler.RegisterPdaEntries();
        }

        /// <summary>
        /// Load any files or assets the mod needs in order to run.
        /// </summary>
        private void LoadFiles()
        {
            _Log.Debug("Loading files...");
            _recipeChanges = new VanillaRecipeChanges();
            // Ignore a compiler warning.
            _ = _recipeChanges.LoadFromDiskAsync();
            
            // Load the assets for the highscore window. This was prepared in the unity editor.
            _Log.Debug("Loading assets...");
            AssetBundle bundle = AssetBundleLoadingUtils.LoadFromAssetsFolder(Hootils.GetAssembly(), "highscores");
            _baseStatsWindow = bundle.LoadAsset<GameObject>("Highscores");
            _baseStatsWindow.SetActive(false);

            _Log.Debug("Assets loaded.");
        }

        /// <summary>
        /// Do everything that needs to be done every time the main menu loads, such as setting up the window
        /// displaying all run stats.
        /// </summary>
        private void OnMainMenuLoaded()
        {
            // Ensure the highscore window is always ready to go.
            var window = Instantiate(_baseStatsWindow, uGUI_MainMenu.main.transform, false);
            _RunStatsWindow = window.GetComponent<RunStatsWindow>();
            var option = uGUI_MainMenu.main.primaryOptions.gameObject.AddComponent<MainMenuCustomPrimaryOption>();
            option.onClick.AddListener(window.GetComponent<MainMenuCustomWindow>().Open);
            option.SetText("Deathrun Stats");
            // Put this new option in the right place - just after the options menu button.
            int index = uGUI_MainMenu.main.primaryOptions.transform.Find("PrimaryOptions/MenuButtons/ButtonOptions").GetSiblingIndex();
            option.SetIndex(index + 1);
        }

        /// <summary>
        /// Do everything that can only be set up once the config is locked in, i.e. either a new game was started or
        /// the save cache of an existing save was loaded during the loading process.
        /// Happens early on in the loading process and generally precedes <see cref="OnPlayerAwake"/>.
        /// </summary>
        private void OnConfigLockedIn(SaveData save)
        {
            var config = save.Config;
            _recipeChanges.LockBatteryBlueprint(config.BatteryCosts);
            // Deal with any recipe changes.
            _recipeChanges.RegisterFragmentChanges(config);
            _recipeChanges.RegisterRecipeChanges(config);
            // Add first aid kits to quick slots.
            CraftDataHandler.SetQuickSlotType(TechType.FirstAidKit, QuickSlotType.Selectable);
            CraftDataHandler.SetEquipmentType(TechType.FirstAidKit, EquipmentType.Hand);
        }
        
        /// <summary>
        /// Do all the necessary work to get the mod going which can only be done when the game has set up most of its
        /// systems and is about to be ready to play.
        /// </summary>
        /// <param name="player">A freshly awoken player instance.</param>
        private void OnPlayerAwake(Player player)
        {
            ConfigSave config = SaveData.Main.Config;
            
            try {
                // Enable the tracker which updates all run statistics.
                player.gameObject.AddComponent<RunStatsTracker>();
                // Set up GUI components.
                RadiationPatcher.CalculateGuiPosition();
                // Register custom story goals relying on custom items.
                _encyclopediaHandler.RegisterStoryGoals();
                // Unlock all encyclopedia entries with Deathrun tutorials.
                _encyclopediaHandler.UnlockPdaIntroEntries();
                TooltipHandler.OverrideVanillaTooltips(config);
            
                // Enable crush depth if the player needs to breathe, i.e. is not in creative mode.
                if (config.PersonalCrushDepth != Difficulty3.Normal && GameModeUtils.RequiresOxygen())
                    player.tookBreathEvent.AddHandler(this, CrushDepthHandler.CrushPlayer);
                // Decrease the free health provided on respawn.
                if (config.DamageTaken != DamageDifficulty.Normal && player.liveMixin)
                    player.playerRespawnEvent.AddHandler(this, DamageTakenPatcher.DecreaseRespawnHealth);
                // Nitrogen and its UI if required by config and game mode settings.
                if (config.NitrogenBends != Difficulty3.Normal && GameModeUtils.RequiresOxygen())
                {
                    HootHudBar.Create<NitrogenBar>("NitrogenBar", -45, out GameObject _);
                    _DepthHud = SafeDepthHud.Create(out GameObject _);
                    player.gameObject.AddComponent<NitrogenHandler>();
                    player.gameObject.AddComponent<FastAscent>();
                }
            }
            catch (Exception ex)
            {
                DeathrunUtils.FatalError(ex);
            }
        }

        /// <summary>
        /// Initialise parts that need to be set up at the very last moment before the loading screen vanishes and
        /// the player gains control of their character.
        /// </summary>
        private void OnPlayerGainControl(Player player)
        {
            try {
                EscapePod.main.gameObject.AddComponent<EscapePodRecharge>();
                EscapePod.main.gameObject.AddComponent<EscapePodStatusScreen>();
                ExplosionCountdown.Create(out GameObject go);
                go.SetActive(true);
                // Ensure we always know about the player's current radiation immunity.
                RadiationPatcher.UpdateIsImmune(null, null);
                Inventory.main.equipment.onEquip += RadiationPatcher.UpdateIsImmune;
                Inventory.main.equipment.onUnequip += RadiationPatcher.UpdateIsImmune;
                Tutorial.RegisterEvents();
            }
            catch (Exception ex)
            {
                DeathrunUtils.FatalError(ex);
            }
        }

        private void RegisterCommands()
        {
#if DEBUG
            ConsoleCommandsHandler.RegisterConsoleCommand<Action>("loc", DumpLocation);
            ConsoleCommandsHandler.RegisterConsoleCommand<Action>("test", TestMe);
#endif
        }

        private void RegisterGameEvents()
        {
            GameEventHandler.RegisterEvents();
            // Initialise deathrun messaging as soon as uGUI_Main is ready, i.e. the main menu loads.
            GameEventHandler.OnMainMenuLoaded += _Notifications.OnMainMenuLoaded;
            GameEventHandler.OnMainMenuLoaded += OnMainMenuLoaded;
            GameEventHandler.OnPlayerAwake += OnPlayerAwake;
            GameEventHandler.OnPlayerGainControl += OnPlayerGainControl;
            GameEventHandler.OnSavedGameLoaded += EscapePodSinker.OnSavedGameLoaded;
            SaveData.OnSaveDataLoaded += OnConfigLockedIn;
        }

        /// <summary>
        /// Register all custom items added by this mod.
        /// </summary>
        private void RegisterItems()
        {
            // Very basic items first, so later items can rely on them for recipes.
            new MobDrop(MobDrop.Variant.LavaLizardScale).Register();
            new MobDrop(MobDrop.Variant.SpineEelScale).Register();
            new MobDrop(MobDrop.Variant.ThermophileSample).Register();

            new AcidBattery().Register();
            new AcidPowerCell().Register();
            // Do this here so that the order of recipes is correct.
            AcidBattery.AddRecyclingRecipe();
            new DecompressionModule().Register();
            new FilterChip().Register();
            
            new Suit(Suit.Variant.ReinforcedFiltrationSuit).Register();
            new Suit(Suit.Variant.ReinforcedSuitMk2).Register();
            new Suit(Suit.Variant.ReinforcedSuitMk3).Register();
            Suit.RegisterCrushDepths();
            Suit.RegisterNitrogenModifiers();
            PDAScanner.onAdd += Suit.UnlockSuitOnScanFish;
            
            new Tank(Tank.Variant.ChemosynthesisTank).Register();
            new Tank(Tank.Variant.PhotosynthesisTank).Register();
            new Tank(Tank.Variant.PhotosynthesisTankSmall).Register();
        }

        /// <summary>
        /// Add all the new nodes to the craft tree.
        /// </summary>
        private void SetupCraftTree()
        {
            Atlas.Sprite suitIcon = Hootils.LoadSprite("SuitTabIcon.png", true);
            Atlas.Sprite tankIcon = Hootils.LoadSprite("TankTabIcon.png", true);

            // This won't actually work because you can't add new tabs to ones which already contain items, but it
            // should play nicely with mods that reorganise the craft tree.
            CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, Suit.WorkbenchSuitTab,
                "Dive Suit Upgrades", suitIcon);
            CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, Tank.WorkbenchTankTab,
                "Specialty O2 Tanks", tankIcon);
        }

        private void DumpLocation()
        {
            _Log.Info($"Current location: {Player.main.transform.position}");
            _Log.InGameMessage($"Current location: {Player.main.transform.position}");
        }

        private void TestMe()
        {
            foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                _Log.Debug($"Bundle: {bundle.name}");
                
                foreach (var path in bundle.GetAllAssetNames())
                {
                    _Log.Debug($"> {path}");
                }
            }
        }
    }
}