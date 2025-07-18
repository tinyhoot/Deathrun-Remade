﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using DeathrunRemade.Components;
using DeathrunRemade.Components.NitrogenUI;
using DeathrunRemade.Configuration;
using DeathrunRemade.Handlers;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using DeathrunRemade.Objects.Exceptions;
using DeathrunRemade.Patches;
using HarmonyLib;
using HootLib;
using HootLib.Components;
using Nautilus.Handlers;
using Story;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace DeathrunRemade
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.snmodding.nautilus", "1.0.0.39")]
    internal class DeathrunInit : BaseUnityPlugin
    {
        public const string GUID = "com.github.tinyhoot.DeathrunRemade";
        public const string NAME = "Deathrun Remade";
        public const string VERSION = "1.1.2";

        internal static Config _Config;
        internal static ILogHandler _Log;
        internal static RunHandler _RunHandler;
        internal static VanillaRecipeChanges _recipeChanges;

        // Persists across scenes, holds vital components.
        private GameObject _persistentObject;
        // The base object from which the main menu highscores window is instantiated.
        private GameObject _baseStatsWindow;
        private Harmony _harmony;
        internal static List<DeathrunPrefabBase> CustomItems { get; private set; }

        /// <summary>
        /// Called when a reset of all save-specific patches and changes is necessary so that everything is ready for
        /// immediately loading another save (or starting a new run).<br />
        /// This mostly concerns data sent to Nautilus like recipe changes.
        /// </summary>
        public static event Action OnReset;

        private void Awake()
        {
            _Log = new HootLogger(NAME);
            _Log.Info($"{NAME} v{VERSION} starting up.");
            CrashIfSmlHelper();

            // Create a holding GameObject under BepInEx's for data which should persist in both main menu and ingame.
            // Will not actually persist if the BepInEx manager object is not tagged with SceneCleanerPreserve.
            _persistentObject = new GameObject("Deathrun Components");
            _persistentObject.transform.SetParent(transform, false);
            
            // Registering config.
            _Config = new Config(Hootils.GetConfigFilePath(NAME), Info.Metadata);
            _Config.Setup();
            _Config.CreateModMenu(NAME, _persistentObject.transform);

            // Ensure the Reset event is always called just before the save data loads.
            WaitScreenHandler.RegisterEarlyLoadTask(NAME, _ => OnReset.Invoke(), "Preparing clean slate");
            // Register the in-game save game of the current run.
            SaveData.Main = SaveDataHandler.RegisterSaveDataCache<SaveData>();
            SaveData.Main.Init();

            try {
                InitHandlers();
                StartCoroutine(Hootils.WrapCoroutine(LoadFilesAsync(), DeathrunUtils.FatalError));
                SetupCraftTree();
                RegisterItems();
                RegisterCommands();
                RegisterGameEvents();
                
                // Set up harmony patching so that we unpatch and re-patch every time the user enters the game.
                _harmony = new Harmony(GUID);
                // Patch everything that needs to be on regardless of settings.
                _Log.Debug("Applying general harmony patches.");
                _harmony.PatchTypesWithCategory(ApplyPatch.Always);
                OnReset += () => UnpatchHarmony(_harmony);
                // Set up mod loading tasks for anything save-specific that needs doing during the loading screen.
                WaitScreenHandler.RegisterEarlyAsyncLoadTask(NAME, OnEarlyModLoadingStage);
                WaitScreenHandler.RegisterLateAsyncLoadTask(NAME, OnLateModLoadingStage);
            }
            catch (Exception ex)
            {
                DeathrunUtils.FatalError(ex);
            }

            _Log.Info("Finished loading.");
        }

        /// <summary>
        /// SMLHelper clashes with Nautilus in a multitude of ways and having both of them active is asking for
        /// anything from invisible player models over unbearable lag to save corruption. If SMLHelper is present,
        /// refuse to load.
        /// </summary>
        private void CrashIfSmlHelper()
        {
            if (!Chainloader.PluginInfos.ContainsKey("com.ahk1221.smlhelper"))
                return;
            
            _Log.InGameMessage($"{NAME} is incompatible with SMLHelper and has refused to load. Uninstall SMLHelper if "
                               + $"you want to play {NAME}.", true);
            DestroyImmediate(this);
            throw new SmlHelperIsPresentException();
        }

        /// <summary>
        /// Unpatch all harmony patches which should only be enabled with specific config settings.
        /// </summary>
        private void UnpatchHarmony(Harmony harmony)
        {
            try
            {
                _Log.Debug("Unpatching config-reliant harmony patches.");
                // Undo all config-reliant harmony patches so we can patch with a clean slate. Does nothing the first time
                // (telling Harmony to unpatch something it never patched is safe) but helps the mod not break when
                // trying to load into a game for the second time.
                harmony.UnpatchTypesWithCategory(ApplyPatch.Config);
            }
            catch (Exception ex)
            {
                DeathrunUtils.FatalError(ex);
            }
        }
        
        /// <summary>
        /// Execute all harmony patches that should only be applied with the right config options enabled. For that
        /// reason, they must be delayed until the game loads and the config is locked in.
        ///
        /// Some of these could totally run at the beginning of the game too, but why patch things when there's no
        /// need to?
        /// </summary>
        private void PatchHarmonyWithConfig(Harmony harmony, ConfigSave config)
        {
            try
            {
                _Log.Debug("Applying config-reliant harmony patches.");
                // I considered creating new categories for each config option but it's just not worth it when
                // each option only has 1-2 patching classes. Category-based unpatching is convenient enough.
                if (config.CreatureAggression != Difficulty4.Normal)
                    harmony.PatchAll(typeof(AggressionPatcher));
                if (config.PersonalCrushDepth != Difficulty3.Normal && GameModeUtils.RequiresOxygen())
                    harmony.PatchAll(typeof(CompassPatcher));
                if (config.DamageTaken != DamageDifficulty.Normal)
                    harmony.PatchAll(typeof(DamageTakenPatcher));
                if (config.SurfaceAir != Difficulty3.Normal)
                    harmony.PatchAll(typeof(BreathingPatcher));
                if (config.NitrogenBends != Difficulty3.Normal && GameModeUtils.RequiresOxygen())
                {
                    harmony.PatchAll(typeof(NitrogenPatcher));
                    harmony.PatchAll(typeof(SurvivalPatcher));
                }
                if (config.PowerCosts != Difficulty4.Normal)
                    harmony.PatchAll(typeof(PowerPatcher));
                if (config.FarmingChallenge != Difficulty3.Normal)
                    harmony.PatchAll(typeof(FarmingChallengePatcher));
                if (config.PacifistChallenge)
                    harmony.PatchAll(typeof(PacifistPatcher));
            }
            catch (Exception ex)
            {
                DeathrunUtils.FatalError(ex);
            }
        }

        private void InitHandlers()
        {
            LocalisationHandler.Init();
            // Load statistics of all runs ever played.
            _RunHandler = new RunHandler(_Log);
            // Prepare in-game messaging.
            var notifications = _persistentObject.AddComponent<NotificationHandler>();
            _ = new WarningHandler(_Config, _Log, notifications, SaveData.Main);
            EncyclopediaHandler.Init();
            
            // Ensure that any variables are re-inserted into text when the language changes.
            Language.OnLanguageChanged += () => EncyclopediaHandler.FormatEncyEntries(SaveData.Main.Config);
            Language.OnLanguageChanged += () => TooltipHandler.OverrideVanillaTooltips(SaveData.Main.Config);
        }

        /// <summary>
        /// Load any files or assets the mod needs in order to run.
        /// </summary>
        private IEnumerator LoadFilesAsync()
        {
            _Log.Debug("Loading assets...");
            _recipeChanges = new VanillaRecipeChanges();
            // There's no need to yield and wait for this to finish, just let it load in parallel.
            Task.Run(_recipeChanges.LoadFromDiskAsync);
            LanguageHandler.RegisterLocalizationFolder(Path.Combine("Assets", "Localization"));
            
            // Load the assets for the highscore window. This was prepared in the unity editor.
            var fileRequest = AssetBundle.LoadFromFileAsync(Hootils.GetAssetHandle("highscoreswindow"));
            yield return fileRequest;
            AssetBundle bundle = fileRequest.assetBundle;
            var objRequest = bundle.LoadAssetAsync<GameObject>("Highscores");
            yield return objRequest;
            _baseStatsWindow = objRequest.asset as GameObject;
            if (_baseStatsWindow == null)
                throw new NullReferenceException("Run stats window loaded from asset bundle is null!");
            
            _baseStatsWindow.SetActive(false);
            // Save a bit of memory by unloading the compressed bundle data.
            bundle.Unload(false);
            
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
            // Create a new main menu option to access Deathrun run stats.
            var option = MainMenuCustomPrimaryOption.Create("Deathrun Highscores", "dr_mainmenu_button");
            option.onClick.AddListener(window.GetComponent<MainMenuCustomWindow>().Open);
            // Put this new option in the right place - just after the options menu button.
            int index = option.transform.Find("../ButtonOptions").GetSiblingIndex();
            option.SetIndex(index + 1);
        }

        private IEnumerator OnEarlyModLoadingStage(WaitScreenHandler.WaitScreenTask task)
        {
            task.Status = "Registering items and recipes";
            SaveData saveData = SaveData.Main;
            SetUpItemsWithConfig(saveData.Config);
            yield return null;
            
            task.Status = "Patching game according to mod settings";
            PatchHarmonyWithConfig(_harmony, saveData.Config);
        }

        /// <summary>
        /// Do everything that can only be set up once the config is locked in, i.e. either a new game was started or
        /// the save cache of an existing save was loaded during the loading process.
        /// Happens very early on in the loading process.
        /// </summary>
        private void SetUpItemsWithConfig(ConfigSave config)
        {
            CustomItems.Do(item => item.Register(config));
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
        private IEnumerator OnLateModLoadingStage(WaitScreenHandler.WaitScreenTask task)
        {
            ConfigSave config = SaveData.Main.Config;
            PreparePlayer(task, Player.main, config);
            // Wait a frame to reduce stuttering on weaker machines.
            yield return null;
            PerformLastSecondSetup(task, config);
        }
        
        /// <summary>
        /// Perform every save/config-specific change directly related to the player.
        /// </summary>
        private void PreparePlayer(WaitScreenHandler.WaitScreenTask task, Player player, ConfigSave config)
        {
            try
            {
                task.Status = "Setting up GUI";
                // Enable the tracker which updates all run statistics.
                player.gameObject.AddComponent<RunStatsTracker>();
                // Set up GUI components.
                RadiationPatcher.CalculateGuiPosition();
                TooltipHandler.OverrideVanillaTooltips(config);

                task.Status = "Preparing Player";
                // Enable crush depth if the player needs to breathe, i.e. is not in creative mode.
                if (config.PersonalCrushDepth != Difficulty3.Normal && GameModeUtils.RequiresOxygen())
                    player.tookBreathEvent.AddHandler(this, CrushDepthHandler.CrushPlayer);
                // Decrease the free health provided on respawn.
                if (config.DamageTaken != DamageDifficulty.Normal && player.liveMixin)
                    player.playerRespawnEvent.AddHandler(this, DamageTakenPatcher.DecreaseRespawnHealth);
                // Nitrogen and its UI if required by config and game mode settings.
                if (config.NitrogenBends != Difficulty3.Normal && GameModeUtils.RequiresOxygen())
                {
                    SafeDepthHud.Create();
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
        private void PerformLastSecondSetup(WaitScreenHandler.WaitScreenTask task, ConfigSave config)
        {
            try
            {
                task.Status = "Setting up Lifepod";
                GameObject pod = EscapePod.main.gameObject;
                pod.AddComponent<EscapePodRecharge>();
                pod.AddComponent<EscapePodStatusScreen>();
                if (config.SinkLifepod)
                    pod.EnsureComponent<EscapePodSinker>().OnSavedGameLoaded();

                task.Status = "Setting up explosion countdown";
                ExplosionCountdown.Create(out GameObject go);
                go.SetActive(true);
                
                // Unlock all encyclopedia entries with Deathrun tutorials, but only on a freshly started game.
                task.Status = "Unlocking helpful encyclopedia entries";
                if (EscapePod.main.isNewBorn)
                    EncyclopediaHandler.UnlockPdaIntroEntries();
                
                // Ensure we always know about the player's current radiation immunity.
                task.Status = "Initialising radiation";
                RadiationPatcher.Init();
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
            GameEventHandler.OnMainMenuLoaded += OnMainMenuLoaded;
        }

        /// <summary>
        /// Register all custom items added by this mod.
        /// </summary>
        private void RegisterItems()
        {
            // Use reflection to instantiate all items.
            CustomItems = Hootils.InstantiateSubclassesInAssembly<DeathrunPrefabBase>(Hootils.GetAssembly());
            // Iterate through this twice so that these items can use each other's TechTypes in their recipes.
            CustomItems.Do(item => item.SetupTechType());
            CustomItems.Do(item => item.SetupPrefab());
        }

        /// <summary>
        /// Add all the new nodes to the craft tree. This does not need to be reset since an empty tab node will simply
        /// not show up.
        /// </summary>
        private void SetupCraftTree()
        {
            Atlas.Sprite suitIcon = Hootils.LoadSprite("SuitTabIcon.png", true);
            Atlas.Sprite tankIcon = Hootils.LoadSprite("TankTabIcon.png", true);
            
            // Set the display name to null to force a fallback on localisable language keys.
            CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, SuitBase.WorkbenchSuitTab, null, suitIcon);
            CraftTreeHandler.AddTabNode(CraftTree.Type.Workbench, TankBase.WorkbenchTankTab, null, tankIcon);
        }

        private void DumpLocation()
        {
            _Log.Info($"Current location: {Player.main.transform.position}");
            _Log.InGameMessage($"Current location: {Player.main.transform.position}");
        }

        private void TestMe()
        {
            // foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles())
            // {
            //     _Log.Debug($"Bundle: {bundle.name}");
            //     
            //     foreach (var path in bundle.GetAllAssetNames())
            //     {
            //         _Log.Debug($"> {path}");
            //     }
            // }
            for (int i = 0; i < 32; i++)
            {
                _Log.Debug($"Layer {i} - '{LayerMask.LayerToName(i)}'");
            }
            StoryGoalManager.main.OnGoalComplete("Story_AuroraWarning3");
        }
    }
}