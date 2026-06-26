using System.Collections.Generic;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public static CircuitManager Instance;

    // シーン内の全回路ノード
    public List<CircuitNode> allNodes = new List<CircuitNode>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple CircuitManager instances found. Keeping the first one.", this);
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // スイッチが操作された時に回路全体の状態を再計算する
    public void RecalculatePower()
    {
        foreach (var node in allNodes)
        {
            if (node != null)
            {
                node.ConnectToNeighbors();
            }
        }

        // 1. まず全てのノードの動力をOFFにリセットする
        Dictionary<CircuitNode, bool> previousPowerStates = new Dictionary<CircuitNode, bool>();

        foreach (var node in allNodes)
        {
            if (node == null)
            {
                continue;
            }

            previousPowerStates[node] = node.isPowered;
            node.isPowered = false;
        }

        // 2. ONになっているスイッチを探してキューに入れる
        Queue<CircuitNode> checkQueue = new Queue<CircuitNode>();
        HashSet<CircuitNode> visited = new HashSet<CircuitNode>();

        foreach (var node in allNodes)
        {
            if (node == null)
            {
                continue;
            }

            if (node.nodeType == CircuitNode.NodeType.Switch && node.isSwitchOn)
            {
                checkQueue.Enqueue(node);
                visited.Add(node);
                node.isPowered = true;
            }
        }

        // 3. 幅優先探索（BFS）で繋がっているノードに順に動力を伝搬する
        while (checkQueue.Count > 0)
        {
            CircuitNode current = checkQueue.Dequeue();

            foreach (var neighbor in current.connectedNodes)
            {
                if (neighbor == null)
                {
                    continue;
                }

                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    neighbor.isPowered = true;
                    checkQueue.Enqueue(neighbor);
                }
            }
        }

        // 4. 全ノードに変更された状態を通知して見た目や動作を更新させる
        foreach (var node in allNodes)
        {
            if (node == null)
            {
                continue;
            }

            bool wasPowered = previousPowerStates.TryGetValue(node, out bool previousPower) && previousPower;
            if (wasPowered != node.isPowered)
            {
                node.OnPowerChanged(node.isPowered);
            }
        }
    }
}
