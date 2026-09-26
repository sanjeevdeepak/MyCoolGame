using UnityEngine;
using UnityEngine.Events;

public class TriggerOnStart : MonoBehaviour
{
    [Tooltip("The event to trigger when this object is created, or the game starts (if the object is already in the level).")]
    public UnityEvent startEvent;

    void Start()
    {
        startEvent.Invoke();
    }
}
