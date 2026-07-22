using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class PolyworksUrpMaterialConverter
{
    private const string RootPath = "Assets/PrivateFolder/Off Axis Studios/Polyworks";

    [MenuItem("Tools/Polyworks/Convert All Materials to URP")]
    public static void ConvertMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("URP Lit shader was not found.");
            return;
        }

        int converted = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { RootPath }))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null || material.shader == null
                    || material.shader.name.StartsWith("Universal Render Pipeline/")) continue;

                Color color = ReadColor(material);
                Texture texture = ReadTexture(material);
                Vector2 scale = ReadTextureScale(material);
                Vector2 offset = ReadTextureOffset(material);
                float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
                float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.35f;
                float oldMode = material.HasProperty("_Mode") ? material.GetFloat("_Mode") : 0f;
                int oldQueue = material.renderQueue;
                bool transparent = oldMode >= 2f || oldQueue >= (int)RenderQueue.Transparent;
                bool cutout = oldMode == 1f || oldQueue == (int)RenderQueue.AlphaTest;

                material.shader = urpLit;
                material.SetColor("_BaseColor", color);
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", scale);
                material.SetTextureOffset("_BaseMap", offset);
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Smoothness", smoothness);

                if (transparent)
                {
                    material.SetFloat("_Surface", 1f);
                    material.SetFloat("_ZWrite", 0f);
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.renderQueue = (int)RenderQueue.Transparent;
                }
                else if (cutout)
                {
                    material.SetFloat("_AlphaClip", 1f);
                    material.SetFloat("_Cutoff", 0.5f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                }
                else
                {
                    material.renderQueue = -1;
                }

                EditorUtility.SetDirty(material);
                converted++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Polyworks: converted {converted} material(s) to URP Lit.");
    }

    private static Color ReadColor(Material material)
    {
        if (material.HasProperty("_Color")) return material.GetColor("_Color");
        if (material.HasProperty("_Tint")) return material.GetColor("_Tint");
        if (material.HasProperty("_Color1")) return material.GetColor("_Color1");
        return Color.white;
    }

    private static Texture ReadTexture(Material material)
    {
        if (material.HasProperty("_MainTex")) return material.GetTexture("_MainTex");
        if (material.HasProperty("_BaseMap")) return material.GetTexture("_BaseMap");
        return null;
    }

    private static Vector2 ReadTextureScale(Material material) =>
        material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;

    private static Vector2 ReadTextureOffset(Material material) =>
        material.HasProperty("_MainTex") ? material.GetTextureOffset("_MainTex") : Vector2.zero;
}
