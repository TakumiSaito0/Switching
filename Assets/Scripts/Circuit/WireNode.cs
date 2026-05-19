using UnityEngine;

public class WireNode : CircuitNode
{
    protected override void Start()
    {
        nodeType = NodeType.Wire;
        base.Start();
    }

    public override void OnPowerChanged(bool powered)
    {
        base.OnPowerChanged(powered);
        // 例: 通電時は赤く光り、通常時は暗い赤色にする
        GetComponent<Renderer>().material.color = powered ? Color.red : new Color(0.3f, 0, 0);
    }
}