using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StageWallVisualSetup
{
    private const string VisualName = "Polyworks_Wall_Visual";

    private static readonly string[] WallVisualPaths =
    {
        null,
        "Assets/PrivateFolder/Off Axis Studios/Polyworks/Prefabs/MaterialsOnly/Prototype/Blocks - Green/Proto_Building_Block_Wall_01.prefab",
        "Assets/PrivateFolder/Off Axis Studios/Polyworks/Prefabs/MaterialsOnly/Prototype/Blocks - Yellow/Proto_Building_Block_Wall_01.prefab",
        "Assets/PrivateFolder/Off Axis Studios/Polyworks/Prefabs/MaterialsOnly/Industrial/Prop_Industrial_Temporary_Lab_Office_Wall_01.prefab",
        "Assets/PrivateFolder/Off Axis Studios/Polyworks/Prefabs/MaterialsOnly/SciFi/SciFi_Modular_Wall_Section_Grey_01.prefab",
        "Assets/PrivateFolder/Off Axis Studios/Polyworks/Prefabs/MaterialsOnly/SciFi/SciFi_Modular_Wall_Section_White_01.prefab"
    };

    [MenuItem("Tools/Stages/Apply Polyworks Wall Themes")]
    public static void Apply()
    {
        RemoveDecorativeWalls(1);
        RemoveDecorativeWalls(2);
        ApplyToStage(2, false);

        for (int stageNumber = 3; stageNumber <= 5; stageNumber++)
            ApplyToStage(stageNumber);

        AssetDatabase.SaveAssets();
        Debug.Log("Kept Stages 1-2 InvisibleWalls undecorated, restored Stage 2 structural walls, and applied themes to Stages 3-5.");
    }

    private static void RemoveDecorativeWalls(int stageNumber)
    {
        string scenePath = $"Assets/Scenes/Stage{stageNumber}.unity";
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        if (openedForSetup)
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        GameObject[] walls = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(transform => IsInvisibleWall(transform.name))
            .Where(transform => transform.GetComponent<BoxCollider>() != null)
            .Select(transform => transform.gameObject)
            .ToArray();

        foreach (GameObject wall in walls)
        {
            Transform decorativeVisual = wall.transform.Find(VisualName);
            if (decorativeVisual != null)
                UnityEngine.Object.DestroyImmediate(decorativeVisual.gameObject);

            // Stages 1-2 use their original invisible collision walls only.
            foreach (Renderer renderer in wall.GetComponents<Renderer>())
                renderer.enabled = false;
        }

        EditorSceneManager.SaveScene(scene);
        if (openedForSetup)
            EditorSceneManager.CloseScene(scene, true);

        Debug.Log($"Stage {stageNumber}: removed decorative wall visuals from {walls.Length} invisible walls.");
    }

    private static void ApplyToStage(int stageNumber, bool includeInvisibleWalls = true)
    {
        string scenePath = $"Assets/Scenes/Stage{stageNumber}.unity";
        GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WallVisualPaths[stageNumber]);
        if (visualPrefab == null)
            throw new MissingReferenceException("Wall visual not found: " + WallVisualPaths[stageNumber]);

        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        if (openedForSetup)
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        GameObject[] walls = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(transform => IsWall(transform.name))
            .Where(transform => includeInvisibleWalls || !IsInvisibleWall(transform.name))
            .Where(transform => transform.GetComponent<BoxCollider>() != null)
            .Select(transform => transform.gameObject)
            .ToArray();

        foreach (GameObject wall in walls)
            ReplaceVisual(wall, visualPrefab);

        EditorSceneManager.SaveScene(scene);
        if (openedForSetup)
            EditorSceneManager.CloseScene(scene, true);

        Debug.Log($"Stage {stageNumber}: themed {walls.Length} wall objects.");
    }

    private static bool IsWall(string objectName)
    {
        if (objectName == VisualName) return false;
        return objectName.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Rail", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Barrier", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Fence", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsInvisibleWall(string objectName) =>
        objectName.StartsWith("InvisibleWall", StringComparison.OrdinalIgnoreCase);

    private static void ReplaceVisual(GameObject wall, GameObject visualPrefab)
    {
        foreach (Renderer renderer in wall.GetComponents<Renderer>())
            renderer.enabled = false;

        Transform previousVisual = wall.transform.Find(VisualName);
        if (previousVisual != null)
            UnityEngine.Object.DestroyImmediate(previousVisual.gameObject);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, wall.scene);
        visual.name = VisualName;
        visual.transform.SetParent(wall.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        FitToRootBounds(wall.transform, visual.transform);
    }

    private static void FitToRootBounds(Transform wall, Transform visual)
    {
        Vector3 originalWallScale = wall.localScale;
        wall.localScale = Vector3.one;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            wall.localScale = originalWallScale;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 localCenter = wall.InverseTransformPoint(bounds.center);
        Vector3 localSize = wall.InverseTransformVector(bounds.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        Vector3 fitScale = new Vector3(
            localSize.x > 0.0001f ? 1f / localSize.x : 1f,
            localSize.y > 0.0001f ? 1f / localSize.y : 1f,
            localSize.z > 0.0001f ? 1f / localSize.z : 1f);

        visual.localScale = fitScale;
        visual.localPosition = -Vector3.Scale(localCenter, fitScale);
        wall.localScale = originalWallScale;
    }
}
