using System.Collections.Generic;
using UnityEngine;

public class CircuitNode : MonoBehaviour
{
    public enum NodeType { Wire, Switch, Door }

    public NodeType nodeType;
    public bool isPowered = false;
    public bool isSwitchOn = false;
    public List<CircuitNode> connectedNodes = new List<CircuitNode>();
    public float connectRadius = 1.1f;
    [SerializeField] private float maxGridConnectionDistance = 1.1f;

    protected virtual void Start()
    {
        if (CircuitManager.Instance == null)
        {
            Debug.LogError("CircuitManager is missing from the scene.", this);
            return;
        }

        CircuitManager.Instance.allNodes.Add(this);
        RefreshAllNodeConnections();
        OnPowerChanged(isPowered);
    }

    protected virtual void OnDestroy()
    {
        if (CircuitManager.Instance == null)
        {
            return;
        }

        CircuitManager.Instance.allNodes.Remove(this);

        foreach (CircuitNode node in CircuitManager.Instance.allNodes)
        {
            if (node != null)
            {
                node.connectedNodes.Remove(this);
            }
        }
    }

    public virtual void ConnectToNeighbors()
    {
        connectedNodes.Clear();

        if (CircuitManager.Instance == null)
        {
            return;
        }

        foreach (CircuitNode otherNode in CircuitManager.Instance.allNodes)
        {
            if (otherNode != null && otherNode != this && IsWithinGridConnectionDistance(otherNode))
            {
                connectedNodes.Add(otherNode);
            }
        }

        OnConnectionsChanged();
    }

    public virtual void OnPowerChanged(bool powered)
    {
        isPowered = powered;
    }

    protected virtual void OnConnectionsChanged()
    {
    }

    private void RefreshAllNodeConnections()
    {
        if (CircuitManager.Instance == null)
        {
            return;
        }

        foreach (CircuitNode node in CircuitManager.Instance.allNodes)
        {
            if (node != null)
            {
                node.ConnectToNeighbors();
            }
        }
    }

    private bool IsWithinGridConnectionDistance(CircuitNode otherNode)
    {
        Vector3 thisGridPosition = new Vector3(
            Mathf.Round(transform.position.x),
            0f,
            Mathf.Round(transform.position.z)
        );
        Vector3 otherGridPosition = new Vector3(
            Mathf.Round(otherNode.transform.position.x),
            0f,
            Mathf.Round(otherNode.transform.position.z)
        );
        Vector3 offset = otherGridPosition - thisGridPosition;

        float effectiveDistance = Mathf.Min(connectRadius, maxGridConnectionDistance);
        return offset.sqrMagnitude <= effectiveDistance * effectiveDistance;
    }
}
