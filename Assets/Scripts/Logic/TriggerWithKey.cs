using UnityEngine;
using UnityEngine.Events;

public class TriggerWithKey : MonoBehaviour
{
    [Tooltip("Whether or not this trigger is enabled. You can activate or deactivate it with another trigger.")]
    public bool triggerEnabled = true;

    [Space]
    [Tooltip("Which keyboard key triggers this event. It's recommended you explain this new control in your game via text.")]
    public string keyToTrigger = "e";
    [Range(1,7)]
    [Tooltip("Which button on a gamepad triggers this event (mandatory). Butons 1-3 are face buttons, 4 and 6 are left triggers, 5 and 7 are right triggers.")]
    public int gamepadButtonToTrigger = 1;

    [Space]
    [Tooltip("The event to trigger when this the key is pressed, or the game starts (if the object is already in the level).")]
    public UnityEvent keyPressEvent;
    public UnityEvent keyReleaseEvent;

    [Space]
    public bool consoleLogOnTrigger = false;

    void Update()
    {
        if(triggerEnabled)
        {
            if(Input.GetKeyDown(keyToTrigger) || Input.GetKeyDown(GetGamepadButton()))
            {
                keyPressEvent.Invoke();
                if(consoleLogOnTrigger) Debug.Log("Key Trigger press activated: " + gameObject.name);
            }
            if(Input.GetKeyUp(keyToTrigger) || Input.GetKeyUp(GetGamepadButton()))
            {
                keyReleaseEvent.Invoke();
                if(consoleLogOnTrigger) Debug.Log("Key Trigger release activated: " + gameObject.name);
            }
        }
    }

    private string GetGamepadButton()
    {
        int realGamepadNumber = gamepadButtonToTrigger;
        if(realGamepadNumber == 1) realGamepadNumber = 0; // gamepad 1 is jump button, so we move it to zero
        return "joystick button " + realGamepadNumber;
    }
}
