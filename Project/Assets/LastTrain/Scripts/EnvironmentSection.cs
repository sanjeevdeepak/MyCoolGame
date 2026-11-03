using UnityEngine;

/// <summary>Places tunnel structures and the station in world space before they enter view.</summary>
public sealed class EnvironmentSection : MonoBehaviour
{
    public TrainLevelDirector director;
    public TrainMotion approachTrain;
    public float activationOffset;
    private Vector2 offset;
    private SpriteRenderer[] images;
    private Collider2D[] surfaces;
    private TrainMotion platform;
    private bool captured;
    public bool IsPlaced { get; private set; }

    private void Awake() { Capture(); ResetSection(); }

    private void Capture()
    {
        if(captured)return;
        offset=(Vector2)(transform.position-approachTrain.transform.position);
        images=GetComponentsInChildren<SpriteRenderer>(true);
        surfaces=GetComponentsInChildren<Collider2D>(true);
        platform=GetComponent<TrainMotion>();
        captured=true;
    }

    private void Update()
    {
        if(IsPlaced || director.IsResetting || director.IsFinished)return;
        Vector2 position=approachTrain.Body.position+offset;
        transform.position=position;
        if(platform!=null)platform.Body.position=position;
        if(director.player.GetComponent<Rigidbody2D>().position.x < approachTrain.Body.position.x+activationOffset)return;
        IsPlaced=true;
        if(platform!=null)
        {
            platform.ResetAt(position);
            platform.SetRunning(true);
        }
        SetVisible(true);
        Physics2D.SyncTransforms();
    }

    public void ResetSection()
    {
        Capture();
        IsPlaced=false;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        foreach(SpriteRenderer image in images)image.enabled=visible;
        foreach(Collider2D surface in surfaces)surface.enabled=visible;
    }
}
