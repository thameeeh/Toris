using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    UIDocument _uiDocument;
    Button _startBtn;
    Button _settingsBtn;
    Button _quitBtn;

    private const string START_BTN_NAME = "btn__main-menu--start";
    private const string SETTINGS_BTN_NAME = "btn__main-menu--settings";
    private const string QUIT_BTN_NAME = "btn__main-menu--quit";

    private void OnEnable()
    {
        if (_uiDocument == null)
            _uiDocument = GetComponent<UIDocument>();
    
        VisualElement root = _uiDocument.rootVisualElement;

        _startBtn = root.Query<Button>(START_BTN_NAME);
        _settingsBtn = root.Query<Button>(SETTINGS_BTN_NAME);
        _quitBtn = root.Query<Button>(QUIT_BTN_NAME);

        _startBtn.clicked += StartGame; // Start Game from MainMenu Scene
        //_settingsBtn.clicked += Setting; // Enter Setting From MainMenu Scene
        _quitBtn.clicked += QuitGame; // Quit Game
    }

    private void OnDisable()
    {
        _startBtn.clicked -= StartGame;
        //_settingsBtn.clicked -= Setting;
        _quitBtn.clicked -= QuitGame;
    }

    //MainMenu Scene
    //This function is called when the "Start Game" button is pressed
    public void StartGame()
    {
        GameInitiator.Instance.ChangeState(GameInitiator.GameState.InTown);
    }

    #region Pause Menu
    //This function is called when the "Resume Game" button is pressed
    public void ResumeGame()
    {
        GameInitiator.Instance.BackToPrevScene();
    }
    #endregion
    //This function is called when the "Quit Game" button is pressed
    public void QuitGame()
    {
        GameInitiator.Instance.ChangeState(GameInitiator.GameState.MainMenu);
    }
}
