using UnityEditor;
using UnityEngine;

/// <summary>Converts the Low Poly Characters Mega Pack's built-in-pipeline materials to URP.</summary>
[InitializeOnLoad]
public static class LowPolyCharactersUrpMaterialConverter
{
    private const string PackPath = "Assets/PrivateFolder/Low Poly Characters Mega Pack";
    private const string ConversionKey = "LowPolyCharactersMegaPack_URP_Conversion_v1";

    static LowPolyCharactersUrpMaterialConverter()
    {
        // Imported store assets use the built-in Standard shader and render pink in URP.
        // Run once after scripts compile; the menu item remains available for re-imports.
        EditorApplication.delayCall += ConvertOnce;
    }

    private static void ConvertOnce()
    {
        if (SessionState.GetBool(ConversionKey, false)) return;
        SessionState.SetBool(ConversionKey, true);
        ConvertMaterials();
    }

    [MenuItem("Tools/Low Poly Characters/Convert Materials to URP")]
    public static void ConvertMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("URP Lit shader was not found. Check that Universal RP is installed and active.");
            return;
        }

        int converted = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { PackPath }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null || material.shader.name != "Standard") continue;

            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            Vector2 textureScale = material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
            Vector2 textureOffset = material.HasProperty("_MainTex") ? material.GetTextureOffset("_MainTex") : Vector2.zero;
            float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
            float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.5f;
            float mode = material.HasProperty("_Mode") ? material.GetFloat("_Mode") : 0f;
            bool emission = material.IsKeywordEnabled("_EMISSION");
            Color emissionColor = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
            Texture emissionMap = material.HasProperty("_EmissionMap") ? material.GetTexture("_EmissionMap") : null;

            Undo.RecordObject(material, "Convert Low Poly Character material to URP");
            material.shader = urpLit;
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", mainTexture);
            material.SetTextureScale("_BaseMap", textureScale);
            material.SetTextureOffset("_BaseMap", textureOffset);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);

            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
                material.SetTexture("_EmissionMap", emissionMap);
            }

            // Standard Fade/Transparent maps to URP Transparent.
            if (mode >= 2f)
            {
                material.SetFloat("_Surface", 1f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            EditorUtility.SetDirty(material);
            converted++;
        }

        if (converted > 0) AssetDatabase.SaveAssets();
        Debug.Log($"Low Poly Characters Mega Pack: converted {converted} material(s) to URP Lit.");
    }
}
