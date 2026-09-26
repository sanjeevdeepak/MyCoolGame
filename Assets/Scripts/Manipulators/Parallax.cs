using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Header("Parallax Amount")]
    [Tooltip("Determines how much parallax the layer will receive in the X direction." + 
        "\n\n1 is closest to the camera, moving almost like normal." + 
        "\n\n0 is farthest from the camera, not moving at all." + 
        "\n\nMore than one would move the layer as if it was foreground, and higher numbers are closer to the camera." + 
        "\n\nIf you need to offset your images, set them as child object to the parallaxing game object! Parallax overwrites all position values.")]
    public float xParallax = 0f; 
    [Tooltip("Determines how much parallax the layer will receive in the Y direction." + 
        "\n1 is closest to the camera, moving almost like normal." + 
        "\n0 is farthest from the camera, not moving at all." + 
        "\nMore than one would move the layer as if it was foreground, and higher numbers are closer to the camera." + 
        "\nIf you need to offset your images, set them as child object to the parallaxing game object! Parallax overwrites all position values.")]
    public float yParallax = 0f;

    private GameObject target;
    private Vector3 anchorPosition;

    void Start()
    {
        target = GameObject.FindGameObjectsWithTag("MainCamera")[0];
        anchorPosition   = target.transform.position;
    }

    void LateUpdate()
    {
        if(target)
        {
            Vector3 parallaxPosition = new Vector3(
                target.transform.position.x - ((target.transform.position.x - anchorPosition.x) * xParallax),
                target.transform.position.y - ((target.transform.position.y - anchorPosition.y) * yParallax),
                transform.position.z
            );

            transform.position = parallaxPosition;
        }
    }
}
