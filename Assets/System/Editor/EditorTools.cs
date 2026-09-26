using UnityEngine;
using UnityEditor;

public class EditorTools : MonoBehaviour
{
    [MenuItem("GameKit//Warp Player To Editor Camera %w")]
    public static void WarpPlayerToEditorCamera()
    {
        if (Application.isPlaying)
        {
            Transform player = PlayerController.instance.transform;
            Transform camera = FindObjectOfType<CameraFollow>().transform;
            player.position = new Vector3(
                SceneView.lastActiveSceneView.camera.transform.position.x,
                SceneView.lastActiveSceneView.camera.transform.position.y,
                0
            );
            camera.position = new Vector3(
                player.position.x,
                player.position.y,
                camera.position.z
            );
        }
        else Debug.Log("Can only jump player to editor camera in play mode!");
    }
}
