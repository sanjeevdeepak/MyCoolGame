using UnityEngine;

namespace MidnightBridge
{
    /// <summary>Optional visual-only adapter; leaves your movement and triggers intact.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class HarborAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private LayerMask groundLayers = ~0;
        [Min(0.001f)] [SerializeField] private float groundProbeDistance = 0.06f;
        [SerializeField] private bool flipWithMovement = true;
        private Animator animator;
        private SpriteRenderer visual;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[12];
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            visual = GetComponent<SpriteRenderer>();
            if (body == null) body = GetComponentInParent<Rigidbody2D>();
            if (bodyCollider == null && body != null) bodyCollider = body.GetComponent<Collider2D>();
        }

        private void LateUpdate()
        {
            if (body == null) return;
#if UNITY_6000_0_OR_NEWER
            Vector2 velocity = body.linearVelocity;
#else
            Vector2 velocity = body.velocity;
#endif
            bool grounded = false;
            if (bodyCollider != null && velocity.y <= 0.1f)
            {
                ContactFilter2D filter = new ContactFilter2D();
                filter.SetLayerMask(groundLayers);
                filter.useTriggers = false;
                int count = bodyCollider.Cast(Vector2.down, filter, groundHits, groundProbeDistance);
                for (int i = 0; i < count; i++)
                {
                    if (groundHits[i].normal.y > 0.5f) { grounded = true; break; }
                }
            }
            animator.SetFloat(Speed, Mathf.Abs(velocity.x));
            animator.SetBool(Grounded, grounded);
            animator.SetFloat(VerticalSpeed, velocity.y);
            if (flipWithMovement && Mathf.Abs(velocity.x) > 0.01f) visual.flipX = velocity.x < 0f;
        }
    }
}
