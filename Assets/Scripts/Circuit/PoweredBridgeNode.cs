using UnityEngine;

public class PoweredBridgeNode : CircuitNode
{
    [Header("Bridge")]
    [SerializeField] private Transform bridgeBody;
    [SerializeField] private Vector3 poweredLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 unpoweredLocalPosition = new Vector3(0f, -3f, 0f);
    [SerializeField] private float moveSpeed = 3f;

    [Header("Visual")]
    [SerializeField] private Color poweredColor = new Color(0.25f, 0.85f, 1f);
    [SerializeField] private Color unpoweredColor = new Color(0.18f, 0.24f, 0.28f);

    private Vector3 targetLocalPosition;
    private Renderer[] bridgeRenderers;

    protected override void Start()
    {
        nodeType = NodeType.Door;
        if (bridgeBody == null)
        {
            bridgeBody = transform;
        }

        bridgeRenderers = bridgeBody.GetComponentsInChildren<Renderer>();
        bridgeBody.localPosition = unpoweredLocalPosition;
        targetLocalPosition = unpoweredLocalPosition;
        base.Start();
    }

    private void Update()
    {
        if (bridgeBody == null)
        {
            return;
        }

        bridgeBody.localPosition = Vector3.MoveTowards(
            bridgeBody.localPosition,
            targetLocalPosition,
            moveSpeed * Time.deltaTime
        );
    }

    public override void OnPowerChanged(bool powered)
    {
        base.OnPowerChanged(powered);
        targetLocalPosition = powered ? poweredLocalPosition : unpoweredLocalPosition;
        ApplyColor(powered);
    }

    private void ApplyColor(bool powered)
    {
        if (bridgeRenderers == null)
        {
            return;
        }

        Color color = powered ? poweredColor : unpoweredColor;
        foreach (Renderer bridgeRenderer in bridgeRenderers)
        {
            if (bridgeRenderer != null)
            {
                bridgeRenderer.material.color = color;
            }
        }
    }
}
