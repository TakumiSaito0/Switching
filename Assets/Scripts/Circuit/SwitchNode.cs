using UnityEngine;

public class SwitchNode : CircuitNode
{
    [Header("ON Indicator")]
    [SerializeField] private GameObject onIndicatorPrefab;
    [SerializeField] private Vector3 onIndicatorLocalPosition = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private float onIndicatorScale = 0.35f;

    private GameObject onIndicator;

    protected override void Start()
    {
        nodeType = NodeType.Switch;
        CreateOnIndicator();
        base.Start();
    }

    // キャラクターが触れたりクリックしたときに他スクリプトから呼び出す
    public void ToggleSwitch()
    {
        isSwitchOn = !isSwitchOn;
        GameSfx.PlayAt("sfx_switch_lowpoly", transform.position);
        CircuitManager.Instance.RecalculatePower();
    }

    public override void OnPowerChanged(bool powered)
    {
        base.OnPowerChanged(powered);
        // 例: ONの時は緑色、OFFの時は灰色
        if (onIndicator != null)
        {
            onIndicator.SetActive(isSwitchOn);
        }
    }

    private void CreateOnIndicator()
    {
        if (onIndicatorPrefab == null || onIndicator != null)
        {
            return;
        }

        onIndicator = Instantiate(onIndicatorPrefab, transform);
        onIndicator.name = "SwitchOnIndicator";
        onIndicator.transform.localPosition = onIndicatorLocalPosition;
        onIndicator.transform.localRotation = Quaternion.identity;
        onIndicator.transform.localScale = Vector3.one * onIndicatorScale;

        foreach (Collider indicatorCollider in onIndicator.GetComponentsInChildren<Collider>(true))
        {
            indicatorCollider.enabled = false;
        }

        onIndicator.SetActive(false);
    }
}
