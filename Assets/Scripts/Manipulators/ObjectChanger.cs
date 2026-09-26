using UnityEngine;

public class ObjectChanger : MonoBehaviour
{
    [Tooltip("The prefab to change another object into")]
    public GameObject changeObjectPrefab;
    [Tooltip("Whether or not the new object maintains the velocity that the old object had.")]
    public bool maintianVelocity = true;

    [Space]
    [Header("Debug")]
    [Tooltip("Whether or not this script prints information to the debug console.")]
    public bool consoleLog = false;

    public void ChangeObjectIntoOther(GameObject gameObject)
    {
        if (gameObject.name != string.Format("{0}(Clone)", changeObjectPrefab.name))
        {
            Rigidbody2D originalRigidbody = gameObject.GetComponent<Rigidbody2D>();
            Vector3 currentVelocity = Vector3.zero;
            if (maintianVelocity && originalRigidbody != null)
            {
                currentVelocity = originalRigidbody.linearVelocity;
            }

            GameObject newObject = GameObject.Instantiate(changeObjectPrefab, gameObject.transform.position, Quaternion.identity, gameObject.transform.parent);
            Destroy(gameObject);

            Rigidbody2D newRigidbody = newObject.GetComponent<Rigidbody2D>();
            if (maintianVelocity && newRigidbody != null)
            {
                newRigidbody.linearVelocity = currentVelocity;
            }

            if (consoleLog) Debug.Log("Changer (" + gameObject.name + ") chainging object " + gameObject.name + " into " + changeObjectPrefab.name);
        }
    }
}
