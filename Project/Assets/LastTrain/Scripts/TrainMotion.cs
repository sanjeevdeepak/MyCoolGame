using UnityEngine;

/// <summary>Train travel, smooth speed changes and a capped hesitation response.</summary>
[DefaultExecutionOrder(-300)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class TrainMotion : MonoBehaviour
{
    [Min(0f)] public float layoutWidth;
    public bool stationary;
    [Min(0f)]
    [Tooltip("This train's normal rightward speed.")]
    public float cruiseSpeed = 0.9f;
    [Header("Coasting")]
    [Min(0f)] public float speedVariation;
    [Min(.5f)] public float cycleDuration = 4f;
    [Range(0f,1f)] public float cycleOffset;
    [Min(0.01f)] public float speedChangeRate = 6f;
    [Min(0f)] public float hesitationDelay = 0.6f;
    [Min(0.01f)] public float pressureAcceleration = 0.8f;
    [Min(0f)]
    [Tooltip("Maximum speed added after the player hesitates.")]
    public float maxPressureSpeed = 1.5f;
    [Min(0f)]
    [Tooltip("Maximum extra distance caused by hesitation before this train returns to normal speed.")]
    public float maxPressureDistance = 2.5f;


    [SerializeField, HideInInspector] private Vector2 startPosition;
    private Rigidbody2D body;
    private float travelDistance;
    private float pressureTime;
    private float pressureDistance;
    private float pressureSpeed;
    private float gapSpeedCap = float.PositiveInfinity;
    private float gapSpeedFloor;
    private float speedOverride;
    private bool hasSpeedOverride;
    private bool running;
    private bool pressureEnabled;
    private bool paused;
    private float cycleTime;

    public float CurrentSpeed { get; private set; }
    public Vector2 StartPosition => startPosition;
    public float PressureTime => pressureTime;
    public float PressureDistance => pressureDistance;
    public float PressureSpeed => pressureSpeed;
    public bool IsPaused => paused;
    public bool IsRunning => running;
    public Rigidbody2D Body => body != null ? body : (body = GetComponent<Rigidbody2D>());
    private float NormalSpeed => Mathf.Max(0f, hasSpeedOverride ? speedOverride :
        cruiseSpeed + speedVariation * Mathf.Sin((cycleTime / Mathf.Max(.5f,cycleDuration) + cycleOffset) * Mathf.PI * 2f));

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        CaptureStart();
    }

    public void CaptureStart()
    {
        startPosition = Body.position;
        travelDistance = 0f;
        pressureTime = 0f;
        pressureDistance = 0f;
        pressureSpeed = 0f;
        pressureEnabled = false;
        gapSpeedCap = float.PositiveInfinity;
        gapSpeedFloor = 0f;
        hasSpeedOverride = false;
        speedOverride = 0f;
        running = false;
        paused = false;
        CurrentSpeed = 0f;
        cycleTime = 0f;
    }

    private void FixedUpdate()
    {
        if (stationary || paused || !running)
        {
            CurrentSpeed = 0f;
            body.linearVelocity = Vector2.zero;
            return;
        }
        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;
        // A deterministic coast/surge cycle starts when this car is introduced.
        // Signals and emergency braking take priority and hold its phase.
        if(!hasSpeedOverride)cycleTime+=dt;

        pressureTime = pressureEnabled ? pressureTime + dt : 0f;
        float remainingPressure = Mathf.Max(0f, maxPressureDistance - pressureDistance);
        float desiredPressure = pressureEnabled && pressureTime >= hesitationDelay && remainingPressure > 0f
            ? maxPressureSpeed : 0f;
        // Brake the added speed as its distance budget runs out instead of
        // abruptly spending the entire remaining budget in one frame.
        desiredPressure = Mathf.Min(desiredPressure,
            Mathf.Sqrt(2f * Mathf.Max(0.01f, pressureAcceleration) * remainingPressure));
        pressureSpeed = Mathf.MoveTowards(pressureSpeed, desiredPressure, pressureAcceleration * dt);
        pressureSpeed = Mathf.Min(pressureSpeed, remainingPressure / dt);

        float normalSpeed = NormalSpeed;
        float desiredSpeed = normalSpeed + pressureSpeed;
        desiredSpeed = Mathf.Min(Mathf.Max(desiredSpeed, gapSpeedFloor), gapSpeedCap);
        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, Mathf.Max(0f, desiredSpeed), Mathf.Max(0.01f, speedChangeRate) * dt);

        // A gap floor may legitimately exceed cruise speed; that safety motion
        // is not counted as extra pressure. Account only for pressure that was
        // actually delivered after gap limits and acceleration were applied.
        float ordinarySpeed = Mathf.Max(normalSpeed, gapSpeedFloor);
        float deliveredExtra = Mathf.Max(0f, CurrentSpeed - ordinarySpeed);
        if (pressureSpeed > 0f || pressureDistance > 0f || (pressureEnabled && pressureTime >= hesitationDelay))
        {
            float deliveredPressure = deliveredExtra;
            float allowedPressure = Mathf.Min(deliveredPressure, remainingPressure / dt);
            // Account for deceleration travel as well as acceleration. The
            // braking curve above makes this final budget trim sub-pixel small.
            CurrentSpeed -= deliveredPressure - allowedPressure;
            pressureDistance = Mathf.Min(maxPressureDistance, pressureDistance + allowedPressure * dt);
        }

        // Integrating each displacement keeps Inspector speed edits from
        // recalculating the whole journey and teleporting the train.
        travelDistance += CurrentSpeed * dt;
        Vector2 next = startPosition + Vector2.right * travelDistance;
        body.MovePosition(next);
    }

    public void SetRunning(bool enabled)
    {
        if (running == enabled) return;
        running = enabled;
        if (!running)
        {
            CurrentSpeed = 0f;
            Body.linearVelocity = Vector2.zero;
            Body.angularVelocity = 0f;
        }
        else if (!paused)
        {
            // Newly introduced trains begin at their authored base speed.
            // Subsequent changes and pressure use smooth acceleration.
            CurrentSpeed = NormalSpeed;
        }
    }

    public void SetPressure(bool enabled)
    {
        pressureEnabled = enabled;
        if (!enabled) pressureTime = 0f;
    }

    public void SetSpeedOverride(float speed)
    {
        speedOverride = Mathf.Max(0f, speed);
        hasSpeedOverride = true;
    }

    public void ClearSpeedOverride()
    {
        hasSpeedOverride = false;
        speedOverride = 0f;
    }

    public void SetGapSpeedCap(float maximumWorldSpeed)
    {
        gapSpeedCap = Mathf.Max(0f, maximumWorldSpeed);
    }

    public void ClearGapSpeedCap() { gapSpeedCap = float.PositiveInfinity; }

    public void LimitSpeed(float maximumWorldSpeed)
    {
        gapSpeedCap = Mathf.Min(gapSpeedCap, Mathf.Max(0f, maximumWorldSpeed));
    }

    public void SetGapSpeedFloor(float minimumWorldSpeed)
    {
        gapSpeedFloor = Mathf.Max(0f, minimumWorldSpeed);
    }

    public void ClearGapSpeedFloor() { gapSpeedFloor = 0f; }

    public void SetPaused(bool pause)
    {
        paused = pause;
        if (paused)
        {
            CurrentSpeed = 0f;
            Body.linearVelocity = Vector2.zero;
            Body.angularVelocity = 0f;
        }
        else
        {
            CurrentSpeed = running ? NormalSpeed : 0f;
        }
    }

    public void ResetToStart()
    {
        travelDistance = 0f;
        pressureTime = 0f;
        pressureDistance = 0f;
        pressureSpeed = 0f;
        pressureEnabled = false;
        gapSpeedCap = float.PositiveInfinity;
        gapSpeedFloor = 0f;
        hasSpeedOverride = false;
        speedOverride = 0f;
        running = false;
        paused = false;
        RigidbodyInterpolation2D interpolation=Body.interpolation;
        Body.interpolation=RigidbodyInterpolation2D.None;
        Body.position = startPosition;
        transform.position=startPosition;
        Body.interpolation=interpolation;
        Body.linearVelocity = Vector2.zero;
        Body.angularVelocity = 0f;
        CurrentSpeed = 0f;
        cycleTime = 0f;
    }

    public void ResetAt(Vector2 position)
    {
        startPosition = position;
        ResetToStart();
    }
}
