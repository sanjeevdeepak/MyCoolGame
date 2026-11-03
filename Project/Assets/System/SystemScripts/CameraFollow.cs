using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance;
    private PlayerController playerControl;
    private Transform target;
    private Transform player;
    [Tooltip("How smoothly the camera follows the player left/right")]
    public float xSmoothTime = 0.2f;
    [Tooltip("How smoothly the camera follows the player upwards")]
    public float yJumpSmoothTime = 0.8f;
    [Tooltip("How smoothly the camera follows the player downwards")]
    public float yFallSmoothTime = 0.1f;
    [Tooltip("How far left/right the camera shifts in the direction the player is looking")]
    public float lookDirectionOffset = 2;
    public bool pixelSnap;

    private float fallThreshold = 2f;
    private float overJumpThreshold = 10f;

    private float xRefVelocity;
    private float yRefVelocity;
    private Vector3 smoothPosition;
    private Camera view;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (instance != null)
        {
            Debug.Log("WARNING: More than one camera detected!");
        }
        instance = this;

        playerControl = PlayerController.instance;
        player = playerControl.transform;
        target = player;
        transform.position = target.position;
        smoothPosition = transform.position;
        view = GetComponentInChildren<Camera>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (target != null && Time.timeScale > 0f && Time.deltaTime > 0f)
        {
            var targetPosition = target.position;
            float currentYSmoothTime = xSmoothTime;
            if (target == player)
            {
                if (playerControl != null)
                {
                    if (playerControl.IsFacingLeft()) targetPosition += new Vector3(-lookDirectionOffset, 0, 0);
                    else targetPosition += new Vector3(lookDirectionOffset, 0, 0);

                    if (playerControl.IsFalling() && target.position.y < (transform.position.y - fallThreshold)) currentYSmoothTime = yFallSmoothTime;
                    else if(target.position.y > (transform.position.y + overJumpThreshold)) currentYSmoothTime = yFallSmoothTime;
                    else currentYSmoothTime = yJumpSmoothTime;
                }
            }

            Vector3 newPosition = Vector3.zero;
            Vector3 start = pixelSnap ? smoothPosition : transform.position;
            newPosition.x = Mathf.SmoothDamp(start.x, targetPosition.x, ref xRefVelocity, xSmoothTime);
            newPosition.y = Mathf.SmoothDamp(start.y, targetPosition.y, ref yRefVelocity, currentYSmoothTime);
            smoothPosition = newPosition;
            if (pixelSnap && view != null)
            {
                float pixel = view.orthographicSize * 2f / Mathf.Max(1, view.pixelHeight);
                newPosition.x = Mathf.Round(newPosition.x / pixel) * pixel;
                newPosition.y = Mathf.Round(newPosition.y / pixel) * pixel;
            }
            transform.position = newPosition;
        }
    }

    public void SetCameraTarget(Transform aTarget)
    {
        target = aTarget;
    }

    public void SnapTo(Transform aTarget)
    {
        target=aTarget;xRefVelocity=yRefVelocity=0;
        transform.position=aTarget.position;smoothPosition=transform.position;
    }

    public void FocusCameraToPlayer()
    {
        target = player;
    }
}
