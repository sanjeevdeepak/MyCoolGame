using UnityEngine;

public class ObjectTeleporter : MonoBehaviour
{
    [Tooltip("The object you want to teleport to this object when \"TeleportSpecificObjectHere()\" is run.")]
    public GameObject objectToTeleport;

    public void TeleportSpecificObjectHere()
    {
        objectToTeleport.transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            0
        );
    }

    public void TeleportOtherObjectHere(GameObject aObject)
    {
        aObject.transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            0
        );
    }

    public void TeleportPlayerHere()
    {
        Transform player = PlayerController.instance.transform;
        player.position = new Vector3(
            transform.position.x,
            transform.position.y,
            0
        );
    }

    public void TeleportPlayerAndCameraHere()
    {
        TeleportPlayerHere();
        Transform player = PlayerController.instance.transform;
        Transform camera = FindObjectOfType<CameraFollow>().transform;
        camera.position = new Vector3(
            player.position.x,
            player.position.y,
            camera.position.z
        );
    }
}
