using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeathrunRemade.Components
{
    /// <summary>
    /// Shows the current safe depth and warns the player when they are about to exceed it.
    /// </summary>
    internal class SafeDepthHud : MonoBehaviour
    {
        private const float HudScale = 0.6f;
        private const float HudPosMult = 1.15f;
        private const float FadeScalar = 0.016f;  // Take around one second to fully fade in/out.
        private readonly Color _damageColor = Color.red;
        private readonly Color _warningColor = Color.yellow;
        private readonly Color _normalColor = Color.white;

        private uGUI_SceneHUD _hud;
        private Image _halfMoon;
        private Image _shadow;
        private Sprite _halfMoonDanger;
        private Sprite _halfMoonNormal;
        private Sprite _shadowDanger;
        private Sprite _shadowNormal;
        private TextMeshProUGUI _depthText;
        private TextMeshProUGUI _suffixText;
        private string _meterSuffix;
        private float _fadeModifier;
        private float _alpha = 1f;

        public bool Visible;
        
        /// <summary>
        /// Create the depth hud object from components belonging to the depth compass.
        /// </summary>
        /// <param name="gameObject">The GameObject that will hold all the components and child objects.</param>
        /// <returns>The SafeDepthHud component.</returns>
        public static SafeDepthHud Create(out GameObject gameObject)
        {
            var hud = uGUI.main.hud;
            var compassObject = hud.transform.Find("Content/DepthCompass/PlayerDepth").gameObject;
            // Create a copy of the object containing depth compass images and such.
            gameObject = Instantiate(compassObject, hud.transform.Find("Content"), true);
            gameObject.name = "SafeDepthHud";
            SafeDepthHud safeDepth = gameObject.AddComponent<SafeDepthHud>();
            gameObject.SetActive(true);
            return safeDepth;
        }

        private void Awake()
        {
            _hud = uGUI.main.hud;
            CopyFromOriginal();
            SetPosition();
            transform.localScale *= HudScale;
            
            Language.OnLanguageChanged += OnLanguageChanged;
            // Trigger once just to set the default correctly.
            OnLanguageChanged();
            
            _halfMoon.enabled = true;
            _shadow.enabled = true;
            _depthText.enabled = true;
            _suffixText.enabled = true;
            SetVisible(true);
        }

        private void Start()
        {
            // Register these events here just to be sure those components had the chance to start up.
            GameEventHandler.OnHudUpdate += OnHudUpdate;
        }

        private void Update()
        {
            Player player = Player.main;
            SaveData save = SaveData.Main;
            if (player is null || save is null)
                return;
            
            float depth = player.GetDepth();
            float safeDepth = save.Nitrogen.safeDepth;
            _depthText.text = IntStringCache.GetStringForInt(Mathf.CeilToInt(safeDepth));

            SafeDepthStatus status = NitrogenHandler.CalculateDepthStatus(depth, safeDepth);
            Color textColor = status switch
            {
                SafeDepthStatus.Approaching => _warningColor,
                SafeDepthStatus.Exceeded => _damageColor,
                _ => _normalColor
            };
            UpdateTextColor(textColor);
            UpdateSprites(status != SafeDepthStatus.Safe);
            UpdateOpacity();
        }

        private void OnDestroy()
        {
            GameEventHandler.OnHudUpdate -= OnHudUpdate;
        }

        /// <summary>
        /// Copy all the useful parts from the original object.
        /// </summary>
        private void CopyFromOriginal()
        {
            uGUI_DepthCompass compass = _hud.GetComponent<uGUI_DepthCompass>();
            // These child objects were copied over during the original instantiation.
            _halfMoon = transform.Find("HalfMoon").GetComponent<Image>();
            _shadow = transform.Find("Shadow").GetComponent<Image>();
            _halfMoonDanger = compass.halfMoonDanger;
            _halfMoonNormal = compass.halfMoonNormal;
            _shadowDanger = compass.shadowDanger;
            _shadowNormal = compass.shadowNormal;

            Transform textObjectHolder = transform.Find("PlayerDepth-Layout");
            _depthText = textObjectHolder.Find("DepthNumberText").GetComponent<TextMeshProUGUI>();
            _suffixText = textObjectHolder.Find("DepthSuffixText").GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Fade in the hud, if it wasn't visible already.
        /// </summary>
        public void FadeIn()
        {
            _fadeModifier = FadeScalar;
        }
        
        /// <summary>
        /// Fade out the hud, if it wasn't hidden already.
        /// </summary>
        public void FadeOut()
        {
            _fadeModifier = -FadeScalar;
        }

        /// <summary>
        /// When the hud updates its vanilla elements, ensure this component is altered to match.
        /// </summary>
        private void OnHudUpdate(uGUI_SceneHUD hud)
        {
            SetVisible(hud._active);
        }

        /// <summary>
        /// Ensure we display the correct text for the language.
        /// </summary>
        private void OnLanguageChanged()
        {
            Language language = Language.main;
            if (language is null)
                return;
            _meterSuffix = language.Get("MeterSuffix");
            _depthText.text = _meterSuffix;
        }

        /// <summary>
        /// Adjust the opacity of the whole component.
        /// </summary>
        /// <param name="alpha">Low values are more see-through. Higher values for more opacity.</param>
        private void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            _halfMoon.color = _halfMoon.color.WithAlpha(alpha);
            _shadow.color = _shadow.color.WithAlpha(alpha);
            _depthText.color = _depthText.color.WithAlpha(alpha);
            _suffixText.color = _suffixText.color.WithAlpha(alpha);
        }

        /// <summary>
        /// Move the hud to the correct position based on the position of the actual depth compass.
        /// </summary>
        private void SetPosition()
        {
            float originalWidth = _hud.transform.Find("Content/DepthCompass").GetComponent<RectTransform>().rect.width;
            var localPos = transform.localPosition;
            localPos.x = originalWidth * HudPosMult;
            transform.localPosition = localPos;
        }

        /// <summary>
        /// Change whether this hud is being shown.
        /// </summary>
        public void SetVisible(bool visible)
        {
            // Set the visual components active/inactive rather than this entire gameobject so that the math still
            // continues running when e.g. the PDA is open.
            _halfMoon.gameObject.SetActive(visible);
            _shadow.gameObject.SetActive(visible);
            _depthText.gameObject.SetActive(visible);
            _suffixText.gameObject.SetActive(visible);
            Visible = visible;
        }

        private void UpdateTextColor(Color color)
        {
            _depthText.color = color.WithAlpha(_alpha);
            _suffixText.color = color.WithAlpha(_alpha);
        }

        /// <summary>
        /// Swap the sprites between the normal blue-green variant and the scary red one. Red means danger.
        /// </summary>
        private void UpdateSprites(bool danger)
        {
            if (danger)
            {
                _halfMoon.sprite = _halfMoonDanger;
                _shadow.sprite = _shadowDanger;
            }
            else
            {
                _halfMoon.sprite = _halfMoonNormal;
                _shadow.sprite = _shadowNormal;
            }
        }

        /// <summary>
        /// Alter the hud's opacity one step in the desired direction.
        /// </summary>
        private void UpdateOpacity()
        {
            // No changes necessary.
            if (_fadeModifier == 0)
                return;

            _alpha = Mathf.Clamp01(_alpha + _fadeModifier);
            SetAlpha(_alpha);
            // Stop making changes on future frames if we already reached one of the extremes.
            if (_alpha >= 1 || _alpha <= 0)
                _fadeModifier = 0f;
        }
    }
}