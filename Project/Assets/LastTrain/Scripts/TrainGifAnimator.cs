using UnityEngine;

/// <summary>Displays extracted GIF frames using their original individual delays.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(800)]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class TrainGifAnimator : MonoBehaviour
{
    public Sprite[] frames = new Sprite[0];
    public float[] frameDurationsSeconds = new float[0];
    [Min(0f)] public float extendedWidth;
    [Header("Suspension")]
    [Min(0f)] public float suspensionHeight=.035f;
    [Min(0f)] public float vibration=.006f;
    [Min(0f)] public float landingDrop=.03f;

    private SpriteRenderer spriteRenderer;
    private float elapsed;
    private int frameIndex;
    private Sprite[] playback;
    private TrainMotion train;
    private Camera view;
    private Vector3 restPosition;
    private float motionTime,impactTime=-1f,impactStrength;

    public int CurrentFrame => frameIndex;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        train=GetComponentInParent<TrainMotion>();view=Camera.main;restPosition=transform.localPosition;
        if (frames == null || frames.Length == 0 || frameDurationsSeconds == null ||
            frameDurationsSeconds.Length != frames.Length)
        {
            Debug.LogError("Train GIF frames need one original frame delay per sprite.", this);
            enabled = false;
            return;
        }
        playback = frames;
        if(extendedWidth>0)
        {
            playback = new Sprite[frames.Length];
            for(int i=0;i<frames.Length;i++)
            {
                Sprite source=frames[i];
                playback[i]=Sprite.Create(source.texture,source.rect,new Vector2(.5f,0),source.pixelsPerUnit,0,SpriteMeshType.FullRect,new Vector4(92,0,92,0));
                playback[i].hideFlags=HideFlags.DontSave;
            }
            spriteRenderer.drawMode=SpriteDrawMode.Tiled;
        }
        ResetAnimation();
    }

    private void Update()
    {
        motionTime+=Time.deltaTime;
        elapsed += Time.deltaTime;
        while (elapsed >= Mathf.Max(0.001f, frameDurationsSeconds[frameIndex]))
        {
            elapsed -= Mathf.Max(0.001f, frameDurationsSeconds[frameIndex]);
            frameIndex = (frameIndex + 1) % frames.Length;
        }
        spriteRenderer.sprite = playback[frameIndex];
        FitLength();
    }

    private void LateUpdate()
    {
        if(Time.timeScale<=0f || train==null || train.stationary || train.IsPaused)return;
        float speed=train.CurrentSpeed>.05f?Mathf.Lerp(.6f,1f,Mathf.Clamp01(train.CurrentSpeed/4f)):0f;
        float phase=motionTime*6.5f+train.StartPosition.x*.37f;
        float x=Mathf.Sin(phase*2.7f)*vibration*speed;
        float y=-suspensionHeight*(.5f+.5f*Mathf.Sin(phase))*speed+Mathf.Sin(phase*3.3f)*vibration*speed;
        if(impactTime>=0f)
        {
            impactTime+=Time.deltaTime;
            if(impactTime>=.24f)impactTime=-1f;
            else y-=Mathf.Sin(impactTime/.24f*Mathf.PI)*landingDrop*impactStrength;
        }
        float pixel=view!=null?view.orthographicSize*2f/Mathf.Max(1,view.pixelHeight):.02f;
        Vector3 offset=new Vector3(Mathf.Round(x/pixel)*pixel,Mathf.Round(y/pixel)*pixel,0);
        transform.localPosition=restPosition+(transform.parent!=null?transform.parent.InverseTransformVector(offset):offset);
    }

    public void Land(float strength){impactTime=0;impactStrength=Mathf.Clamp01(strength);}

    public void ResetAnimation()
    {
        elapsed = 0f;
        motionTime=0;impactTime=-1f;transform.localPosition=restPosition;
        frameIndex = 0;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0) spriteRenderer.sprite = playback!=null ? playback[0] : frames[0];
        FitLength();
    }

    private void FitLength()
    {
        if(extendedWidth>0 && spriteRenderer!=null)
            spriteRenderer.size=new Vector2(extendedWidth/Mathf.Abs(transform.lossyScale.x),128f/32f);
    }

    private void OnDestroy()
    {
        if(playback!=null && playback!=frames)foreach(Sprite image in playback)if(image!=null)Destroy(image);
    }

    private void OnDisable(){transform.localPosition=restPosition;impactTime=-1f;}
}
