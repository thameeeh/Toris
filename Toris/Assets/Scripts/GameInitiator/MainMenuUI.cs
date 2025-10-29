using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    //MainMenu Scene
    //This function is called when the "Start Game" button is pressed
    public void StartGame()
    {
        GameInitiator.Instance.ChangeState(GameInitiator.GameState.InTown);
    }

    #region Pase Menu
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
