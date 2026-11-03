using UnityEngine;

public sealed class TrainFinish : MonoBehaviour
{
    public TrainLevelDirector director;
    public bool checkpoint;
    public int trainIndex = 7;
    public Transform checkpointSpawn;
    public SpriteRenderer checkpointLight;
    public TrainMotion finishPlatform;
    private readonly ContactPoint2D[] contacts=new ContactPoint2D[24];
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (checkpoint) director.SetCheckpoint(trainIndex, checkpointSpawn, checkpointLight);
        else TryFinish(other);
    }
    private void OnTriggerStay2D(Collider2D other){if(!checkpoint)TryFinish(other);}
    private void TryFinish(Collider2D other)
    {
        if(director.IsFinished || other.GetComponentInParent<PlayerController>()==null)return;
        var body=other.GetComponentInParent<Rigidbody2D>();
        if(body==null || body.linearVelocity.y>.25f)return;
        var platform=finishPlatform!=null?finishPlatform:GetComponentInParent<TrainMotion>();
        int count=body.GetContacts(contacts);
        for(int i=0;i<count;i++)
            if(contacts[i].normal.y>=.7f && (contacts[i].collider.GetComponentInParent<TrainMotion>()==platform || contacts[i].otherCollider.GetComponentInParent<TrainMotion>()==platform))
            {director.CompleteLevel();return;}
    }
}
