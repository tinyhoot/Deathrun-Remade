using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeathrunRemade.Monos
{
    internal class NitrogenBar : MonoBehaviour
    {
        public static NitrogenBar Main;
        public float NitrogenCapacity { get; private set; }
        public float NitrogenLevel { get; private set; }
        private Animation _animation;
        private GameObject _background;
        private uGUI_CircularBar _bar;
        private uGUI_SceneHUD _hud;
        private RectTransform _icon;
        private TextMeshProUGUI _text;

        private readonly Color _barColor = Color.green;
        private const float _BarCompletionTime = 0.5f;  // How long it should take the bar to reach the target value.
        private float _barSpeed;
        private float _currentBarPos;
        
        private float _currentIconRotation;
        private float _iconRotationSpringCoef;
        private float _iconRotationVelDamp;
        private float _iconRotationVelocity;
        private float _lastFixedUpdateTime;

        /// <summary>
        /// Create the nitrogen object from components belonging to other hud bars.
        /// </summary>
        /// <returns>The new nitrogen object.</returns>
        public static GameObject Create()
        {
            var hud = uGUI.main.hud;
            // Create a copy of the health bar to use as a baseline.
            var nitro = Instantiate<GameObject>(hud.barHealth, hud.transform.Find("Content/BarsPanel"), false);
            nitro.name = "NitrogenBar";
            Main = nitro.AddComponent<NitrogenBar>();
            nitro.SetActive(true);
            return nitro;
        }

        private void Awake()
        {
            Main = this;
            NitrogenLevel = 1f;  // Set to 1 because 0 will cause the text to not appear for some reason.
            NitrogenCapacity = 100f;
            _hud = uGUI.main.hud;
            
            var healthBar = gameObject.GetComponent<uGUI_HealthBar>();
            CopyRelevantParts(healthBar);
            DestroyImmediate(healthBar);
            ReplaceNitrogenIcon(_icon);
            SetPosition(transform);
            // Do this after moving this whole object so we don't get weird offset errors.
            _background = Instantiate(_hud.backgroundBarsDouble, transform, true);
            _background.transform.RotateAround(_hud.barOxygen.transform.position, Vector3.right, -45);
            _background.transform.SetAsFirstSibling();
            _background.SetActive(true);
            // Prevent the backgrounds from being rendered on top of all the vanilla bars.
            transform.SetAsFirstSibling();
            
            _lastFixedUpdateTime = PDA.time;
        }

        private void LateUpdate()
        {
            PDA pda = Player.main.GetPDA();
            bool showNumbers = (pda != null && pda.isInUse);
            
            // Flip the icon if we just switched to/from PDA mode.
            FlipIcon(showNumbers);
            // Move the bar closer to the current nitrogen level if isn't there already.
            UpdateBarPosition(NitrogenLevel, NitrogenCapacity);
        }

        /// <summary>
        /// Copy all parts from other hud bars that are useful for the nitrogen bar.
        /// </summary>
        private void CopyRelevantParts(uGUI_HealthBar healthBar)
        {
            _animation = healthBar.animation;
            _animation.wrapMode = WrapMode.Loop;
            //_animation.Stop();
            
            _bar = healthBar.bar;
            _bar.color = _barColor;
            _bar.UpdateMaterialBorderColor();

            _iconRotationSpringCoef = healthBar.rotationSpringCoef;
            _iconRotationVelDamp = healthBar.rotationVelocityDamp;
            
            _icon = healthBar.icon;
            _text = healthBar.text;
        }
        
        /// <summary>
        /// Rotate the icon/text in the middle of the bar.
        /// </summary>
        private void FlipIcon(bool showNumbers)
        {
            if (MathExtensions.CoinRotation(ref _currentIconRotation, showNumbers ? 180f : 0f, 
                    ref _lastFixedUpdateTime, PDA.time, 
                    ref _iconRotationVelocity, _iconRotationSpringCoef, _iconRotationVelDamp, -1f))
            {
                _icon.localRotation = Quaternion.Euler(0f, _currentIconRotation, 0f);
            }
        }

        /// <summary>
        /// Replace the front (heart) part of the nitrogen hud icon with "N".
        /// </summary>
        private void ReplaceNitrogenIcon(Transform icon)
        {
            var iconFront = icon.Find("Icon");
            Destroy(iconFront.GetComponent<Image>());
            var textMesh = iconFront.gameObject.AddComponent<TextMeshProUGUI>();
            textMesh.text = "N";
            textMesh.alignment = TextAlignmentOptions.Center;
            iconFront.localScale = Vector3.one * 0.65f;
        }
        
        /// <summary>
        /// Set the current nitrogen level.
        /// </summary>
        public void SetNitrogen(float level)
        {
            NitrogenLevel = level;
        }

        /// <summary>
        /// Set the position of this component on screen based on the position of the other bar hud elements.
        /// </summary>
        private void SetPosition(Transform nitroTransform)
        {
            // Find the health and food bars.
            var healthBar = _hud.barHealth;
            var foodBar = _hud.barFood;
            Vector3 localPos = nitroTransform.localPosition;
            var foodPos = foodBar.transform.localPosition;
            // Set the position to mirror the food bar as if the health bar was the mirroring axis.
            localPos.x = foodPos.x;
            var heightDelta = healthBar.transform.localPosition.y - foodPos.y;
            localPos.y = foodPos.y + heightDelta * 2;
            nitroTransform.localPosition = localPos;
        }

        /// <summary>
        /// Makes the bar move to a different value over several frames.
        /// </summary>
        private void UpdateBarPosition(float value, float capacity)
        {
            float percentage = Mathf.Clamp01(value / capacity);
            // Don't do any of this math if the bar is already where it should be.
            if (Mathf.Approximately(_currentBarPos, percentage))
                return;
            
            _currentBarPos = Mathf.SmoothDamp(_currentBarPos, percentage, ref _barSpeed, _BarCompletionTime,
                float.PositiveInfinity, PDA.deltaTime);
            // Ensure the bar does not get caught in tiny rounding errors and finishes within reasonable time.
            if (Mathf.Abs(_currentBarPos - percentage) < 0.002)
                _currentBarPos = percentage;
            _bar.value = _currentBarPos;
            _text.text = IntStringCache.GetStringForInt(Mathf.CeilToInt(_currentBarPos * capacity));
        }
    }
}