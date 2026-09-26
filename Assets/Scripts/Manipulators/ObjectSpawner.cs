using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Tooltip("The object that can be spawned by this component. When spawning specific objects, choose the index of the object to spawn, starting with 0.")]
    public GameObject[] prefabs;
    private int currentIndex = 0;

    [Space]
    [Header("Debug")]
    [Tooltip("Whether or not this script prints information to the debug console.")]
    public bool consoleLog = false;

    public void SpawnObject(int index)
    {
        GameObject.Instantiate(prefabs[index], transform.position, Quaternion.identity, null);
        if (consoleLog) Debug.Log("Spawner (" + gameObject.name + ") spawning object: " + prefabs[index].name);
    }

    public void SpawnRandomObject()
    {
        int randomIndex = Random.Range(0, prefabs.Length);
        GameObject.Instantiate(prefabs[randomIndex], transform.position, Quaternion.identity, null);
        if (consoleLog) Debug.Log("Spawner (" + gameObject.name + ") spawning object: " + prefabs[randomIndex].name);
    }

    public void SpawnNextObject()
    {
        GameObject.Instantiate(prefabs[currentIndex], transform.position, Quaternion.identity, null);
        if (consoleLog) Debug.Log("Spawner (" + gameObject.name + ") spawning object " + currentIndex + ": " + prefabs[currentIndex].name);
        currentIndex++;
        if (currentIndex >= prefabs.Length) currentIndex = 0;
    }
}
