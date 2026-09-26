using UnityEngine;

public class PhysicsObjectManipulator : MonoBehaviour
{
    private Rigidbody2D rigidbody2D;
    public Vector3 forceToAdd = Vector3.zero;

    [Space]
    [Header("Debug")]
    [Tooltip("Whether or not this script prints information to the debug console.")]
    public bool consoleLog = false;

    void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
    }

    public void ChangeBodyTypeToDynamic()
    {
        rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        if (consoleLog) Debug.Log("Changing RigidbodyType to Dynamic: " + gameObject.name);
    }

    public void ChangeBodyTypeToKinematic()
    {
        rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        if (consoleLog) Debug.Log("Changing RigidbodyType to Kinematic: " + gameObject.name);
    }

    public void ChangeBodyTypeToStatic()
    {
        rigidbody2D.bodyType = RigidbodyType2D.Static;
        if (consoleLog) Debug.Log("Changing RigidbodyType to Static: " + gameObject.name);
    }

    public void SetBodySimulated(bool enable)
    {
        rigidbody2D.simulated = enable;
        if (consoleLog) Debug.Log("Changing Rigidbody simulated to " +  enable + ": " + gameObject.name);
    }

    public void ApplyForce()
    {
        rigidbody2D.AddForce(forceToAdd);
        if (consoleLog) Debug.Log("Applying Force to Object: " + gameObject.name);
    }

    public void ChangeOtherBodyTypeToDynamic(GameObject aObject)
    {
        aObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        if (consoleLog) Debug.Log("Changing " + aObject.name + " RigidbodyType to Dynamic: " + gameObject.name);
    }

    public void ChangeOtherBodyTypeToKinematic(GameObject aObject)
    {
        aObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        if (consoleLog) Debug.Log("Changing " + aObject.name + " RigidbodyType to Kinematic: " + gameObject.name);
    }

    public void ChangeOtherBodyTypeToStatic(GameObject aObject)
    {
        aObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        if (consoleLog) Debug.Log("Changing " + aObject.name + " RigidbodyType to Static: " + gameObject.name);
    }

    public void SetOtherBodySimulatedTrue(GameObject aObject)
    {
        aObject.GetComponent<Rigidbody2D>().simulated = true;
        if (consoleLog) Debug.Log("Changing Rigidbody simulated to true: " + gameObject.name);
    }

    public void SetOtherBodySimulatedFalse(GameObject aObject)
    {
        aObject.GetComponent<Rigidbody2D>().simulated = false;
        if (consoleLog) Debug.Log("Changing Rigidbody simulated to false: " + gameObject.name);
    }


    public void ApplyForceToOtherObject(GameObject aObject)
    {
        aObject.GetComponent<Rigidbody2D>().AddForce(forceToAdd);
        if (consoleLog) Debug.Log("Applying Force to Object: " + gameObject.name);
    }
}
