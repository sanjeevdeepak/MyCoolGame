using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The first switch hint and the finished-level message.</summary>
[DefaultExecutionOrder(1200)]
public sealed class TrainUI : MonoBehaviour
{
    public TrainLevelDirector director;
    public TrainEquipment firstInteraction;
    public Camera view;
    public RectTransform prompt;
    public RectTransform passed;
    public RectTransform replay;
    public CanvasGroup replayVisibility;
    public Image replayFade;
    private bool tutorialUsed, popupPlayed;
    private Canvas canvas;
    private TMP_Text promptText, passedText;
    private Collider2D playerCollider;
    private Rect previousViewport;
    private TrainView framing;
    private float passedAt=-1f;
    private TMP_Text replayText;
    private bool replaying;
    public bool IsReplaying => replaying;
    private Rect Viewport => framing!=null?framing.PixelRect:view.pixelRect;

    private void Awake()
    {
        canvas=GetComponent<Canvas>();
        framing=view.GetComponent<TrainView>();
        playerCollider=director.player.GetComponent<Collider2D>();
        promptText=prompt.GetComponentsInChildren<TMP_Text>(true)[1];
        passedText=passed.GetComponentsInChildren<TMP_Text>(true)[1];
        replayText=replay.GetComponentsInChildren<TMP_Text>(true)[1];
        replay.gameObject.SetActive(false);replayFade.gameObject.SetActive(false);
        replayFade.color=Color.clear;
        prompt.gameObject.SetActive(false);passed.gameObject.SetActive(false);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))Replay();
    }

    public void Replay()
    {
        if(replaying || !director.IsFinished || !passed.gameObject.activeSelf)return;
        replaying=true;Time.timeScale=1f;
        if(director.sounds!=null)director.sounds.StopForReplay();
        StartCoroutine(ReplayGame());
    }

    private IEnumerator ReplayGame()
    {
        replayFade.transform.SetAsLastSibling();replayFade.gameObject.SetActive(true);
        const float duration=.4f;
        for(float time=0;time<duration;time+=Time.unscaledDeltaTime)
        {
            replayFade.color=new Color(0,0,0,Mathf.Clamp01(time/duration));yield return null;
        }
        replayFade.color=Color.black;yield return null;
        director.RestartLevel();
    }

    private void LateUpdate()
    {
        Rect viewport=Viewport;
        if(previousViewport!=viewport)
        {
            previousViewport=viewport;Canvas.ForceUpdateCanvases();
        }
        passed.localScale=Vector3.one*Scale(passedText,4);
        float bob=passed.gameObject.activeSelf && passedAt>=0f
            ? Mathf.Round(Mathf.Sin((Time.unscaledTime-passedAt)*Mathf.PI*.75f)*2f)*Mathf.Max(1,Mathf.Round(viewport.height/720f)) : 0f;
        Position(passed,new Vector2(viewport.center.x,viewport.yMin+viewport.height*.75f+bob));
        bool showReplay=passed.gameObject.activeSelf && passedAt>=0f;
        replay.gameObject.SetActive(showReplay);
        if(showReplay)
        {
            int replayScale=Mathf.Max(1,Mathf.Min(Mathf.RoundToInt(2f*viewport.height/720f),Mathf.FloorToInt(viewport.width*.8f/Mathf.Max(1,replayText.preferredWidth))));
            replay.localScale=Vector3.one*replayScale;
            Position(replay,new Vector2(viewport.center.x,viewport.yMin+viewport.height*.32f));
            if(!replaying)replayVisibility.alpha=Mathf.Repeat(Time.unscaledTime-passedAt,1.5f)<.95f?1f:0f;
        }
        if(firstInteraction.Activated)tutorialUsed=true;
        bool visible=!tutorialUsed && director.CanInteract && !director.IsFinished && Time.timeScale>0f && firstInteraction.PlayerInRange;
        prompt.gameObject.SetActive(visible);
        if(!visible)return;
        if(!popupPlayed){popupPlayed=true;if(director.sounds!=null)director.sounds.PlayMenu();}
        float scale=Scale(promptText,2);prompt.localScale=Vector3.one*scale;
        Vector3 anchor=firstInteraction.transform.position+Vector3.up*5.3f;
        anchor.y=Mathf.Max(anchor.y,playerCollider.bounds.max.y+1.2f);
        Vector3 point=view.WorldToScreenPoint(anchor);
        float half=promptText.preferredWidth*scale*.5f;
        float halfHeight=promptText.preferredHeight*scale*.5f;
        float margin=Mathf.Max(8,Mathf.Round(12*viewport.height/720f));
        point.x=Mathf.Clamp(point.x,viewport.xMin+half+margin,viewport.xMax-half-margin);
        point.y=Mathf.Clamp(point.y,viewport.yMin+halfHeight+margin,viewport.yMax-halfHeight-margin);
        Position(prompt,point);
    }

    private int Scale(TMP_Text text,int maximum)
    {
        int desired=Mathf.Max(1,Mathf.RoundToInt(maximum*Viewport.height/720f));
        int fits=Mathf.Max(1,Mathf.FloorToInt((Viewport.width-24)/Mathf.Max(1,text.preferredWidth)));
        return Mathf.Min(desired,fits);
    }

    private void Position(RectTransform label,Vector2 point)
    {
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,point,view,out Vector2 local))
            label.position=canvas.transform.TransformPoint(new Vector3(Mathf.Round(local.x),Mathf.Round(local.y),0));
    }

    public void ShowPassed()
    {
        if(!passed.gameObject.activeSelf)passedAt=Time.unscaledTime;
        prompt.gameObject.SetActive(false);passed.gameObject.SetActive(true);
    }
}
