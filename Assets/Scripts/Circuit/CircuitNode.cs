using System.Collections.Generic;
using UnityEngine;

public class CircuitNode : MonoBehaviour
{
    public enum NodeType { Wire, Switch, Door }
    public NodeType nodeType;

    public bool isPowered = false;    // 電気が通っているか
    public bool isSwitchOn = false;   // (自身がスイッチの場合) ONになっているか

    // 接続されている隣接ノードのリスト
    public List<CircuitNode> connectedNodes = new List<CircuitNode>();

    // 地震からの距離で隣接判定を行う（グリッド配置なら 1.1f など調整）
    public float connectRadius = 1.1f;

    protected virtual void Start()
    {
        // 起動時にマネージャーに自身を登録し、周囲のノードと接続する
        CircuitManager.Instance.allNodes.Add(this);
        ConnectToNeighbors();

        // 追加されたときに色などの初期状態を反映させる
        OnPowerChanged(isPowered);
    }

    // 周囲にある他のノードを見つけて接続する
    public void ConnectToNeighbors()
    {
        connectedNodes.Clear();
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, connectRadius);
        foreach (var hit in hitColliders)
        {
            CircuitNode otherNode = hit.GetComponent<CircuitNode>();
            if (otherNode != null && otherNode != this)
            {
                connectedNodes.Add(otherNode);
            }
        }
    }

    // 動力が変化した時に呼ばれる
    public virtual void OnPowerChanged(bool powered)
    {
        isPowered = powered;
    }
}