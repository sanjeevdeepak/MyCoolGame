using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    private Camera camera;
    private float originalZoom = 15;
    private float targetZoom = 15;
    private float refVelocity;

    [Tooltip("How smoothly the camera zooms in/out")]
    public float smoothTime = 0.5f;

    void Start()
    {
        camera = GetComponent<Camera>();
        originalZoom = camera.orthographicSize;
        targetZoom = camera.orthographicSize;
    }

    void Update()
    {
        camera.orthographicSize = Mathf.SmoothDamp(camera.orthographicSize, targetZoom, ref refVelocity, smoothTime);
    }

    public void SetCameraZoom(float amount)
    {
        targetZoom = amount;
    }

    public void ResetCameraZoom()
    {
        targetZoom = originalZoom;
    }

    public void SetSmoothTime(float amount)
    {
        smoothTime = amount;
    }
}
