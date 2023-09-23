using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeathrunRemade.Components.RunStatsUI
{
    // Resharper complains about unity serialised fields. None of these will ever get serialised, so make it shut up.
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class RunStatsWindow : MonoBehaviour
    {
        private GameObject _backButton;
        private GameObject _scorePanel;

        public GameObject StatsRow;
        public Sprite MainMenuStandard;
        public Sprite MainMenuHover;
        public Sprite MainMenuPressed;
        public Sprite ScrollBar;
        public Sprite ScrollBarHandle;

        private void Awake()
        {
            // Go ahead and grab all the things that were already set up in the editor.
            _backButton = transform.Find("Content/Footer/ButtonBack").gameObject;
            _scorePanel = transform.Find("Content/Body/HighscorePanel/Viewport/Content").gameObject;
            
            LoadAssets();
            ReplaceBackgrounds();
            SetupComponents();
        }

        /// <summary>
        /// Find and/or load the assets needed for the window to look right.
        /// </summary>
        private void LoadAssets()
        {
            // Get the sprites we know we can grab from existing vanilla components.
            Transform mainMenu = uGUI_MainMenu.main.transform;
            MainMenuStandard = mainMenu.Find("Panel/Options").GetComponent<Image>().sprite;
            var scrollObj = mainMenu.Find("Panel/MainMenu/RightSide/SavedGames/Scroll View/Scrollbar");
            ScrollBar = scrollObj.GetComponent<Image>().sprite;
            ScrollBarHandle = scrollObj.Find("Sliding Area/Handle").GetComponent<Image>().sprite;
            
            // These sprites are harder to get. Load them from their bundles.
            AddressablesUtility.LoadAsync<Sprite>("Assets/Textures/GUI/MainMenu/Elements/MainMenuHoverSprite.png")
                .Completed += handle => MainMenuHover = handle.Result;
            AddressablesUtility.LoadAsync<Sprite>("Assets/Textures/GUI/MainMenu/Elements/MainMenuPressedSprite.png")
                .Completed += handle => MainMenuPressed = handle.Result;
        }

        /// <summary>
        /// Replace all the white placeholder backgrounds with Subnautica's proper menu assets.
        /// </summary>
        private void ReplaceBackgrounds()
        {
            // Replace the backgrounds. The colour of the main window also needs resetting because I set that to a
            // rough approximation of the sprite colour for better working in the editor.
            Image mainImage = GetComponent<Image>();
            mainImage.sprite = MainMenuStandard;
            mainImage.color = Color.white;
            
            var scrollBarObj = transform.Find("Content/Body/HighscorePanel/Scrollbar Vertical");
            scrollBarObj.GetComponent<Image>().sprite = ScrollBar;
            scrollBarObj.Find("Sliding Area/Handle").GetComponent<Image>().sprite = ScrollBarHandle;
        }

        /// <summary>
        /// Add our custom components to all the parts that need it.
        /// </summary>
        private void SetupComponents()
        {
            _backButton.EnsureComponent<BackButton>();
        }

        /// <summary>
        /// Close the window and return to the main menu.
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
            uGUI_MainMenu.main.ClosePanel();
        }

        /// <summary>
        /// Open the window and hide the main menu.
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            uGUI_MainMenu.main.ShowPrimaryOptions(false);
        }
        
        /// <summary>
        /// Insert a new primary option for bringing up the highscore window in the main menu.
        /// </summary>
        public void InsertPrimaryOption()
        {
            Transform optionsContainer = uGUI_MainMenu.main.primaryOptions.transform.Find("PrimaryOptions/MenuButtons");
            // Create a new option by cloning the Play button.
            GameObject highscoreOption = Instantiate(optionsContainer.GetChild(0).gameObject, optionsContainer, false);
            highscoreOption.name = "Highscores";
            var text = highscoreOption.GetComponentInChildren<TextMeshProUGUI>();
            text.text = "Deathrun Highscores";
            // Prevent the button text from being overriden.
            Destroy(text.GetComponent<TranslationLiveUpdate>());
            // Replace the button's onClick event to point to our highscore window instead.
            Button button = highscoreOption.GetComponent<Button>();
            var clickEvent = new Button.ButtonClickedEvent();
            clickEvent.AddListener(Open);
            button.onClick = clickEvent;
            // Put this new option in the right place - just after the options menu button.
            int index = optionsContainer.Find("ButtonOptions").GetSiblingIndex();
            highscoreOption.transform.SetSiblingIndex(index + 1);
        }
        
        public void AddRun()
        {
            Instantiate(StatsRow, _scorePanel.transform, false);
        }
    }
}