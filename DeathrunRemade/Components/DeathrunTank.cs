using DeathrunRemade.Items;
using UnityEngine;

namespace DeathrunRemade.Components
{
    /// <summary>
    /// This component is added to the player and from there controls the special tanks' unique behaviour.
    /// Due to the check of whether the tank is currently equipped it ends up more efficient than putting this
    /// component on every tank added by Deathrun.
    /// </summary>
    internal class DeathrunTank : MonoBehaviour
    {
        // Do not update every frame, only every so often.
        private const float UpdateInterval = 3.0f;
        private const float MinLight = 0.3f;
        private const float MinTemp = 30f;
        private float _nextUpdate;
        private Equipment _equipment;

        private DayNightCycle _dayNightCycle;
        private OxygenManager _oxygenManager;
        private WaterTemperatureSimulation _waterTemperature;

        private void Start()
        {
            _equipment = Inventory.main.equipment;
            _oxygenManager = Player.main.oxygenMgr;
        }

        private void Update()
        {
            // Don't check every single frame.
            if (Time.time < _nextUpdate)
                return;
            
            _nextUpdate = Time.time + UpdateInterval;
            // Unequipped tanks do not fill up.
            TechType techType = _equipment.GetItemInSlot("Tank")?.techType ?? TechType.None;
            if (techType.Equals(Tank.ChemosynthesisTank))
                UpdateChemosynthesisTank();
            if (techType.Equals(Tank.PhotosynthesisTank) || techType.Equals(Tank.PhotosynthesisTankSmall))
                UpdatePhotoSynthesisTank();
        }

        /// <summary>
        /// Chemosynthesis tanks generate oxygen based on temperature.
        /// </summary>
        private void UpdateChemosynthesisTank()
        {
            // Just for safety.
            if (_waterTemperature is null)
            {
                _waterTemperature = WaterTemperatureSimulation.main;
                return;
            }
            
            float temperature = _waterTemperature.GetTemperature(Player.main.transform.position);
            if (temperature < MinTemp)
                return;
            
            // Works out to around 1/s at 90C.
            _oxygenManager.AddOxygen(UpdateInterval * temperature * 0.01f);
        }

        /// <summary>
        /// Photosynthesis tanks fill up based on available light, meaning day/night cycle and depth.
        /// </summary>
        private void UpdatePhotoSynthesisTank()
        {
            // Just for safety.
            if (_dayNightCycle is null)
            {
                _dayNightCycle = DayNightCycle.main;
                return;
            }
            
            float brightness = _dayNightCycle.GetLocalLightScalar();
            // The game ensures that depth is never negative.
            float depth = Player.main.GetDepth();
            if (brightness < MinLight || depth > 200f)
                return;
            
            // Works out to around 1/s in full daylight at sea level.
            _oxygenManager.AddOxygen(UpdateInterval * brightness * ((200f - depth) / 200f));
        }
    }
}