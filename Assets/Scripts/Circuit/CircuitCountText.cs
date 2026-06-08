using TMPro;
using UnityEngine;

public class CircuitCountText : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private string label = "回路";

    private int lastCount = -1;

    [System.Obsolete]
    private void Awake()
    {
        // Playerがアタッチされていなければ、同じオブジェクトから探すか、
        // シーン内のPlayerControllerを探す
        if (player == null)
        {
            // FindObjectOfTypeは非推奨のため、FindFirstObjectByTypeに変更
            player = FindFirstObjectByType<PlayerController>();
        }

        // CountTextがアタッチされていなければ、自分のコンポーネントから探す
        if (countText == null)
        {
            countText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        if (player == null || countText == null)
        {
            return;
        }

        int currentCount = player.CurrentCircuitCount;
        if (currentCount == lastCount)
        {
            return;
        }

        lastCount = currentCount;
        countText.text = $"{label}: {currentCount}";
    }
}
