using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class StartIntroVideoTransition : MonoBehaviour
{
    public const string DefaultVideoFileName = "start_intro.mp4";

    private static bool isPlaying;

    private string targetSceneName;
    private string videoFileName;
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private bool videoFinished;

    public static bool PlayThenLoad(string targetSceneName, string videoFileName = DefaultVideoFileName)
    {
        if (isPlaying)
            return true;

        if (string.IsNullOrWhiteSpace(targetSceneName))
            return false;

        string videoPath = GetVideoPath(videoFileName);
        if (!File.Exists(videoPath))
        {
            Debug.LogWarning($"[StartIntroVideoTransition] Video not found: {videoPath}");
            return false;
        }

        GameObject playerObject = new GameObject("Start Intro Video Transition");
        DontDestroyOnLoad(playerObject);

        StartIntroVideoTransition transition = playerObject.AddComponent<StartIntroVideoTransition>();
        transition.targetSceneName = targetSceneName;
        transition.videoFileName = videoFileName;
        isPlaying = true;
        transition.StartCoroutine(transition.PlayRoutine());
        return true;
    }

    private IEnumerator PlayRoutine()
    {
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseBGM();
        }

        GameObject canvasObject = new GameObject("Start Intro Video Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("Start Intro Video Image");
        imageObject.transform.SetParent(canvasObject.transform, false);
        RawImage image = imageObject.AddComponent<RawImage>();
        image.color = Color.white;

        RectTransform imageRect = image.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        int width = Mathf.Max(1280, Screen.width);
        int height = Mathf.Max(720, Screen.height);
        renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        image.texture = renderTexture;

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = GetVideoPath(videoFileName);
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;

        AudioSource videoAudio = gameObject.AddComponent<AudioSource>();
        videoAudio.playOnAwake = false;
        videoAudio.spatialBlend = 0f;
        videoPlayer.SetTargetAudioSource(0, videoAudio);

        videoPlayer.Prepare();
        float prepareDeadline = Time.realtimeSinceStartup + 5f;
        while (!videoPlayer.isPrepared && Time.realtimeSinceStartup < prepareDeadline)
        {
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogWarning("[StartIntroVideoTransition] Video prepare timed out.");
            LoadTargetScene();
            yield break;
        }

        videoPlayer.Play();
        videoAudio.Play();

        while (!videoFinished)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                break;
            }

            yield return null;
        }

        LoadTargetScene();
    }

    private static string GetVideoPath(string videoFileName)
    {
        string safeFileName = string.IsNullOrWhiteSpace(videoFileName) ? DefaultVideoFileName : videoFileName;
        return Path.Combine(Application.streamingAssetsPath, safeFileName);
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        videoFinished = true;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning($"[StartIntroVideoTransition] Video error: {message}");
        videoFinished = true;
    }

    private void LoadTargetScene()
    {
        CleanupVideo();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeBGM();
        }

        BloodManager.ResetGameState();
        SceneManager.LoadScene(targetSceneName);
        Destroy(gameObject);
    }

    private void CleanupVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.Stop();
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        isPlaying = false;
    }

    private void OnDestroy()
    {
        CleanupVideo();
    }
}
