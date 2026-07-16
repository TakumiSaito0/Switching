using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Stage3BouncePadVisualSetup
{
    private const string ScenePath = "Assets/Scenes/Stage3.unity";
    private const string VisualPath = "Assets/Off Axis Studios/Polyworks/Prefabs/MaterialsOnly/Prototype/Proto_Warp_Pad_01.prefab";
    private const string VisualName = "Polyworks_Warp_Pad_Visual";
    private const string SessionKey = "Stage3BouncePadVisualSetup_WarpPad_v1";

    static Stage3BouncePadVisualSetup() => EditorApplication.delayCall += ApplyOnce;

    private static void ApplyOnce()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        Apply();
    }

    [MenuItem("Tools/Stages/Stage 3/Use Warp Pads")]
    public static void Apply()
    {
        GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
        if (visualPrefab == null)
            throw new MissingReferenceException("Warp pad prefab not found: " + VisualPath);

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        if (openedForSetup)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        GameObject[] bouncePads = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(transform => transform.name.StartsWith("Stage3_BouncePad"))
            .Select(transform => transform.gameObject)
            .ToArray();

        foreach (GameObject bouncePad in bouncePads)
            ReplaceVisual(bouncePad, visualPrefab);

        EditorSceneManager.SaveScene(scene);
        if (openedForSetup)
            EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"Stage 3: applied Polyworks warp-pad visuals to {bouncePads.Length} bounce pads.");
    }

    private static void ReplaceVisual(GameObject bouncePad, GameObject visualPrefab)
    {
        MeshRenderer oldRenderer = bouncePad.GetComponent<MeshRenderer>();
        if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);

        MeshFilter oldFilter = bouncePad.GetComponent<MeshFilter>();
        if (oldFilter != null) Object.DestroyImmediate(oldFilter);

        Transform previousVisual = bouncePad.transform.Find(VisualName);
        if (previousVisual != null) Object.DestroyImmediate(previousVisual.gameObject);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, bouncePad.scene);
        visual.name = VisualName;
        visual.transform.SetParent(bouncePad.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        // The original box collider and BouncePad component remain authoritative.
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        FitVisualToPad(bouncePad.transform, visual.transform);
    }

    private static void FitVisualToPad(Transform pad, Transform visual)
    {
        Vector3 originalPadScale = pad.localScale;
        pad.localScale = Vector3.one;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            pad.localScale = originalPadScale;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 localCenter = pad.InverseTransformPoint(bounds.center);
        Vector3 localSize = pad.InverseTransformVector(bounds.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

        // The gameplay root is very flat (Y scale 0.2), so give the model enough
        // local height to retain a recognizable raised jump-pad silhouette.
        Vector3 targetSize = new Vector3(0.92f, 2.5f, 0.92f);
        Vector3 fitScale = new Vector3(
            localSize.x > 0.0001f ? targetSize.x / localSize.x : 1f,
            localSize.y > 0.0001f ? targetSize.y / localSize.y : 1f,
            localSize.z > 0.0001f ? targetSize.z / localSize.z : 1f);

        visual.localScale = fitScale;
        visual.localPosition = -Vector3.Scale(localCenter, fitScale);
        pad.localScale = originalPadScale;
    }
}
