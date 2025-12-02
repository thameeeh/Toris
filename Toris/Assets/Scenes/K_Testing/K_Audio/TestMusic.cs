using UnityEngine;
using UnityEngine.EventSystems;

public class TestMusic : MonoBehaviour
{
    public static TestMusic Instance {  get; private set; }

    [Header("Assign .ogg clips here")]
    public AudioClip clipGuitar;
    public AudioClip clipOboe;
    public AudioClip clipGuitarTwo;
    public AudioClip clipPadA;
    public AudioClip clipPadB;

    [Header("Behavior")]
    public bool buildPerLoop = true;
    [Range(0.1f, 10f)] public float fadeTime = 2f;

    // sources
    private AudioSource mGtr, mOboe, mGtr2, mPadA, mPadB;

    #region Timing Variables

    private float loopLen;          // decoded seconds
    private double dspStart;        // schedule anchor
    private double nextBoundary;    // next loop in dspTime
    private int loopIndex = 0;
    private bool padsEnabled = false;

    #endregion

    #region Fade State Variables

    private bool fadeOboe, fadeGtr2, fadePads;
    private double fadeOboeEnd, fadeGtr2End, fadePadsEnd;
    private float oboeStartV, gtr2StartV, padsStartV;

    #endregion
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        mGtr = gameObject.AddComponent<AudioSource>();
        mOboe = gameObject.AddComponent<AudioSource>();
        mGtr2 = gameObject.AddComponent<AudioSource>();
        mPadA = gameObject.AddComponent<AudioSource>();
        mPadB = gameObject.AddComponent<AudioSource>();

        SetupLooping(mGtr, clipGuitar);
        SetupLooping(mOboe, clipOboe);
        SetupLooping(mGtr2, clipGuitarTwo);

        // pads are here so we could alternate between A and B without clashing them on top of each other
        SetupOneShot(mPadA, clipPadA);
        SetupOneShot(mPadB, clipPadB);

        // starting point
        mGtr.volume = 1f;
        mOboe.volume = 0f;
        mGtr2.volume = 0f;
        mPadA.volume = 0f;
        mPadB.volume = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        // length/rate check for same length and rate
        if (!AllSameLenRate(clipGuitar, clipOboe, clipGuitarTwo, clipPadA, clipPadB))
        {
            //Debug.LogError("Stems are different in length or sample-rate. Please ensure they match.");
            mGtr.Play();
            enabled = false;
            return;
        }

        loopLen = clipGuitar.length;
        dspStart = AudioSettings.dspTime + 0.1;
        nextBoundary = dspStart + loopLen;

        // start continuous layers
        mGtr.PlayScheduled(dspStart);
        mOboe.PlayScheduled(dspStart);
        mGtr2.PlayScheduled(dspStart);

        // if not using Build Per Loop, then bring everything now
        if (!buildPerLoop)
        {
            mOboe.volume = 1f;
            mGtr2.volume = 1f;
            padsEnabled = true;
            // schedule first pad immediately on first boundary
            ScheduledPad(loopIndex + 1, nextBoundary);
        }
    }

    void Update()
    {
        double now = AudioSettings.dspTime;

        if (fadeOboe)
        {
            //Debug.Log("from within if (fadeOboe)");
            float t = Mathf.Clamp01((float)((now - (fadeOboeEnd - fadeTime)) / fadeTime));
            mOboe.volume = Mathf.Lerp(oboeStartV, 1f, t);
            if (now >= fadeOboeEnd) { mOboe.volume = 1f; fadeOboe = false; }
        }
        if (fadeGtr2)
        {
            float t = Mathf.Clamp01((float)((now - (fadeGtr2End - fadeTime)) / fadeTime));
            mGtr2.volume = Mathf.Lerp(gtr2StartV, 1f, t);
            if (now >= fadeGtr2End) { mGtr2.volume = 1f; fadeGtr2 = false; }
        }
        if (fadePads)
        {
            float t = Mathf.Clamp01((float)((now - (fadePadsEnd - fadeTime)) / fadeTime));
            float v = Mathf.Lerp(padsStartV, 1f, t);
            mPadA.volume = v;
            mPadB.volume = v;
            if (now >= fadePadsEnd) { mPadA.volume = mPadB.volume = 1f; fadePads = false; }
        }

        if (now >= nextBoundary)
        {
            loopIndex++;

            if (buildPerLoop)
            {
                if (loopIndex == 1 && mOboe.volume < 1f)
                {
                    //Debug.Log("fadeOboe starts");
                    BeginFadeOboe(now);
                }

                if (loopIndex == 2 && mGtr2.volume < 1f)
                {
                    //Debug.Log("fadeGtr2 starts");
                    BeginFadeGtr2(now);
                }

                if (loopIndex == 3 && !padsEnabled)
                {
                    //Debug.Log("fadePads starts");
                    padsEnabled = true;
                    BeginFadePads(now);
                }
            }
            if (padsEnabled)
                ScheduledPad(loopIndex, nextBoundary);

            nextBoundary += loopLen;
        }
    }

    #region Helpers
    void SetupLooping(AudioSource m, AudioClip c)
    {
        m.clip = c; m.loop = true; m.playOnAwake = false;
        m.spatialBlend = 0f; m.dopplerLevel = 0f; m.rolloffMode = AudioRolloffMode.Linear;
    }

    void SetupOneShot(AudioSource m, AudioClip c)
    {
        m.clip = c; m.loop = false; m.playOnAwake = false;
        m.spatialBlend = 0f; m.dopplerLevel = 0f; m.rolloffMode = AudioRolloffMode.Linear;
    }

    bool AllSameLenRate(params AudioClip[] clips)
    {
        int samp = clips[0].samples, rate = clips[0].frequency;
        for (int i = 1; i < clips.Length; i++)
            if (clips[i] == null || clips[i].samples != samp || clips[i].frequency != rate) return false;
        return true;
    }

    void BeginFadeOboe(double now)
    {
        oboeStartV = mOboe.volume;
        fadeOboeEnd = now + fadeTime;
        fadeOboe = true;
    }

    void BeginFadeGtr2(double now)
    {
        gtr2StartV = mGtr2.volume;
        fadeGtr2End = now + fadeTime;
        fadeGtr2 = true;
    }

    void BeginFadePads(double now)
    {
        padsStartV = Mathf.Max(mPadA.volume, mPadB.volume);
        fadePadsEnd = now + fadeTime;
        fadePads = true;
    }

    // Alternate between Pad A and Pad B (pad1, pad2) on every boundary once enabled
    void ScheduledPad(int loopNum, double boundary)
    {
        bool useA = (loopNum % 2 == 0);

        if (useA)
        {
            mPadA.Stop();
            mPadA.PlayScheduled(boundary);
        }
        else
        {
            mPadB.Stop();
            mPadB.PlayScheduled(boundary);
        }
    }
    #endregion
}