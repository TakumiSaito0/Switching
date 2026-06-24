using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage4Builder
{
    private const string ScenePath = "Assets/Scenes/Stage4.unity";
    private const string StageSelectPath = "Assets/Scenes/StageSelectScene.unity";
    private const string MaterialFolder = "Assets/Generated/Stage4";

    [MenuItem("Tools/Stages/Build Stage 4")]
    public static void BuildStage4()
    {
        EnsureFolders();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Stage4";

        var palette = new StagePalette();
        palette.CreateOrLoad(MaterialFolder);

        CreateLighting();
        CreateCamera();
        CreateEventSystem();
        new GameObject("CircuitManager").AddComponent<CircuitManager>();

        GameObject levelRoot = new GameObject("Stage4_Level");
        BuildPlatforms(levelRoot.transform, palette);
        BuildRailsAndWalls(levelRoot.transform, palette);

        GameObject player = InstantiatePrefab("Assets/Prefabs/Player.prefab", "Player", new Vector3(-9f, 1.1f, -5.5f), Quaternion.Euler(0f, 90f, 0f));
        PlayerController playerController = player.GetComponent<PlayerController>();
        SetSerializedObjectReference(playerController, "wirePrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Wire.prefab"));
        SetSerializedInt(playerController, "startingCircuitCount", 1);
        CreateCircuitCountText(playerController);

        GameObject respawnPoint = new GameObject("RespawnPoint");
        respawnPoint.transform.position = player.transform.position;

        GameObject respawnManager = InstantiatePrefab("Assets/Prefabs/RespawnManager.prefab", "RespawnManager", new Vector3(-9f, 0f, -5.5f), Quaternion.identity);
        RespawnManager respawn = respawnManager.GetComponent<RespawnManager>();
        SetSerializedObjectReference(respawn, "player", player.transform);
        SetSerializedObjectReference(respawn, "respawnPoint", respawnPoint.transform);
        SetSerializedFloat(respawn, "fallY", -8f);

        GameObject box = CreateCube("CarryBox", new Vector3(-7.2f, 0.75f, -4.1f), new Vector3(1f, 1f, 1f), palette.Box);
        box.tag = "Box";
        Rigidbody boxRb = box.AddComponent<Rigidbody>();
        boxRb.mass = 0.85f;
        boxRb.linearDamping = 1.5f;
        boxRb.angularDamping = 1.5f;

        GameObject ladder = InstantiatePrefab("Assets/Prefabs/Ladder.prefab", "Ladder_To_CircuitShelf", new Vector3(-4.6f, 2f, -6.05f), Quaternion.identity);
        ladder.transform.localScale = new Vector3(2f, 3.2f, 0.1f);

        GameObject bonusPickup = InstantiatePrefab("Assets/Prefabs/CircuitPickUp.prefab", "CircuitPickUp_MidShelf", new Vector3(-3.2f, 3.75f, -4.8f), Quaternion.identity);
        bonusPickup.transform.localScale = Vector3.one * 1.2f;

        GameObject elevator = InstantiatePrefab("Assets/Prefabs/Elevator.prefab", "Elevator_To_TopCircuit", new Vector3(2.5f, 0f, -3.8f), Quaternion.identity);
        MoveChildToWorldPosition(elevator, "2F", new Vector3(2.5f, 0.35f, -3.8f));
        SetSerializedFloat(elevator.GetComponentInChildren<Elevator>(), "travelHeight", 5f);

        GameObject doorSwitchCircuit = InstantiatePrefab("Assets/Prefabs/CIrcuit.prefab", "DoorPowerSwitch", Vector3.zero, Quaternion.identity);
        MoveChildToWorldPosition(doorSwitchCircuit, "Switch", new Vector3(-4f, 5.55f, 1f));

        CreateWireRun("Wire_To_FinalDoor", new[]
        {
            new Vector3(-3f, 5.52f, 1f),
            new Vector3(-2f, 5.52f, 1f),
            new Vector3(-1f, 5.52f, 1f),
            new Vector3(1f, 5.52f, 1f),
            new Vector3(2f, 5.52f, 1f),
            new Vector3(3f, 5.52f, 1f),
            new Vector3(4f, 5.52f, 1f)
        });

        GameObject bridgeSwitchCircuit = InstantiatePrefab("Assets/Prefabs/CIrcuit.prefab", "BridgePowerSwitch", Vector3.zero, Quaternion.identity);
        MoveChildToWorldPosition(bridgeSwitchCircuit, "Switch", new Vector3(-4f, 5.55f, -1f));

        CreateWireRun("Wire_To_Bridge", new[]
        {
            new Vector3(-3f, 5.52f, -1f),
            new Vector3(-2f, 5.52f, -1f),
            new Vector3(-1f, 5.52f, -1f),
            new Vector3(1f, 5.52f, -1f),
            new Vector3(2f, 5.52f, -1f),
            new Vector3(3f, 5.52f, -1f),
            new Vector3(4f, 5.52f, -1f)
        });

        GameObject door = InstantiatePrefab("Assets/Prefabs/Door.prefab", "PoweredDoor_Final", new Vector3(5f, 6f, 1f), Quaternion.Euler(0f, 90f, 0f));
        door.transform.localScale = new Vector3(2.2f, 3.2f, 1.1f);
        SetSerializedFloat(door.GetComponent<DoorNode>(), "connectRadius", 1.25f);

        CreatePoweredBridge(new Vector3(5f, 5.52f, -1f), palette);

        GameObject goal = InstantiatePrefab("Assets/Prefabs/ClearPlace.prefab", "ClearPlace_Goal", new Vector3(8.7f, 5.75f, 1f), Quaternion.identity);
        goal.transform.localScale = new Vector3(1.35f, 1.35f, 1.35f);

        InstantiatePrefab("Assets/Prefabs/Rotato Button.prefab", "CameraRotateButtons", Vector3.zero, Quaternion.identity);
        InstantiatePrefab("Assets/Prefabs/PauseMenuCanvas.prefab", "PauseMenuCanvas", Vector3.zero, Quaternion.identity);

        Selection.activeGameObject = levelRoot;
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built Stage4 at " + ScenePath);
    }

    [MenuItem("Tools/Stages/Build Stage 4 And Update Select")]
    public static void BuildStage4AndUpdateStageSelect()
    {
        BuildStage4();
        AddStage4ToStageSelect();
    }

    [MenuItem("Tools/Stages/Add Stage 4 To Stage Select")]
    public static void AddStage4ToStageSelect()
    {
        Scene scene = EditorSceneManager.OpenScene(StageSelectPath, OpenSceneMode.Single);
        GameSceneManager sceneManager = Object.FindFirstObjectByType<GameSceneManager>();
        if (sceneManager == null)
        {
            GameObject managerObject = new GameObject("GameSceneManager");
            sceneManager = managerObject.AddComponent<GameSceneManager>();
        }

        Button stage3Button = FindButtonCalling("LoadStage3");
        if (stage3Button == null)
        {
            Debug.LogWarning("Stage3 button was not found. Stage4 scene was built, but the select button was not added.");
            return;
        }

        Button stage4Button = FindButtonCalling("LoadStage4");
        if (stage4Button == null)
        {
            stage4Button = Object.Instantiate(stage3Button, stage3Button.transform.parent);
            stage4Button.name = "Stage4Button";
        }

        RectTransform stage3Rect = stage3Button.GetComponent<RectTransform>();
        RectTransform stage4Rect = stage4Button.GetComponent<RectTransform>();
        if (stage3Rect != null && stage4Rect != null)
        {
            stage4Rect.anchorMin = stage3Rect.anchorMin;
            stage4Rect.anchorMax = stage3Rect.anchorMax;
            stage4Rect.sizeDelta = stage3Rect.sizeDelta;
            stage4Rect.anchoredPosition = stage3Rect.anchoredPosition + new Vector2(0f, -120f);
        }

        SetButtonLabel(stage4Button, "Stage4");
        stage4Button.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(stage4Button.onClick, sceneManager.LoadStage4);
        EditorUtility.SetDirty(stage4Button);
        EditorUtility.SetDirty(sceneManager);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Added Stage4 button to StageSelectScene.");
    }

    private static void BuildPlatforms(Transform parent, StagePalette palette)
    {
        CreateCube("Lower_StartDeck", new Vector3(-6.8f, 0f, -4.8f), new Vector3(8.5f, 0.35f, 5.2f), palette.Floor, parent).tag = "Ground";
        CreateCube("Lower_ElevatorWalk", new Vector3(-0.8f, 0f, -3.8f), new Vector3(4.8f, 0.32f, 2f), palette.Floor, parent).tag = "Ground";
        CreateCube("Elevator_BottomPad", new Vector3(2.5f, 0f, -3.8f), new Vector3(3.8f, 0.35f, 3.8f), palette.Floor, parent).tag = "Ground";
        CreateCube("CircuitShelf", new Vector3(-3.4f, 3f, -4.8f), new Vector3(3.8f, 0.32f, 2.4f), palette.Shelf, parent).tag = "Ground";
        CreateCube("Upper_CircuitDeck", new Vector3(-0.5f, 5f, 1f), new Vector3(9f, 0.35f, 4f), palette.Floor, parent).tag = "Ground";
        CreateCube("Upper_ElevatorLanding", new Vector3(2.5f, 5f, -2.1f), new Vector3(4.2f, 0.35f, 2.2f), palette.Floor, parent).tag = "Ground";
        CreateCube("GoalDeck", new Vector3(8.2f, 5f, 1f), new Vector3(4.6f, 0.35f, 4f), palette.GoalDeck, parent).tag = "Ground";
        CreateCube("BoxRamp", new Vector3(-3.1f, 0.65f, -3.85f), new Vector3(3.2f, 0.28f, 1.7f), palette.Ramp, parent).tag = "Ground";
        parent.Find("BoxRamp").rotation = Quaternion.Euler(0f, 0f, -18f);
    }

    private static void BuildRailsAndWalls(Transform parent, StagePalette palette)
    {
        CreateCube("Lower_BackWall", new Vector3(-3.8f, 1.5f, -7.55f), new Vector3(12f, 3f, 0.35f), palette.Wall, parent);
        CreateCube("Lower_LeftWall", new Vector3(-11.2f, 1.5f, -4.8f), new Vector3(0.35f, 3f, 5.4f), palette.Wall, parent);
        CreateCube("Shelf_BackRail", new Vector3(-3.4f, 4f, -6.1f), new Vector3(4f, 1.7f, 0.25f), palette.Rail, parent);
        CreateCube("Upper_BackRail", new Vector3(1.8f, 6f, -1.15f), new Vector3(12f, 1.7f, 0.25f), palette.Rail, parent);
        CreateCube("Upper_FrontRail_Left", new Vector3(-3.2f, 6f, 3.15f), new Vector3(3.5f, 1.7f, 0.25f), palette.Rail, parent);
        CreateCube("Goal_RightRail", new Vector3(10.5f, 6f, 1f), new Vector3(0.25f, 1.7f, 4f), palette.Rail, parent);
        CreateCube("Door_Frame_Left", new Vector3(5f, 6.5f, -1.05f), new Vector3(0.35f, 3f, 0.35f), palette.Wall, parent);
        CreateCube("Door_Frame_Right", new Vector3(5f, 6.5f, 3.05f), new Vector3(0.35f, 3f, 0.35f), palette.Wall, parent);
    }

    private static void CreateWireRun(string wireName, IEnumerable<Vector3> positions)
    {
        foreach (Vector3 position in positions)
        {
            GameObject wire = InstantiatePrefab("Assets/Prefabs/Wire.prefab", wireName, position, Quaternion.identity);
            wire.transform.localScale = new Vector3(0.04f, 0.12f, 0.12f);
        }
    }

    private static void CreatePoweredBridge(Vector3 nodePosition, StagePalette palette)
    {
        GameObject bridgeNode = new GameObject("PoweredBridge_FinalGap");
        bridgeNode.transform.position = nodePosition;

        PoweredBridgeNode bridge = bridgeNode.AddComponent<PoweredBridgeNode>();
        SetSerializedFloat(bridge, "connectRadius", 1.25f);

        GameObject bridgeBody = CreateCube(
            "BridgeBody",
            nodePosition,
            new Vector3(2.8f, 0.25f, 2f),
            palette.EnergyBridge,
            bridgeNode.transform
        );
        bridgeBody.transform.localPosition = new Vector3(1.05f, -3.2f, 0f);

        SetSerializedObjectReference(bridge, "bridgeBody", bridgeBody.transform);
        SetSerializedVector3(bridge, "poweredLocalPosition", new Vector3(1.05f, -0.35f, 0f));
        SetSerializedVector3(bridge, "unpoweredLocalPosition", new Vector3(1.05f, -3.2f, 0f));
        SetSerializedFloat(bridge, "moveSpeed", 3.5f);
    }

    private static Button FindButtonCalling(string methodName)
    {
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == methodName)
                {
                    return button;
                }
            }
        }

        return null;
    }

    private static void SetButtonLabel(Button button, string label)
    {
        TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            tmpText.text = label;
            EditorUtility.SetDirty(tmpText);
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = label;
            EditorUtility.SetDirty(legacyText);
        }
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
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
        SetSerializedFloat(manager, "height", 8.5f);
        SetSerializedFloat(manager, "distance", 11f);
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

    private static void SetSerializedVector3(Object target, string propertyName, Vector3 value)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.vector3Value = value;
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
            AssetDatabase.CreateFolder("Assets/Generated", "Stage4");
        }
    }

    private sealed class StagePalette
    {
        public Material Floor;
        public Material Wall;
        public Material Rail;
        public Material Box;
        public Material GoalDeck;
        public Material Shelf;
        public Material Ramp;
        public Material EnergyBridge;

        public void CreateOrLoad(string folder)
        {
            Floor = GetMaterial(folder, "Stage4_Floor.mat", new Color(0.33f, 0.35f, 0.39f));
            Wall = GetMaterial(folder, "Stage4_Wall.mat", new Color(0.20f, 0.23f, 0.25f));
            Rail = GetMaterial(folder, "Stage4_Rail.mat", new Color(0.70f, 0.62f, 0.34f));
            Box = GetMaterial(folder, "Stage4_Box.mat", new Color(0.70f, 0.34f, 0.18f));
            GoalDeck = GetMaterial(folder, "Stage4_GoalDeck.mat", new Color(0.16f, 0.47f, 0.44f));
            Shelf = GetMaterial(folder, "Stage4_Shelf.mat", new Color(0.40f, 0.32f, 0.46f));
            Ramp = GetMaterial(folder, "Stage4_Ramp.mat", new Color(0.45f, 0.42f, 0.31f));
            EnergyBridge = GetMaterial(folder, "Stage4_EnergyBridge.mat", new Color(0.18f, 0.24f, 0.28f));
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
