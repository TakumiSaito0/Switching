using UnityEngine;

public class ElevatorButton : MonoBehaviour
{
    [Header("Elevator")]
    [SerializeField] private Elevator targetElevator;
    [SerializeField] private Elevator.Floor buttonFloor = Elevator.Floor.First;

    public void PressButton()
    {
        if (targetElevator == null)
        {
            return;
        }

        targetElevator.PressFloorButton(buttonFloor);
    }
}
