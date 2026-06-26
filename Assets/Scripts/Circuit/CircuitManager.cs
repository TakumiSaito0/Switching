using System.Collections.Generic;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public static CircuitManager Instance;

    public List<CircuitNode> allNodes = new List<CircuitNode>();

    private void Awake()
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

    public void RecalculatePower()
    {
        foreach (var node in allNodes)
        {
            if (node != null)
            {
                node.ConnectToNeighbors();
            }
        }

        Dictionary<CircuitNode, bool> previousPowerStates = new Dictionary<CircuitNode, bool>();
        foreach (var node in allNodes)
        {
            if (node != null)
            {
                previousPowerStates[node] = node.isPowered;
            }
        }

        Queue<CircuitNode> checkQueue = new Queue<CircuitNode>();
        HashSet<CircuitNode> poweredNodes = new HashSet<CircuitNode>();

        foreach (var node in allNodes)
        {
            if (node != null && IsPowerSource(node))
            {
                checkQueue.Enqueue(node);
                poweredNodes.Add(node);
            }
        }

        while (checkQueue.Count > 0)
        {
            CircuitNode current = checkQueue.Dequeue();

            foreach (var neighbor in current.connectedNodes)
            {
                if (neighbor == null || !CanReceivePower(neighbor) || poweredNodes.Contains(neighbor))
                {
                    continue;
                }

                poweredNodes.Add(neighbor);
                checkQueue.Enqueue(neighbor);
            }
        }

        foreach (var node in allNodes)
        {
            if (node == null)
            {
                continue;
            }

            bool wasPowered = previousPowerStates.TryGetValue(node, out bool previousPower) && previousPower;
            bool isPowered = poweredNodes.Contains(node);
            node.isPowered = isPowered;

            if (wasPowered != isPowered)
            {
                node.OnPowerChanged(isPowered);
            }
        }
    }

    private bool IsPowerSource(CircuitNode node)
    {
        return node.nodeType == CircuitNode.NodeType.Switch && node.isSwitchOn;
    }

    private bool CanReceivePower(CircuitNode node)
    {
        return node.nodeType != CircuitNode.NodeType.Switch || node.isSwitchOn;
    }
}
