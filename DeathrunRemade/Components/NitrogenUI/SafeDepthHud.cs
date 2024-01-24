using System.Collections;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using HootLib;
using UnityEngine;
using UnityEngine.UI;

namespace DeathrunRemade.Components.NitrogenUI
{
    /// <summary>
    /// Shows the current safe depth and warns the player when they are about to exceed it.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup), typeof(HorizontalLayoutGroup))]
    internal class SafeDepthHud : MonoBehaviour
    {
        private const float HudScale = 0.6f;
        private const float HudPosMult = 1.15f;
        private const float FadeScalar = 0.016f;  // Take around one second to fully fade in/out.
        
        private float _fadeModifier;

        private CanvasGroup _canvasGroup;
        private FastAscent _fastAscent;
        private MovingBarTween _accelTween;
        
        /// <summary>
        /// Create the depth hud object from components belonging to the depth compass.
        /// </summary>
        /// <returns>The GameObject that will hold all the components and child objects.</returns>
        public static GameObject Create()
        {
            GameObject gameObject = new GameObject("SafeDepthHud", typeof(SafeDepthHud));
            gameObject.transform.SetParent(uGUI.main.hud.transform.Find("Content"), false);
            gameObject.SetActive(true);
            
            return gameObject;
        }

        private void Awake()
        {
            _canvasGroup ??= GetComponent<CanvasGroup>();
            _fastAscent ??= Player.main.GetComponent<FastAscent>();
            SetupLayout();
            SetPosition();
            transform.localScale *= HudScale;
            
            GameEventHandler.OnHudUpdate += OnHudUpdate;
            NitrogenHandler.OnDepthHudFadeIn += FadeIn;
            NitrogenHandler.OnDepthHudFadeOut += FadeOut;

            StartCoroutine(CreateChildren());
        }

        /// <summary>
        /// Initialise the layout of all hud components.
        /// </summary>
        private void SetupLayout()
        {
            var layout = GetComponent<HorizontalLayoutGroup>();
            // We don't want controlled size, just the neat horizontal alignment.
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
        }
        
        private IEnumerator CreateChildren()
        {
            // Create the depth meter from existing vanilla components.
            BendsDepthCompass.Create(transform);
            
            // Load the accelerometer from a bundle made in unity editor.
            var fileRequest = AssetBundle.LoadFromFileAsync(Hootils.GetAssetHandle("safedepthhud"));
            yield return fileRequest;
            AssetBundle bundle = fileRequest.assetBundle;
            
            var objRequest = bundle.LoadAssetAsync<GameObject>("Accelerometer");
            yield return objRequest;
            GameObject accelPrefab = (GameObject)objRequest.asset;
            GameObject accelInstance = Instantiate(accelPrefab, transform, false);
            _accelTween = accelInstance.GetComponentInChildren<MovingBarTween>();
            FastAscent.OnAscentRateChanged += rate => _accelTween.SetTarget(0f, (rate - FastAscent.ResetThreshold) / FastAscent.DamageThreshold);
            
            // We're done with the bundle, unload it to free some memory. Sync because Subnautica's unity version
            // does not support UnloadAsync.
            bundle.Unload(false);
            
            // Enable all children.
            SetVisible(true);
        }

        private void Update()
        {
            Player player = Player.main;
            SaveData save = SaveData.Main;
            if (player == null || save == null)
                return;
            
            // TODO
            // UpdateOpacity();
        }

        private void OnDestroy()
        {
            GameEventHandler.OnHudUpdate -= OnHudUpdate;
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
        /// Move the hud to the correct position based on the position of the actual depth compass.
        /// </summary>
        private void SetPosition()
        {
            float originalWidth = uGUI.main.hud.transform.Find("Content/DepthCompass").GetComponent<RectTransform>().rect.width;
            var localPos = transform.localPosition;
            localPos.x = originalWidth * HudPosMult;
            transform.localPosition = localPos;

            // TODO
            transform.localPosition = new Vector3(200f, 450f, 0f);
        }

        /// <summary>
        /// Change whether this hud's elements are being shown.
        /// </summary>
        public void SetVisible(bool visible)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(visible);
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

            float alpha = Mathf.Clamp01(_canvasGroup.alpha + _fadeModifier);
            _canvasGroup.alpha = alpha;
            // Stop making changes on future frames if we already reached one of the extremes.
            if (alpha >= 1 || alpha <= 0)
                _fadeModifier = 0f;
        }
    }
}