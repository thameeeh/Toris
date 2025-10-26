using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GameInitiator : MonoBehaviour
{
    public static GameInitiator Instance { get; private set; }
    public enum GameState
    {
        MainMenu,
        GamePlay,
        Paused
    }

    private GameState currentState;

    private string _mainMenuScene = "MainMenu";
    private string _gamePlayScene = "MainArea";
    private string _pausedScene = "Paused";

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case GameState.MainMenu:
                SceneManager.LoadScene(_mainMenuScene);
                break;
            case GameState.GamePlay:
                SceneManager.LoadScene(_gamePlayScene);
                break;
            case GameState.Paused:
                SceneManager.LoadScene(_pausedScene);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
