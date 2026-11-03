using System.Collections.Generic;
using MidnightBridge;
using UnityEngine;
using UnityEngine.Video;

/// <summary>Station light passes and the visual response to an actual landing.</summary>
[DefaultExecutionOrder(1050)]
[DisallowMultipleComponent]
public sealed class TrainEffects : MonoBehaviour
{
    public TrainLevelDirector director;
    public ParticleSystem landingDust;
    public StationSigns stationSigns;
    [Range(0f,.35f)] public float lightStrength=.22f;
    public Color trainTint = new Color(.78f,.87f,.94f,1f);
    public Color equipmentTint = new Color(.88f,.94f,1f,1f);
    public Color playerTint = new Color(1f,.98f,.95f,1f);
    [Min(0f)] public float landingBump=.06f;
    [Min(.05f)] public float bumpDuration=.18f;
    private Camera view;
    private VideoPlayer video;
    private SpriteRenderer[] images;
    private MaterialPropertyBlock[] original,lighting;
    private Color[] baseColors;
    private Vector3 cameraRest;
    private float bumpTime=-1f,bumpStrength;
    private int burst;
    private static readonly int Tint=Shader.PropertyToID("_Color");

    private void Awake()
    {
        view=Camera.main;cameraRest=view.transform.localPosition;
        video=director.background.GetComponent<VideoPlayer>();
        var sprites=new List<SpriteRenderer>();
        var tints=new List<Color>();
        var indicators=new HashSet<SpriteRenderer>();
        foreach(TrainEquipment control in director.equipment)if(control.indicator!=null)indicators.Add(control.indicator);
        foreach(TrainMotion train in director.trains)
        {
            TrainGifAnimator art=train.GetComponentInChildren<TrainGifAnimator>();
            if(art!=null){sprites.Add(art.GetComponent<SpriteRenderer>());tints.Add(trainTint);}
        }
        foreach(SpriteRenderer image in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(image.sharedMaterial!=null && image.sharedMaterial.shader.name=="TrainLevel/Equipment" && !indicators.Contains(image))
            {sprites.Add(image);tints.Add(equipmentTint);}
        foreach(HarborAnimationDriver visual in director.player.GetComponentsInChildren<HarborAnimationDriver>(true))
        {sprites.Add(visual.GetComponent<SpriteRenderer>());tints.Add(playerTint);}
        images=sprites.ToArray();original=new MaterialPropertyBlock[images.Length];lighting=new MaterialPropertyBlock[images.Length];baseColors=new Color[images.Length];
        for(int i=0;i<images.Length;i++)
        {
            original[i]=new MaterialPropertyBlock();lighting[i]=new MaterialPropertyBlock();
            images[i].GetPropertyBlock(original[i]);images[i].GetPropertyBlock(lighting[i]);
            baseColors[i]=(images[i].sharedMaterial.HasProperty(Tint)?images[i].sharedMaterial.GetColor(Tint):Color.white)*tints[i];
            lighting[i].SetColor(Tint,baseColors[i]);images[i].SetPropertyBlock(lighting[i]);
        }
    }

    private void LateUpdate()
    {
        if(director.IsResetting || director.IsFinished || Time.timeScale<=0f)return;
        if(video.isPrepared && video.length>0)
        {
            float spacing=(float)video.length*27.5f*.5f;
            for(int i=0;i<images.Length;i++)
            {
                if(images[i]==null || !images[i].gameObject.activeInHierarchy)continue;
                if(stationSigns!=null)
                {
                    Color light=images[i].sharedMaterial.HasProperty("_Spill")?Color.black:stationSigns.SampleLight(images[i].bounds.center);
                    lighting[i].SetColor("_Spill",Color.clear);
                    lighting[i].SetColor(Tint,baseColors[i]*new Color(1+light.r*.25f,1+light.g*.25f,1+light.b*.25f,1));
                    images[i].SetPropertyBlock(lighting[i]);continue;
                }
                float phase=Mathf.Repeat((images[i].bounds.center.x-view.transform.position.x)/spacing+(float)(video.time/video.length)*2f+.22f,1f);
                float distance=Mathf.Min(phase,1-phase)*spacing;
                float amount=1-Mathf.SmoothStep(0,1,distance/3.8f);
                float white=1-Mathf.SmoothStep(0,1,distance/1.1f);
                float warm=.5f+.5f*Mathf.Sin((float)video.time/(float)video.length*Mathf.PI*2f);
                Color tube=Color.Lerp(new Color(.6f,1.35f,1.5f,1),new Color(1.5f,.45f,1.1f,1),warm);
                Color pass=Color.Lerp(tube,new Color(1.35f,.85f,1.3f,1),white);
                lighting[i].SetColor(Tint,baseColors[i]*Color.Lerp(Color.white,pass,amount*lightStrength));
                images[i].SetPropertyBlock(lighting[i]);
            }
        }
        float offset=0;
        if(bumpTime>=0)
        {
            bumpTime+=Time.deltaTime;
            if(bumpTime>=bumpDuration)bumpTime=-1;
            else offset=-Mathf.Sin(bumpTime/bumpDuration*Mathf.PI)*bumpStrength;
        }
        float pixel=view.orthographicSize*2f/Mathf.Max(1,view.pixelHeight);
        Vector3 world=Vector3.up*(Mathf.Round(offset/pixel)*pixel);
        view.transform.localPosition=cameraRest+(view.transform.parent!=null?view.transform.parent.InverseTransformVector(world):world);
    }

    public void Land(Vector2 position,float speed,TrainMotion surface)
    {
        if(director.IsResetting || director.IsFinished || Time.timeScale<=0f)return;
        float strength=Mathf.InverseLerp(2f,25f,speed);
        if(speed>=12f)
        {
            float nextBump=landingBump*Mathf.InverseLerp(8f,25f,speed);
            bumpStrength=bumpTime>=0?Mathf.Max(bumpStrength,nextBump):nextBump;bumpTime=0;
        }
        if(surface!=null)
        {
            TrainGifAnimator art=surface.GetComponentInChildren<TrainGifAnimator>();if(art!=null)art.Land(strength);
        }
        if(landingDust==null)return;
        if(!landingDust.isPlaying)landingDust.Play();
        int count=Mathf.RoundToInt(Mathf.Lerp(3,9,strength));float carrier=surface!=null?surface.CurrentSpeed:0;
        for(int n=0;n<count;n++)
        {
            float side=n%2==0?-1f:1f;float scatter=.5f+.5f*Mathf.Sin(n*2.4f+burst);
            var particle=new ParticleSystem.EmitParams{
                position=new Vector3(position.x+side*scatter*.3f,position.y+.08f,0),
                velocity=new Vector3(carrier+side*Mathf.Lerp(.9f,2.4f,scatter),Mathf.Lerp(.7f,1.6f,strength)*(1-scatter*.35f),0),
                startLifetime=Mathf.Lerp(.16f,.25f,strength),startSize=.18f,applyShapeToPosition=false};
            landingDust.Emit(particle,1);
        }
        burst++;
    }

    public void Clear()
    {
        bumpTime=-1;bumpStrength=0;burst=0;
        if(view!=null)view.transform.localPosition=cameraRest;
        if(landingDust!=null)landingDust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(images!=null)for(int i=0;i<images.Length;i++)if(images[i]!=null)
        {lighting[i].SetColor("_Spill",Color.clear);lighting[i].SetColor(Tint,baseColors[i]);images[i].SetPropertyBlock(lighting[i]);}
    }

    private void OnDisable()
    {
        Clear();
        if(images!=null)for(int i=0;i<images.Length;i++)if(images[i]!=null)images[i].SetPropertyBlock(original[i]);
    }
}

