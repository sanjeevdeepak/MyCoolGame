using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MoveInDirection : MonoBehaviour
{
    [Tooltip("Applies a constant force in this direction.")]
    public Vector2 moveDirection;
    public bool scriptEnabled = true;
    private Rigidbody2D rigidbody2D;

    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if(scriptEnabled)
        {
            rigidbody2D.AddForce(moveDirection);
        }
    }
}
