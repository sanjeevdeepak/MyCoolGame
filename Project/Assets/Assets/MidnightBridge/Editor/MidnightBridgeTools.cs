#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace MidnightBridge.Editor
{
    public static class MidnightBridgeTools
    {
        private const string Root="Assets/MidnightBridge";
        private static GameObject Spawn(string relative,Vector3 position)
        {
            GameObject p=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+relative+".prefab");
            if (p==null) { Debug.LogError("Missing Midnight Bridge prefab: "+relative);return null; }
            GameObject g=(GameObject)PrefabUtility.InstantiatePrefab(p);g.transform.position=position;
            Undo.RegisterCreatedObjectUndo(g,"Add "+relative);return g;
        }
        [MenuItem("Tools/Midnight Bridge/Add Waterfront At Camera (Native Scale)")]
        private static void AddBackground()
        {
            Camera c=Camera.main;if(c==null) {Debug.LogWarning("Tag your actual camera MainCamera first.");return;}
            GameObject g=Spawn("Midnight_Bridge_Background",new Vector3(c.transform.position.x,c.transform.position.y,0));
            Selection.activeGameObject=g;
            Debug.Log("Added at32PPU and scale1. Background waterline is3.3125 units below its center. Use only one parallax driver on each layer.");
        }
        [MenuItem("Tools/Midnight Bridge/Attach Character Visual To Selected Player")]
        private static void AttachPlayer()
        {
            GameObject selected=Selection.activeGameObject;
            if(selected==null || selected.GetComponent<Rigidbody2D>()==null)
            {Debug.LogWarning("Select your existing Player root with its Rigidbody2D.");return;}
            GameObject visual=Spawn("Player_Visual",selected.transform.position);if(visual==null)return;
            visual.transform.SetParent(selected.transform,false);
            visual.transform.localPosition=Vector3.zero;
            Collider2D collider=selected.GetComponent<Collider2D>();
            if(collider!=null)
            {
                Vector3 feet=new Vector3(collider.bounds.center.x,collider.bounds.min.y,selected.transform.position.z);
                visual.transform.position=feet;
            }
            SpriteRenderer old=selected.GetComponent<SpriteRenderer>();
            if(old!=null){Undo.RecordObject(old,"Hide previous player artwork");old.enabled=false;}
            Selection.activeGameObject=visual;
            Debug.Log("Attached visual at the existing player's feet. Movement, Rigidbody2D and colliders remain on the original root. Adjust the ART child's uniform scale only if your player is a different size.");
        }
        [MenuItem("Tools/Midnight Bridge/Create Tile Assets")]
        public static void CreateTiles()
        {
            string dir=Root+"/GeneratedTiles";
            if(!AssetDatabase.IsValidFolder(dir))AssetDatabase.CreateFolder(Root,"GeneratedTiles");
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Art/Tiles"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);Sprite sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(sprite==null)continue;
                string dest=dir+"/"+Path.GetFileNameWithoutExtension(path)+".asset";
                if(AssetDatabase.LoadAssetAtPath<Tile>(dest)!=null)continue;
                Tile tile=ScriptableObject.CreateInstance<Tile>();tile.sprite=sprite;
                tile.colliderType=path.Contains("Slopes") || path.Contains("Rail") ? Tile.ColliderType.None : Tile.ColliderType.Sprite;
                AssetDatabase.CreateAsset(tile,dest);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Tiles created. Use Grid cell size1x1 and TilemapCollider2D. Slopes use the supplied smooth EdgeCollider2D platform prefabs; rails have no collision.");
        }
        [MenuItem("Tools/Midnight Bridge/Create Art Preview Scene")]
        private static void PreviewScene()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Camera c=new GameObject("Main Camera").AddComponent<Camera>();c.tag="MainCamera";
            c.orthographic=true;c.orthographicSize=342f/64f;c.transform.position=new Vector3(0,0,-10);
            c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.21f,.07f,.31f);
            Spawn("Midnight_Bridge_Background",Vector3.zero);
            Spawn("Water_Death_Zone",new Vector3(0,-3.3125f,0));
            Spawn("Platforms/Bridge_Reference_Arch_128x32",new Vector3(-6f,-2.25f,0));
            Spawn("Platforms/Bridge_Reference_Arch_128x32",new Vector3(-.4f,-1.5f,0));
            Spawn("Platforms/Slope_Up",new Vector3(3.6f,-1.8f,0));
            Spawn("Platforms/Bridge_Reference_Arch_128x32",new Vector3(7,-.8f,0));
            Spawn("Start_Flag",new Vector3(-7.2f,-2.25f,0));
            Spawn("Finish_Flag",new Vector3(7.4f,-.8f,0));
            Spawn("Player_Visual",new Vector3(-6.5f,-2.25f,0));
            Spawn("Bounce_Fan",new Vector3(-1.8f,-1.5f,0));
            Spawn("Five_Yen",new Vector3(-3,-.6f,0));Spawn("Five_Yen",new Vector3(2,.1f,0));
            CreateTiles();
            Selection.activeGameObject=c.gameObject;
            Debug.Log("Art preview scene created. PressPlay to preview water/flags/coin animations. This scene has no replacement movement controller: integrate Player_Visual into your working class-kit Player for gameplay.");
        }
    }
}
#endif
