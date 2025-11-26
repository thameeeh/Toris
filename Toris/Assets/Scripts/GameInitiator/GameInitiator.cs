using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GameInitiator : MonoBehaviour
{
    public static GameInitiator Instance { get; private set; }
    public enum GameState
    {
        MainMenu,
        Paused,
        InTown,
        InDungeon,
        InUIOverlay
    }

    private GameState currentState;
    private GameState prevState;

    [Header ("Scene Names\n")]
    [SerializeField] private string _mainMenuScene;
    [SerializeField] private string _pausedScene;
    [SerializeField] private string _townScene;
    [SerializeField] private string _dungeonScene;

    [SerializeField] private InputActionReference pauseAction;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            pauseAction.action.performed += OnPausePress;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        pauseAction.action.Enable();
    }

    private void OnDisable()
    {
        pauseAction.action.Disable();
    }

    void Start()
    {
        ChangeState(GameState.MainMenu);
    }

    private void OnPausePress(InputAction.CallbackContext context)
    {
        Debug.Log("Pause Button Pressed");
        if (currentState == GameState.Paused)
        {
            currentState = prevState;
            SceneManager.UnloadSceneAsync(_pausedScene);
            Time.timeScale = 1f;
            Debug.Log("Game Resumed");
            return;
        }
        //ESC only calls pause if in town or dungeon
        if (currentState == GameState.InTown || currentState == GameState.InDungeon)
        {
            ChangeState(GameState.Paused);
            Debug.Log("Game Paused");
        }
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
            case GameState.InDungeon:
                SceneManager.LoadSceneAsync(_dungeonScene);
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
