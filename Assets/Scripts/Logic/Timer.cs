using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using TMPro;

public class Timer : MonoBehaviour
{
    [Tooltip("The time left to wait before triggering the 'Timed Event'.")]
    public float timeToWait = 1;
    [Tooltip("The event that triggers when 'Time To Wait' reaches zero.")]
    public UnityEvent timedEvent;
    [Tooltip("The number of times this timer will repeat after reaching zero. The event 'Timed Event' will trigger each time the timer reaches zero, then the timer will reset to the value in 'Time To Wait'.")]
    public int repeatCount = 0;
    [Tooltip("Whether or not the timer loops infinitely. The event 'Timed Event' will trigger each time the timer reaches zero, then the timer will reset to the value in 'Time To Wait'.")]
    public bool loopInfinitely = false;
    [Tooltip("A in-game text object that will display the time left.")]
    public TMP_Text displayText;
    [TextAreaAttribute]
    [Tooltip("The format for how 'Display Text' will show the time left. \"{0:0}\" is a placeholder for the number left, but you can add any text you want before or after it. You can change the decimal point displayed by adding decimal zeros to the right number, e.g. \"{0:0.0}\" will show one decimal point and \"{0:0.00}\" will show two.")]
    public string displayFormat = "{0:0}";
    [TextAreaAttribute]
    [Tooltip("The text that will display on 'Display Text' when the timer reaches zero.")]
    public string completeText = "";

    private bool timerActive = false;
    private float currentTimeLeft = 0;
    private int currentRepeats = 0;



    [Space]
    [Header("Debug")]
    [Tooltip("Whether or not this script prints information to the debug console.")]
    public bool consoleLogTimer = false;

    public void StartTimer()
    {
        timerActive = true;
        currentTimeLeft = timeToWait;
        currentRepeats = repeatCount;
        if (consoleLogTimer) Debug.Log("Timer Started (" + timeToWait + "s): " + gameObject.name);
    }

    public void CancelTimer()
    {
        timerActive = false;
        if (consoleLogTimer) Debug.Log("Timer Cancelled: " + gameObject.name);
    }

    public void Update()
    {
        if (timerActive)
        {
            currentTimeLeft -= Time.deltaTime;
            if (currentTimeLeft <= 0)
            {
                timedEvent.Invoke();

                if (loopInfinitely)
                {
                    currentTimeLeft = timeToWait;
                    if (consoleLogTimer) Debug.Log("Timer Triggered (" + timeToWait + "s), looping infinitely: " + gameObject.name);
                }
                else if (currentRepeats > 0)
                {
                    currentRepeats--;
                    currentTimeLeft = timeToWait;
                    if (consoleLogTimer) Debug.Log("Timer Triggered (" + timeToWait + "s), " + (currentRepeats + 1) + " repeats left: " + gameObject.name);
                }
                else
                {
                    timerActive = false;
                    if (displayText != null)
                    {
                        displayText.text = completeText;
                    }
                    if (consoleLogTimer) Debug.Log("Timer Triggered: " + gameObject.name);
                }
            }

            if (displayText != null && timerActive)
            {
                displayText.text = string.Format(displayFormat, currentTimeLeft);
            }
        }
        
    }

    public void SetTimerDuration(float amount)
    {
        timeToWait = amount;
    }

    public void AdjustTimerDuration(float amount)
    {
        timeToWait += amount;
    }

    public void SetTimerRepeatCount(int amount)
    {
        repeatCount = amount;
    }

    public void AdjustTimerRepeatCount(int amount)
    {
        repeatCount += amount;
    }

    public void SetTimerToLoopInfinitely(bool enable)
    {
        loopInfinitely = enable;
    }
}
