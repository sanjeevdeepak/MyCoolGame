using UnityEngine;

public class ObjectDestroyer : MonoBehaviour
{
    [Space]
    [Header("Debug")]
    [Tooltip("Whether or not this script prints information to the debug console.")]
    public bool consoleLog = false;

    public void DestroyObject()
    {
        if (consoleLog) Debug.Log("Destroying object: " + gameObject.name);
        Destroy(gameObject);
    }

    public void DestroyOtherObject(GameObject aObject)
    {
        if (consoleLog) Debug.Log("Destroying object: " + aObject.name);
        PlayerController player = aObject.GetComponent<PlayerController>();
        if(player == null) Destroy(aObject);
        else player.Respawn();
    }

    public void RespawnPlayer()
    {
        PlayerController.instance.Respawn();
    }
}
