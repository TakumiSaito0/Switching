using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ClearManager : MonoBehaviour
{
    [SerializeField]
    private string boxTag = "Box";

    [SerializeField]
    private string nextStageName;

    [SerializeField]
    private string stageSelectSceneName = "StageSelectScene";

    [Header("Clear Menu")]
    [SerializeField]
    private TMP_FontAsset menuFont;

    [SerializeField]
    private string titleText = "CLEAR";

    [SerializeField]
    private string nextStageButtonText = "NEXT STAGE";

    [SerializeField]
    private string stageSelectButtonText = "STAGE SELECT";

    [SerializeField]
    private Color panelColor = new Color(0.08f, 0.1f, 0.12f, 0.92f);

    [SerializeField]
    private Color buttonColor = new Color(0.95f, 0.95f, 0.95f, 1f);

    [SerializeField]
    private Color titleColor = Color.white;

    [SerializeField]
    private Color buttonTextColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    [SerializeField]
    private UnityEvent onClear;

    private bool isCleared;

    private void OnTriggerEnter(Collider other)
    {
        if (isCleared)
        {
            return;
        }

        if (!other.CompareTag(boxTag))
        {
            return;
        }

        isCleared = true;
        onClear?.Invoke();
        GameSfx.Play("sfx_clear_lowpoly");
        ClearResultMenu.Show(
            nextStageName,
            stageSelectSceneName,
            menuFont,
            titleText,
            nextStageButtonText,
            stageSelectButtonText,
            panelColor,
            buttonColor,
            titleColor,
            buttonTextColor);
        Debug.Log("Clear");
    }
}
