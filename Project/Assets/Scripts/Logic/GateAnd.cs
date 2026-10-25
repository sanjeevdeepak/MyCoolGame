using UnityEngine;

public class GateAnd : Gate
{
    protected override bool IsGateTrue()
    {
        bool allTrue = true;
        foreach (bool each in inputs)
        {
            if (!each) allTrue = false;
        }

        return allTrue;
    }
}
