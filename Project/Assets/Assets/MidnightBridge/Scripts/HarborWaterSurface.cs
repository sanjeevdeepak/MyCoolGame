using UnityEngine;
namespace MidnightBridge
{
    /// <summary>World-space height shared by water and platform reflections.</summary>
    [DisallowMultipleComponent]
    public sealed class HarborWaterSurface : MonoBehaviour
    {
        public static HarborWaterSurface Active { get; private set; }
        public float Height { get { return transform.position.y; } }
        private void OnEnable() { Active = this; }
        private void OnDisable() { if (Active == this) Active = null; }
    }
}
