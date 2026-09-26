using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public void SetAsSpawnPoint()
    {
    PlayerController.instance.SetSpawnPoint(transform);
    }
}
