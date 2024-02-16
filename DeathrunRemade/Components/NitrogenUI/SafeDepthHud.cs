using System.Collections;
using DeathrunRemade.Handlers;
using HootLib;
using Nautilus.Options;
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
            SetPositionFromConfig();
            transform.localScale *= HudScale;
            
            GameEventHandler.OnHudUpdate += OnHudUpdate;
            GameEventHandler.OnPdaStateChanged += OnPdaStateChanged;
            DeathrunInit._Config.ModOptions.OnChanged += OnSettingChanged;

            StartCoroutine(CreateChildren());
        }

        private void Start()
        {
            // This one isn't ready yet during Awake().
            Player.main.GetComponent<FastAscent>().OnAscentRateChanged += OnAscentRateChanged;
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
            
            // We're done with the bundle, unload it to free some memory. Sync because Subnautica's unity version
            // does not support UnloadAsync.
            bundle.Unload(false);
            
            // Enable all children.
            SetVisible(true);
        }

        private void OnDestroy()
        {
            GameEventHandler.OnHudUpdate -= OnHudUpdate;
            GameEventHandler.OnPdaStateChanged -= OnPdaStateChanged;
            DeathrunInit._Config.ModOptions.OnChanged -= OnSettingChanged;
        }

        /// <summary>
        /// Ensure the accelerometer receives data to act on.
        /// </summary>
        private void OnAscentRateChanged(float rate)
        {
            _accelTween.SetTarget(0f, (rate - FastAscent.ResetThreshold) / FastAscent.DamageThreshold);
        }
        
        /// <summary>
        /// When the hud updates its vanilla elements, ensure this component is altered to match.
        /// </summary>
        private void OnHudUpdate(uGUI_SceneHUD hud)
        {
            SetVisible(hud._active);
        }
        
        /// <summary>
        /// When the PDA is opened make sure the hud disappears just like the vanilla depth compass.
        /// </summary>
        private void OnPdaStateChanged(bool open)
        {
            // Not visible when the pda is open.
            SetVisible(!open);
        }

        /// <summary>
        /// Reposition the UI when the user changes the corresponding setting in the mod menu.
        /// </summary>
        private void OnSettingChanged(object _, OptionEventArgs args)
        {
            if (args.Id == DeathrunInit._Config.NitrogenUiPosX.GetId() || args.Id == DeathrunInit._Config.NitrogenUiPosY.GetId())
                SetPositionFromConfig();
        }
        
        private void SetPositionFromConfig()
        {
            DeathrunUtils.SetRelativeScreenPosition(transform, DeathrunInit._Config.NitrogenUiPosX.Value,
                DeathrunInit._Config.NitrogenUiPosY.Value);
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
    }
}