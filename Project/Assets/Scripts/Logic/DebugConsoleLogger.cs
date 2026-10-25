using UnityEngine;

public class DebugConsoleLogger : MonoBehaviour
{
    public void LogToConsole(string aMessage)
    {
        Debug.Log(aMessage);
    }

    // public void LogIntToConsole(int amount)
    // {
    //     Debug.Log("Int: " + amount);
    // }

    // public void LogFloatToConsole(float amount)
    // {
    //     Debug.Log("Float: " + amount);
    // }

    // public void LogBoolToConsole(bool amount)
    // {
    //     Debug.Log("Bool: " + amount);
    // }

    public void LogPositionToConsole(Transform transform)
    {
        Debug.Log("Position {" + transform.gameObject.name + "): (" + transform.position.x + "," + transform.position.y + "," + transform.position.z + "}");
    }
    
    public void LogObjectToConsole(GameObject gameObject)
    {
        Debug.Log("Object:" + gameObject.name);
    }
}
