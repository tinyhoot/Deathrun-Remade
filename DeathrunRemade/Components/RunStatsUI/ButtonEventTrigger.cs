using HootLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeathrunRemade.Components.RunStatsUI
{
    internal class ButtonEventTrigger : EventTrigger
    {
        public Image background;
        public uGUI_BasicColorSwap colorSwap;
        public Sprite mainMenuStandard;
        public Sprite mainMenuHover;
        public Sprite mainMenuPressed;

        private void Awake()
        {
            background ??= GetComponent<Image>();
            colorSwap ??= GetComponent<uGUI_BasicColorSwap>();
            
            // Request the necessary vanilla game assets.
            AddressablesUtility.LoadAsync<Sprite>(AssetFilePaths.MainMenuStandardSprite)
                .Completed += handle =>
            {
                mainMenuStandard = handle.Result;
                background.sprite = mainMenuStandard;
            };
            AddressablesUtility.LoadAsync<Sprite>(AssetFilePaths.MainMenuHoverSprite)
                .Completed += handle => mainMenuHover = handle.Result;
            AddressablesUtility.LoadAsync<Sprite>(AssetFilePaths.MainMenuPressedSprite)
                .Completed += handle => mainMenuPressed = handle.Result;
        }

        /// <summary>
        /// Ensure the button does not start yellow when we repeatedly open and close the window.
        /// </summary>
        private void OnDisable()
        {
            background.sprite = mainMenuStandard;
            colorSwap.makeTextWhite();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            background.sprite = mainMenuHover;
            colorSwap.makeTextBlack();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            background.sprite = mainMenuStandard;
            colorSwap.makeTextWhite();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            background.sprite = mainMenuHover;
            colorSwap.makeTextBlack();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            background.sprite = mainMenuStandard;
            colorSwap.makeTextWhite();
        }
    }
}