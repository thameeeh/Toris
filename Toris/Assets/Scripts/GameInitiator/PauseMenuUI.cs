using UnityEngine;
using UnityEngine.UIElements;

public class PauseMenuUI : MonoBehaviour
{
    private UIDocument _uiDocumen;
    private Button _resumeButton;
    private Button _quitButton;

    private const string RESUME_BTN_NAME = "btn__pause-menu--resume";
    private const string QUIT_BTN_NAME = "btn__pause-menu--quit";

    private void OnEnable()
    {
        _uiDocumen = GetComponent<UIDocument>();
    }

    private void Start()
    {
        VisualElement root = _uiDocumen.rootVisualElement;
        
        _resumeButton = root.Q<Button>(RESUME_BTN_NAME);
        _quitButton = root.Q<Button>(QUIT_BTN_NAME);

        if (_resumeButton != null)
            _resumeButton.clicked += ResumeGame;
        else
            Debug.LogError($"Could not find button: {RESUME_BTN_NAME}");

        if (_quitButton != null)
            _quitButton.clicked += QuitToMainMenu;
        else
            Debug.LogError($"Could not find button: {QUIT_BTN_NAME}");
    }

    private void OnDisable()
    {
        if (_resumeButton != null)
            _resumeButton.clicked -= ResumeGame;
        if (_quitButton != null)
            _quitButton.clicked -= QuitToMainMenu;
    }

    private void ResumeGame()
    {
        GameInitiator.Instance.BackToPrevScene();
    }
    private void QuitToMainMenu()
    {
        GameInitiator.Instance.ChangeState(GameInitiator.GameState.MainMenu);
    }
}
