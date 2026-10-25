using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RotateObject : MonoBehaviour
{
    public float rotateDirection = 0;
    private Rigidbody2D rigidbody;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        rigidbody.MoveRotation(rigidbody.rotation + rotateDirection * Time.fixedDeltaTime);
    }

    public void SetRotateDirection(float value)
    {
        rotateDirection = value;
    }
}
