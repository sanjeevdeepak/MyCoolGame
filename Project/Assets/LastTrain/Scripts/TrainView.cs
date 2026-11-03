using UnityEngine;

/// <summary>Keep the same gameplay frame when the window or display changes.</summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class TrainView : MonoBehaviour
{
    [Min(.1f)] public float viewHeight=22f;
    private const int AspectWidth=16,AspectHeight=9;
    private Camera view;
    private Rect originalRect;
    private float originalSize;
    public Rect PixelRect { get; private set; }
    public Rect NormalizedRect => new Rect(PixelRect.x/Mathf.Max(1,Screen.width),PixelRect.y/Mathf.Max(1,Screen.height),PixelRect.width/Mathf.Max(1,Screen.width),PixelRect.height/Mathf.Max(1,Screen.height));
    public float HalfWidth => viewHeight*.5f*AspectWidth/AspectHeight;

    private void OnEnable()
    {
        view=GetComponent<Camera>();originalRect=view.rect;originalSize=view.orthographicSize;
        FitView();
    }

    private void Update(){FitView();}
    private void LateUpdate(){FitView();}

    private void FitView()
    {
        int width=Mathf.Max(1,Screen.width),height=Mathf.Max(1,Screen.height);
        // Whole pixels and an exact aspect avoid distortion at odd window sizes.
        int unit=Mathf.Max(1,Mathf.Min(width/AspectWidth,height/AspectHeight));
        int fittedWidth=unit*AspectWidth,fittedHeight=unit*AspectHeight;
        int left=(width-fittedWidth)/2,bottom=(height-fittedHeight)/2;
        PixelRect=new Rect(left,bottom,fittedWidth,fittedHeight);
        // Keep the renderer's output full size. The fitted area contains exactly
        // the authored world view; only the unused surrounding area is masked.
        view.rect=new Rect(0,0,1,1);
        view.aspect=(float)width/height;
        view.orthographicSize=viewHeight*.5f*height/fittedHeight;
    }

    private void OnGUI()
    {
        if(Event.current.type!=EventType.Repaint)return;
        Rect frame=PixelRect;float top=Screen.height-frame.yMax;
        Color color=GUI.color;int depth=GUI.depth;GUI.color=Color.black;GUI.depth=-1000;
        if(top>0)GUI.DrawTexture(new Rect(0,0,Screen.width,top),Texture2D.whiteTexture);
        if(frame.yMin>0)GUI.DrawTexture(new Rect(0,Screen.height-frame.yMin,Screen.width,frame.yMin),Texture2D.whiteTexture);
        if(frame.xMin>0)GUI.DrawTexture(new Rect(0,top,frame.xMin,frame.height),Texture2D.whiteTexture);
        if(frame.xMax<Screen.width)GUI.DrawTexture(new Rect(frame.xMax,top,Screen.width-frame.xMax,frame.height),Texture2D.whiteTexture);
        GUI.color=color;GUI.depth=depth;
    }

    private void OnDisable()
    {
        if(view==null)return;
        view.rect=originalRect;view.orthographicSize=originalSize;view.ResetAspect();
    }
}
