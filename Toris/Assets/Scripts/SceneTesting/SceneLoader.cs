using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader I;

    [Header("Fade Settings")]
    [SerializeField] CanvasGroup fadeCanvas;   // Will be created if null
    [SerializeField] float fadeDuration = 0.5f;
    [SerializeField] bool startBlack = false;  // optional: start with black screen then fade in on first scene
    [SerializeField] float blackHoldTime = 1f;


    Coroutine _activeFade;

    void Awake()
    {
        if (I) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        EnsureOverlay(); // make sure we have a persistent full-screen fader

        // Optional: fade in when the app starts
        if (startBlack)
        {
            fadeCanvas.alpha = 1f;
            StartCoroutine(FadeTo(0f));
        }
        else
        {
            fadeCanvas.alpha = 0f;
        }
    }

    public void GoTo(string nextScene)
    {
        // Avoid overlapping loads
        StopAllCoroutines();
        StartCoroutine(LoadSingle(nextScene));
    }

    IEnumerator LoadSingle(string nextScene)
    {
        // 1) Fade to full black (covers old scene)
        yield return FadeTo(1f);

        // 2) Load target scene
        if (!IsSceneInBuild(nextScene))
        {
            Debug.LogError($"[SceneLoader] Scene '{nextScene}' not found in Build Settings.");
            yield return FadeTo(0f);
            yield break;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        // 3) Make it active and let a frame render under black
        var s = SceneManager.GetSceneByName(nextScene);
        if (s.IsValid()) SceneManager.SetActiveScene(s);
        yield return null;                 // 1 frame to settle (still black)
        yield return new WaitForEndOfFrame(); // optional: extra safety

        // 4) Hold black over the **new** scene using serialized field
        if (blackHoldTime > 0f)
            yield return new WaitForSecondsRealtime(blackHoldTime);

        // 5) Fade back in
        yield return FadeTo(0f);
    }

    IEnumerator FadeTo(float target)
    {
        if (!fadeCanvas) yield break;

        // Make sure the overlay is on & raycasts block during fade
        var go = fadeCanvas.gameObject;
        if (!go.activeSelf) go.SetActive(true);
        bool oldBlocks = fadeCanvas.blocksRaycasts;
        fadeCanvas.blocksRaycasts = true;

        float start = fadeCanvas.alpha;
        float t = 0f;
        if (Mathf.Approximately(start, target))
        {
            fadeCanvas.blocksRaycasts = oldBlocks;
            yield break;
        }

        while (t < fadeDuration)
        {
            fadeCanvas.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        fadeCanvas.alpha = target;

        fadeCanvas.blocksRaycasts = oldBlocks;
    }

    bool IsSceneInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }

    void EnsureOverlay()
    {
        if (fadeCanvas) { DontDestroyOnLoad(fadeCanvas.gameObject); return; }

        // Create a full-screen overlay that persists
        var root = new GameObject("SceneLoader_Overlay");
        DontDestroyOnLoad(root);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // always on top

        root.AddComponent<GraphicRaycaster>();

        var imageGO = new GameObject("FadeRect");
        imageGO.transform.SetParent(root.transform, false);
        var img = imageGO.AddComponent<Image>();
        img.raycastTarget = true;
        img.color = Color.black;

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeCanvas = root.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 0f;
        fadeCanvas.blocksRaycasts = false; // we turn this on during fades
    }
}
