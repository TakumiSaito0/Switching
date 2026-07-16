using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BoxPrefabSetup
{
    private const string BoxPrefabPath = "Assets/Prefabs/Box.prefab";

    [MenuItem("Tools/Stages/Replace Boxes With Prefab")]
    public static void ReplaceBoxesWithPrefab()
    {
        GameObject boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoxPrefabPath);
        if (boxPrefab == null)
        {
            throw new MissingReferenceException("Box prefab not found: " + BoxPrefabPath);
        }

        for (int stageNumber = 1; stageNumber <= 5; stageNumber++)
        {
            string scenePath = $"Assets/Scenes/Stage{stageNumber}.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject[] oldBoxes = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject)
                .Where(gameObject => gameObject.CompareTag("Box"))
                .Distinct()
                .ToArray();

            foreach (GameObject oldBox in oldBoxes)
            {
                ReplaceBox(oldBox, boxPrefab);
            }

            // Stage 2's source Box was removed while creating Box.prefab.
            if (stageNumber == 2 && oldBoxes.Length == 0)
            {
                GameObject restoredBox = (GameObject)PrefabUtility.InstantiatePrefab(boxPrefab, scene);
                restoredBox.name = "Box";
                restoredBox.transform.position = new Vector3(6.28f, 0.02f, -0.63f);
            }

            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Replaced stage Boxes with Assets/Prefabs/Box.prefab.");
    }

    private static void ReplaceBox(GameObject oldBox, GameObject boxPrefab)
    {
        Transform oldTransform = oldBox.transform;
        Transform oldParent = oldTransform.parent;
        int oldSiblingIndex = oldTransform.GetSiblingIndex();
        Vector3 oldPosition = oldTransform.position;
        Quaternion oldRotation = oldTransform.rotation;
        float oldBottom = GetBottom(oldBox);

        GameObject newBox = (GameObject)PrefabUtility.InstantiatePrefab(boxPrefab, oldBox.scene);
        newBox.name = oldBox.name;
        newBox.transform.SetParent(oldParent, true);
        newBox.transform.SetSiblingIndex(oldSiblingIndex);
        newBox.transform.SetPositionAndRotation(oldPosition, oldRotation);

        Physics.SyncTransforms();
        float newBottom = GetBottom(newBox);
        newBox.transform.position += Vector3.up * (oldBottom - newBottom);

        Object.DestroyImmediate(oldBox);
    }

    private static float GetBottom(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        return colliders.Length == 0
            ? target.transform.position.y
            : colliders.Min(collider => collider.bounds.min.y);
    }
}
