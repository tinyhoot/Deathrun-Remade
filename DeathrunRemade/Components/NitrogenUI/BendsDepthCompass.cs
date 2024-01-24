using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeathrunRemade.Components.NitrogenUI
{
    public class BendsDepthCompass : MonoBehaviour
    {
        private readonly Color _damageColor = Color.red;
        private readonly Color _warningColor = Color.yellow;
        private readonly Color _normalColor = Color.white;
        
        private Image _halfMoon;
        private Image _shadow;
        private Sprite _halfMoonDanger;
        private Sprite _halfMoonNormal;
        private Sprite _shadowDanger;
        private Sprite _shadowNormal;
        private TextMeshProUGUI _depthText;
        private TextMeshProUGUI _suffixText;
        private string _meterSuffix;
        
        /// <summary>
        /// Create the bends depth compass from components belonging to the vanilla depth compass.
        /// Use this function to prevent any of the components from Awake()-ing before our modifications are done. 
        /// </summary>
        public static BendsDepthCompass Create(Transform parent)
        {
            var hud = uGUI.main.hud;
            var compassObject = hud.transform.Find("Content/DepthCompass/PlayerDepth").gameObject;
            // Create a copy of the object containing depth compass images and such.
            GameObject gameObject = Instantiate(compassObject, parent, false);
            gameObject.name = "DepthCompass";
            // The vanilla compass has a size of 0. Approximate an appropriate size.
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(156f, 120f);
            BendsDepthCompass compass = gameObject.AddComponent<BendsDepthCompass>();
            gameObject.SetActive(true);
            
            return compass;
        }

        private void Awake()
        {
            CopyFromOriginal();
            
            Language.OnLanguageChanged += OnLanguageChanged;
            // Trigger once just to set the default correctly.
            OnLanguageChanged();
            
            _halfMoon.enabled = true;
            _shadow.enabled = true;
            _depthText.enabled = true;
            _suffixText.enabled = true;
        }

        private void Update()
        {
            Player player = Player.main;
            SaveData save = SaveData.Main;
            if (player == null || save == null)
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
        }

        private void OnDestroy()
        {
            Language.OnLanguageChanged -= OnLanguageChanged;
        }

        /// <summary>
        /// Copy all the useful parts from the original object.
        /// </summary>
        private void CopyFromOriginal()
        {
            uGUI_DepthCompass compass = uGUI.main.hud.GetComponent<uGUI_DepthCompass>();
            // These child objects were copied over during the original instantiation.
            _halfMoon = transform.Find("HalfMoon").GetComponent<Image>();
            _shadow = transform.Find("Shadow").GetComponent<Image>();
            _halfMoonDanger = compass.halfMoonDanger;
            _halfMoonNormal = compass.halfMoonNormal;
            _shadowDanger = compass.shadowDanger;
            _shadowNormal = compass.shadowNormal;
            
            _depthText = transform.Find("PlayerDepth-Layout/DepthNumberText").GetComponent<TextMeshProUGUI>();
            _suffixText = transform.Find("PlayerDepth-Layout/DepthSuffixText").GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Ensure we display the correct text for the language.
        /// </summary>
        private void OnLanguageChanged()
        {
            Language language = Language.main;
            if (language == null)
                return;
            _meterSuffix = language.Get("MeterSuffix");
            _depthText.text = _meterSuffix;
        }

        private void UpdateTextColor(Color color)
        {
            _depthText.color = color;
            _suffixText.color = color;
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
    }
}