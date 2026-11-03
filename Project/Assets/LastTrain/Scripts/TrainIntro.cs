using System.Collections;
using System.Collections.Generic;
using MidnightBridge;
using UnityEngine;
using UnityEngine.Video;

/// <summary>A staged rooftop escape; normal simulation starts after the black transition.</summary>
[DefaultExecutionOrder(-450)]
[DisallowMultipleComponent]
public sealed class TrainIntro : MonoBehaviour
{
    public TrainLevelDirector director;
    public Sprite wallSurface;
    public Material wallMaterial;
    public AudioClip wind, crash;
    [Range(0f,1f)] public float volume=.65f;
    [Min(.1f)] public float closeViewHeight=10.8f;
    [Min(.1f)] public float fadeDuration=.4f;
    public bool IsPlaying => enabled && !finished;
    public string Stage { get; private set; }="Preparing";
    private bool finished, restored;
    private float black=1f, clock, height, originalTime, originalFixed;
    private Transform actors, runaway, landing, wall;
    private Rigidbody2D body;
    private TrainView framing;
    private Camera view;
    private StationSigns signs;
    private PauseController pause;
    private bool pauseEnabled, signsEnabled;
    private Vector3 cameraLocal, takeoff, touchdown, runawayStart;
    private float roof, wallX;
    private readonly List<SpriteRenderer> hidden=new List<SpriteRenderer>();
    private readonly List<bool> visibility=new List<bool>();
    private HarborAnimationDriver[] drivers;
    private Animator[] animators;
    private AnimatorUpdateMode[] animatorModes;
    private AudioSource rumble, clacks, air, effects, impact;
    private Sprite wallTile;

    private void Awake()
    {
        originalTime=Time.timeScale;originalFixed=Time.fixedDeltaTime;
        body=director.player.GetComponent<Rigidbody2D>();
        director.player.EnablePlayerControl(false);director.player.enabled=false;
        body.simulated=false;
        pause=FindAnyObjectByType<PauseController>();
        if(pause!=null){pauseEnabled=pause.enabled;pause.enabled=false;}
    }

    public IEnumerator Play()
    {
        view=Camera.main;framing=view.GetComponent<TrainView>();height=framing.viewHeight;cameraLocal=view.transform.localPosition;
        director.cameraFollow.SetCameraTarget(null);director.cameraFollow.enabled=false;
        director.player.GetComponent<TrainPlayerPresentation>().Freeze();
        drivers=director.player.GetComponentsInChildren<HarborAnimationDriver>(true);
        foreach(var driver in drivers)driver.enabled=false;
        animators=new Animator[drivers.Length];for(int i=0;i<drivers.Length;i++)animators[i]=drivers[i].GetComponent<Animator>();
        animatorModes=new AnimatorUpdateMode[animators.Length];
        for(int i=0;i<animators.Length;i++){animatorModes[i]=animators[i].updateMode;animators[i].updateMode=AnimatorUpdateMode.UnscaledTime;animators[i].speed=1;}
        signs=director.background.GetComponent<StationSigns>();signsEnabled=signs!=null && signs.enabled;if(signs!=null)signs.enabled=false;
        foreach(var image in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(!image.transform.IsChildOf(director.player.transform)){hidden.Add(image);visibility.Add(image.enabled);image.enabled=false;}
        Shader.SetGlobalFloat("_IntroLighting",1);Shader.SetGlobalFloat("_IntroBlur",.009f);
        actors=new GameObject("Opening").transform;
        var source=director.trains[0].GetComponentInChildren<TrainGifAnimator>();
        runaway=CopyTrain(source,"Runaway Train");landing=CopyTrain(source,"Landing Train");
        roof=director.player.GetComponent<Collider2D>().bounds.min.y;
        float width=TrainLevelDirector.TrainWidth(director.trains[0]);
        Vector3 origin=director.player.transform.position;
        float front=5.3f;
        runawayStart=source.transform.position;runawayStart.x=origin.x+front-width*.5f;
        wallX=origin.x+60.5f;
        takeoff=origin+Vector3.right*53.2f;
        touchdown=new Vector3(wallX+9f,origin.y+1.5f,origin.z);
        landing.position=new Vector3(wallX+2.5f+width*.5f,source.transform.position.y+1.5f,source.transform.position.z);
        BuildWall();
        rumble=Voice("Runaway Rumble",director.sounds.rumble,true,.1f);
        clacks=Voice("Fast Track",director.sounds.track,true,.1f);
        air=Voice("Wind",wind,true,.05f);effects=Voice("Opening Effects",null,false,1);impact=Voice("Crash",crash,false,.72f);
        var video=director.background.GetComponent<VideoPlayer>();video.playbackSpeed=4.5f;
        SetActor(origin,true,0);framing.viewHeight=closeViewHeight;CameraPose(origin,0,0);
        float wait=0;while(!video.isPrepared && wait<5f){wait+=Time.unscaledDeltaTime;yield return null;}
        yield return Fade(0,fadeDuration);
        bool brakePlayed=false,jumped=false,crashed=false,landed=false;
        clock=0;
        while(clock<5.8f)
        {
            float dt=Time.unscaledDeltaTime;clock+=dt;
            float t=clock;
            if(t<3.05f)
            {
                Stage=t<2.4f?"Runaway":"Wall";
                float travel=t<1.8f?t*22f:39.6f+(t-1.8f)*10.88f;
                Vector3 position=origin+Vector3.right*Mathf.Min(53.2f,travel);
                SetActor(position,true,0);runaway.position=runawayStart+Vector3.right*Mathf.Min(53.2f,travel)+Vibration(t,.055f);
                if(t>2.2f && !brakePlayed){brakePlayed=true;Cue(director.sounds.brake,.48f,.85f);}
                float intensity=Mathf.SmoothStep(.35f,1f,t/3.05f);
                SetBeds(intensity,1.6f+intensity*.65f);CameraPose(position,.04f+intensity*.045f,Mathf.SmoothStep(0,.67f,(t-1.8f)/.6f));
            }
            else
            {
                if(!jumped){jumped=true;Time.timeScale=.22f;Cue(director.player.jumpSound,.6f,1f);}
                float p=Mathf.Clamp01((t-3.05f)/2f);
                Vector3 position=Vector3.Lerp(takeoff,touchdown,p)+Vector3.up*(Mathf.Sin(p*Mathf.PI)*5.8f);
                float vertical=(touchdown.y-takeoff.y)/2f+Mathf.Cos(p*Mathf.PI)*5.8f*Mathf.PI/2f;
                Stage=p<1?"Jump":"Landing";
                SetActor(position,p>=1,vertical);
                runaway.position=runawayStart+Vector3.right*(53.2f+Mathf.Min(2f,(t-3.05f)/1.75f*2f))+Vibration(t,.04f);
                SetBeds(p<1?.28f:.12f,p<1?.7f:1f);
                if(t>=4.8f && !crashed)
                {
                    crashed=true;Stage="Crash";Time.timeScale=1f;
                    runaway.gameObject.SetActive(false);impact.Play();
                }
                if(p>=1 && !landed){landed=true;Time.timeScale=1f;Cue(director.sounds.hardLand,.48f,1f);}
                float bump=crashed?Mathf.Clamp01(1-(t-4.8f)/.23f)*.16f:0;
                CameraPose(position,.018f+bump,Mathf.SmoothStep(0,1,p/.7f));
            }
            yield return null;
        }
        Stage="Fade Out";yield return Fade(1,fadeDuration);
        Stage="Black";RestoreSettings();
        if(actors!=null){actors.gameObject.SetActive(false);Destroy(actors.gameObject);actors=null;}
        director.PrepareOpeningFinish();
        yield return null;
        director.sounds.BeginGameplay();
        Stage="Fade In";yield return Fade(0,fadeDuration);
        finished=true;Stage="Gameplay";
        foreach(var driver in drivers)if(driver!=null)driver.enabled=true;
        if(pause!=null)pause.enabled=pauseEnabled;
    }

    private Transform CopyTrain(TrainGifAnimator source,string name)
    {
        var copy=Instantiate(source.gameObject,actors);copy.name=name;
        copy.transform.localScale=source.transform.lossyScale;
        foreach(var image in copy.GetComponentsInChildren<SpriteRenderer>(true))image.enabled=true;
        return copy.transform;
    }
    private void BuildWall()
    {
        wallTile=Sprite.Create(wallSurface.texture,wallSurface.rect,new Vector2(.5f,.5f),32f,0,SpriteMeshType.FullRect);
        wallTile.hideFlags=HideFlags.DontSave;
        wall=new GameObject("Track Wall").transform;wall.SetParent(actors,false);
        float top=roof+1.1f,bottom=director.trains[0].transform.position.y-.6f;
        Panel("Wall Panels",new Vector2(wallX+1.2f,(top+bottom)*.5f),new Vector2(2.4f,top-bottom),new Color(.67f,.72f,.8f));
        Panel("Wall Edge",new Vector2(wallX+.1f,(top+bottom)*.5f),new Vector2(.2f,top-bottom+.2f),new Color(.95f,.98f,1));
        Panel("Wall Cap",new Vector2(wallX+1.2f,top),new Vector2(2.7f,.22f),Color.white);
        for(int i=0;i<7;i++)Panel("Steel Joint",new Vector2(wallX+1.2f,bottom+.5f+i),new Vector2(2.35f,.08f),new Color(.35f,.4f,.5f));
    }
    private void Panel(string name,Vector2 position,Vector2 size,Color color)
    {
        var obj=new GameObject(name);obj.transform.SetParent(wall,false);obj.transform.position=new Vector3(position.x,position.y,0);
        obj.transform.localScale=Vector3.one*1.5696203f;
        var image=obj.AddComponent<SpriteRenderer>();image.sprite=wallTile;image.sharedMaterial=wallMaterial;image.color=color;
        image.drawMode=SpriteDrawMode.Tiled;image.size=size/1.5696203f;image.sortingOrder=4;
    }
    private AudioSource Voice(string name,AudioClip clip,bool loop,float gain)
    {
        var obj=new GameObject(name);obj.transform.SetParent(actors,false);var source=obj.AddComponent<AudioSource>();
        source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;source.clip=clip;source.volume=Gain(gain);
        if(loop && clip!=null)source.Play();return source;
    }
    private float Gain(float gain)=>director.sounds.masterVolume*volume*gain;
    private void Cue(AudioClip clip,float gain,float pitch)
    {
        effects.Stop();effects.clip=clip;effects.pitch=pitch;effects.volume=Gain(gain);if(clip!=null)effects.Play();
    }
    private void SetBeds(float intensity,float pitch)
    {
        rumble.volume=Gain(.42f*intensity);clacks.volume=Gain(.25f*intensity);air.volume=Gain(.22f*intensity);
        rumble.pitch=Mathf.Lerp(.85f,1.25f,intensity);clacks.pitch=pitch;air.pitch=Mathf.Lerp(.7f,1.1f,intensity);
        director.background.GetComponent<VideoPlayer>().playbackSpeed=clock<3.05f?4.5f:clock<4.8f?.5f:1.2f;
        Shader.SetGlobalFloat("_IntroBlur",clock<3.05f?.009f:.002f);
    }
    private void SetActor(Vector3 position,bool grounded,float vertical)
    {
        director.player.transform.position=position;body.position=position;
        foreach(var animator in animators){animator.speed=grounded?1f:.55f;animator.SetFloat("Speed",0);animator.SetBool("Grounded",grounded);animator.SetFloat("VerticalSpeed",vertical);}
        foreach(var driver in drivers)driver.GetComponent<SpriteRenderer>().flipX=false;
    }
    private Vector3 Vibration(float t,float strength)=>new Vector3(Mathf.Sin(t*93)*strength*.45f,Mathf.Sin(t*67)*strength,0);
    private void CameraPose(Vector3 position,float shake,float reveal)
    {
        float pixel=framing.viewHeight/Mathf.Max(1,framing.PixelRect.height);
        Vector3 focus=position+new Vector3(Mathf.Lerp(2.5f,5.5f,reveal),.65f,0)+Vibration(clock,shake);focus.z=0;
        focus.x=Mathf.Round(focus.x/pixel)*pixel;focus.y=Mathf.Round(focus.y/pixel)*pixel;
        director.cameraFollow.transform.position=focus;
    }
    private IEnumerator Fade(float target,float duration)
    {
        float from=black;for(float t=0;t<duration;t+=Time.unscaledDeltaTime)
        {
            black=Mathf.Lerp(from,target,t/duration);
            if(target>0 && actors!=null)foreach(var source in actors.GetComponentsInChildren<AudioSource>())source.volume*=Mathf.Clamp01(1-Time.unscaledDeltaTime/Mathf.Max(.02f,duration-t));
            yield return null;
        }
        black=target;
    }
    private void RestoreSettings()
    {
        if(restored)return;restored=true;
        Time.timeScale=originalTime;Time.fixedDeltaTime=originalFixed;
        Shader.SetGlobalFloat("_IntroLighting",0);Shader.SetGlobalFloat("_IntroBlur",0);
        if(framing!=null)framing.viewHeight=height;
        if(view!=null)view.transform.localPosition=cameraLocal;
        if(director.background!=null)director.background.GetComponent<VideoPlayer>().playbackSpeed=1f;
        for(int i=0;i<hidden.Count;i++)if(hidden[i]!=null)hidden[i].enabled=visibility[i];
        if(signs!=null)signs.enabled=signsEnabled;
        if(director.cameraFollow!=null)director.cameraFollow.enabled=true;
        if(animators!=null)for(int i=0;i<animators.Length;i++)if(animators[i]!=null){animators[i].updateMode=animatorModes[i];animators[i].speed=1;}
    }
    private void OnGUI()
    {
        if(black<=0 || Event.current.type!=EventType.Repaint)return;
        int depth=GUI.depth;Color color=GUI.color;GUI.depth=-2000;GUI.color=new Color(0,0,0,black);
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.depth=depth;GUI.color=color;
    }
    private void OnDisable()
    {
        RestoreSettings();if(actors!=null)Destroy(actors.gameObject);
        if(pause!=null)pause.enabled=pauseEnabled;
    }
    private void OnDestroy(){if(wallTile!=null)Destroy(wallTile);}
}
