using UnityEngine;
using UnityEngine.Events;

public abstract class Gate : MonoBehaviour
{
    [Tooltip("The inputs required to activate this gate. Triggers can set booleans at different slots to true or false.")]
    public bool[] inputs = new bool[2];
    [Tooltip("The event that will trigger when the conditions in the inputs are met.")]
    public UnityEvent gateEvent;

    private void Start()
    {
        CheckInputs();
    }

    public void SetInputTrue(int input)
    {
        inputs[input] = true;
        CheckInputs();
    }

    public void SetInputFalse(int input)
    {
        inputs[input] = false;
        CheckInputs();
    }

    private void CheckInputs()
    {
        if (IsGateTrue())
        {
            gateEvent.Invoke();
        }
    }

    protected abstract bool IsGateTrue();
}
