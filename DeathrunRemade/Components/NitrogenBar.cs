using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using HootLib.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeathrunRemade.Components
{
    internal class NitrogenBar : HootHudBar
    {
        public static NitrogenBar Main;

        protected void Start()
        {
            Main = this;
            ReplaceNitrogenIcon(_icon);
            AddBackground(_hud.backgroundBarsDouble, _angle);
            PreparePulseCurves();
            GameEventHandler.OnHudUpdate += HarmonyUpdateHudElements;
        }

        protected override void LateUpdate()
        {
            // If the save isn't ready yet, substitute a dummy value.
            // This prevents the bar from just straight up breaking. I don't know why, but it cannot handle
            // being initialised with zero.
            if (!SaveData.Main.Ready)
                Value = 1;
            else
                Value = SaveData.Main.Nitrogen.nitrogen;
            base.LateUpdate();
        }

        private void OnDestroy()
        {
            GameEventHandler.OnHudUpdate -= HarmonyUpdateHudElements;
        }

        /// <summary>
        /// Change how the danger pulse around the hud bar behaves.
        /// </summary>
        private void PreparePulseCurves()
        {
            var activationCurve = new AnimationCurve(new[]
            {
                new Keyframe(0.69f, -1f), // Do not activate at this percentage and below.
                new Keyframe(0.70f, 0.5f),
                new Keyframe(0.99f, 0.5f),
                new Keyframe(1f, -1f), // Turn it off once 100% is reached.
            });
            var speedCurve = new AnimationCurve(new []
            {
                new Keyframe(0.70f, 1f),
                new Keyframe(1f, 1.5f),
            });
            _dangerPulse.SetAnimationCurves(activationCurve, speedCurve);
        }

        /// <summary>
        /// Replace the front (heart) part of the nitrogen hud icon with "N".
        /// </summary>
        private void ReplaceNitrogenIcon(Transform icon)
        {
            var iconFront = icon.Find("Icon");
            // Needs to be immediate or we can't add the TextMeshPro component.
            DestroyImmediate(iconFront.GetComponent<Image>());
            var textMesh = iconFront.gameObject.AddComponent<TextMeshProUGUI>();
            textMesh.text = "N";
            textMesh.alignment = TextAlignmentOptions.Center;
            iconFront.localScale = Vector3.one * 0.65f;
        }
    }
}