using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DoorVisualSetup
{
    private const string DoorPrefabPath = "Assets/Prefabs/Door.prefab";
    private const string VisualPrefabPath = "Assets/Off Axis Studios/Polyworks/Prefabs/MaterialsOnly/Dungeon/Prop_Fantasy_Dungeon_Door_Wooden_Metal_01.prefab";
    private const string VisualName = "Polyworks_Wooden_Metal_Hinged_Door";
    private const string PreviousVisualName = "Polyworks_Industrial_Hinged_Door";
    private const string SessionKey = "DoorVisualSetup_WoodenMetalHinged_v2";

    static DoorVisualSetup() => EditorApplication.delayCall += ApplyOnce;

    private static void ApplyOnce()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        Apply();
    }

    [MenuItem("Tools/Door/Use Industrial Hinged Door")]
    public static void Apply()
    {
        GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
        GameObject door = PrefabUtility.LoadPrefabContents(DoorPrefabPath);
        if (visualPrefab == null || door == null)
            throw new MissingReferenceException("Door prefab or Polyworks visual could not be loaded.");

        MeshRenderer oldRenderer = door.GetComponent<MeshRenderer>();
        if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
        MeshFilter oldFilter = door.GetComponent<MeshFilter>();
        if (oldFilter != null) Object.DestroyImmediate(oldFilter);

        Transform previous = door.transform.Find(VisualName);
        if (previous != null) Object.DestroyImmediate(previous.gameObject);
        Transform previousIndustrial = door.transform.Find(PreviousVisualName);
        if (previousIndustrial != null) Object.DestroyImmediate(previousIndustrial.gameObject);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
        visual.name = VisualName;
        visual.transform.SetParent(door.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        FitToUnitBounds(door.transform, visual.transform);

        DoorNode doorNode = door.GetComponent<DoorNode>();
        Collider rootCollider = door.GetComponent<Collider>();
        SerializedObject serializedDoor = new SerializedObject(doorNode);
        serializedDoor.FindProperty("doorBody").objectReferenceValue = visual.transform;
        serializedDoor.FindProperty("blockingCollider").objectReferenceValue = rootCollider;
        serializedDoor.FindProperty("disableColliderWhenOpen").boolValue = true;
        serializedDoor.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(door, DoorPrefabPath);
        PrefabUtility.UnloadPrefabContents(door);
        AssetDatabase.SaveAssets();
        Debug.Log("Door.prefab now uses the Polyworks wooden and metal single hinged door.");
    }

    private static void FitToUnitBounds(Transform door, Transform visual)
    {
        Vector3 originalDoorScale = door.localScale;
        door.localScale = Vector3.one;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            door.localScale = originalDoorScale;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 localCenter = door.InverseTransformPoint(bounds.center);
        Vector3 localSize = door.InverseTransformVector(bounds.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        Vector3 fitScale = new Vector3(
            localSize.x > 0.0001f ? 1f / localSize.x : 1f,
            localSize.y > 0.0001f ? 1f / localSize.y : 1f,
            localSize.z > 0.0001f ? 1f / localSize.z : 1f);

        visual.localScale = fitScale;
        visual.localPosition = -Vector3.Scale(localCenter, fitScale);
        door.localScale = originalDoorScale;
    }
}
