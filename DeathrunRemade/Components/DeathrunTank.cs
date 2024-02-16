using DeathrunRemade.Items;
using UnityEngine;

namespace DeathrunRemade.Components
{
    /// <summary>
    /// This component is added to custom deathrun tanks and controls their unique behaviour.
    /// </summary>
    /// <seealso cref="DeathrunRemade.Items.TankBase"/>
    internal class DeathrunTank : MonoBehaviour
    {
        // Do not update every frame, only every so often.
        private const float UpdateInterval = 3.0f;
        private const float MinLight = 0.3f;
        private const float MinTemp = 30f;
        private const float UnequippedMult = 0.33f;
        private Equipment _equipment;

        private DayNightCycle _dayNightCycle;
        private Oxygen _oxygen;
        private WaterTemperatureSimulation _waterTemperature;
        private TechType _techType;
        private bool _isEquipped;

        private void Awake()
        {
            _equipment = Inventory.main.equipment;
            _oxygen = GetComponent<Oxygen>();
            _techType = GetComponent<Pickupable>().GetTechType();

            _equipment.onEquip += OnEquip;
            _equipment.onUnequip += OnUnequip;
            // Items in the player's inventory have their GameObjects set to inactive. Invoke runs regardless.
            InvokeRepeating(nameof(UpdateOxygen), 0f, UpdateInterval);
        }

        private void OnDestroy()
        {
            // It is possible that this happens during scene clean back to main menu and the inventory is already gone.
            if (_equipment != null)
            {
                _equipment.onEquip -= OnEquip;
                _equipment.onUnequip -= OnUnequip;
            }
            CancelInvoke();
        }

        private void OnEquip(string slot, InventoryItem item)
        {
            if (item.item.gameObject != gameObject)
                return;
            
            _isEquipped = true;
        }

        private void OnUnequip(string slot, InventoryItem item)
        {
            if (item.item.gameObject != gameObject)
                return;
            
            _isEquipped = false;
        }

        private void UpdateOxygen()
        {
            if (_techType == ChemosynthesisTank.s_TechType)
                UpdateChemosynthesisTank();
            if (_techType == PhotosynthesisTank.s_TechType || _techType == PhotosynthesisTankSmall.s_TechType)
                UpdatePhotoSynthesisTank();
        }

        /// <summary>
        /// Chemosynthesis tanks generate oxygen based on temperature.
        /// </summary>
        private void UpdateChemosynthesisTank()
        {
            // Just for safety.
            if (_waterTemperature == null)
            {
                _waterTemperature = WaterTemperatureSimulation.main;
                return;
            }
            
            float temperature = _waterTemperature.GetTemperature(Player.main.transform.position);
            if (temperature < MinTemp)
                return;
            float equippedMult = _isEquipped ? 1f : UnequippedMult;
            
            // Works out to around 1/s at 90C if equipped.
            _oxygen.AddOxygen(UpdateInterval * temperature * 0.01f * equippedMult);
        }

        /// <summary>
        /// Photosynthesis tanks fill up based on available light, meaning day/night cycle and depth.
        /// </summary>
        private void UpdatePhotoSynthesisTank()
        {
            // Just for safety.
            if (_dayNightCycle == null)
            {
                _dayNightCycle = DayNightCycle.main;
                return;
            }
            
            float brightness = _dayNightCycle.GetLocalLightScalar();
            // The game ensures that depth is never negative.
            float depth = Player.main.GetDepth();
            if (brightness < MinLight || depth > 200f)
                return;
            float equippedMult = _isEquipped ? 1f : UnequippedMult;
            
            // Works out to around 1/s in full daylight at sea level if equipped.
            _oxygen.AddOxygen(UpdateInterval * brightness * ((200f - depth) / 200f) * equippedMult);
        }
    }
}