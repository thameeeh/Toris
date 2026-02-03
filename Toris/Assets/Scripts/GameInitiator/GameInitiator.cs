using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class GameInitiator : MonoBehaviour
{
    public static GameInitiator Instance { get; private set; }
    public enum GameState
    {
        MainMenu,
        Paused,
        InTown,
        InOverworld,
        InUIOverlay
    }

    private GameState currentState;
    private GameState prevState;

    [Header ("Scene Names\n")]
    [SerializeField] private string _mainMenuScene;
    [SerializeField] private string _pausedScene;
    [SerializeField] private string _townScene;
    [SerializeField] private string _overworldScene;

    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private EventSystem _eventSystem;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(Instance.gameObject);
            //DontDestroyOnLoad(_eventSystem);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        pauseAction.action.started += OnPausePress;
        pauseAction.action.Enable();
    }

    private void OnDisable()
    {
        pauseAction.action.Disable();
        pauseAction.action.started -= OnPausePress;
    }

    void Start()
    {
        //ChangeState(GameState.MainMenu);
    }

    public GameState GetState() 
    {
        return currentState;
    }

    private void OnPausePress(InputAction.CallbackContext context)
    {
        if (currentState == GameState.Paused)
        {
            BackToPrevScene(); // single unpause path
            return;
        }

        if (currentState == GameState.InTown || currentState == GameState.InOverworld)
            ChangeState(GameState.Paused);
    }


    public void ChangeState(GameState newState)
    {
        prevState = currentState;
        currentState = newState;

        switch (currentState)
        {
            case GameState.MainMenu:
                SceneManager.LoadScene(_mainMenuScene);
                Time.timeScale = 0f;
                break;
            case GameState.Paused:
                SceneManager.LoadSceneAsync(_pausedScene, LoadSceneMode.Additive);
                Time.timeScale = 0f;
                break;
            case GameState.InTown:
                SceneManager.LoadSceneAsync(_townScene);
                Time.timeScale = 1f;
                break;
            case GameState.InOverworld:
                SceneManager.LoadSceneAsync(_overworldScene);
                Time.timeScale = 1f;
                break;
            case GameState.InUIOverlay:
                // Handle UI Overlay state
                break;
        }

    }
    public void BackToPrevScene() 
    {
        if (currentState == GameState.Paused)
        {
            currentState = prevState;
            SceneManager.UnloadSceneAsync(_pausedScene);
            Time.timeScale = 1f;
        }
    }
}
