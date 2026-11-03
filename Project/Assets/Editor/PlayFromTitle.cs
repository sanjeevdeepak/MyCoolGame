using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
internal static class PlayFromTitle
{
    static PlayFromTitle()
    {
        EditorApplication.delayCall += SetStartScene;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += SetStartScene;
        };
    }

    private static void SetStartScene()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                "Assets/Scenes/UiScenes/TitleScreen.unity");
    }
}
