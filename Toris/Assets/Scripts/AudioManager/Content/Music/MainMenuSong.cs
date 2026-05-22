using UnityEngine;
using UnityEngine.Audio;

public class MainMenuSong : MonoBehaviour
{
    public static MainMenuSong Instance { get; private set; }

    [Header("Assign .ogg clips here")]
    public AudioClip clipGuitar;
    public AudioClip clipOboe;
    public AudioClip clipGuitarTwo;
    public AudioClip clipPadA;
    public AudioClip clipPadB;

    [Header("Routing")]
    [SerializeField] private AudioMixerGroup outputMixerGroup;
    [SerializeField, Range(0f, 2f)] private float arrangementVolume = 1f;

    [Header("Behavior")]
    public bool buildPerLoop = true;
    [Range(0.1f, 10f)] public float fadeTime = 2f;
    [SerializeField, Min(0f)] private float defaultExitFadeOutSeconds = 1f;

    [Header("Pad Panning")]
    [SerializeField, Range(-1f, 1f)] private float padAPan = -1f;
    [SerializeField, Range(-1f, 1f)] private float padBPan = 1f;

    // sources
    private AudioSource mGtr, mOboe, mGtr2, mPadA, mPadB;
    private float gtrBaseVolume;
    private float oboeBaseVolume;
    private float gtr2BaseVolume;
    private float padsBaseVolume;
    private float songFadeVolume = 1f;
    private float songFadeStartVolume = 1f;
    private float songFadeElapsed;
    private float songFadeDuration;
    private bool fadeSongOut;
    private bool stopAfterSongFade;

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

    public float FadeOutAndStop(float fadeOutSeconds = -1f)
    {
        float duration = fadeOutSeconds >= 0f ? fadeOutSeconds : defaultExitFadeOutSeconds;
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
        {
            songFadeVolume = 0f;
            ApplySourceVolumes();
            StopAllSources();
            enabled = false;
            return 0f;
        }

        // Music-only scene-exit hook: MainMenuController can fade this custom stem director before loading gameplay.
        songFadeStartVolume = songFadeVolume;
        songFadeElapsed = 0f;
        songFadeDuration = duration;
        fadeSongOut = true;
        stopAfterSongFade = true;
        return duration;
    }

    private void Awake()
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

        mPadA.panStereo = padAPan;
        mPadB.panStereo = padBPan;

        // starting point
        gtrBaseVolume = 1f;
        oboeBaseVolume = 0f;
        gtr2BaseVolume = 0f;
        padsBaseVolume = 0f;
        ApplySourceVolumes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        // length/rate check for same length and rate
        if (!AllSameLenRate(clipGuitar, clipOboe, clipGuitarTwo, clipPadA, clipPadB))
        {
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
            oboeBaseVolume = 1f;
            gtr2BaseVolume = 1f;
            padsBaseVolume = 1f;
            ApplySourceVolumes();
            padsEnabled = true;
            // schedule first pad immediately on first boundary
            SchedulePad(loopIndex + 1, nextBoundary);
        }
    }

    private void Update()
    {
        double now = AudioSettings.dspTime;

        if (fadeOboe)
        {
            float t = Mathf.Clamp01((float)((now - (fadeOboeEnd - fadeTime)) / fadeTime));
            oboeBaseVolume = Mathf.Lerp(oboeStartV, 1f, t);
            if (now >= fadeOboeEnd) { oboeBaseVolume = 1f; fadeOboe = false; }
        }
        if (fadeGtr2)
        {
            float t = Mathf.Clamp01((float)((now - (fadeGtr2End - fadeTime)) / fadeTime));
            gtr2BaseVolume = Mathf.Lerp(gtr2StartV, 1f, t);
            if (now >= fadeGtr2End) { gtr2BaseVolume = 1f; fadeGtr2 = false; }
        }
        if (fadePads)
        {
            float t = Mathf.Clamp01((float)((now - (fadePadsEnd - fadeTime)) / fadeTime));
            padsBaseVolume = Mathf.Lerp(padsStartV, 1f, t);
            if (now >= fadePadsEnd) { padsBaseVolume = 1f; fadePads = false; }
        }

        UpdateSongFade();
        ApplySourceVolumes();

        if (now >= nextBoundary)
        {
            loopIndex++;

            if (buildPerLoop)
            {
                if (loopIndex == 1 && oboeBaseVolume < 1f)
                {
                    BeginFadeOboe(now);
                }

                if (loopIndex == 2 && gtr2BaseVolume < 1f)
                {
                    BeginFadeGtr2(now);
                }

                if (loopIndex == 3 && !padsEnabled)
                {
                    padsEnabled = true;
                    BeginFadePads(now);
                }
            }
            if (padsEnabled)
                SchedulePad(loopIndex, nextBoundary);

            nextBoundary += loopLen;
        }
    }

    #region Helpers
    private void SetupLooping(AudioSource m, AudioClip c)
    {
        m.clip = c; m.loop = true; m.playOnAwake = false;
        m.spatialBlend = 0f; m.dopplerLevel = 0f; m.rolloffMode = AudioRolloffMode.Linear;
        if (outputMixerGroup != null)
            m.outputAudioMixerGroup = outputMixerGroup;
    }

    private void SetupOneShot(AudioSource m, AudioClip c)
    {
        m.clip = c; m.loop = false; m.playOnAwake = false;
        m.spatialBlend = 0f; m.dopplerLevel = 0f; m.rolloffMode = AudioRolloffMode.Linear;
        if (outputMixerGroup != null)
            m.outputAudioMixerGroup = outputMixerGroup;
    }

    private bool AllSameLenRate(params AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || clips[0] == null)
            return false;

        int samp = clips[0].samples, rate = clips[0].frequency;
        for (int i = 1; i < clips.Length; i++)
            if (clips[i] == null || clips[i].samples != samp || clips[i].frequency != rate) return false;
        return true;
    }

    private void BeginFadeOboe(double now)
    {
        oboeStartV = oboeBaseVolume;
        fadeOboeEnd = now + fadeTime;
        fadeOboe = true;
    }

    private void BeginFadeGtr2(double now)
    {
        gtr2StartV = gtr2BaseVolume;
        fadeGtr2End = now + fadeTime;
        fadeGtr2 = true;
    }

    private void BeginFadePads(double now)
    {
        padsStartV = padsBaseVolume;
        fadePadsEnd = now + fadeTime;
        fadePads = true;
    }

    private void ApplySourceVolumes()
    {
        // Music-only routing: this custom stem director bypasses MusicManager, so it applies the same music volume setting itself.
        ApplySourceVolume(mGtr, gtrBaseVolume);
        ApplySourceVolume(mOboe, oboeBaseVolume);
        ApplySourceVolume(mGtr2, gtr2BaseVolume);
        ApplySourceVolume(mPadA, padsBaseVolume);
        ApplySourceVolume(mPadB, padsBaseVolume);
    }

    private void ApplySourceVolume(AudioSource source, float stemVolume)
    {
        if (source == null)
            return;

        source.volume = Mathf.Clamp(stemVolume * songFadeVolume * arrangementVolume * AudioVolumeSettings.EffectiveMusicVolume, 0f, 2f);
    }

    private void UpdateSongFade()
    {
        if (!fadeSongOut)
            return;

        songFadeElapsed += Time.unscaledDeltaTime;
        float t = songFadeDuration <= 0f ? 1f : Mathf.Clamp01(songFadeElapsed / songFadeDuration);
        songFadeVolume = Mathf.Lerp(songFadeStartVolume, 0f, t);

        if (t < 1f)
            return;

        fadeSongOut = false;
        if (!stopAfterSongFade)
            return;

        StopAllSources();
        enabled = false;
    }

    private void StopAllSources()
    {
        mGtr?.Stop();
        mOboe?.Stop();
        mGtr2?.Stop();
        mPadA?.Stop();
        mPadB?.Stop();
    }

    // Alternate between Pad A and Pad B (pad1, pad2) on every boundary once enabled
    private void SchedulePad(int loopNum, double boundary)
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
