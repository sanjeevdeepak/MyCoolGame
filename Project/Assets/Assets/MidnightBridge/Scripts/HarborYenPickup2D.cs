using UnityEngine;
using UnityEngine.Events;
namespace MidnightBridge
{
    public sealed class HarborYenPickup2D : MonoBehaviour
    {
        [SerializeField] private string playerTag="Player";
        [SerializeField] private int value=5;
        [SerializeField] private UnityEvent<int> onCollected=new UnityEvent<int>();
        public static int CollectedYen { get; private set; }
        private bool collected;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCount() { CollectedYen=0; }
        private void OnTriggerEnter2D(Collider2D other)
        {
            GameObject p=other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (collected || !p.CompareTag(playerTag)) return;
            collected=true;CollectedYen+=value;onCollected.Invoke(value);gameObject.SetActive(false);
        }
    }
}
