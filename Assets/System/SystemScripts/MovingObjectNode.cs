using UnityEngine;

public class MovingObjectNode : MonoBehaviour
{
    [Tooltip("The animation curve at which the object moves. \n\nWith this curve, you can change the object's position over time. The x axis represents the objects distance to the next node, and the y axis represents time. The preset icons at the bottom allow you to chose basic motions, like linear movement or smooth movement. You may also adjust the curve manually, and even add your own points.")]
    public AnimationCurve movementCurve;
    [Tooltip("How many seconds to wait at this node before proceeding.")]
    public float waitAtNodeTime = 0;
    public enum RotateDirection {Closest, Left, Right}
    [Tooltip("Which direction the object should rotate if the node is rotated.")]
    public RotateDirection rotateDirection = RotateDirection.Closest;

    [Space]
    [Header("Gizmo")]
    [Tooltip("Whether or not to draw this node. This won't be visible in the game while playing.")]
    public bool drawNode = true;
    [Tooltip("The color of this node. This won't be visible in the game while playing.")]
    public Color nodeColor = Color.white;
    [Tooltip("The icon for this node. Must correlate with the name of an image in the 'Gizmos' folder. This won't be visible in the game while playing.")]
    public string nodeIcon = "";

    [HideInInspector]
    public int lastKnownIndex = -1;
    [HideInInspector]
    public MovingObject movingObject;

    public void SetMovingObject(MovingObject movingObject)
    {
        this.movingObject = movingObject;
    }

    private void OnDrawGizmos()
    {
        if (drawNode)
        {
            Gizmos.DrawIcon(transform.position, nodeIcon, true, nodeColor);
        }
    }
}