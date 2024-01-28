using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
using HarmonyLib;
using Nautilus.Assets;

namespace DeathrunRemade.Items
{
    internal abstract class DeathrunPrefabBase
    {
        public const string ClassIdPrefix = "deathrunremade_";
        
        private PrefabInfo _prefabInfo;
        private CustomPrefab _prefab;

        public virtual string ClassId => _prefabInfo.ClassID;

        public virtual CustomPrefab Prefab => _prefab;
        
        public virtual PrefabInfo PrefabInfo => _prefabInfo;

        // Ideally this would be static abstract but C#11 is unsupported on Subnautica's .NET Framework.
        public virtual TechType TechType => _prefabInfo.TechType;

        /// <summary>
        /// Create the <see cref="PrefabInfo"/> for this item, thereby registering the custom TechType with Nautilus.
        /// </summary>
        public void SetupTechType()
        {
            _prefabInfo = CreatePrefabInfo();
        }

        /// <summary>
        /// Set up and modify the prefab that will be instantiated.
        /// </summary>
        public void SetupPrefab()
        {
            _prefab = CreatePrefab(_prefabInfo);
        }

        /// <inheritdoc cref="SetupTechType"/>
        protected abstract PrefabInfo CreatePrefabInfo();

        /// <inheritdoc cref="SetupPrefab"/>
        protected abstract CustomPrefab CreatePrefab(PrefabInfo info);

        /// <summary>
        /// Check whether to register this prefab with Nautilus under the current settings.
        /// </summary>
        /// <param name="config">The config for this save file.</param>
        /// <returns>Whether the prefab should activate for this save.</returns>
        protected abstract bool ShouldActivateForConfig(ConfigSave config);

        /// <summary>
        /// Register the prefab with Nautilus. This is also the place for any additional work that needs to be done once
        /// the prefab is supposed to activate.
        /// </summary>
        public void Register(ConfigSave config)
        {
            if (!ShouldActivateForConfig(config))
                return;
            
            Register();
        }

        /// <inheritdoc cref="Register"/>
        protected virtual void Register()
        {
            _prefab.Register();
            // Unregister this custom item on every reset.
            DeathrunInit.OnReset += Unregister;
        }

        /// <summary>
        /// Unregister and clean up any additional things.
        /// </summary>
        public virtual void Unregister()
        {
            _prefab.Unregister();
            // Nautilus does not undo changes made by Gadgets, so we do it ourselves.
            _prefab.GetAllGadgets().Do(gadget => gadget.Value.Teardown());
            DeathrunInit.OnReset -= Unregister;
        }
    }
}