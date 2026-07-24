using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TitleBgm : MonoBehaviour
{
    private const string TitleSceneName = "TitleScene";
    private const string StageSelectSceneName = "StageSelectScene";
    private const string TitleClipResourcePath = "Audio/Title";
    private const string StageClipResourcePath = "Audio/maou_bgm_8bit22";
    private const float TitleVolume = 0.6f;
    private const float StageVolume = 0.25f;

    private static TitleBgm instance;

    private AudioSource audioSource;
    private AudioClip titleClip;
    private AudioClip stageClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject playerObject = new GameObject(nameof(TitleBgm));
        instance = playerObject.AddComponent<TitleBgm>();
        DontDestroyOnLoad(playerObject);
        instance.UpdatePlayback(SceneManager.GetActiveScene());
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        titleClip = Resources.Load<AudioClip>(TitleClipResourcePath);
        stageClip = Resources.Load<AudioClip>(StageClipResourcePath);
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (titleClip == null)
        {
            Debug.LogWarning($"Title BGM clip not found at Resources/{TitleClipResourcePath}.");
        }

        if (stageClip == null)
        {
            Debug.LogWarning($"Stage BGM clip not found at Resources/{StageClipResourcePath}.");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdatePlayback(scene);
    }

    private void UpdatePlayback(Scene scene)
    {
        bool isMenuScene = scene.name == TitleSceneName || scene.name == StageSelectSceneName;
        bool isStageScene = scene.name.StartsWith("Stage") && scene.name != StageSelectSceneName;
        AudioClip desiredClip = isMenuScene ? titleClip : isStageScene ? stageClip : null;

        if (desiredClip == null)
        {
            audioSource.Stop();
            audioSource.clip = null;
            return;
        }

        if (audioSource.clip != desiredClip)
        {
            audioSource.Stop();
            audioSource.clip = desiredClip;
        }

        audioSource.volume = desiredClip == stageClip ? StageVolume : TitleVolume;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}
