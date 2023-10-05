using HootLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Handlers;

namespace DeathrunRemade.Items
{
    /// <summary>
    /// Represents all items that can be acquired by knifing specific fish.
    /// </summary>
    internal class MobDrop : DeathrunPrefabBase
    {
        public enum Variant
        {
            LavaLizardScale,
            SpineEelScale,
            ThermophileSample,
        }

        public Variant DropVariant { get; }
        public static TechType LavaLizardScale;
        public static TechType SpineEelScale;
        public static TechType ThermophileSample;

        public MobDrop(Variant variant)
        {
            DropVariant = variant;
            
            _prefabInfo = Hootils.CreatePrefabInfo(
                GetClassId(variant),
                GetDisplayName(variant),
                GetDescription(variant),
                GetSprite(variant)
            );
            AssignTechType(_prefabInfo, variant);

            _prefab = new CustomPrefab(_prefabInfo);
            _prefab.SetPdaGroupCategory(TechGroup.Resources, TechCategory.BasicMaterials);

            var template = new CloneTemplate(_prefabInfo, TechType.StalkerTooth);
            _prefab.SetGameObject(template);
            
            RegisterHarvestData(variant);
        }
        
        private void AssignTechType(PrefabInfo info, Variant variant)
        {
            switch (variant)
            {
                case Variant.LavaLizardScale:
                    LavaLizardScale = info.TechType;
                    break;
                case Variant.SpineEelScale:
                    SpineEelScale = info.TechType;
                    break;
                case Variant.ThermophileSample:
                    ThermophileSample = info.TechType;
                    break;
            }
        }

        /// <summary>
        /// Allow the mob drops to be collected from fish with the knife.
        /// </summary>
        private void RegisterHarvestData(Variant variant)
        {
            TechType target = variant switch
            {
                Variant.LavaLizardScale => TechType.LavaLizard,
                Variant.SpineEelScale => TechType.SpineEel,
                Variant.ThermophileSample => TechType.LavaLarva,
                _ => TechType.None
            };
            CraftDataHandler.SetHarvestOutput(target, TechType);
            CraftDataHandler.SetHarvestType(target, HarvestType.DamageAlive);
        }

        /// <summary>
        /// Get the class id for the type of drop.
        /// </summary>
        private string GetClassId(Variant variant)
        {
            string id = variant switch
            {
                Variant.LavaLizardScale => "lavalizardscale",
                Variant.SpineEelScale => "spineeelscale",
                Variant.ThermophileSample => "thermophilsample",
                _ => null
            };
            return $"{Constants.ClassIdPrefix}{id}";
        }

        /// <summary>
        /// Gets the display name for the type of drop.
        /// </summary>
        private string GetDisplayName(Variant variant)
        {
            return variant switch
            {
                Variant.LavaLizardScale => "Lava Lizard Scale",
                Variant.SpineEelScale => "River Prowler Scale",
                Variant.ThermophileSample => "Thermophile Sample",
                _ => null
            };
        }

        /// <summary>
        /// Gets the description for the type of drop.
        /// </summary>
        private string GetDescription(Variant variant)
        {
            return variant switch
            {
                Variant.LavaLizardScale => "A scale from a Lava Lizard. Has uses in depth and heat resistant fabrication.",
                Variant.SpineEelScale => "A scale from the head of a River Prowler. Has uses in depth-resistant fabrication.",
                Variant.ThermophileSample => "A viable sample of a unique thermophile bacteria found in Lava Larvae. "
                                          + "Undergoes chemosynthesis at high temperatures.",
                _ => null
            };
        }

        /// <summary>
        /// Gets the right sprite for the drop.
        /// </summary>
        private Atlas.Sprite GetSprite(Variant variant)
        {
            string filePath = variant switch
            {
                Variant.LavaLizardScale => "lavalizardscale.png",
                Variant.SpineEelScale => "rivereelscale.png",
                Variant.ThermophileSample => "thermophilesample.png",
                _ => null
            };
            return Hootils.LoadSprite(filePath, true);
        }
    }
}