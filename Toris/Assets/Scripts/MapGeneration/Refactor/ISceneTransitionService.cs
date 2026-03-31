public interface ISceneTransitionService
{
    bool IsLoading { get; }
    void LoadScene(string sceneName);
}
