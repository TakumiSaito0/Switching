using UnityEditor;
using UnityEngine;

public static class LadderVisualSetup
{
    private const string LadderPath = "Assets/Prefabs/Ladder.prefab";
    private const string VisualPath = "Assets/Prefabs/Proto_Ladder_Wooden_02_Atlased.prefab";
    [MenuItem("Tools/Ladder/Use Wooden Ladder Visual")]
    public static void Setup()
    {
        GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
        GameObject ladder = PrefabUtility.LoadPrefabContents(LadderPath);
        if (visualPrefab == null || ladder == null)
        {
            Debug.LogError("The ladder prefab or its new visual could not be loaded.");
            return;
        }

        // Remove the old cube renderer while retaining the trigger, tag and Ladder script.
        MeshRenderer oldRenderer = ladder.GetComponent<MeshRenderer>();
        if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
        MeshFilter oldFilter = ladder.GetComponent<MeshFilter>();
        if (oldFilter != null) Object.DestroyImmediate(oldFilter);

        Transform previous = ladder.transform.Find("Wooden_Ladder_Visual");
        if (previous != null) Object.DestroyImmediate(previous.gameObject);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
        visual.name = "Wooden_Ladder_Visual";
        visual.transform.SetParent(ladder.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        // The root trigger is the authoritative gameplay volume. Mesh colliders would
        // block the player from entering it, so the imported model is visual-only.
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        NormalizeToUnitBounds(ladder.transform, visual.transform);

        PrefabUtility.SaveAsPrefabAsset(ladder, LadderPath);
        PrefabUtility.UnloadPrefabContents(ladder);
        AssetDatabase.SaveAssets();
        Debug.Log("Ladder.prefab now uses the wooden ladder model; climbing behavior is unchanged.");
    }

    private static void NormalizeToUnitBounds(Transform ladder, Transform visual)
    {
        Vector3 originalRootScale = ladder.localScale;
        ladder.localScale = Vector3.one;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            ladder.localScale = originalRootScale;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        Vector3 localCenter = ladder.InverseTransformPoint(bounds.center);
        Vector3 localSize = ladder.InverseTransformVector(bounds.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        Vector3 fitScale = new Vector3(
            localSize.x > 0.0001f ? 1f / localSize.x : 1f,
            localSize.y > 0.0001f ? 1f / localSize.y : 1f,
            localSize.z > 0.0001f ? 1f / localSize.z : 1f);

        visual.localScale = fitScale;
        visual.localPosition = -Vector3.Scale(localCenter, fitScale);
        ladder.localScale = originalRootScale;
    }
}
