using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage3Builder
{
    private const string ScenePath = "Assets/Scenes/Stage3.unity";
    private const string MaterialFolder = "Assets/Generated/Stage3";

    [MenuItem("Tools/Stages/Build Stage 3")]
    public static void BuildStage3()
    {
        EnsureFolders();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Stage3";

        var palette = new StagePalette();
        palette.CreateOrLoad(MaterialFolder);

        CreateLighting();
        CreateCamera();
        CreateEventSystem();
        GameObject circuitManager = new GameObject("CircuitManager");
        circuitManager.AddComponent<CircuitManager>();

        GameObject levelRoot = new GameObject("Stage3_Level");
        BuildPlatforms(levelRoot.transform, palette);
        BuildRailsAndWalls(levelRoot.transform, palette);

        GameObject player = InstantiatePrefab("Assets/Prefabs/Player.prefab", "Player", new Vector3(-8f, 1.1f, -6f), Quaternion.Euler(0f, 90f, 0f));
        SetSerializedObjectReference(player.GetComponent<PlayerController>(), "wirePrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Wire.prefab"));
        SetSerializedInt(player.GetComponent<PlayerController>(), "startingCircuitCount", 1);
        CreateCircuitCountText(player.GetComponent<PlayerController>());

        GameObject respawnPoint = new GameObject("RespawnPoint");
        respawnPoint.transform.position = player.transform.position;

        GameObject respawnManager = InstantiatePrefab("Assets/Prefabs/RespawnManager.prefab", "RespawnManager", new Vector3(-8f, 0f, -6f), Quaternion.identity);
        RespawnManager respawn = respawnManager.GetComponent<RespawnManager>();
        SetSerializedObjectReference(respawn, "player", player.transform);
        SetSerializedObjectReference(respawn, "respawnPoint", respawnPoint.transform);
        SetSerializedFloat(respawn, "fallY", -8f);

        GameObject ladder = InstantiatePrefab("Assets/Prefabs/Ladder.prefab", "Ladder_To_MidDeck", new Vector3(-2.5f, 2.15f, -6.05f), Quaternion.identity);
        ladder.transform.localScale = new Vector3(2.1f, 3.6f, 0.1f);

        GameObject elevator = InstantiatePrefab("Assets/Prefabs/Elevator.prefab", "Elevator_To_UpperDeck", new Vector3(6.6f, 0f, -2f), Quaternion.identity);
        MoveChildToWorldPosition(elevator, "2F", new Vector3(6.6f, 0.35f, -2f));
        SetSerializedFloat(elevator.GetComponentInChildren<Elevator>(), "travelHeight", 4f);

        GameObject circuitPickup = InstantiatePrefab("Assets/Prefabs/CircuitPickUp.prefab", "CircuitPickUp_Bonus", new Vector3(0f, 4.9f, -5.2f), Quaternion.identity);
        circuitPickup.transform.localScale = Vector3.one * 1.2f;

        GameObject switchCircuit = InstantiatePrefab("Assets/Prefabs/CIrcuit.prefab", "DoorPowerSwitch", Vector3.zero, Quaternion.identity);
        MoveChildToWorldPosition(switchCircuit, "Switch", new Vector3(1f, 4.55f, 1f));

        CreateWireRun(new[]
        {
            new Vector3(2f, 4.52f, 1f),
            new Vector3(3f, 4.52f, 1f),
            new Vector3(4f, 4.52f, 1f)
        });

        GameObject door = InstantiatePrefab("Assets/Prefabs/Door.prefab", "PoweredDoor_To_Goal", new Vector3(5f, 5f, 1f), Quaternion.Euler(0f, 90f, 0f));
        door.transform.localScale = new Vector3(2.2f, 3.2f, 1.1f);
        SetSerializedFloat(door.GetComponent<DoorNode>(), "connectRadius", 1.25f);

        InstantiatePrefab("Assets/Prefabs/Box.prefab", "CarryBox", new Vector3(-6.5f, 0.27f, -5.2f), Quaternion.identity);

        GameObject goal = InstantiatePrefab("Assets/Prefabs/ClearPlace.prefab", "ClearPlace_Goal", new Vector3(8.4f, 4.75f, 3f), Quaternion.identity);
        goal.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);

        InstantiatePrefab("Assets/Prefabs/Rotato Button.prefab", "CameraRotateButtons", Vector3.zero, Quaternion.identity);
        InstantiatePrefab("Assets/Prefabs/PauseMenuCanvas.prefab", "PauseMenuCanvas", Vector3.zero, Quaternion.identity);

        Selection.activeGameObject = levelRoot;
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built Stage3 at " + ScenePath);
    }

    private static void BuildPlatforms(Transform parent, StagePalette palette)
    {
        CreateCube("Lower_StartDeck", new Vector3(-5f, 0f, -5.5f), new Vector3(9f, 0.35f, 5f), palette.Floor, parent).tag = "Ground";
        CreateCube("Mid_LadderDeck", new Vector3(0f, 3f, -5.5f), new Vector3(6f, 0.35f, 4.5f), palette.Floor, parent).tag = "Ground";
        CreateCube("Elevator_BottomPad", new Vector3(6.6f, 0f, -2f), new Vector3(4f, 0.35f, 4f), palette.Floor, parent).tag = "Ground";
        CreateCube("Upper_CircuitDeck", new Vector3(2.5f, 4f, 1f), new Vector3(8f, 0.35f, 4f), palette.Floor, parent).tag = "Ground";
        CreateCube("GoalDeck", new Vector3(8f, 4f, 3f), new Vector3(4.5f, 0.35f, 4f), palette.GoalDeck, parent).tag = "Ground";
        CreateCube("Bridge_To_Elevator", new Vector3(3.2f, 3f, -3.8f), new Vector3(4.8f, 0.3f, 1.6f), palette.Floor, parent).tag = "Ground";
        CreateCube("Upper_Landing_Bridge", new Vector3(6.5f, 4f, -0.2f), new Vector3(3f, 0.3f, 1.6f), palette.Floor, parent).tag = "Ground";
    }

    private static void BuildRailsAndWalls(Transform parent, StagePalette palette)
    {
        CreateCube("Back_SafetyWall", new Vector3(1f, 1.6f, -8.2f), new Vector3(18f, 3.2f, 0.35f), palette.Wall, parent);
        CreateCube("Left_SafetyWall", new Vector3(-9.7f, 1.6f, -5.3f), new Vector3(0.35f, 3.2f, 5.6f), palette.Wall, parent);
        CreateCube("Upper_BackRail", new Vector3(4f, 5f, -1.2f), new Vector3(10f, 1.8f, 0.25f), palette.Rail, parent);
        CreateCube("Goal_RightRail", new Vector3(10.5f, 5f, 3f), new Vector3(0.25f, 1.8f, 4f), palette.Rail, parent);
        CreateCube("Door_Frame_Left", new Vector3(5f, 5.5f, -1.05f), new Vector3(0.35f, 3f, 0.35f), palette.Wall, parent);
        CreateCube("Door_Frame_Right", new Vector3(5f, 5.5f, 3.05f), new Vector3(0.35f, 3f, 0.35f), palette.Wall, parent);
    }

    private static void CreateWireRun(IEnumerable<Vector3> positions)
    {
        foreach (Vector3 position in positions)
        {
            GameObject wire = InstantiatePrefab("Assets/Prefabs/Wire.prefab", "Wire_To_Door", position, Quaternion.identity);
            wire.transform.localScale = new Vector3(0.04f, 0.12f, 0.12f);
        }
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.9f;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 1000f;
        cameraObject.AddComponent<AudioListener>();
        CameraManager manager = cameraObject.AddComponent<CameraManager>();
        SetSerializedFloat(manager, "height", 8f);
        SetSerializedFloat(manager, "distance", 10f);
        SetSerializedFloat(manager, "tiltAngle", 58f);
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static void CreateCircuitCountText(PlayerController player)
    {
        GameObject canvasObject = new GameObject("Canvas");
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject textObject = new GameObject("CircuitCountText");
        textObject.layer = LayerMask.NameToLayer("UI");
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(-791f, 456f);
        rectTransform.sizeDelta = new Vector2(200f, 50f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = "Circuit";
        text.color = Color.white;
        text.fontSize = 36f;

        CircuitCountText circuitCountText = textObject.AddComponent<CircuitCountText>();
        SetSerializedObjectReference(circuitCountText, "player", player);
        SetSerializedObjectReference(circuitCountText, "countText", text);
        SetSerializedString(circuitCountText, "label", "Circuit");
    }

    private static GameObject InstantiatePrefab(string path, string name, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : new GameObject(name);

        instance.name = name;
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, true);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        return cube;
    }

    private static void MoveChildToWorldPosition(GameObject root, string childName, Vector3 targetWorldPosition)
    {
        Transform child = root.transform.Find(childName);
        if (child == null)
        {
            return;
        }

        root.transform.position += targetWorldPosition - child.position;
    }

    private static void SetSerializedObjectReference(Object target, string propertyName, Object value)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static void SetSerializedFloat(Object target, string propertyName, float value)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static void SetSerializedInt(Object target, string propertyName, int value)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static void SetSerializedString(Object target, string propertyName, string value)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.stringValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == scenePath)
            {
                scene.enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Generated", "Stage3");
        }
    }

    private sealed class StagePalette
    {
        public Material Floor;
        public Material Wall;
        public Material Rail;
        public Material Box;
        public Material GoalDeck;

        public void CreateOrLoad(string folder)
        {
            Floor = GetMaterial(folder, "Stage3_Floor.mat", new Color(0.32f, 0.36f, 0.34f));
            Wall = GetMaterial(folder, "Stage3_Wall.mat", new Color(0.21f, 0.25f, 0.27f));
            Rail = GetMaterial(folder, "Stage3_Rail.mat", new Color(0.75f, 0.67f, 0.38f));
            Box = GetMaterial(folder, "Stage3_Box.mat", new Color(0.72f, 0.36f, 0.22f));
            GoalDeck = GetMaterial(folder, "Stage3_GoalDeck.mat", new Color(0.18f, 0.48f, 0.46f));
        }

        private static Material GetMaterial(string folder, string fileName, Color color)
        {
            string path = folder + "/" + fileName;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
