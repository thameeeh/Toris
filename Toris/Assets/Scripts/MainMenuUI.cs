using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void StartGame()
    {
        GameInitiator.Instance.ChangeState(GameInitiator.GameState.GamePlay);
    }
}
