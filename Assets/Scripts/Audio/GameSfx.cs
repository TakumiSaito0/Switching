using System.Collections.Generic;
using UnityEngine;

public static class GameSfx
{
    private const string ResourceRoot = "Audio/SFX/";
    private const string SettingsPath = "Audio/GameSfxSettings";
    private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();
    private static GameSfxSettings settings;

    public static void Play(string clipName, float volume = 1f)
    {
        Vector3 position = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        PlayAt(clipName, position, volume);
    }

    public static void PlayAt(string clipName, Vector3 position, float volume = 1f)
    {
        AudioClip clip = LoadClip(clipName);
        if (clip == null)
        {
            return;
        }

        GameObject audioObject = new GameObject("SFX_" + clipName);
        audioObject.transform.position = position;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume * GetVolumeMultiplier(clipName);
        source.spatialBlend = GetSpatialBlend(clipName);
        source.minDistance = GetMinDistance();
        source.maxDistance = GetMaxDistance();
        source.rolloffMode = AudioRolloffMode.Linear;
        source.Play();

        Object.Destroy(audioObject, clip.length + 0.1f);
    }

    private static AudioClip LoadClip(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName))
        {
            return null;
        }

        if (ClipCache.TryGetValue(clipName, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        AudioClip loadedClip = Resources.Load<AudioClip>(ResourceRoot + clipName);
        if (loadedClip == null)
        {
            Debug.LogWarning($"SFX clip not found: {clipName}");
        }

        ClipCache[clipName] = loadedClip;
        return loadedClip;
    }

    private static float GetVolumeMultiplier(string clipName)
    {
        if (settings == null)
        {
            settings = Resources.Load<GameSfxSettings>(SettingsPath);
        }

        return settings != null ? settings.GetVolumeMultiplier(clipName) : 1f;
    }

    private static float GetSpatialBlend(string clipName)
    {
        EnsureSettingsLoaded();
        return settings != null ? settings.GetSpatialBlend(clipName) : 0.2f;
    }

    private static float GetMinDistance()
    {
        EnsureSettingsLoaded();
        return settings != null ? settings.minDistance : 8f;
    }

    private static float GetMaxDistance()
    {
        EnsureSettingsLoaded();
        return settings != null ? settings.maxDistance : 40f;
    }

    private static void EnsureSettingsLoaded()
    {
        if (settings == null)
        {
            settings = Resources.Load<GameSfxSettings>(SettingsPath);
        }
    }
}
