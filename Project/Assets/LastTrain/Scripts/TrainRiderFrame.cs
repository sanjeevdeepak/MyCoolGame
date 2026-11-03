using UnityEngine;

/// <summary>
/// Gives the player controller a train-relative horizontal reference.
/// Retains the takeoff velocity until landing, then rebases to the new surface.
/// No Rigidbody parenting, snapping or extra transform movement is involved.
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(TrainRiderFrameAfter))]
public sealed class TrainRiderFrame : MonoBehaviour
{
    [Min(0f)]
    [Tooltip("A tiny roof probe used only while already at or just above a roof.")]
    public float supportProbeDistance = 0.035f;
    [Range(0f, 1f)] public float minimumRoofNormal = 0.7f;

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private PhysicsMaterial2D originalMaterial;
    private PhysicsMaterial2D runtimeMaterial;
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[24];
    private readonly RaycastHit2D[] probeHits = new RaycastHit2D[12];
    private float pendingCarrierSpeed;
    private bool frameOpen;
    private TrainMotion previousSupport;
    private float previousCarrierSpeed;
    private float airCarrierSpeed;

    public TrainMotion Support { get; private set; }
    public float CarrierSpeed => frameOpen ? pendingCarrierSpeed : 0f;
    public float ReferenceSpeed => frameOpen ? pendingCarrierSpeed : airCarrierSpeed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        // The existing PlayerController writes sharedMaterial.friction each tick.
        // Isolate those writes to this scene's runtime instance.
        originalMaterial = body.sharedMaterial;
        if (originalMaterial != null)
        {
            runtimeMaterial = Instantiate(originalMaterial);
            runtimeMaterial.name = originalMaterial.name + " (Runtime)";
            runtimeMaterial.hideFlags = HideFlags.DontSave;
            if (GetComponent<PlayerController>().responsiveMovement)
            {
                runtimeMaterial.friction = 0f;
                runtimeMaterial.frictionCombine = PhysicsMaterialCombine2D.Multiply;
            }
            body.sharedMaterial = runtimeMaterial;
        }
    }

    private void FixedUpdate()
    {
        frameOpen = false;
        pendingCarrierSpeed = 0f;
        Support = null;
        if (body.bodyType != RigidbodyType2D.Dynamic || !body.simulated)
        {
            ClearCarrierHistory();
            airCarrierSpeed = 0f;
            return;
        }

        bool grounded = false;
        int count = body.linearVelocity.y <= .2f ? body.GetContacts(contacts) : 0;
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y < minimumRoofNormal) continue;
            grounded = true;
            TrainMotion train = FindTrain(contacts[i].collider, contacts[i].otherCollider);
            if (train != null) { Support = train; break; }
        }

        if (Support == null && body.linearVelocity.y <= .2f && bodyCollider != null && supportProbeDistance > 0f)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            int hits = bodyCollider.Cast(Vector2.down, filter, probeHits, supportProbeDistance);
            for (int i = 0; i < hits; i++)
            {
                if (probeHits[i].normal.y < minimumRoofNormal) continue;
                grounded = true;
                TrainMotion train = probeHits[i].collider.GetComponentInParent<TrainMotion>();
                if (train != null) { Support = train; break; }
            }
        }

        if (Support == null)
        {
            if (previousSupport != null) airCarrierSpeed = previousCarrierSpeed;
            if (grounded) airCarrierSpeed = 0f;
            ClearCarrierHistory();
            pendingCarrierSpeed = airCarrierSpeed;
        }
        else pendingCarrierSpeed = Support.CurrentSpeed;
        // Execution order: train (-300), subtract (-200), existing controller (0), add (+200).
        // While continuously supported, the body still contains the previous
        // step's carrier speed. Remove that value, then restore the new speed
        // after the controller so acceleration is carried without relative slip.
        float speedToRemove = Support != null && Support == previousSupport ? previousCarrierSpeed : pendingCarrierSpeed;
        body.linearVelocity = new Vector2(body.linearVelocity.x - speedToRemove, body.linearVelocity.y);
        frameOpen = true;
    }

    private static TrainMotion FindTrain(Collider2D first, Collider2D second)
    {
        TrainMotion result = first != null ? first.GetComponentInParent<TrainMotion>() : null;
        return result != null ? result : (second != null ? second.GetComponentInParent<TrainMotion>() : null);
    }

    public void RestoreWorldVelocity()
    {
        if (!frameOpen) return;
        frameOpen = false;
        if (body.bodyType == RigidbodyType2D.Dynamic && body.simulated)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x + pendingCarrierSpeed, body.linearVelocity.y);
            previousSupport = Support;
            previousCarrierSpeed = pendingCarrierSpeed;
            airCarrierSpeed = pendingCarrierSpeed;
        }
        else
        {
            ClearCarrierHistory();
            Support = null;
        }
        pendingCarrierSpeed = 0f;
    }

    private void ClearCarrierHistory()
    {
        previousSupport = null;
        previousCarrierSpeed = 0f;
    }

    public void ResetSupport()
    {
        frameOpen = false;
        pendingCarrierSpeed = 0f;
        Support = null;
        airCarrierSpeed = 0f;
        ClearCarrierHistory();
    }

    private void OnDisable()
    {
        // Do not strand the velocity in a relative frame if disabled mid-step.
        RestoreWorldVelocity();
        ResetSupport();
    }

    private void OnDestroy()
    {
        if (body != null && runtimeMaterial != null && body.sharedMaterial == runtimeMaterial)
            body.sharedMaterial = originalMaterial;
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}
