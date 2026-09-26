using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingObject : MonoBehaviour
{
    [Tooltip("Whether or not the object starts moving along its path at game start.")]
    public bool playOnStart = false;
    [Tooltip("Whether or not the object will loop back to the first node after reaching the last node")]
    public bool looping = false;
    [Tooltip("If true, object will stop progressing after reaching a node. Good for using triggers to move the object to certain positions.")]
    public bool stopAtEachNode = false;
    [Tooltip("The speed at which the object moves along its path. NOTE: If you change this number while the platform is moving, the speed won't update until it reaches the next node.")]
    public float speed = 1;
    [Tooltip("The default animation curve at which the object moves. \n\nWith this curve, you can change the object's position over time. The x axis represents the objects distance to the next node, and the y axis represents time. The preset icons at the bottom allow you to chose basic motions, like linear movement or smooth movement. You may also adjust the curve manually, and even add your own points.")]
    public AnimationCurve defaultMovementCurve;
    [Tooltip("The nodes that form the path for this object. To add a new node, click the \"Add Node\" button below this list. This will create a new node wherever your editor camera is positioned.\n\nYou can drag nodes around to alter their position. You can also rotate and scale nodes, and the moving object will rotate and scale accordingly to the node as it moves towards it. You can delete node objects and they will be removed from this list automatically.")]
    [SerializeField]
    public List<MovingObjectNode> nodes = new List<MovingObjectNode>();

    private int currentNodeIndex = 0;
    private MovingObjectNode currentNode;
    private float currentNodeStartTime = 0;
    private Vector3 currentNodeStartPosition = Vector3.zero;
    private Vector3 currentNodeStartScale = Vector3.zero;
    private float currentNodeStartRotation = 0;
    private Vector3 objectOriginalScale = Vector3.one;
    private Vector3 currentTargetScale = Vector3.one;

    private bool isMoving = false;
    private Rigidbody2D rigidbody2D;
    private bool startedWaitingForNextNode = false;

    [Space]
    [Header("Gizmos")]
    [Tooltip("Whether or not this moving object should draw lines between its nodes. This won't be visible in the game while playing.")]
    public bool drawLines = true;
    [Tooltip("The color of the nodes for this moving object. This won't be visible in the game while playing.")]
    public Color nodeColor = new Color(1, 1, 1, 1f);
    [Tooltip("Whether or not this script prints information to the debug console.")]
    public bool consoleLog = false;
    [HideInInspector]
    public string movingObjectIcon = "MovingObjectNode.png";
    private MovingObjectNode startNode;

    void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        Transform startPoint = new GameObject(gameObject.name + "StartPoint").transform;
        startPoint.parent = transform.parent;
        startPoint.position = transform.position;
        startNode = startPoint.gameObject.AddComponent<MovingObjectNode>();
        startNode.movementCurve = defaultMovementCurve;
        objectOriginalScale = transform.localScale;
        isMoving = playOnStart;
        if (nodes.Count > 0) currentNode = nodes[0];
        RecordCurrentStartValues();
    }

    void FixedUpdate()
    {
        if (isMoving && currentNode != null)
        {
            float currentCurveValue = currentNode.movementCurve.Evaluate((Time.time - currentNodeStartTime) * speed);

            rigidbody2D.MovePositionAndRotation(
                Vector2.Lerp(currentNodeStartPosition, currentNode.transform.position, currentCurveValue),
                Mathf.Lerp(currentNodeStartRotation, currentNode.transform.eulerAngles.z, currentCurveValue)
            );
            transform.localScale = Vector3.Lerp(currentNodeStartScale, currentTargetScale, currentCurveValue);

            if (!stopAtEachNode && currentCurveValue >= 1)
            {
                if (currentNode.waitAtNodeTime > 0)
                {
                    if (!startedWaitingForNextNode)
                    {
                        startedWaitingForNextNode = true;
                        StartCoroutine(WaitToMoveToNextNode(currentNode.waitAtNodeTime));
                    }
                }
                else MoveToNextNode();
            }
        }
    }

    private IEnumerator WaitToMoveToNextNode(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        MoveToNextNode();
    }

    public void MoveToNextNode()
    {
        currentNodeIndex++;
        if (currentNodeIndex >= nodes.Count)
        {
            if (looping || !isMoving)
            {
                currentNodeIndex = 0;
                MoveToNode(currentNodeIndex);
            }
            else isMoving = false;
        }
        else MoveToNode(currentNodeIndex);
    }

    public void MoveToNode(int index)
    {
        isMoving = true;
        currentNodeIndex = index;
        currentNode = nodes[currentNodeIndex];
        RecordCurrentStartValues();
        GetStartRotation();
        startedWaitingForNextNode = false;
    }

    private void GetStartRotation()
    {
        if (currentNode.rotateDirection == MovingObjectNode.RotateDirection.Left)
        {
            if (currentNode.transform.eulerAngles.z < currentNodeStartRotation)
            {
                currentNodeStartRotation -= 360;
            }
        }
        else if (currentNode.rotateDirection == MovingObjectNode.RotateDirection.Right)
        {
            if (currentNode.transform.eulerAngles.z > currentNodeStartRotation)
            {
                currentNodeStartRotation += 360;
            }
        }
        else
        {
            currentNodeStartRotation = GetBestStartRotation(currentNodeStartRotation, currentNode.transform.eulerAngles.z);
        }
    }

    private float GetBestStartRotation(float myAngle, float targetAngle)
    {
        float bestAngle = myAngle;
        float currentDistane = Mathf.Abs((myAngle) - targetAngle);
        float plusDistane = Mathf.Abs((myAngle + 360) - targetAngle);
        float minusDistane = Mathf.Abs((myAngle - 360) - targetAngle);

        if (plusDistane < currentDistane)
        {
            return myAngle + 360;
        }
        else if (minusDistane < currentDistane)
        {
            return myAngle - 360;
        }
        return myAngle;
    }

    private void RecordCurrentStartValues()
    {
        currentNodeStartTime = Time.time;
        currentNodeStartPosition = transform.position;
        currentNodeStartRotation = transform.eulerAngles.z;
        currentNodeStartScale = transform.localScale;
        currentTargetScale = GetCurrentNodeScale();
    }

    public void MoveInstantToNode(int index)
    {
        currentNode = nodes[index];
        transform.position = currentNode.transform.position;
        transform.rotation = currentNode.transform.rotation;
        transform.localScale = GetCurrentNodeScale();
        RecordCurrentStartValues();
        isMoving = false;
    }

    private Vector3 GetCurrentNodeScale()
    {
        if (objectOriginalScale != null && currentNode != null)
        {
            return new Vector3(
                currentNode.transform.localScale.x * objectOriginalScale.x,
                currentNode.transform.localScale.y * objectOriginalScale.y,
                currentNode.transform.localScale.z * objectOriginalScale.z
            );
        }
        return Vector3.one;
    }

    public void ResetPosition()
    {
        currentNode = startNode;
    }

    private void OnDrawGizmos()
    {
        if (drawLines)
        {
            Gizmos.color = nodeColor;

            for (int i = 0; i < nodes.Count; i++)
            {
                MovingObjectNode eachNode = nodes[i];
                if (eachNode != null && eachNode.transform != null)
                {
                    if (i > 0 && nodes[i - 1] != null && nodes[i - 1].transform != null)
                    {
                        Gizmos.DrawLine(nodes[i - 1].transform.position, nodes[i].transform.position);
                    }
                    else
                    {
                        Gizmos.DrawLine(transform.position, nodes[i].transform.position);
                    }
                }
            }
        }
    }

    public void SetLooping(bool enable)
    {
        looping = enable;
    }

    public void SetStopAtEachNode(bool enable)
    {
        stopAtEachNode = enable;
    }

    public void SetSpeed(float aSpeed)
    {
        speed = aSpeed;
        currentNodeStartTime = Time.time;
        currentNodeStartPosition = transform.position;
        currentNodeStartRotation = transform.eulerAngles.z;
        currentNodeStartScale = transform.localScale;
        currentTargetScale = GetCurrentNodeScale();
    }
}