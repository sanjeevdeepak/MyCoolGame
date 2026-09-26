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

    private float fallThreshold = 2f;
    private float overJumpThreshold = 10f;

    private float xRefVelocity;
    private float yRefVelocity;

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
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (target != null)
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
            newPosition.x = Mathf.SmoothDamp(transform.position.x, targetPosition.x, ref xRefVelocity, xSmoothTime);
            newPosition.y = Mathf.SmoothDamp(transform.position.y, targetPosition.y, ref yRefVelocity, currentYSmoothTime);
            transform.position = newPosition;
        }
    }

    public void SetCameraTarget(Transform aTarget)
    {
        target = aTarget;
    }

    public void FocusCameraToPlayer()
    {
        target = player;
    }
}
