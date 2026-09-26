using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MoveTowardsTarget : MonoBehaviour
{
    public bool targetIsPlayer = true;
    public Transform alternativeTarget;

    private Transform playerTransform;
    private Rigidbody2D rigidbody2D;

    public float moveSpeed = 15;
    public float maxSpeed = 50;

    public bool moveInXDirection = true;
    public bool moveInYDirection = true;

    void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        playerTransform = PlayerController.instance.transform;
    }

    void FixedUpdate()
    {
        Vector2 moveDirection = Vector2.zero;
        Transform target = alternativeTarget;

        if(targetIsPlayer)
        {
            target = playerTransform;
        }

        moveDirection = (target.position - transform.position).normalized;
        moveDirection = new Vector2(
            moveInXDirection ? moveDirection.x : 0,
            moveInYDirection ? moveDirection.y : 0
        );

        rigidbody2D.AddForce(moveDirection * moveSpeed);
        rigidbody2D.linearVelocity = Vector3.ClampMagnitude(rigidbody2D.linearVelocity,maxSpeed);
    }
}
