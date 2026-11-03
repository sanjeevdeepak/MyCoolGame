using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

/// <summary>Mount signs to brick pillars detected in the original moving video.</summary>
[DefaultExecutionOrder(1020)]
[DisallowMultipleComponent]
public sealed class StationSigns : MonoBehaviour
{
    public SubwayVideoBackground background;
    public Sprite[] signs;
    public Material signMaterial;
    public Material edgeMaterial;
    [Range(0f,1f)] public float spillStrength=1f;
    private VideoPlayer video;
    private Texture2D scan;
    private Transform root;
    private readonly SpriteRenderer[] images=new SpriteRenderer[8];
    private readonly float[] centers=new float[4];
    private readonly float[] velocities=new float[4];
    private readonly bool[] visible=new bool[4];
    private readonly int[] styles=new int[4];
    private readonly int[] lostFrames=new int[4];
    private readonly Vector4[] positions=new Vector4[8],colors=new Vector4[8];
    private readonly Vector4[] lightPositions=new Vector4[8];
    private readonly Vector3[] world=new Vector3[8];
    private long lastFrame=-1;
    private int nextStyle,count;
    private float pillarWidth=80f,flow;
    private readonly List<GameObject> edges=new List<GameObject>();
    public int TrackedPillars { get; private set; }
    public int LightCount => count;
    private void Awake()
    {
        video=background.GetComponent<VideoPlayer>();root=new GameObject("Station Signs").transform;root.SetParent(transform,false);
        for(int i=0;i<images.Length;i++)
        {
            var obj=new GameObject(i%2==0?"Pillar Sign":"Station Advertisement");obj.transform.SetParent(root,false);
            var image=obj.AddComponent<SpriteRenderer>();image.sharedMaterial=signMaterial;image.sortingOrder=-80;image.enabled=false;
            image.transform.localScale=Vector3.one*1.5696203f;images[i]=image;
        }
        int index=0;
        foreach(var train in FindAnyObjectByType<TrainLevelDirector>().trains)
        {
            foreach(var polygon in train.GetComponents<PolygonCollider2D>())
            {
                var points=polygon.points;
                for(int p=0;p<points.Length;p++)
                {
                    var a=points[p];var b=points[(p+1)%points.Length];
                    if(a.y>5.7f && Mathf.Abs(a.y-b.y)<.02f && Mathf.Abs(a.x-b.x)>1f)
                        Edge(polygon.transform,new Vector3(a.x,a.y+.025f,0),new Vector3(b.x,b.y+.025f,0),index++);
                }
            }
            foreach(var box in train.GetComponentsInChildren<BoxCollider2D>())
                if(box.name=="Step Surface" || box.name=="End Landing" || box.name=="Front Landing" || box.name=="Rear Landing" || box.name=="Cabinet Surface" || box.name=="Vent Surface" || box.name=="Platform Surface")
                {
                    var a=box.offset+new Vector2(-box.size.x*.5f,box.size.y*.5f+.025f);
                    var b=box.offset+new Vector2(box.size.x*.5f,box.size.y*.5f+.025f);
                    Edge(box.transform,a,b,index++);
                }
        }
    }
    private void Edge(Transform parent,Vector3 a,Vector3 b,int index)
    {
        var obj=new GameObject("Landing LED");obj.transform.SetParent(parent,false);edges.Add(obj);
        var line=obj.AddComponent<LineRenderer>();line.sharedMaterial=edgeMaterial;line.useWorldSpace=false;line.positionCount=2;
        line.SetPosition(0,a);line.SetPosition(1,b);line.widthMultiplier=.049f;line.numCapVertices=0;line.numCornerVertices=0;
        line.startColor=line.endColor=index%3==0?new Color(1,.58f,.94f):new Color(.58f,.95f,1);
        line.sortingOrder=7;
    }
    private void LateUpdate()
    {
        if(!video.isPrepared || background.screen==null || signs==null || signs.Length<4 || video.targetTexture==null)
        {
            foreach(var image in images)if(image!=null)image.enabled=false;
            ClearLight();lastFrame=-1;return;
        }
        var target=video.targetTexture;
        if(lastFrame!=video.frame)
        {
            lastFrame=video.frame;
            int firstRow=Mathf.RoundToInt(target.height*.34f),lastRow=Mathf.RoundToInt(target.height*.51f);
            if(scan!=null && (scan.width!=target.width || scan.height!=lastRow-firstRow+1)){Destroy(scan);scan=null;}
            if(scan==null){scan=new Texture2D(target.width,lastRow-firstRow+1,TextureFormat.RGBA32,false);scan.hideFlags=HideFlags.DontSave;}
            RenderTexture previous=RenderTexture.active;
            try
            {
                RenderTexture.active=target;
                scan.ReadPixels(new Rect(0,firstRow,target.width,scan.height),0,0,false);
            }
            finally{RenderTexture.active=previous;}
            var pixels=scan.GetPixelData<Color32>(0);var found=new float[4];var partial=new bool[4];int foundCount=0,start=-1,last=-1;
            for(int x=0;x<target.width;x++)
            {
                int votes=0;
                for(int row=0;row<3;row++){int y=Mathf.RoundToInt(target.height*(.34f+row*.085f))-firstRow;var c=pixels[y*target.width+x];if(c.r>85 && c.r>c.g*1.25f && c.r>c.b*1.7f)votes++;}
                if(votes>=2){if(start<0)start=x;last=x;}
                if(start>=0 && (x-last>8 || x==target.width-1))
                {
                    int width=last-start+1;
                    bool edge=start==0 || last==target.width-1;
                    if(width>=(edge?5:28) && width<=180 && foundCount<4)
                    {
                        if(!edge)pillarWidth=Mathf.Lerp(pillarWidth,width,.2f);
                        float center=start==0?last-pillarWidth*.5f:last==target.width-1?start+pillarWidth*.5f:(start+last)*.5f;
                        partial[foundCount]=edge;found[foundCount++]=center/target.width;
                    }
                    start=-1;
                }
            }
            var assigned=new bool[4];var occupied=new bool[4];
            for(int n=0;n<foundCount;n++)
            {
                int best=-1;float distance=.14f;
                for(int i=0;i<4;i++)if(visible[i] && !assigned[i] && Mathf.Abs(centers[i]-found[n])<distance){best=i;distance=Mathf.Abs(centers[i]-found[n]);}
                if(best<0)for(int i=0;i<4;i++)if(!visible[i] && !assigned[i]){best=i;styles[i]=nextStyle++%3;break;}
                if(best<0)for(int i=0;i<4;i++)if(!assigned[i]){best=i;styles[i]=nextStyle++%3;break;}
                if(best>=0)
                {
                    if(visible[best]){velocities[best]=found[n]-centers[best];if(!partial[n] && Mathf.Abs(velocities[best])>.002f && Mathf.Abs(velocities[best])<.06f)flow=Mathf.Lerp(flow,velocities[best],.5f);}
                    else velocities[best]=flow;
                    assigned[best]=occupied[best]=true;centers[best]=found[n];lostFrames[best]=0;
                }
            }
            // Let partially visible signs leave the frame with their pillar;
            // loss of the last source pixels must not pop a sign out of view.
            for(int i=0;i<4;i++)if(visible[i] && !occupied[i] && (centers[i]<.07f || centers[i]>.93f))
            {centers[i]+=flow;lostFrames[i]++;occupied[i]=lostFrames[i]<20 && centers[i]>-.30f && centers[i]<1.15f;}
            for(int i=0;i<4;i++)visible[i]=occupied[i];TrackedPillars=foundCount;
        }
        count=0;
        for(int i=0;i<4;i++)
        {
            for(int j=0;j<2;j++)
            {
                var image=images[i*2+j];image.enabled=visible[i] && background.screen.enabled;
                if(!image.enabled)continue;
                int style=j==0?0:styles[i]+1;image.sprite=signs[style];
                Vector2 uv=new Vector2(centers[i]+(j==0?0:.145f),j==0?.555f:.52f);
                Vector3 p=SourcePoint(uv);image.transform.position=p;
                bool cyan=style==1;
                positions[count]=new Vector4(uv.x,uv.y,cyan?.17f:style==0?.11f:.20f,cyan?.14f:style==0?.22f:.18f);
                Color light=cyan?new Color(.05f,.8f,1f):new Color(1f,.09f,.85f);
                colors[count]=light*spillStrength;world[count]=p;lightPositions[count]=new Vector4(p.x,p.y,cyan?8f:10f,0);count++;
            }
        }
        var material=background.screen.sharedMaterial;material.SetInt("_NeonCount",count);material.SetVectorArray("_NeonPositions",positions);material.SetVectorArray("_NeonColors",colors);
        Shader.SetGlobalInt("_StationCount",count);Shader.SetGlobalVectorArray("_StationPositions",lightPositions);Shader.SetGlobalVectorArray("_StationColors",colors);
    }
    private Vector3 SourcePoint(Vector2 uv)
    {
        Camera view=background.view;var frame=view.GetComponent<TrainView>();
        float cropY=background.VideoAspect/(16f/9f);
        return new Vector3(view.transform.position.x+(uv.x-.5f)*frame.HalfWidth*2f,
            background.trackHeight+(uv.y-background.videoRailHeight)*frame.viewHeight/cropY,view.transform.position.z+95f);
    }
    public Color SampleLight(Vector3 position)
    {
        Color result=Color.black;
        for(int i=0;i<count;i++)
        {
            Vector2 d=position-world[i];d.y*=.6f;float amount=Mathf.Clamp01(1f-d.magnitude/lightPositions[i].z);amount*=amount;
            result+=(Color)colors[i]*amount;
        }
        result.a=1;return result;
    }
    private void OnEnable()
    {
        if(root!=null)root.gameObject.SetActive(true);
        foreach(var edge in edges)if(edge!=null)edge.SetActive(true);
    }
    private void OnDisable()
    {
        ClearLight();
        if(root!=null)root.gameObject.SetActive(false);
        foreach(var edge in edges)if(edge!=null)edge.SetActive(false);
    }
    private void ClearLight()
    {
        if(background!=null && background.screen!=null && background.screen.sharedMaterial!=null)background.screen.sharedMaterial.SetInt("_NeonCount",0);
        Shader.SetGlobalInt("_StationCount",0);count=0;
    }
    private void OnDestroy()
    {
        if(root!=null)Destroy(root.gameObject);if(scan!=null)Destroy(scan);
        foreach(var edge in edges)if(edge!=null)Destroy(edge);
    }
}

