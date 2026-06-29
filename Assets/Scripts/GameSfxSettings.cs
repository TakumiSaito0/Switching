using UnityEngine;

[CreateAssetMenu(menuName = "Switching/Game SFX Settings")]
public class GameSfxSettings : ScriptableObject
{
    [Header("Master")]
    [Range(0f, 3f)] public float masterVolume = 1.8f;

    [Header("Circuit")]
    [Range(0f, 3f)] public float circuitPlaceVolume = 1.6f;
    [Range(0f, 3f)] public float circuitRemoveVolume = 1.5f;
    [Range(0f, 3f)] public float circuitPowerVolume = 1.4f;
    [Range(0f, 3f)] public float switchVolume = 1.5f;

    [Header("Objects")]
    [Range(0f, 3f)] public float boxPickupVolume = 1.5f;
    [Range(0f, 3f)] public float boxDropVolume = 1.6f;
    [Range(0f, 3f)] public float doorVolume = 1.5f;

    [Header("Result")]
    [Range(0f, 3f)] public float clearVolume = 1.5f;
    [Range(0f, 3f)] public float gameOverVolume = 1.5f;

    [Header("Distance")]
    [Range(0f, 1f)] public float spatialBlend = 0.2f;
    [Range(0f, 1f)] public float boxSpatialBlend = 0f;
    [Range(0.1f, 30f)] public float minDistance = 8f;
    [Range(1f, 100f)] public float maxDistance = 40f;

    public float GetVolumeMultiplier(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName))
        {
            return masterVolume;
        }

        string normalizedName = clipName.Replace("_lowpoly", string.Empty);
        if (normalizedName.Contains("circuit_place")) return masterVolume * circuitPlaceVolume;
        if (normalizedName.Contains("circuit_remove")) return masterVolume * circuitRemoveVolume;
        if (normalizedName.Contains("circuit_power")) return masterVolume * circuitPowerVolume;
        if (normalizedName.Contains("switch")) return masterVolume * switchVolume;
        if (normalizedName.Contains("box_pickup")) return masterVolume * boxPickupVolume;
        if (normalizedName.Contains("box_drop")) return masterVolume * boxDropVolume;
        if (normalizedName.Contains("door")) return masterVolume * doorVolume;
        if (normalizedName.Contains("clear")) return masterVolume * clearVolume;
        if (normalizedName.Contains("game_over")) return masterVolume * gameOverVolume;

        return masterVolume;
    }

    public float GetSpatialBlend(string clipName)
    {
        if (!string.IsNullOrWhiteSpace(clipName) && clipName.Contains("box"))
        {
            return boxSpatialBlend;
        }

        return spatialBlend;
    }
}
