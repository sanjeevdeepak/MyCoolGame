using MidnightBridge;
using UnityEngine;

/// <summary>Keep existing idle/run/jump art meaningful on a moving roof.</summary>
[DefaultExecutionOrder(700)]
public sealed class TrainPlayerPresentation : MonoBehaviour
{
    [Min(0f)] public float respawnFlashDuration=3f;
    [Min(.05f)] public float respawnFlashInterval=.15f;
    [Min(.1f)] public float runAnimationSpeed=18f;
    private PlayerController player;
    private Rigidbody2D body;
    private TrainRiderFrame rider;
    private HarborAnimationDriver[] visuals;
    private SpriteRenderer[] sprites;
    private bool[] originalRenderingOff;
    private float flashElapsed=-1f;
    private readonly ContactPoint2D[] contacts=new ContactPoint2D[24];
    private int resetFrame=-1;
    private bool frozen;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody2D>();
        rider = GetComponent<TrainRiderFrame>();
        visuals = GetComponentsInChildren<HarborAnimationDriver>(true);
        sprites=new SpriteRenderer[visuals.Length];originalRenderingOff=new bool[visuals.Length];
        for(int i=0;i<visuals.Length;i++)
        {
            sprites[i]=visuals[i].GetComponent<SpriteRenderer>();
            originalRenderingOff[i]=sprites[i].forceRenderingOff;
        }
    }

    private void LateUpdate()
    {
        if(frozen)return;
        UpdateFlash();
        float carrier = rider.ReferenceSpeed;
        bool onRoof=false;
        if(body.linearVelocity.y<=.2f)
        {
            int count=body.GetContacts(contacts);
            for(int i=0;i<count;i++)
            {
                if(contacts[i].normal.y<player.groundSlopeThreshold)continue;
                onRoof=true;
                TrainMotion train=contacts[i].collider.GetComponentInParent<TrainMotion>();
                if(train==null)train=contacts[i].otherCollider.GetComponentInParent<TrainMotion>();
                if(train!=null)carrier=train.CurrentSpeed;
                break;
            }
        }
        if(Time.frameCount==resetFrame)onRoof=true;
        foreach (HarborAnimationDriver visual in visuals)
        {
            if (!visual.gameObject.activeInHierarchy) continue;
            Animator animator = visual.GetComponent<Animator>();
            float speed = Time.frameCount==resetFrame?0f:Mathf.Abs(body.linearVelocity.x - carrier);
            animator.SetFloat("Speed", speed);
            animator.SetBool("Grounded", onRoof);
            animator.SetFloat("VerticalSpeed", body.linearVelocity.y);
            animator.speed = onRoof && speed > .1f ? Mathf.Clamp(speed/runAnimationSpeed,.35f,1.25f) : 1f;
            visual.GetComponent<SpriteRenderer>().flipX = player.IsFacingLeft();
        }
    }

    public void ResetPose(bool flash=true)
    {
        StopFlashing();
        frozen=false;resetFrame=Time.frameCount;
        if(flash && respawnFlashDuration>0f)flashElapsed=0f;
        foreach(HarborAnimationDriver visual in visuals)
        {
            Animator animator=visual.GetComponent<Animator>();animator.speed=1;animator.Rebind();
            animator.SetFloat("Speed",0);animator.SetFloat("VerticalSpeed",0);animator.SetBool("Grounded",true);animator.Update(0);
        }
    }

    public void Freeze()
    {
        StopFlashing();
        frozen=true;foreach(HarborAnimationDriver visual in visuals)visual.GetComponent<Animator>().speed=0;
    }

    private void UpdateFlash()
    {
        if(flashElapsed<0f)return;
        flashElapsed+=Time.deltaTime;
        if(flashElapsed>=respawnFlashDuration){StopFlashing();return;}
        bool hidden=(Mathf.FloorToInt(flashElapsed/Mathf.Max(.05f,respawnFlashInterval))&1)==1;
        for(int i=0;i<sprites.Length;i++)sprites[i].forceRenderingOff=originalRenderingOff[i] || hidden;
    }

    private void StopFlashing()
    {
        flashElapsed=-1f;
        if(sprites==null)return;
        for(int i=0;i<sprites.Length;i++)if(sprites[i]!=null)sprites[i].forceRenderingOff=originalRenderingOff[i];
    }

    private void OnDisable(){StopFlashing();}
}
