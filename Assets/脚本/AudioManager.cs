using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM Tracks")]
    public AudioClip[] bgmTracks;

    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string BGM_MUTED_KEY = "BGMMuted";
    private const string TRACK_INDEX_KEY = "BGMTrackIndex";
    private const string BGM_RECOVERED_KEY = "BGMRecoveredFromSilent";

    private AudioSource bgmSource;
    private Coroutine fadeCoroutine;
    private float bgmVolume = 0.7f;
    private bool isMuted;

    public int CurrentTrackIndex { get; private set; }
    public int TrackCount => bgmTracks != null ? bgmTracks.Length : 0;
    public float BGMVolume => bgmVolume;
    public bool IsMuted => isMuted;

    public string CurrentTrackName
    {
        get
        {
            if (bgmTracks == null || CurrentTrackIndex < 0 || CurrentTrackIndex >= bgmTracks.Length)
                return "No BGM";

            AudioClip clip = bgmTracks[CurrentTrackIndex];
            return GetDisplayTrackName(clip, CurrentTrackIndex);
        }
    }

    public event System.Action SettingsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateGlobalAudioManager()
    {
        if (Instance != null)
            return;

        GameObject manager = new GameObject("Global Audio Manager");
        manager.AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.ignoreListenerPause = true;
        bgmSource.mute = false;

        LoadSettings();
        LoadTracksIfNeeded();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        PlaySavedTrack();
        EnsureAudioSettingsUI();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAudioSettingsUI();
    }

    public void PlayBGM(int index)
    {
        if (bgmTracks == null || index < 0 || index >= bgmTracks.Length || bgmTracks[index] == null)
            return;

        CurrentTrackIndex = index;
        PlayerPrefs.SetInt(TRACK_INDEX_KEY, index);
        PlayerPrefs.Save();

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToNewTrack(bgmTracks[index]));
        NotifyChanged();
    }

    public void PlayNextBGM()
    {
        if (TrackCount == 0)
            return;

        PlayBGM((CurrentTrackIndex + 1) % TrackCount);
    }

    public void PlayPrevBGM()
    {
        if (TrackCount == 0)
            return;

        PlayBGM((CurrentTrackIndex - 1 + TrackCount) % TrackCount);
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmVolume > 0.01f && isMuted)
        {
            isMuted = false;
            PlayerPrefs.SetInt(BGM_MUTED_KEY, 0);
        }

        AudioListener.volume = 1f;
        if (!isMuted)
        {
            bgmSource.volume = bgmVolume;
        }

        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        PlayerPrefs.Save();
        EnsureTrackIsPlaying();
        NotifyChanged();
    }

    public void AdjustBGMVolume(float delta)
    {
        SetBGMVolume(Mathf.Clamp01(bgmVolume + delta));
    }

    public void ToggleBGM()
    {
        SetMuted(!isMuted);
    }

    public void SetMuted(bool muted)
    {
        isMuted = muted;
        AudioListener.volume = 1f;
        bgmSource.volume = isMuted ? 0f : bgmVolume;
        PlayerPrefs.SetInt(BGM_MUTED_KEY, isMuted ? 1 : 0);
        PlayerPrefs.Save();
        EnsureTrackIsPlaying();
        NotifyChanged();
    }

    public void RestoreAudibleDefaults()
    {
        bgmVolume = 0.7f;
        isMuted = false;
        AudioListener.volume = 1f;
        bgmSource.mute = false;
        bgmSource.volume = bgmVolume;
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        PlayerPrefs.SetInt(BGM_MUTED_KEY, 0);
        PlayerPrefs.Save();
        EnsureTrackIsPlaying();
        NotifyChanged();
    }

    public void PauseBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.UnPause();
            bgmSource.volume = isMuted ? 0f : bgmVolume;
        }
    }

    public string GetTrackName(int index)
    {
        if (bgmTracks == null || index < 0 || index >= bgmTracks.Length || bgmTracks[index] == null)
            return "No BGM";

        return GetDisplayTrackName(bgmTracks[index], index);
    }

    private string GetDisplayTrackName(AudioClip clip, int index)
    {
        if (clip == null)
            return "No BGM";

        string cleanName = StripNonAsciiTrackName(clip.name);
        return string.IsNullOrWhiteSpace(cleanName) ? $"Track {index + 1}" : cleanName;
    }

    private string StripNonAsciiTrackName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder(rawName.Length);
        bool previousWasSpace = false;
        foreach (char c in rawName)
        {
            char output = c;
            if (c == '_' || c == '-' || c == '.')
            {
                output = ' ';
            }

            bool keep = output >= 32 && output <= 126;
            if (!keep)
                continue;

            if (char.IsWhiteSpace(output))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }
            }
            else
            {
                builder.Append(output);
                previousWasSpace = false;
            }
        }

        return RemoveSourceWatermark(builder.ToString().Trim());
    }

    private string RemoveSourceWatermark(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        int watermarkIndex = name.IndexOf("aigei com", System.StringComparison.OrdinalIgnoreCase);
        if (watermarkIndex >= 0)
        {
            name = name.Substring(0, watermarkIndex).Trim();
        }

        return name.Trim('-', '_', ' ');
    }

    private void LoadSettings()
    {
        bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.7f);
        CurrentTrackIndex = PlayerPrefs.GetInt(TRACK_INDEX_KEY, 0);
        isMuted = PlayerPrefs.GetInt(BGM_MUTED_KEY, 0) == 1;

        bool recoveredBefore = PlayerPrefs.GetInt(BGM_RECOVERED_KEY, 0) == 1;
        if (!recoveredBefore && (isMuted || bgmVolume < 0.05f))
        {
            bgmVolume = 0.7f;
            isMuted = false;
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
            PlayerPrefs.SetInt(BGM_MUTED_KEY, 0);
            PlayerPrefs.SetInt(BGM_RECOVERED_KEY, 1);
            PlayerPrefs.Save();
        }
    }

    private void LoadTracksIfNeeded()
    {
        if (bgmTracks != null && bgmTracks.Length > 0)
            return;

        bgmTracks = Resources.LoadAll<AudioClip>("BGM");
        if (bgmTracks == null || bgmTracks.Length == 0)
        {
            bgmTracks = Resources.LoadAll<AudioClip>("");
        }

        System.Array.Sort(bgmTracks, (a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
    }

    private void PlaySavedTrack()
    {
        if (TrackCount == 0)
            return;

        CurrentTrackIndex = Mathf.Clamp(CurrentTrackIndex, 0, TrackCount - 1);
        bgmSource.clip = bgmTracks[CurrentTrackIndex];
        AudioListener.volume = 1f;
        bgmSource.mute = false;
        bgmSource.volume = isMuted ? 0f : bgmVolume;
        bgmSource.Play();
        NotifyChanged();
    }

    private void EnsureTrackIsPlaying()
    {
        if (TrackCount == 0)
            return;

        if (bgmSource.clip == null)
        {
            CurrentTrackIndex = Mathf.Clamp(CurrentTrackIndex, 0, TrackCount - 1);
            bgmSource.clip = bgmTracks[CurrentTrackIndex];
        }

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    private IEnumerator FadeToNewTrack(AudioClip newClip)
    {
        float startVolume = bgmSource.volume;
        const float fadeTime = 0.45f;

        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }

        bgmSource.clip = newClip;
        bgmSource.Play();

        float targetVolume = isMuted ? 0f : bgmVolume;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeTime);
            yield return null;
        }

        bgmSource.volume = targetVolume;
        fadeCoroutine = null;
    }

    private void EnsureAudioSettingsUI()
    {
        if (FindObjectOfType<AudioSettingsUI>() != null)
            return;

        GameObject ui = new GameObject("Global BGM Settings UI");
        DontDestroyOnLoad(ui);
        ui.AddComponent<AudioSettingsUI>();
    }

    private void NotifyChanged()
    {
        SettingsChanged?.Invoke();
    }
}
