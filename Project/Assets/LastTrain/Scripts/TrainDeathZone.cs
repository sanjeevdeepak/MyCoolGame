using UnityEngine;

public sealed class TrainDeathZone : MonoBehaviour
{
    public TrainLevelDirector director;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null) director.RespawnPlayer();
    }
    private void OnTriggerStay2D(Collider2D other){OnTriggerEnter2D(other);}
}
