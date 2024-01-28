using System;
using System.Collections.Generic;
using System.Reflection;
using DeathrunRemade.Objects.Exceptions;
using HarmonyLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Handlers;

namespace DeathrunRemade.Objects
{
    /// <summary>
    /// At the time of writing, Nautilus does not provide a way to undo any setup done by its Gadgets system. A lot
    /// of access modifiers are super closed off so even subclassing won't do much good. Use reflection instead.
    /// </summary>
    internal static class GadgetExtensions
    {
        public static void Teardown(this Gadget gadget)
        {
            switch (gadget)
            {
                case CraftingGadget craft:
                    craft.Teardown();
                    return;
                case EquipmentGadget equip:
                    equip.Teardown();
                    return;
                case ScanningGadget scan:
                    scan.Teardown();
                    return;
            }

            throw new DeathrunException($"Unhandled type of gadget in teardown: {gadget.GetType()}");
        }
        
        public static void Teardown(this CraftingGadget gadget)
        {
            ICustomPrefab prefab = GetPrefab(gadget);
            if (prefab is null)
                throw new NullReferenceException($"Failed to get prefab for gadget '{gadget.GetType().Name}'!");
            
            if (gadget.RecipeData != null)
                CraftDataHandler.SetRecipeData(prefab.Info.TechType, null);
            if (gadget.FabricatorType != CraftTree.Type.None)
                CraftTreeHandler.RemoveNode(gadget.FabricatorType, gadget.StepsToFabricatorTab.AddToArray(prefab.Info.TechType.AsString()));
        }
        
        public static void Teardown(this EquipmentGadget gadget)
        {
            ICustomPrefab prefab = GetPrefab(gadget);
            if (prefab is null)
                throw new NullReferenceException($"Failed to get prefab for gadget '{gadget.GetType().Name}'!");
            
            if (gadget.EquipmentType != EquipmentType.None)
                CraftDataHandler.SetEquipmentType(prefab.Info.TechType, EquipmentType.None);
            if (gadget.QuickSlotType != QuickSlotType.None)
                CraftDataHandler.SetQuickSlotType(prefab.Info.TechType, QuickSlotType.None);
        }
        
        public static void Teardown(this ScanningGadget gadget)
        {
            ICustomPrefab prefab = GetPrefab(gadget);
            if (prefab is null)
                throw new NullReferenceException($"Failed to get prefab for gadget '{gadget.GetType().Name}'!");
            
            if (gadget.GroupForPda != TechGroup.Uncategorized)
                CraftDataHandler.RemoveFromGroup(gadget.GroupForPda, gadget.CategoryForPda, prefab.Info.TechType);
            
            KnownTechHandler.RemoveAllCurrentAnalysisTechEntry(prefab.Info.TechType);
            // Encyclopedia entries are annoying to reset but they should not unlock if the item is never acquired,
            // which it cannot be after these Teardowns. This should be alright.
        }
        
        /// <summary>
        /// Grab the protected prefab of a gadget via reflection.
        /// </summary>
        private static ICustomPrefab GetPrefab(this Gadget gadget)
        {
            FieldInfo field = AccessTools.Field(typeof(Gadget), "prefab");
            return (ICustomPrefab)field.GetValue(gadget);
        }

        public static Dictionary<Type, Gadget> GetAllGadgets(this CustomPrefab prefab)
        {
            FieldInfo field = AccessTools.Field(typeof(CustomPrefab), "_gadgets");
            return (Dictionary<Type, Gadget>)field.GetValue(prefab);
        }
    }
}