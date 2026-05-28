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
    private RectTransform videoRect;
    private RectTransform canvasRect;
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
        canvasRect = canvasObject.GetComponent<RectTransform>();

        GameObject blockerObject = new GameObject("Start Intro Fullscreen Blocker");
        blockerObject.transform.SetParent(canvasObject.transform, false);
        Image blocker = blockerObject.AddComponent<Image>();
        blocker.color = Color.black;
        blocker.raycastTarget = true;

        RectTransform blockerRect = blocker.rectTransform;
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        GameObject imageObject = new GameObject("Start Intro Video Image");
        imageObject.transform.SetParent(canvasObject.transform, false);
        RawImage image = imageObject.AddComponent<RawImage>();
        image.color = Color.white;
        image.raycastTarget = true;

        videoRect = image.rectTransform;
        videoRect.anchorMin = new Vector2(0.5f, 0.5f);
        videoRect.anchorMax = new Vector2(0.5f, 0.5f);
        videoRect.pivot = new Vector2(0.5f, 0.5f);
        videoRect.anchoredPosition = Vector2.zero;
        SetVideoRectCover(16f / 9f);

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = GetVideoPath(videoFileName);
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
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

        int videoWidth = videoPlayer.width > 0 ? (int)videoPlayer.width : 1920;
        int videoHeight = videoPlayer.height > 0 ? (int)videoPlayer.height : 1080;
        float videoAspect = videoHeight > 0 ? videoWidth / (float)videoHeight : 16f / 9f;
        renderTexture = new RenderTexture(videoWidth, videoHeight, 0, RenderTextureFormat.ARGB32);
        renderTexture.name = "StartIntroVideoTexture";
        image.texture = renderTexture;
        videoPlayer.targetTexture = renderTexture;
        SetVideoRectCover(videoAspect);

        videoPlayer.Play();
        videoAudio.Play();

        while (!videoFinished)
        {
            SetVideoRectCover(videoAspect);

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

    private void SetVideoRectCover(float videoAspect)
    {
        if (videoRect == null)
            return;

        Vector2 canvasSize = GetCanvasSize();
        float screenAspect = canvasSize.x / Mathf.Max(1f, canvasSize.y);
        float width = canvasSize.x;
        float height = canvasSize.y;

        if (videoAspect > screenAspect)
        {
            width = height * videoAspect;
        }
        else
        {
            height = width / Mathf.Max(0.01f, videoAspect);
        }

        videoRect.sizeDelta = new Vector2(width, height);
        videoRect.anchoredPosition = Vector2.zero;
    }

    private Vector2 GetCanvasSize()
    {
        if (canvasRect != null && canvasRect.rect.width > 1f && canvasRect.rect.height > 1f)
            return canvasRect.rect.size;

        return new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
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
