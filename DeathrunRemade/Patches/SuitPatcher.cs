using DeathrunRemade.Items;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Always)]
    internal class SuitPatcher
    {
        public const float MinTemperatureLimit = 49f;
        
        /// <summary>
        /// Ensure that some of the special suits are also recognised as reinforced suits.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.HasReinforcedSuit))]
        private static void RecogniseUpgradedSuits(ref bool __result)
        {
            if (__result)
                return;

            var equipment = Inventory.main.equipment;
            __result = equipment.GetCount(ReinforcedFiltrationSuit.s_TechType) > 0
                       || equipment.GetCount(ReinforcedSuitMk2.s_TechType) > 0
                       || equipment.GetCount(ReinforcedSuitMk3.s_TechType) > 0;
        }

        /// <summary>
        /// Ensure that temperature is updated properly for custom suits.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateReinforcedSuit))]
        private static void UpdateSuitValues(ref Player __instance)
        {
            TechType suit = Inventory.main.equipment.GetTechTypeInSlot("Body");

            // Only change things if this is a suit added by our mod.
            if (!SuitBase.TryGetTemperatureLimit(suit, out float limit))
                return;
            if (__instance.HasReinforcedGloves())
                limit += 6f;
            
            // Other mods might add extra equipment that raises the limit even higher. Do not overwrite that,
            // but ensure a floor.
            if (__instance.temperatureDamage.minDamageTemperature < limit)
                __instance.temperatureDamage.minDamageTemperature = limit;
        }

        /// <summary>
        /// Set textures properly for the custom suits, so they don't just use the default suit texture
        /// when worn.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.EquipmentChanged))]
        private static void UpdateSuitTextures() {
            TechType suit = Inventory.main.equipment.GetTechTypeInSlot("Body");

            // Determine the suit appearance we want to clone,
            // or do nothing if not wearing a custom suit.
            string suitClonePath;
            if (suit == ReinforcedSuitMk2.s_TechType || suit == ReinforcedSuitMk3.s_TechType) {
                suitClonePath = "reinforcedSuit/reinforced_suit_01_body_geo";
            } else if (suit == ReinforcedFiltrationSuit.s_TechType) {
                suitClonePath = "stillSuit/still_suit_01_body_geo";
            } else {
                return;
            }

            string defaultTextureName = "_MainTex";

            // Get GameObjects for the default suit and the suit we want to clone.
            Transform geo = Player.main.transform.Find("body/player_view/male_geo");
            GameObject cloneSuit = geo.Find(suitClonePath).gameObject;
            GameObject defaultSuit = geo.Find("diveSuit/diveSuit_body_geo").gameObject;
            // Get renderer and texture for the clone suit.
            Renderer renderer = cloneSuit.GetComponent<Renderer>();
            Texture texture = renderer.material.GetTexture(defaultTextureName);

            // Activate the model for the clone suit, and deactivate the default suit model.
            cloneSuit.SetActive(true);
            defaultSuit.SetActive(false);

            // Set the suit texture.
            renderer.materials[0].SetTexture(defaultTextureName, (Texture2D)texture);
        }
    }

}