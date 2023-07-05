using DeathrunRemade.Interfaces;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using SubnauticaCommons;

namespace DeathrunRemade.Items
{
    internal class AcidBattery : IDeathrunPrefab
    {
        private readonly PrefabInfo _prefabInfo;
        private readonly CustomPrefab _prefab;

        private int _maxCapacity = 200;
        
        public AcidBattery()
        {
            var sprite = Hootils.LoadSprite("AcidBattery.png", true);
            _prefabInfo = Hootils.CreatePrefabInfo("acidbattery", "yes", "very yes", sprite);
            
            _prefab = new CustomPrefab(_prefabInfo);
            _prefab.SetRecipe(new RecipeData(new CraftData.Ingredient(TechType.Titanium, 2)))
                .WithFabricatorType(CraftTree.Type.Fabricator)
                .WithStepsToFabricatorTab(CraftTreeHandler.Paths.FabricatorsElectronics);
            _prefab.SetPdaGroupCategory(TechGroup.Resources, TechCategory.Electronics);
            _prefab.SetUnlock(TechType.AcidMushroom);
            _prefab.SetEquipment(EquipmentType.BatteryCharger);

            var template = new EnergySourceTemplate(_prefabInfo, _maxCapacity);
            _prefab.SetGameObject(template);
            _prefab.Register();
            
            CraftDataHandler.SetMaxCharge(_prefabInfo.TechType, _maxCapacity);
        }

        public string GetClassId() => _prefabInfo.ClassID;

        public PrefabInfo GetPrefabInfo() => _prefabInfo;

        public CustomPrefab GetPrefab() => _prefab;
    }
}