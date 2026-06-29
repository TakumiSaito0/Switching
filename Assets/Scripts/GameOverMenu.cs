using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour
{
    private const string DefaultStageSelectSceneName = "StageSelectScene";

    private string stageSelectSceneName;

    public static void Show(string stageSelectSceneName = DefaultStageSelectSceneName)
    {
        if (FindAnyObjectByType<GameOverMenu>() != null)
        {
            return;
        }

        var menuObject = new GameObject("GameOverMenu");
        var menu = menuObject.AddComponent<GameOverMenu>();
        menu.stageSelectSceneName = string.IsNullOrWhiteSpace(stageSelectSceneName)
            ? DefaultStageSelectSceneName
            : stageSelectSceneName;
        menu.Build();

        Time.timeScale = 0f;
    }

    public void RestartStage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        canvas.sortingOrder = 200;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        var overlay = CreatePanel("Overlay", transform, new Color(0f, 0f, 0f, 0.76f));
        Stretch(overlay.rectTransform);

        var panel = CreatePanel("Panel", overlay.transform, new Color(0.08f, 0.08f, 0.1f, 0.95f));
        panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        panel.rectTransform.anchoredPosition = Vector2.zero;
        panel.rectTransform.sizeDelta = new Vector2(520f, 300f);

        CreateText("Title", panel.transform, "GAME OVER", 44f, new Vector2(0f, 86f), new Vector2(420f, 70f), new Color(1f, 0.25f, 0.25f, 1f));
        CreateButton("RestartButton", panel.transform, "RESTART", new Vector2(0f, -18f), RestartStage);
        CreateButton("StageSelectButton", panel.transform, "STAGE SELECT", new Vector2(0f, -98f), LoadStageSelect);
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

    private static void CreateText(string name, Transform parent, string text, float fontSize, Vector2 position, Vector2 size, Color color)
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
    }

    private static void CreateButton(string name, Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
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
        image.color = new Color(0.2f, 0.22f, 0.26f, 1f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        CreateText("Text", buttonObject.transform, text, 24f, Vector2.zero, rectTransform.sizeDelta, Color.white);
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }
}
