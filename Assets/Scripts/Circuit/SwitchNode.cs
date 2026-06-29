using UnityEngine;

public class SwitchNode : CircuitNode
{
    protected override void Start()
    {
        nodeType = NodeType.Switch;
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
        GetComponent<Renderer>().material.color = isSwitchOn ? Color.green : Color.gray;
    }
}
