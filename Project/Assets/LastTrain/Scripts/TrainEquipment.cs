using UnityEngine;

/// <summary>One-shot rooftop controls and faults, reset with the train course.</summary>
[DefaultExecutionOrder(-250)]
public sealed class TrainEquipment : MonoBehaviour
{
    public enum Kind { Bridge, Signal, Hatch, Brake }
    private enum State { Idle, Warning, Active, Spent }

    public Kind kind;
    public TrainLevelDirector director;
    public TrainMotion sourceTrain;
    public TrainMotion targetTrain;
    [Min(0f)] public float warningTime = .8f;
    [Min(0f)] public float duration = 6f;
    [Min(0f)] public float speed = 1.4f;
    [Tooltip("Capped forward speed of the next train during a braking escape. Zero leaves its normal coasting unchanged.")]
    [Min(0f)] public float escapeSpeed;
    public bool requiresInteraction = true;
    public Collider2D triggerCollider;
    public SpriteRenderer switchVisual;
    public Sprite idleSprite;
    public Sprite pressedSprite;
    public SpriteRenderer indicator;
    public Transform lever;
    [Min(0f)] public float leverTravel = .1f;

    [Header("Connection")]
    public Transform bridgeStart;
    public Transform bridgeEnd;
    public BoxCollider2D connectionCollider;
    public SpriteRenderer connectionVisual;
    [Min(.02f)] public float connectionThickness = .18f;
    [Min(0f)] public float connectionOverlap = .25f;
    [Min(.01f)] public float extensionTime = .18f;
    [Min(0f)] public float retractWarning = .6f;

    [Header("Hatch")]
    public Collider2D hatchCollider;
    public SpriteRenderer hatchVisual;
    public Sprite hatchClosedSprite;
    public Sprite hatchOpenSprite;
    public SpriteRenderer holeVisual;
    public Transform hatchFlap;

    [Header("Indicator")]
    public Color idleColor = new Color32(76, 102, 83, 255);
    public Color helpfulColor = new Color32(170, 222, 138, 255);
    public Color warningColor = new Color32(230, 184, 89, 255);
    public Color hazardColor = new Color32(215, 100, 90, 255);
    public Color spentColor = new Color32(70, 80, 74, 255);
    [Min(1f)] public float flashRate = 5f;

    private State state;
    private bool frozen;
    private float elapsed;
    private float lightClock;
    private float extension;
    private float leverPosition;
    private Vector3 leverStart;
    private bool initialValuesCaptured;
    private Sprite initialHatchSprite;
    private Sprite initialSwitchSprite;
    private PlayerController playerInRange;
    private Quaternion flapClosed;
    private float flapAngle;

    public bool Activated => state != State.Idle;
    public bool IsWarning => state == State.Warning;
    public bool IsActive => state == State.Active;
    public bool IsSpent => state == State.Spent;
    public bool HatchOpen => kind == Kind.Hatch && (state == State.Active || state == State.Spent);
    public bool PlayerInRange => playerInRange != null;
    public int ActivationCount { get; private set; }
    public float Remaining => state == State.Warning ? Mathf.Max(0f, warningTime - elapsed) :
        state == State.Active && duration > 0f ? Mathf.Max(0f, duration - elapsed) : 0f;

    private void Awake()
    {
        CaptureInitialValues();
        ResetEquipment();
    }

    private bool Helpful => kind == Kind.Bridge || kind == Kind.Signal;

    private void CaptureInitialValues()
    {
        if (initialValuesCaptured) return;
        if (triggerCollider == null) triggerCollider = GetComponent<Collider2D>();
        initialHatchSprite = hatchVisual != null ? hatchVisual.sprite : null;
        initialSwitchSprite = switchVisual != null ? switchVisual.sprite : null;
        if (lever != null) leverStart = lever.localPosition;
        if (hatchFlap != null) flapClosed = hatchFlap.localRotation;
        initialValuesCaptured = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnterRange(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        EnterRange(other);
    }

    private void EnterRange(Collider2D other)
    {
        PlayerController candidate = other.GetComponentInParent<PlayerController>();
        if (candidate == null) return;
        playerInRange = candidate;
        if (!Helpful || !requiresInteraction) Activate();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == playerInRange) playerInRange = null;
    }

    private void Update()
    {
        if (Time.timeScale<=0f || !Helpful || !requiresInteraction || !PlayerInRange || frozen) return;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            TryInteract();
    }

    public bool TryInteract()
    {
        if (Time.timeScale<=0f || (director!=null && !director.CanInteract) || !Helpful || !PlayerInRange || frozen || state != State.Idle) return false;
        Activate();
        return Activated;
    }

    public void Activate()
    {
        if (Time.timeScale<=0f || frozen || state != State.Idle ||
            (Helpful && requiresInteraction && !PlayerInRange) ||
            (director != null && !director.CanInteract)) return;
        ActivationCount++;
        elapsed = lightClock = 0f;
        state = warningTime > 0f ? State.Warning : State.Active;
        ApplyState();
        UpdateIndicator();
        if(Helpful && requiresInteraction && director!=null && director.sounds!=null)director.sounds.PlaySwitch(transform.position);
    }

    private void FixedUpdate()
    {
        if (!frozen)
        {
            float dt = Time.fixedDeltaTime;
            lightClock += dt;
            if (state == State.Warning || state == State.Active)
            {
                elapsed += dt;
                if (state == State.Warning && elapsed >= warningTime)
                {
                    elapsed -= warningTime;
                    state = State.Active;
                    ApplyState();
                }
                if (state == State.Active && duration > 0f && elapsed >= duration)
                {
                    elapsed = duration;
                    state = State.Spent;
                    ApplyState();
                }
            }
            extension = Mathf.MoveTowards(extension, kind == Kind.Bridge && state == State.Active ? 1f : 0f,
                dt / Mathf.Max(.01f, extensionTime));
            leverPosition = Mathf.MoveTowards(leverPosition, Activated ? 1f : 0f, dt * 12f);
            if (lever != null) lever.localPosition = leverStart + Vector3.down * (leverTravel * leverPosition);
            if(hatchFlap!=null)
            {
                flapAngle=Mathf.MoveTowards(flapAngle,HatchOpen?90f:0f,480f*dt);
                hatchFlap.localRotation=flapClosed*Quaternion.Euler(0,0,flapAngle);
            }
        }
        if (kind == Kind.Bridge) FitConnection();
        UpdateIndicator();
    }

    private void ApplyState()
    {
        if (switchVisual != null)
        {
            Sprite image = Activated ? pressedSprite : idleSprite;
            switchVisual.sprite = image != null ? image : initialSwitchSprite;
        }
        if (kind == Kind.Hatch)
        {
            bool open = HatchOpen;
            if (hatchCollider != null) hatchCollider.enabled = !open;
            if (hatchVisual != null)
            {
                Sprite image = open ? hatchOpenSprite : hatchClosedSprite;
                hatchVisual.sprite = image != null ? image : initialHatchSprite;
            }
            if (holeVisual != null) holeVisual.enabled = open;
        }
        if (kind == Kind.Bridge && connectionCollider != null && state != State.Active)
            connectionCollider.enabled = false;
    }

    private static Vector2 PhysicsAnchor(Transform anchor, TrainMotion train)
    {
        if (train == null) return anchor.position;
        // Remove render interpolation from the offset used to resize collision geometry.
        return train.Body.position + (Vector2)(anchor.position - train.transform.position);
    }

    private void FitConnection()
    {
        if (bridgeStart == null || bridgeEnd == null)
        {
            if (connectionCollider != null) connectionCollider.enabled = false;
            if (connectionVisual != null) connectionVisual.enabled = false;
            return;
        }
        Vector2 start = PhysicsAnchor(bridgeStart, sourceTrain);
        Vector2 end = PhysicsAnchor(bridgeEnd, targetTrain);
        Vector2 difference = end - start;
        float distance = difference.magnitude;
        Vector2 direction = distance > .001f ? difference / distance : Vector2.right;
        start -= direction * connectionOverlap;
        float length = (distance + connectionOverlap * 2f) * extension;
        Vector2 normal = new Vector2(-direction.y, direction.x);
        Vector2 center = start + direction * (length * .5f) - normal * (connectionThickness * .5f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (connectionCollider != null)
        {
            SetSpanPose(connectionCollider.transform, center, angle);
            Vector3 scale = connectionCollider.transform.lossyScale;
            connectionCollider.offset = Vector2.zero;
            connectionCollider.size = new Vector2(Mathf.Max(.01f, length / Mathf.Max(.001f, Mathf.Abs(scale.x))),
                connectionThickness / Mathf.Max(.001f, Mathf.Abs(scale.y)));
            connectionCollider.enabled = state == State.Active && extension >= .99f;
        }
        if (connectionVisual != null)
        {
            SetSpanPose(connectionVisual.transform, center, angle);
            Vector3 scale = connectionVisual.transform.lossyScale;
            connectionVisual.drawMode = SpriteDrawMode.Tiled;
            connectionVisual.size = new Vector2(Mathf.Max(.01f, length / Mathf.Max(.001f, Mathf.Abs(scale.x))),
                connectionThickness / Mathf.Max(.001f, Mathf.Abs(scale.y)));
            connectionVisual.enabled = length > .02f;
        }
    }

    private static void SetSpanPose(Transform span, Vector2 center, float angle)
    {
        span.position = new Vector3(center.x, center.y, span.position.z);
        span.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdateIndicator()
    {
        if (indicator == null) return;
        bool expiring = kind == Kind.Bridge && state == State.Active && duration > 0f && Remaining <= retractWarning;
        bool blink = state == State.Warning || expiring;
        indicator.enabled = !blink || ((int)(lightClock * flashRate) & 1) == 0;
        if (blink) indicator.color = warningColor;
        else if (state == State.Active) indicator.color = kind == Kind.Hatch || kind == Kind.Brake ? hazardColor : helpfulColor;
        else if (state == State.Spent) indicator.color = spentColor;
        else indicator.color = Helpful && PlayerInRange ? Color.Lerp(idleColor, helpfulColor, .45f) : idleColor;
    }

    public void Freeze(bool pause)
    {
        frozen = pause;
    }

    public void ResetEquipment()
    {
        CaptureInitialValues();
        state = State.Idle;
        frozen = false;
        elapsed = lightClock = extension = leverPosition = 0f;
        flapAngle=0;
        if(hatchFlap!=null)hatchFlap.localRotation=flapClosed;
        ActivationCount = 0;
        playerInRange = null;
        if (lever != null) lever.localPosition = leverStart;
        if (triggerCollider != null) triggerCollider.enabled = true;
        if (connectionCollider != null) connectionCollider.enabled = false;
        if (connectionVisual != null) connectionVisual.enabled = false;
        ApplyState();
        UpdateIndicator();
    }
}
