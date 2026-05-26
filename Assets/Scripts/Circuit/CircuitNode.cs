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
        CircuitManager.Instance.allNodes.Add(this);
        ConnectToNeighbors();
        OnPowerChanged(isPowered);
    }

    public virtual void ConnectToNeighbors()
    {
        connectedNodes.Clear();

        float effectiveRadius = Mathf.Min(connectRadius, maxGridConnectionDistance);
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, effectiveRadius);
        foreach (var hit in hitColliders)
        {
            CircuitNode otherNode = hit.GetComponent<CircuitNode>();
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

    private bool IsWithinGridConnectionDistance(CircuitNode otherNode)
    {
        Vector3 offset = otherNode.transform.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= maxGridConnectionDistance * maxGridConnectionDistance;
    }
}
