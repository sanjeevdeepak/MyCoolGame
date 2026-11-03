using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Plays the supplied clip unchanged, with point sampling and a proportional camera crop.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1100)]
[RequireComponent(typeof(VideoPlayer))]
public sealed class SubwayVideoBackground : MonoBehaviour
{
    public Camera view;
    public Renderer screen;
    public Material screenMaterial;
    public string videoFile="Background.mp4";
    public Vector2Int videoSize=new Vector2Int(1556,1168);
    [Range(0f,1f)] public float videoRailHeight=.145f;
    public float trackHeight=-7.82f;
    private VideoPlayer player;
    private RenderTexture target;
    private Material material;
    private bool pausedByGame;
    private bool frozen;
    private TrainView framing;
    public float VideoAspect => (float)videoSize.x/Mathf.Max(1,videoSize.y);

    private void OnEnable()
    {
        player = GetComponent<VideoPlayer>();
        player.source=VideoSource.Url;
        player.url=Application.streamingAssetsPath.TrimEnd('/')+"/"+videoFile;
        player.audioOutputMode=VideoAudioOutputMode.None;
        player.playbackSpeed=1f;
        player.isLooping=true;
        if(screen!=null)
        {
            if(view==null)view=Camera.main;
            framing=view.GetComponent<TrainView>();
            target=new RenderTexture(videoSize.x,videoSize.y,0,RenderTextureFormat.ARGB32);
            target.useMipMap=false;target.autoGenerateMips=false;target.antiAliasing=1;
            target.name="Background Video";target.filterMode=FilterMode.Point;target.wrapMode=TextureWrapMode.Clamp;target.Create();
            player.renderMode=VideoRenderMode.RenderTexture;player.targetTexture=target;
            player.aspectRatio=VideoAspectRatio.NoScaling;
            material=new Material(screenMaterial);material.mainTexture=target;screen.sharedMaterial=material;screen.enabled=false;
            screen.transform.SetParent(view.transform,false);
            FitScreen();
        }
        player.prepareCompleted += OnPrepared;
        player.frameReady += OnFrameReady;
        player.errorReceived += OnVideoError;
        player.sendFrameReadyEvents = true;
        player.Prepare();
    }

    private void OnPrepared(VideoPlayer source)
    {
        UsePointSampling(source);
        if(screen!=null)screen.enabled=true;
        source.Play();
        if(Time.timeScale<=0f || frozen){source.Pause();pausedByGame=true;}
    }

    private void LateUpdate()
    {
        FitScreen();
        if(player==null || !player.isPrepared)return;
        if(frozen){if(player.isPlaying)player.Pause();}
        else if(Time.timeScale<=0f && !pausedByGame){player.Pause();pausedByGame=true;}
        else if(Time.timeScale>0f && pausedByGame){player.Play();pausedByGame=false;}
    }

    private void FitScreen()
    {
        if(screen==null || view==null || material==null)return;
        float depth=Mathf.Min(100f,view.farClipPlane-1f);
        Rect frame=framing!=null?framing.NormalizedRect:new Rect(0,0,1,1);
        Vector3 lower=view.transform.InverseTransformPoint(view.ViewportToWorldPoint(new Vector3(frame.xMin,frame.yMin,depth)));
        Vector3 upper=view.transform.InverseTransformPoint(view.ViewportToWorldPoint(new Vector3(frame.xMax,frame.yMax,depth)));
        screen.transform.localPosition=(lower+upper)*.5f;
        screen.transform.localRotation=Quaternion.identity;
        screen.transform.localScale=new Vector3(upper.x-lower.x,upper.y-lower.y,1);
        float aspect=VideoAspect;
        float frameAspect=(upper.x-lower.x)/(upper.y-lower.y);
        Vector2 crop=frameAspect>=aspect?new Vector2(1,aspect/frameAspect):new Vector2(frameAspect/aspect,1);
        float rail=(view.WorldToViewportPoint(new Vector3(view.transform.position.x,trackHeight,0)).y-frame.yMin)/frame.height;
        material.mainTextureScale=crop;
        material.mainTextureOffset=new Vector2((1-crop.x)*.5f,Mathf.Clamp(videoRailHeight-rail*crop.y,0,1-crop.y));
    }

    public void Freeze()
    {
        frozen=true;
        if(player!=null && player.isPrepared)player.Pause();
    }

    private void OnFrameReady(VideoPlayer source, long frame)
    {
        // Some decoders only expose their texture after the first decoded frame.
        if (UsePointSampling(source))
            source.sendFrameReadyEvents = false;
    }

    private static bool UsePointSampling(VideoPlayer source)
    {
        if (source.texture == null)
            return false;

        source.texture.filterMode = FilterMode.Point;
        source.texture.wrapMode = TextureWrapMode.Clamp;
        return true;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError("Subway background video: " + message, this);
    }

    private void OnDisable()
    {
        if (player == null)
            return;

        player.prepareCompleted -= OnPrepared;
        player.frameReady -= OnFrameReady;
        player.errorReceived -= OnVideoError;
        player.Stop();
        pausedByGame=false;
        frozen=false;
        if(screen!=null){screen.enabled=false;screen.sharedMaterial=screenMaterial;}
        player.targetTexture=null;
        if(target!=null){target.Release();Destroy(target);target=null;}
        if(material!=null){Destroy(material);material=null;}
    }
}
