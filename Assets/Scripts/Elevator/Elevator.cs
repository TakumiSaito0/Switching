using UnityEngine;

public class Elevator : MonoBehaviour
{
    public enum Floor
    {
        First = 1,
        Second = 2
    }

    [Header("Floor Positions")]
    [SerializeField] private Transform firstFloorPoint;
    [SerializeField] private Transform secondFloorPoint;
    [SerializeField] private float travelHeight = 4.0f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float arrivalDistance = 0.01f;

    public Floor CurrentFloor { get; private set; } = Floor.First;
    public bool IsMoving { get; private set; }

    private Vector3 firstFloorPosition;
    private Vector3 secondFloorPosition;
    private Vector3 targetPosition;

    private void Awake()
    {
        firstFloorPosition = firstFloorPoint != null ? firstFloorPoint.position : transform.position;
        secondFloorPosition = secondFloorPoint != null
            ? secondFloorPoint.position
            : firstFloorPosition + Vector3.up * travelHeight;

        transform.position = firstFloorPosition;
        targetPosition = firstFloorPosition;
    }

    private void Update()
    {
        if (!IsMoving)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) > arrivalDistance)
        {
            return;
        }

        transform.position = targetPosition;
        IsMoving = false;
        CurrentFloor = targetPosition == secondFloorPosition ? Floor.Second : Floor.First;
    }

    public void RequestFloor(Floor floor)
    {
        if (IsMoving || floor == CurrentFloor)
        {
            return;
        }

        targetPosition = floor == Floor.Second ? secondFloorPosition : firstFloorPosition;
        IsMoving = true;
    }

    public void PressFloorButton(Floor buttonFloor)
    {
        if (IsMoving)
        {
            return;
        }

        if (CurrentFloor != buttonFloor)
        {
            RequestFloor(buttonFloor);
            return;
        }

        ToggleElevator();
    }

    public void ToggleElevator()
    {
        RequestFloor(CurrentFloor == Floor.First ? Floor.Second : Floor.First);
    }
}
