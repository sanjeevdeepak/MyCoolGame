using UnityEngine;

/// <summary>Completes TrainRiderFrame after the existing movement controller.</summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class TrainRiderFrameAfter : MonoBehaviour
{
    private TrainRiderFrame rider;

    private void Awake()
    {
        rider = GetComponent<TrainRiderFrame>();
    }

    private void FixedUpdate()
    {
        if (rider != null) rider.RestoreWorldVelocity();
    }
}
