using UnityEngine;

public class CameraFocuser : MonoBehaviour
{
    public void FocusCameraHere()
    {
        CameraFollow.instance.SetCameraTarget(transform);
    }

    public void FocusCameraToObject(GameObject aObject)
    {
        CameraFollow.instance.SetCameraTarget(aObject.transform);
    }

    public void FocusCameraToPlayer()
    {
        CameraFollow.instance.FocusCameraToPlayer();
    }
}
