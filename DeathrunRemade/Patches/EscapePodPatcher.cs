using System.Linq;
using DeathrunRemade.Configuration;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using HarmonyLib;
using HootLib;
using Nautilus.Utility;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class EscapePodPatcher
    {
        private static float _avgDistance;
        private static float _lastFrozenTime; // The last point in time the lifepod's rigidbody was frozen.
        private static Vector3 _previousPos; // The lifepod's position on the previous frame

        /// <summary>
        /// Re-apply physics changes to the pod when a saved game is loaded.
        /// </summary>
        public static void OnSavedGameLoaded()
        {
            // If the pod is still sinking for some reason, do nothing.
            if (SaveData.Main.Config.SinkLifepod || !SaveData.Main.EscapePod.isAnchored)
                return;
            EscapePod pod = EscapePod.main;
            AnchorLifepod(pod, pod.GetComponent<WorldForces>());
        }

        /// <summary>
        /// Anchor the lifepod at the bottom of the sea and reset any modified physics values.
        /// </summary>
        private static void AnchorLifepod(EscapePod pod, WorldForces wf)
        {
            // Reset gravity and movement precision.
            wf.aboveWaterGravity = 9.81f;
            wf.underwaterGravity = -10f;
            wf.lockInterpolation = false;
            pod.rigidbodyComponent.interpolation = RigidbodyInterpolation.Interpolate;
            // Freeze the pod in place.
            pod.rigidbodyComponent.constraints |= RigidbodyConstraints.FreezePositionY;
            pod.GetComponent<Stabilizer>().enabled = false;
            SaveData.Main.EscapePod.isAnchored = true;
        }
        
        /// <summary>
        /// Override the spawn location of the lifepod at the start of the game.
        /// </summary>
        /// <param name="__result">The spawnpoint chosen by the game.</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RandomStart), nameof(RandomStart.GetRandomStartPoint))]
        private static void OverrideStart(ref Vector3 __result)
        {
            Vector3 start = GetStartPoint(SaveData.Main.Config, out string name);
            SaveData.Main.Stats.startPoint = name;
            // Show the intro sequence.
            NotificationHandler.Main.AddMessage(NotificationHandler.Centre, $"DEATHRUN\nStart: {name}")
                .SetDuration(10f, 2f);
            
            // If the setting was on Vanilla, do not override anything.
            if (start == default)
                return;
            
            DeathrunInit._Log.Debug($"Replacing spawn point with {start}");
            __result = start;
        }

        /// <summary>
        /// Sink the lifepod if necessary.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EscapePod), nameof(EscapePod.FixedUpdate))]
        private static bool OverrideLifepodPhysics(EscapePod __instance)
        {
            if (!SaveData.Main.Config.SinkLifepod)
                return true;
            
            // If the pod is already anchored at the bottom there is nothing left to do.
            if (SaveData.Main.EscapePod.isAnchored)
                return true;

            // Do not sink the pod while the game is loading or during the intro cinematic.
            if (WaitScreen.IsWaiting || uGUI.main.intro.showing || __instance.IsPlayingIntroCinematic())
                return true;

            WorldForces wf = __instance.GetComponent<WorldForces>();
            SinkLifepod(__instance, wf);
            
            // If the pod is currently frozen (e.g. by the player moving too far away), account for it.
            if (__instance.rigidbodyComponent.isKinematic)
                _lastFrozenTime = Time.time;

            // Average the distance moved over the past second.
            float distance = Vector3.Distance(__instance.transform.position, _previousPos);
            _avgDistance = Hootils.AverageOverTime(_avgDistance, distance, Time.fixedDeltaTime, 0.5f);
            // If the pod stopped moving, assume it hit the ground and anchor it.
            if (Time.time - _lastFrozenTime > 2f && _avgDistance < 0.2f && Ocean.GetDepthOf(__instance.gameObject) > 10f)
            {
                if (SaveData.Main.Config.ToppleLifepod)
                {
                    // Move the pod back up a bit to give it some space to tilt.
                    __instance.transform.Translate(0f, 2f, 0f);
                    TiltLifepod(__instance);
                }
                AnchorLifepod(__instance, wf);
                // Only shake the camera if the player is inside the pod.
                if (Player.main.currentEscapePod != null)
                    MainCameraControl.main.ShakeCamera(3f, 1f, MainCameraControl.ShakeMode.Cos);
                // Play an impact sound centred on the pod to really sell it.
                var asset = AudioUtils.GetFmodAsset("event:/sub/cyclops/impact_solid_hard");
                FMODUWE.PlayOneShot(asset, __instance.transform.position);

                DeathrunInit._Log.InGameMessage("The lifepod has hit bottom!");
            }

            _previousPos = __instance.transform.position;
            return false;
        }
        
        /// <summary>
        /// Get the spawn point of the escape pod.
        /// </summary>
        /// <returns></returns>
        public static Vector3 GetStartPoint(ConfigSave config, out string name)
        {
            string setting = config.StartLocation;
            // If the save data has not yet initialised, fall back to the actual config.
            setting ??= DeathrunInit._Config.StartLocation.Value;

            // This will throw an exception if the setting name has been altered for some reason, but that's intended.
            StartLocation location = DeathrunInit._Config._startLocations.First(l => l.Name == setting);
            if (setting == "Random")
                location = DeathrunInit._Config._startLocations.Where(l => l.Name != "Random").ToList().GetRandom();

            name = location.Name;
            if (location.Name == "Vanilla")
                return default;
            return new Vector3(location.X, location.Y, location.Z);
        }

        /// <summary>
        /// Change physics values to make the lifepod sink gracefully.
        /// </summary>
        private static void SinkLifepod(EscapePod pod, WorldForces wf)
        {
            wf.aboveWaterGravity = 50f;
            wf.underwaterGravity = 30f;
            // Temporarily disable interpolation to enable precision movement and prevent jitter while sinking.
            pod.rigidbodyComponent.interpolation = RigidbodyInterpolation.None;
            wf.lockInterpolation = true;
        }

        /// <summary>
        /// Give the lifepod a push to make it roll over.
        /// </summary>
        private static void TiltLifepod(EscapePod pod)
        {
            float angle = Random.Range(0f, 1f) * 360f;
            float tiltX = Random.Range(0f, 1f);
            float tiltZ = Random.Range(0f, 1f);
            Quaternion quaternion = Quaternion.AngleAxis(angle, new Vector3(tiltX, 0f, tiltZ));
            // Experimentally, multiplying by this force ensures that the pod ends up roughly at the angle defined
            // by the above random values, with 1 meaning the pod is upside down.
            const float force = 400000f;
            DeathrunInit._Log.Debug($"Pushing escape pod - x: {tiltX}, z:{tiltZ}, Scaled: {quaternion.eulerAngles * force}");
            pod.rigidbodyComponent.AddTorque(quaternion.eulerAngles * force, ForceMode.Impulse);
        }
    }
}