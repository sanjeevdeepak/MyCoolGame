using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Stationary station backdrop, restrained neon animation and Space to start.</summary>
[DefaultExecutionOrder(1200)]
public sealed class TitleScreenController : MonoBehaviour
{
    public TrainView framing;
    public RawImage background;
    public RectTransform title;
    public TMP_Text titleText;
    public TMP_Text[] neonLetters;
    public RectTransform prompt;
    public TMP_Text promptText;
    public CanvasGroup promptVisibility;
    public Image[] fixtures;
    public Vector4[] fixtureLights;
    public Color[] fixtureColors;
    public string gameplayScene="LastTrain";
    public AudioSource musicSource, startSource;
    public Image fade;
    [Range(0f,1f)] public float masterVolume=.8f, musicVolume=.2f, effectVolume=.4f;
    [Range(0f,2f)] public float musicGain=1.18f;
    [Min(.1f)] public float fadeDuration=.4f;
    private Canvas canvas;
    private Material stationMaterial;
    private Color[] letterColors;
    private readonly Vector4[] lights=new Vector4[8],colors=new Vector4[8];
    private float began;
    private bool starting;
    public bool IsStarting => starting;

    private void Awake()
    {
        Time.timeScale=1f;
        canvas=background.canvas;began=Time.unscaledTime;
        stationMaterial=new Material(background.material);stationMaterial.name="Title Lighting";
        background.material=stationMaterial;
        letterColors=new Color[neonLetters.Length];
        for(int i=0;i<neonLetters.Length;i++)letterColors[i]=neonLetters[i].color;
        fade.color=Color.clear;fade.gameObject.SetActive(false);
        ApplyVolumes();musicSource.loop=true;musicSource.timeSamples=0;musicSource.Play();
        Layout(0);
    }

    private void Update()
    {
        if(!starting && Input.GetKeyDown(KeyCode.Space))BeginGame();
    }

    public void BeginGame()
    {
        if(starting)return;
        starting=true;
        musicSource.Stop();startSource.Play();
        StartCoroutine(StartGame());
    }

    private IEnumerator StartGame()
    {
        fade.transform.SetAsLastSibling();fade.gameObject.SetActive(true);
        for(float time=0;time<fadeDuration;time+=Time.unscaledDeltaTime)
        {
            fade.color=new Color(0,0,0,Mathf.Clamp01(time/fadeDuration));yield return null;
        }
        fade.color=Color.black;
        yield return null; // Present a fully black frame before replacing the scene.
        SceneManager.LoadSceneAsync(gameplayScene,LoadSceneMode.Single);
    }

    private void LateUpdate()
    {
        float t=Time.unscaledTime-began;
        ApplyVolumes();
        Layout(t);
        float lamp=.97f+.03f*Mathf.Sin(t*1.7f);
        float phase=Mathf.Repeat(t,9.7f);
        lamp-=.26f*Pulse(phase,1.6f,.08f)+.13f*Pulse(phase,1.86f,.1f)+.19f*Pulse(phase,4.5f,.13f)+.21f*Pulse(phase,7.8f,.11f);
        for(int i=0;i<neonLetters.Length;i++)
        {
            Color c=letterColors[i];c.r*=lamp;c.g*=lamp;c.b*=lamp;neonLetters[i].color=c;
        }
        if(!starting)promptVisibility.alpha=Mathf.Repeat(t,1.5f)<.95f?1f:0f;
        int count=Mathf.Min(8,fixtures.Length);
        for(int i=0;i<count;i++)
        {
            float clock=Mathf.Repeat(t+i*2.17f,11.3f);
            float brightness=.98f+.02f*Mathf.Sin(t*.8f+i)-.23f*Pulse(clock,4.1f,.12f)-.12f*Pulse(clock,4.45f,.22f);
            fixtures[i].color=new Color(brightness,brightness,brightness,1);
            lights[i]=fixtureLights[i];colors[i]=(Vector4)(fixtureColors[i]*brightness);
        }
        stationMaterial.SetInt("_NeonCount",count);
        stationMaterial.SetVectorArray("_NeonPositions",lights);
        stationMaterial.SetVectorArray("_NeonColors",colors);
    }

    private void Layout(float time)
    {
        Rect frame=framing.PixelRect;
        float unit=Mathf.Max(1,Mathf.Round(frame.height/720f));
        var rect=background.rectTransform;
        rect.sizeDelta=frame.size;Place(rect,frame.center);
        float sourceAspect=(float)background.texture.width/background.texture.height;
        float crop=Mathf.Min(1f,sourceAspect/(16f/9f));
        background.uvRect=new Rect(0,Mathf.Clamp(.145f-((11f-7.82f)/22f)*crop,0,1-crop),1,crop);
        float width=titleText.preferredWidth+8;
        float scale=Mathf.Max(1,Mathf.Min(Mathf.Round(8f*frame.height/720f),Mathf.Floor(frame.width*.8f/width)));
        title.localScale=Vector3.one*scale;
        float bob=Mathf.Round(Mathf.Sin(time*Mathf.PI*.65f)*2f)*unit;
        Place(title,new Vector2(frame.center.x,frame.yMin+frame.height*.55f+bob));
        int promptScale=Mathf.Max(1,Mathf.Min(Mathf.RoundToInt(2f*frame.height/720f),Mathf.FloorToInt(frame.width*.8f/Mathf.Max(1,promptText.preferredWidth))));
        prompt.localScale=Vector3.one*promptScale;
        Place(prompt,new Vector2(frame.center.x,frame.yMin+frame.height*.32f));
        for(int i=0;i<fixtures.Length;i++)
        {
            Vector4 uv=fixtureLights[i];
            Vector2 center=new Vector2(frame.xMin+uv.x*frame.width,frame.yMin+(uv.y-background.uvRect.y)/crop*frame.height);
            var image=fixtures[i];var size=image.sprite.rect.size;
            float density=1.5696203f/image.sprite.pixelsPerUnit*frame.height/22f;
            image.rectTransform.sizeDelta=new Vector2(Mathf.Round(size.x*density),Mathf.Round(size.y*density));
            Place(image.rectTransform,center);
        }
    }

    private void Place(RectTransform label,Vector2 point)
    {
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,point,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out Vector2 local))
            label.anchoredPosition=new Vector2(Mathf.Round(local.x),Mathf.Round(local.y));
    }
    private static float Pulse(float time,float center,float width)=>Mathf.Exp(-Mathf.Pow((time-center)/width,2));
    private void ApplyVolumes()
    {
        musicSource.volume=Mathf.Clamp01(masterVolume*musicVolume*musicGain);
        startSource.volume=masterVolume*effectVolume;
    }
    private void OnDisable()
    {
        if(musicSource!=null)musicSource.Stop();if(startSource!=null)startSource.Stop();
    }
    private void OnDestroy(){if(stationMaterial!=null)Destroy(stationMaterial);}
}
