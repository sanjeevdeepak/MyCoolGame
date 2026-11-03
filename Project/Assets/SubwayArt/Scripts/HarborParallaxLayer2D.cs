using System.Collections.Generic;
using UnityEngine;

namespace MidnightBridge
{

/// <summary>
/// Optional replacement for an existing parallax component, not an addition to it.
/// Attach to one background SpriteRenderer. Never attach to a playable platform.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(10000)]
public sealed class HarborParallaxLayer2D : MonoBehaviour
{
    [Tooltip("Drag the actual Main Camera here, not a Cinemachine virtual camera.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Apparent scrolling speed: 0 = stays on screen; 1 = normal level speed.")]
    [Range(0f, 1f)]
    [SerializeField] private float scrollSpeed = 0.15f;

    [Tooltip("Keep the background at the same screen height if the camera moves up/down.")]
    [SerializeField] private bool followCameraY = false;

    [Tooltip("Repeat a horizontally seamless background strip. Repeats have no colliders.")]
    [SerializeField] private bool repeatHorizontally = true;

    private SpriteRenderer original;
    private Vector3 startPosition;
    private Vector3 startCameraPosition;
    private readonly List<SpriteRenderer> repeats = new List<SpriteRenderer>();
    private int currentRadius = -1;
    private bool initialized;

    private void Start()
    {
        original = GetComponent<SpriteRenderer>();
        if (targetCamera == null) targetCamera = Camera.main;

        if (targetCamera == null || original.sprite == null)
        {
            Debug.LogWarning("Harbor parallax needs a Main Camera and an assigned sprite.", this);
            enabled = false;
            return;
        }

        if (repeatHorizontally && !targetCamera.orthographic)
        {
            Debug.LogWarning("Background repeating needs an orthographic 2D camera.", this);
            enabled = false;
            return;
        }

        startPosition = transform.position;
        startCameraPosition = targetCamera.transform.position;
        initialized = true;
        UpdateLayer();
    }

    private void LateUpdate()
    {
        if (initialized && targetCamera != null) UpdateLayer();
    }

    private void UpdateLayer()
    {
        Vector3 cameraPosition = targetCamera.transform.position;
        Vector3 delta = cameraPosition - startCameraPosition;
        Vector3 next = startPosition;
        next.x += delta.x * (1f - scrollSpeed);
        if (followCameraY) next.y += delta.y;

        if (repeatHorizontally)
        {
            // Use a centered sprite, Simple draw mode, positive scale and no rotation.
            float width = original.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x);
            if (width <= 0.0001f) return;

            // Moving by a whole tile width preserves the visible pattern, even
            // after a long move, a backwards move or a player respawn/teleport.
            next.x += Mathf.Round((cameraPosition.x - next.x) / width) * width;

            // Extra copies also cover a wider view when an existing zoom trigger fires.
            float halfView = targetCamera.orthographicSize * targetCamera.aspect;
            int radius = Mathf.CeilToInt(halfView / width) + 1;
            EnsureCopies(radius);
        }
        else
        {
            ClearCopies();
        }

        float pixel = Mathf.Abs(transform.lossyScale.x) / original.sprite.pixelsPerUnit;
        if (pixel > 0f) next.x = Mathf.Round(next.x / pixel) * pixel;
        transform.position = next;
        UpdateCopies();
    }

    private void EnsureCopies(int radius)
    {
        if (radius == currentRadius) return;
        ClearCopies();
        currentRadius = radius;

        for (int offset = -radius; offset <= radius; offset++)
        {
            if (offset == 0) continue;
            // Copy only the visual component, never this script or gameplay components.
            GameObject copyObject = new GameObject("City background repeat " + offset);
            copyObject.transform.SetParent(transform, false);
            SpriteRenderer renderer = copyObject.AddComponent<SpriteRenderer>();
            repeats.Add(renderer);
        }
    }

    private void UpdateCopies()
    {
        if (currentRadius < 0) return;
        float localWidth = original.sprite.bounds.size.x;
        int index = 0;
        for (int offset = -currentRadius; offset <= currentRadius; offset++)
        {
            if (offset == 0) continue;
            SpriteRenderer renderer = repeats[index++];
            renderer.transform.localPosition = new Vector3(offset * localWidth, 0f, 0f);
            renderer.sprite = original.sprite;
            renderer.sharedMaterials = original.sharedMaterials;
            renderer.color = original.color;
            renderer.flipX = original.flipX;
            renderer.flipY = original.flipY;
            renderer.sortingLayerID = original.sortingLayerID;
            renderer.sortingOrder = original.sortingOrder;
            renderer.maskInteraction = original.maskInteraction;
            renderer.enabled = original.enabled;
        }
    }

    private void ClearCopies()
    {
        if (repeats.Count == 0 && currentRadius < 0) return;
        foreach (SpriteRenderer renderer in repeats)
        {
            if (renderer != null)
            {
                renderer.gameObject.SetActive(false);
                Destroy(renderer.gameObject);
            }
        }
        repeats.Clear();
        currentRadius = -1;
    }

    private void OnDisable()
    {
        foreach (SpriteRenderer renderer in repeats)
            if (renderer != null) renderer.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        foreach (SpriteRenderer renderer in repeats)
            if (renderer != null) renderer.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        ClearCopies();
    }
}

}
