using UnityEngine;
using UnityEngine.Events;
namespace MidnightBridge
{
    [DisallowMultipleComponent]
    public sealed class HarborWaterDeath2D : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private bool respawnHere = true;
        [SerializeField] private UnityEvent onFellInWater = new UnityEvent();
        private Transform initialPlayer;
        private Vector3 initialPosition;
        private float nextHit;
        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) { initialPlayer=p.transform;initialPosition=p.transform.position; }
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D body=other.attachedRigidbody;
            Transform player=body != null ? body.transform : other.transform;
            if (!player.CompareTag(playerTag) || Time.time<nextHit) return;
            nextHit=Time.time+.2f; onFellInWater.Invoke();
            if (!respawnHere) return;
            if (respawnPoint == null && initialPlayer != player)
            { Debug.LogWarning("Assign Respawn Point, or place the tagged Player in the scene before Play.",this); return; }
            Vector3 target=respawnPoint != null ? respawnPoint.position : initialPosition;
            if (body != null)
            {
#if UNITY_6000_0_OR_NEWER
                body.linearVelocity=Vector2.zero;
#else
                body.velocity=Vector2.zero;
#endif
                body.angularVelocity=0f;body.position=target;
            }
            else player.position=target;
        }
    }
}
