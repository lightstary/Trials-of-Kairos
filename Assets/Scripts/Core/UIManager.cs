using UnityEngine;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Master screen router. Holds references to all screens and
    /// activates/deactivates them on request.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screens")]
        [SerializeField] private MainMenuScreen     mainMenuScreen;
        [SerializeField] private LevelSelectScreen  levelSelectScreen;
        [SerializeField] private HUDScreen          hudScreen;
        [SerializeField] private BossHUDScreen      bossHUDScreen;
        [SerializeField] private PauseMenuScreen    pauseMenuScreen;
        [SerializeField] private ControlsScreen     controlsScreen;
        [SerializeField] private WinScreen          winScreen;
        [SerializeField] private GameOverScreen     gameOverScreen;
        [SerializeField] private EndgameScreen      endgameScreen;

        private UIScreenBase    _currentScreen;
        private UIScreenType    _currentScreenType    = UIScreenType.None;
        private UIScreenType    _previousScreenType   = UIScreenType.None;
        private UIScreenType    _screenBeforePause;
        private UIScreenType    _screenBeforeControls;

        /// <summary>The screen that was active immediately before the current one.</summary>
        public UIScreenType PreviousScreenType => _previousScreenType;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ShowScreen(UIScreenType.MainMenu);
        }

        /// <summary>Navigates to the given screen, hiding the current one.</summary>
        public void ShowScreen(UIScreenType screenType)
        {
            if (screenType == UIScreenType.Controls)
                _screenBeforeControls = _currentScreenType;

            _previousScreenType = _currentScreenType;

            _currentScreen?.Hide();

            UIScreenBase next = GetScreen(screenType);
            if (next == null)
            {
                Debug.LogWarning($"[UIManager] Screen not found: {screenType}");
                return;
            }

            _currentScreenType = screenType;
            _currentScreen     = next;
            _currentScreen.Show();
        }

        /// <summary>Returns from Controls back to whichever screen opened it.</summary>
        public void CloseControls()
        {
            UIScreenType returnTo = _screenBeforeControls == UIScreenType.None
                ? UIScreenType.MainMenu
                : _screenBeforeControls;
            ShowScreen(returnTo);
        }

        /// <summary>Opens the pause menu while remembering the current gameplay screen.</summary>
        public void OpenPause()
        {
            _screenBeforePause = _currentScreenType;
            ShowScreen(UIScreenType.Pause);
        }

        /// <summary>Closes the pause menu and restores the previous gameplay screen.</summary>
        public void ClosePause()
        {
            ShowScreen(_screenBeforePause);
        }

        /// <summary>Shows a Boss HUD and configures its variant.</summary>
        public void ShowBossHUD(BossVariant variant, string bossName, float bossHealthMax)
        {
            if (bossHUDScreen == null) return;
            bossHUDScreen.Configure(variant, bossName, bossHealthMax);
            ShowScreen(UIScreenType.BossHUD_A); // BossHUDScreen handles variant internally
        }

        private UIScreenBase GetScreen(UIScreenType type)
        {
            return type switch
            {
                UIScreenType.MainMenu  => mainMenuScreen,
                UIScreenType.LevelSelect => levelSelectScreen,
                UIScreenType.HUD       => hudScreen,
                UIScreenType.BossHUD_A => bossHUDScreen,
                UIScreenType.BossHUD_B => bossHUDScreen,
                UIScreenType.BossHUD_C => bossHUDScreen,
                UIScreenType.Pause     => pauseMenuScreen,
                UIScreenType.Controls  => controlsScreen,
                UIScreenType.Win       => winScreen,
                UIScreenType.GameOver  => gameOverScreen,
                UIScreenType.Endgame   => endgameScreen,
                _                      => null
            };
        }
    }
}
