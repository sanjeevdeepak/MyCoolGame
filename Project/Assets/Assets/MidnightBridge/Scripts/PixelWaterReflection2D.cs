using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MidnightBridge
{
    /// <summary>Visual-only, native-pixel horizontal bands. No render texture or shader dependency.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(11000)]
    public sealed class PixelWaterReflection2D : MonoBehaviour
    {
        [SerializeField] private Transform waterSurface;
        [SerializeField] private float fallbackWaterY = -3.3125f;
        [SerializeField, Min(1)] private int stripHeightPixels = 2;
        [SerializeField, Min(1f)] private float rippleFramesPerSecond = 5f;
        [SerializeField, Range(0,3)] private int ripplePixels = 1;
        [SerializeField] private Color tint = new Color(0.70f, 0.38f, 0.79f, 0.55f);
        [SerializeField] private int sortingOrder = -16;
        private SpriteRenderer source;
        private Sprite cached;
        private readonly List<SpriteRenderer> strips = new List<SpriteRenderer>();
        private readonly List<Sprite> createdSprites = new List<Sprite>();
        private readonly List<Vector2> localCenters = new List<Vector2>();
        private readonly int[] wave = {0,1,0,-1};
        private void Awake() { source = GetComponent<SpriteRenderer>(); }
        private void LateUpdate()
        {
            if (source == null) source = GetComponent<SpriteRenderer>();
            if (source.sprite != cached) Rebuild();
            float line = waterSurface != null ? waterSurface.position.y :
                (HarborWaterSurface.Active != null ? HarborWaterSurface.Active.Height : fallbackWaterY);
            int phase = Mathf.FloorToInt(Time.time * rippleFramesPerSecond);
            float ppu = cached != null ? cached.pixelsPerUnit : 32f;
            Vector3 scale = transform.lossyScale;
            for (int i=0;i<strips.Count;i++)
            {
                Vector2 local = localCenters[i];
                if (source.flipX) local.x = -local.x;
                if (source.flipY) local.y = -local.y;
                Vector3 originalPoint = transform.TransformPoint(local);
                Vector3 point = new Vector3(originalPoint.x, 2f*line-originalPoint.y, transform.position.z);
                point.x += wave[(phase+i)%4] * ripplePixels * Mathf.Abs(scale.x) / ppu;
                SpriteRenderer r = strips[i];
                r.transform.position = point;
                r.transform.rotation = Quaternion.Euler(0f,0f,-transform.eulerAngles.z);
                r.transform.localScale = scale;
                r.flipX = source.flipX;
                r.flipY = !source.flipY;
                r.color = source.color * tint;
                r.sharedMaterial = source.sharedMaterial;
                r.sortingLayerID = source.sortingLayerID;
                r.sortingOrder = sortingOrder;
                // Only complete bands below the waterline are drawn. This also
                // clips decorative parts that dip through the water surface.
                float halfHeight = r.sprite.bounds.extents.y * Mathf.Abs(scale.y);
                r.enabled = source.enabled && point.y+halfHeight <= line;
            }
        }
        private void Rebuild()
        {
            Clear(); cached = source.sprite;
            if (cached == null) return;
            // Use the supplied individual PNG sprites; do not atlas-pack these
            // reflected sprites with rotation/tight packing enabled.
            Rect rect = cached.rect; Vector2 pivot = cached.pivot;
            int height = Mathf.RoundToInt(rect.height);
            for (int y=0;y<height;y+=Mathf.Max(1,stripHeightPixels))
            {
                int h = Mathf.Min(stripHeightPixels,height-y);
                Sprite sprite = Sprite.Create(cached.texture,new Rect(rect.x,rect.y+y,rect.width,h),
                    new Vector2(.5f,.5f),cached.pixelsPerUnit,0,SpriteMeshType.FullRect);
                createdSprites.Add(sprite);
                GameObject go = new GameObject(name+" water band "+y);
                // Root objects avoid applying parent scale twice. Explicit scene
                // ownership keeps additive scene loads/unloads predictable.
                SceneManager.MoveGameObjectToScene(go,gameObject.scene);
                SpriteRenderer r = go.AddComponent<SpriteRenderer>();r.sprite=sprite;strips.Add(r);
                localCenters.Add(new Vector2((rect.width*.5f-pivot.x)/cached.pixelsPerUnit,
                    (y+h*.5f-pivot.y)/cached.pixelsPerUnit));
            }
        }
        private void Clear()
        {
            foreach (SpriteRenderer r in strips) if (r != null) { r.gameObject.SetActive(false); Destroy(r.gameObject); }
            foreach (Sprite s in createdSprites) if (s != null) Destroy(s);
            strips.Clear();createdSprites.Clear();localCenters.Clear();cached=null;
        }
        private void OnDisable() { foreach (SpriteRenderer r in strips) if (r != null) r.enabled=false; }
        private void OnDestroy() { Clear(); }
    }
}
