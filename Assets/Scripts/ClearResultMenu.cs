using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClearResultMenu : MonoBehaviour
{
    private const string DefaultStageSelectSceneName = "StageSelectScene";

    private string nextStageName;
    private string stageSelectSceneName;
    private TMP_FontAsset menuFont;
    private string titleText;
    private string nextStageButtonText;
    private string stageSelectButtonText;
    private Color panelColor;
    private Color buttonColor;
    private Color titleColor;
    private Color buttonTextColor;

    public static void Show(
        string explicitNextStageName,
        string stageSelectSceneName,
        TMP_FontAsset menuFont,
        string titleText,
        string nextStageButtonText,
        string stageSelectButtonText,
        Color panelColor,
        Color buttonColor,
        Color titleColor,
        Color buttonTextColor)
    {
        if (FindFirstObjectByType<ClearResultMenu>() != null)
        {
            return;
        }

        var menuObject = new GameObject("ClearResultMenu");
        var menu = menuObject.AddComponent<ClearResultMenu>();
        menu.nextStageName = string.IsNullOrWhiteSpace(explicitNextStageName)
            ? GetNextStageName(SceneManager.GetActiveScene().name)
            : explicitNextStageName;
        menu.stageSelectSceneName = string.IsNullOrWhiteSpace(stageSelectSceneName)
            ? DefaultStageSelectSceneName
            : stageSelectSceneName;
        menu.menuFont = menuFont;
        menu.titleText = string.IsNullOrWhiteSpace(titleText) ? "CLEAR" : titleText;
        menu.nextStageButtonText = string.IsNullOrWhiteSpace(nextStageButtonText) ? "NEXT STAGE" : nextStageButtonText;
        menu.stageSelectButtonText = string.IsNullOrWhiteSpace(stageSelectButtonText) ? "STAGE SELECT" : stageSelectButtonText;
        menu.panelColor = panelColor;
        menu.buttonColor = buttonColor;
        menu.titleColor = titleColor;
        menu.buttonTextColor = buttonTextColor;
        menu.Build();

        Time.timeScale = 0f;
    }

    public void LoadNextStage()
    {
        if (string.IsNullOrWhiteSpace(nextStageName))
        {
            LoadStageSelect();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextStageName);
    }

    public void LoadStageSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        var overlay = CreatePanel("Overlay", transform, MakeColor(0f, 0f, 0f, 0.72f));
        Stretch(overlay.rectTransform);

        var panel = CreatePanel("Panel", overlay.transform, panelColor);
        panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        panel.rectTransform.anchoredPosition = Vector2.zero;
        panel.rectTransform.sizeDelta = new Vector2(520f, 300f);

        CreateText("Title", panel.transform, titleText, 44f, new Vector2(0f, 86f), new Vector2(420f, 70f), titleColor);

        bool hasNextStage = !string.IsNullOrWhiteSpace(nextStageName) && Application.CanStreamedLevelBeLoaded(nextStageName);
        if (hasNextStage)
        {
            CreateButton("NextStageButton", panel.transform, nextStageButtonText, new Vector2(0f, -18f), LoadNextStage);
            CreateButton("StageSelectButton", panel.transform, stageSelectButtonText, new Vector2(0f, -98f), LoadStageSelect);
        }
        else
        {
            CreateButton("StageSelectButton", panel.transform, stageSelectButtonText, new Vector2(0f, -42f), LoadStageSelect);
        }
    }

    private static string GetNextStageName(string currentSceneName)
    {
        var match = Regex.Match(currentSceneName, @"^Stage(\d+)$");
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out int stageNumber))
        {
            return string.Empty;
        }

        return $"Stage{stageNumber + 1}";
    }

    private static Image CreatePanel(string name, Transform parent, Color color)
    {
        var panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);
        var rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.localScale = Vector3.one;

        var image = panelObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private void CreateText(string name, Transform parent, string text, float fontSize, Vector2 position, Vector2 size, Color color)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        var rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        var label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        if (menuFont != null)
        {
            label.font = menuFont;
        }
    }

    private void CreateButton(string name, Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        var rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(280f, 54f);

        var image = buttonObject.AddComponent<Image>();
        image.color = buttonColor;

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        CreateText("Text", buttonObject.transform, text, 24f, Vector2.zero, rectTransform.sizeDelta, buttonTextColor);
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private static Color MakeColor(float r, float g, float b, float a)
    {
        return new Color(r, g, b, a);
    }
}
