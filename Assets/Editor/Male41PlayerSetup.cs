using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class Male41PlayerSetup
{
    private const string PlayerPath = "Assets/Prefabs/Player.prefab";
    private const string CharacterPath = "Assets/Low Poly Characters Mega Pack/Assets/Prefabs/PreMade/Male/Male_41.prefab";
    private const string OutputFolder = "Assets/Generated/Player";
    private const string ControllerPath = OutputFolder + "/Male41Player.controller";
    private const string CarryMaskPath = OutputFolder + "/Male41UpperBody.mask";
    private const string SessionKey = "Male41PlayerSetup_v1";

    static Male41PlayerSetup() => EditorApplication.delayCall += SetupOnce;

    private static void SetupOnce()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        Setup();
    }

    [MenuItem("Tools/Player/Use Male 41 With Animations")]
    public static void Setup()
    {
        GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
        GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPath);
        if (characterPrefab == null || playerRoot == null)
        {
            Debug.LogError("Male 41 or Player.prefab could not be loaded.");
            return;
        }

        EnsureFolder("Assets/Generated");
        EnsureFolder(OutputFolder);
        AnimatorController controller = BuildController(characterPrefab);

        // Replace only the old primitive visuals; gameplay components and collider remain intact.
        foreach (MeshRenderer renderer in playerRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.transform == playerRoot.transform || renderer.gameObject.name == "Cube")
                Object.DestroyImmediate(renderer.gameObject == playerRoot ? renderer : renderer.gameObject);
        }

        Transform oldVisual = playerRoot.transform.Find("Female_18_Visual");
        if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);
        oldVisual = playerRoot.transform.Find("Male_41_Visual");
        if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
        visual.name = "Male_41_Visual";
        visual.transform.SetParent(playerRoot.transform, false);
        visual.transform.localPosition = new Vector3(0f, -1f, 0f);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
        {
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
        }

        if (playerRoot.GetComponent<PlayerCharacterAnimator>() == null)
            playerRoot.AddComponent<PlayerCharacterAnimator>();

        PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPath);
        PrefabUtility.UnloadPrefabContents(playerRoot);
        AssetDatabase.SaveAssets();
        Debug.Log("Player now uses Male 41. Existing gameplay and animation behavior was preserved.");
    }

    private static AnimatorController BuildController(GameObject characterPrefab)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AddState(machine, "Idle", Clip("Assets/Low Poly Characters Mega Pack/Assets/Animations/Unarmed/Idle_1.FBX"), true, 1f);
        AddState(machine, "Walk", Clip("Assets/Low Poly Characters Mega Pack/Assets/Animations/Unarmed/Walk.FBX"), true, 1.25f);
        AddState(machine, "Air", Clip("Assets/Low Poly Characters Mega Pack/Assets/Animations/02_Jump/Relaxed/Fall_NoRootMotion.FBX"), true, 1f);
        AddState(machine, "Climb", Clip("Assets/Low Poly Characters Mega Pack/Assets/Animations/Unarmed/Walk.FBX"), true, 0.8f);
        AddState(machine, "Action", Clip("Assets/Low Poly Characters Mega Pack/Assets/Animations/Unarmed/Attack_1.FBX"), false, 1f);
        machine.defaultState = machine.states.First(x => x.state.name == "Idle").state;

        AvatarMask carryMask = BuildUpperBodyMask(characterPrefab);
        AnimatorStateMachine carryMachine = new AnimatorStateMachine { name = "Carry Layer" };
        AssetDatabase.AddObjectToAsset(carryMachine, controller);
        AnimatorState carry = carryMachine.AddState("Carry");
        carry.motion = Clip("Assets/Low Poly Characters Mega Pack/Assets/Animations/Bow/Attack_Idle.FBX");
        carryMachine.defaultState = carry;
        controller.AddLayer(new AnimatorControllerLayer
        {
            name = "Carry Layer",
            defaultWeight = 0f,
            blendingMode = AnimatorLayerBlendingMode.Override,
            avatarMask = carryMask,
            stateMachine = carryMachine
        });
        return controller;
    }

    private static AvatarMask BuildUpperBodyMask(GameObject characterPrefab)
    {
        AssetDatabase.DeleteAsset(CarryMaskPath);
        AvatarMask mask = new AvatarMask { name = "Male41UpperBody" };
        mask.AddTransformPath(characterPrefab.transform, true);
        for (int i = 0; i < mask.transformCount; i++)
        {
            string path = mask.GetTransformPath(i);
            bool upperBody = string.IsNullOrEmpty(path)
                || path.Contains("Rig_Spine")
                || path.Contains("Rig_Chest")
                || path.Contains("Rig_Neck")
                || path.Contains("Rig_Head")
                || path.Contains("Rig_Collarbone")
                || path.Contains("Rig_Upper_Arm")
                || path.Contains("Rig_Lower_Arm")
                || path.Contains("Rig_Hand")
                || path.Contains("Hand_Dummy");
            mask.SetTransformActive(i, upperBody);
        }
        AssetDatabase.CreateAsset(mask, CarryMaskPath);
        return mask;
    }

    private static void AddState(AnimatorStateMachine machine, string name, AnimationClip clip, bool loop, float speed)
    {
        if (clip == null) Debug.LogWarning($"Animation clip for {name} was not found.");
        AnimatorState state = machine.AddState(name);
        state.motion = clip;
        state.speed = speed;
        if (clip != null)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }
    }

    private static AnimationClip Clip(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
