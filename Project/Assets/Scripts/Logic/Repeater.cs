using UnityEngine;
using UnityEngine.Events;

public class Repeater : MonoBehaviour
{
    [Tooltip("The number of times 'Repeat Event' will trigger.")]
    public int repeatNumber = 0;
    [Tooltip("The event that will be triggered the number of times equal to 'Repeat Number'")]
    public UnityEvent repeatEvent;

    [Space]
    [Header("Debug")]
    [Tooltip("Whether or not this script prints information to the debug console.")]
    public bool consoleLogRepeater = false;

    public void RepeatEvent()
    {
        RepeatEventInternal(repeatNumber);
    }

    public void RepeateEventCustomAmount(int amount)
    {
        RepeatEventInternal(amount);
    }

    private void RepeatEventInternal(int amount)
    {
        if (consoleLogRepeater) Debug.Log("Repating Event " + amount + " times: " + gameObject.name);
        for (int i = 0; i < amount; i++)
        {
            repeatEvent.Invoke();
        }
    }

    public void SetRepeatNumber(int amount)
    {
        repeatNumber = amount;
    }
    
    public void AdjustRepeatNumber(int amount)
    {
        repeatNumber += amount;
    }
}
