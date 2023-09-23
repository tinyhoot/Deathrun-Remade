using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeathrunRemade.Components.RunStatsUI
{
    internal class BackButton : EventTrigger
    {
        private RunStatsWindow _mainWindow;
        private Image _background;
        private uGUI_BasicColorSwap _colorSwap;

        private void Awake()
        {
            // Do the basic setup to make the button look right as a baseline.
            _mainWindow = GetComponentInParent<RunStatsWindow>();
            _colorSwap = GetComponent<uGUI_BasicColorSwap>();
            _background = GetComponent<Image>();
            _background.sprite = _mainWindow.MainMenuStandard;
        }

        /// <summary>
        /// Ensure the button does not start yellow when we repeatedly open and close the window.
        /// </summary>
        private void OnDisable()
        {
            _background.sprite = _mainWindow.MainMenuStandard;
            _colorSwap.makeTextWhite();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            _background.sprite = _mainWindow.MainMenuPressed;
            _mainWindow.Close();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            _background.sprite = _mainWindow.MainMenuPressed;
            _mainWindow.Close();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            _background.sprite = _mainWindow.MainMenuHover;
            _colorSwap.makeTextBlack();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            _background.sprite = _mainWindow.MainMenuStandard;
            _colorSwap.makeTextWhite();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            _background.sprite = _mainWindow.MainMenuHover;
            _colorSwap.makeTextBlack();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            _background.sprite = _mainWindow.MainMenuStandard;
            _colorSwap.makeTextWhite();
        }
    }
}