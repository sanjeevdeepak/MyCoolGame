using UnityEngine;

public class GateXOr : Gate
{
    public int numberOfTrues = 1;

    protected override bool IsGateTrue()
    {
        int trueCount = 0;
        foreach (bool each in inputs)
        {
            if (each) trueCount++;
        }

        return trueCount == numberOfTrues;
    }

    public void SetNumberOfTrues(int amount)
    {
        numberOfTrues = amount;
    }
}
