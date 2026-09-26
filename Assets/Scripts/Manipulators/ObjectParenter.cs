using UnityEngine;
using UnityEngine.Events;

public class ObjectParenter : MonoBehaviour
{
    [Tooltip("The object you want to set to be a child of this object when \"ParentSpecificObject()\" is run.")]
    public GameObject objectToParent;

    [Tooltip("The events you want to trigger on all children when \"TriggerWithAllChildren()\" is run.")]
    public UnityEvent<GameObject> childObjectsEvent;

    public void ParentSpecificObject()
    {
        objectToParent.transform.parent = transform;
    }

    public void ParentOtherObject(GameObject aObject)
    {
        aObject.transform.parent = transform;
    }

    public void UnparentAllChildren()
    {
        foreach(Transform each in transform)
        {
            each.parent = null;
        }
    }

    public void TriggerWithAllChildren()
    {
        foreach(Transform each in transform)
        {
            childObjectsEvent.Invoke(each.gameObject);
        }
    }

    public void ParentPlayer()
    {
        PlayerController.instance.transform.parent = transform;
    }
}
