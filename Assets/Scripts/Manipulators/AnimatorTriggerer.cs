using UnityEngine;

public class AnimatorTriggerer : MonoBehaviour
{
    public string animatorParameterName = "";

    private Animator anim;

    private Animator GetAnim()
    {
        if(anim == null)
        {
            anim = GetComponent<Animator>();
        }
        return anim;
    }

    public void TriggerParameter()
    {
        GetAnim().SetTrigger(animatorParameterName);
    }

    public void SetParameterBoolToTrue()
    {
        GetAnim().SetBool(animatorParameterName,true);
    }

    public void SetParameterBoolToFalse()
    {
        GetAnim().SetBool(animatorParameterName,false);
    }

    public void SetParameterInt(int value)
    {
        GetAnim().SetInteger(animatorParameterName,value);
    }

    public void AdjustParameterInt(int value)
    {
        int currentValue = GetAnim().GetInteger(animatorParameterName);
        GetAnim().SetInteger(animatorParameterName,currentValue+value);
    }

    public void TriggerOtherParameter(GameObject other)
    {
        other.GetComponent<Animator>().SetTrigger(animatorParameterName);
    }

    public void SetOtherParameterBoolToTrue(GameObject other)
    {
        other.GetComponent<Animator>().SetBool(animatorParameterName,true);
    }

    public void SetOtherParameterBoolToFalse(GameObject other)
    {
        other.GetComponent<Animator>().SetBool(animatorParameterName,false);
    }
}
