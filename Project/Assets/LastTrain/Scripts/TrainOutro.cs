using System.Collections;
using System.Collections.Generic;
using MidnightBridge;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The final platform escape and delayed completion presentation.</summary>
[DefaultExecutionOrder(1250)]
[DisallowMultipleComponent]
public sealed class TrainOutro : MonoBehaviour
{
    public TrainLevelDirector director;
    public TrainFinish finishMarker;
    public Sprite wallSurface;
    public Material wallMaterial, lightMaterial;
    [Min(.1f)] public float runDuration=3.6f;
    [Min(.1f)] public float holdDuration=1.1f;
    [Min(.1f)] public float fadeDuration=.4f;
    public bool IsPlaying { get; private set; }
    public string Stage { get; private set; }="Waiting";
    private Transform scenery, doorway;
    private TrainView framing;
    private float originalViewHeight;
    private Sprite tile, pixel;
    private Image fade;
    private readonly List<SpriteRenderer> flicker=new List<SpriteRenderer>();
    private readonly List<Color> lightColors=new List<Color>();
    private HarborAnimationDriver[] drivers;
    private bool[] driverStates, renderingOff;
    private SpriteRenderer[] playerImages;
    private Animator[] animators;
    private bool started;

    private void Awake()
    {
        var visuals=director.player.GetComponentsInChildren<HarborAnimationDriver>(true);
        var image=visuals[0].GetComponent<SpriteRenderer>();
        var platform=finishMarker.finishPlatform;
        foreach(var surface in platform.GetComponentsInChildren<BoxCollider2D>(true))
            if(surface.name=="Platform Surface")
            {
                float floor=surface.transform.TransformPoint(surface.offset+Vector2.up*surface.size.y*.5f).y;
                float edge=surface.transform.TransformPoint(surface.offset+Vector2.right*surface.size.x*.5f).x;
                BuildExit(edge-.5f,floor,image.sortingOrder,image.sortingLayerID);
                scenery.SetParent(platform.transform,true);scenery.gameObject.SetActive(false);break;
            }
    }
    private void Update()
    {
        if(!started && scenery!=null)
        {
            var section=finishMarker.finishPlatform.GetComponent<EnvironmentSection>();
            scenery.gameObject.SetActive(section==null || section.IsPlaced);
        }
    }

    public IEnumerator Play()
    {
        if(started)yield break;started=true;IsPlaying=true;Stage="Arrival";
        var view=Camera.main;framing=view.GetComponent<TrainView>();originalViewHeight=framing.viewHeight;
        director.cameraFollow.enabled=false;
        var body=director.player.GetComponent<Rigidbody2D>();
        Vector3 origin=director.player.transform.position;
        drivers=director.player.GetComponentsInChildren<HarborAnimationDriver>(true);
        driverStates=new bool[drivers.Length];animators=new Animator[drivers.Length];playerImages=new SpriteRenderer[drivers.Length];renderingOff=new bool[drivers.Length];
        for(int i=0;i<drivers.Length;i++)
        {
            driverStates[i]=drivers[i].enabled;drivers[i].enabled=false;
            animators[i]=drivers[i].GetComponent<Animator>();playerImages[i]=drivers[i].GetComponent<SpriteRenderer>();
            renderingOff[i]=playerImages[i].forceRenderingOff;
        }
        scenery.gameObject.SetActive(true);
        float exitX=doorway.position.x;
        float closeHeight=originalViewHeight*.9f;
        float rightEdge=exitX+.5f;
        float clearance=0;
        foreach(var image in playerImages)clearance=Mathf.Max(clearance,image.bounds.size.x);
        float distance=Mathf.Max(exitX+3.6f,rightEdge+clearance+.25f)-origin.x;
        float runSpeed=distance/runDuration;
        var signs=director.background.GetComponent<StationSigns>();
        if(signs!=null)foreach(var image in signs.GetComponentsInChildren<SpriteRenderer>())
            if(image.enabled && flicker.Count<3){flicker.Add(image);lightColors.Add(image.color);}
        var overlay=new GameObject("Ending Fade",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        overlay.transform.SetParent(director.ui.transform,false);overlay.transform.SetAsFirstSibling();
        fade=overlay.GetComponent<Image>();fade.color=Color.clear;fade.raycastTarget=false;
        var rect=(RectTransform)overlay.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        director.ui.passed.SetAsLastSibling();
        Vector3 cameraStart=director.cameraFollow.transform.position;
        Vector3 cameraOffset=view.transform.position-cameraStart;
        Vector3 cameraEnd=cameraStart;
        // The doorway crosses the fitted gameplay frame's right edge, including
        // on displays with bars. Settle here before the player runs out of view.
        cameraEnd.x=rightEdge-closeHeight*.5f*(16f/9f)-cameraOffset.x;
        cameraEnd.y=origin.y+(view.transform.position.y-origin.y)*.9f-cameraOffset.y;
        float settleAt=Mathf.Min(1.8f,.3f+runDuration*.45f);
        float elapsed=0,footAt=.3f;bool escaped=false;
        float runEnd=.3f+runDuration,holdEnd=runEnd+holdDuration;
        while(elapsed<holdEnd)
        {
            if(Time.timeScale>0f)
            {
                elapsed+=Time.deltaTime;
                float p=Mathf.Clamp01((elapsed-.3f)/runDuration);
                Stage=p<=0?"Arrival":p<1?"Running":"Empty Platform";
                Vector3 position=origin+Vector3.right*(p*distance);
                director.player.transform.position=position;body.position=position;
                SetPose(p>0 && p<1,runSpeed);
                float zoom=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/1.2f));
                framing.viewHeight=Mathf.Lerp(originalViewHeight,closeHeight,zoom);
                float follow=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/settleAt));
                Vector3 camera=Vector3.Lerp(cameraStart,cameraEnd,follow);
                float step=framing.viewHeight/Mathf.Max(1,framing.PixelRect.height);
                camera.x=Mathf.Round(camera.x/step)*step;
                camera.y=Mathf.Round(camera.y/step)*step;
                director.cameraFollow.transform.position=camera;
                bool outside=true;
                float visibleRight=camera.x+cameraOffset.x+framing.HalfWidth;
                foreach(var image in playerImages)
                    if(image.enabled && image.gameObject.activeInHierarchy && image.bounds.min.x<=visibleRight)outside=false;
                if(outside && !escaped)
                {
                    escaped=true;director.sounds.StopEndingSteps();
                }
                if(!escaped && p>0 && p<1 && elapsed>=footAt){director.sounds.EndingStep();footAt=elapsed+.38f;}
                Flicker(elapsed);
            }
            yield return null;
        }
        Stage="Fade";director.sounds.StopEndingSteps();
        for(float t=0;t<fadeDuration;t+=Time.deltaTime)
        {
            fade.color=new Color(0,0,0,Mathf.Clamp01(t/fadeDuration));yield return null;
        }
        fade.color=Color.black;
        Stage="Passed";director.ui.ShowPassed();Canvas.ForceUpdateCanvases();director.sounds.ShowCompletion();IsPlaying=false;
    }
    private void SetPose(bool running,float speed)
    {
        var presentation=director.player.GetComponent<TrainPlayerPresentation>();
        float animationSpeed=presentation!=null?presentation.runAnimationSpeed:18f;
        for(int i=0;i<animators.Length;i++)
        {
            animators[i].speed=running?Mathf.Clamp(speed/animationSpeed,.35f,1.25f):1f;animators[i].SetFloat("Speed",running?speed:0);
            animators[i].SetBool("Grounded",true);animators[i].SetFloat("VerticalSpeed",0);playerImages[i].flipX=false;
        }
    }
    private void BuildExit(float x,float floor,int order,int layer)
    {
        scenery=new GameObject("Station Exit").transform;
        tile=Sprite.Create(wallSurface.texture,wallSurface.rect,new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);
        pixel=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f),Texture2D.whiteTexture.width,0,SpriteMeshType.FullRect);
        tile.hideFlags=pixel.hideFlags=HideFlags.DontSave;
        doorway=Panel("Dark Doorway",new Vector2(x,floor+2.9f),new Vector2(4.4f,5.8f),new Color(.008f,.011f,.02f),order-1,layer,false).transform;
        Panel("Exit Pier",new Vector2(x-2.6f,floor+3.2f),new Vector2(.8f,6.4f),new Color(.7f,.73f,.8f),order+1,layer,true);
        Panel("Exit Lintel",new Vector2(x+1f,floor+6.1f),new Vector2(8f,.65f),new Color(.7f,.73f,.8f),order+2,layer,true);
        Panel("Exit Return Wall",new Vector2(x+4.6f,floor+3f),new Vector2(5f,6f),new Color(.43f,.45f,.53f),order+2,layer,true);
        var lamp=Panel("Exit Light",new Vector2(x-2.09f,floor+4.3f),new Vector2(.049f,2.5f),new Color(.48f,.91f,1),order+3,layer,false);
        flicker.Add(lamp);lightColors.Add(lamp.color);
        Panel("Pink Fixture",new Vector2(x,floor+5.68f),new Vector2(2.7f,.049f),new Color(1,.43f,.84f),order+3,layer,false);
    }
    private SpriteRenderer Panel(string name,Vector2 position,Vector2 size,Color color,int order,int layer,bool textured)
    {
        var obj=new GameObject(name);obj.transform.SetParent(scenery,false);obj.transform.position=new Vector3(position.x,position.y,0);
        var image=obj.AddComponent<SpriteRenderer>();image.sprite=textured?tile:pixel;image.sharedMaterial=textured?wallMaterial:lightMaterial;
        image.color=color;image.sortingLayerID=layer;image.sortingOrder=order;
        if(textured){obj.transform.localScale=Vector3.one*1.5696203f;image.drawMode=SpriteDrawMode.Tiled;image.size=size/1.5696203f;}
        else obj.transform.localScale=new Vector3(size.x,size.y,1);
        return image;
    }
    private void Flicker(float time)
    {
        for(int i=0;i<flicker.Count;i++)
        {
            float phase=time+i*.37f;
            float dip=.13f*Mathf.Exp(-Mathf.Pow((phase-1.6f)/.1f,2))+.16f*Mathf.Exp(-Mathf.Pow((phase-3.15f)/.12f,2))+.12f*Mathf.Exp(-Mathf.Pow((phase-4.4f)/.08f,2));
            float brightness=.98f+.02f*Mathf.Sin(phase*3.1f)-dip;
            Color color=lightColors[i];color.r*=brightness;color.g*=brightness;color.b*=brightness;flicker[i].color=color;
        }
    }
    private void OnDisable()
    {
        if(director!=null && director.sounds!=null)director.sounds.StopEndingSteps();
        if(framing!=null)framing.viewHeight=originalViewHeight;
        for(int i=0;i<flicker.Count;i++)if(flicker[i]!=null)flicker[i].color=lightColors[i];
        if(drivers!=null)for(int i=0;i<drivers.Length;i++)if(drivers[i]!=null){drivers[i].enabled=driverStates[i];playerImages[i].forceRenderingOff=renderingOff[i];}
        if(scenery!=null)Destroy(scenery.gameObject);if(fade!=null)Destroy(fade.gameObject);
        if(tile!=null)Destroy(tile);if(pixel!=null)Destroy(pixel);
    }
}
