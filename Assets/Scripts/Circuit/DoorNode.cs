using UnityEngine;

public class DoorNode : CircuitNode
{
    protected override void Start()
    {
        nodeType = NodeType.Door;
        base.Start();
    }

    public override void OnPowerChanged(bool powered)
    {
        base.OnPowerChanged(powered);
        // ドアが開く（通電時）は緑色、閉まっている（非通電時）は赤色にする例
        GetComponent<Renderer>().material.color = powered ? Color.cyan : Color.red;
    }
}