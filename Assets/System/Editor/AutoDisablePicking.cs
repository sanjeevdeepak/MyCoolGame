using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AutoDisablePicking
{
    static AutoDisablePicking()
    {
        EditorApplication.delayCall += DisablePicking;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        // delayCall ensures the scene is fully loaded before we query objects
        EditorApplication.delayCall += DisablePicking;
    }

    private static void DisablePicking()
    {
        // Find your target object(s) by tag, name, or type
        GameObject obj = GameObject.Find("GameController");
        if (obj == null) return;

        var svm = SceneVisibilityManager.instance;

        // 'true' = also apply to children
        if (!svm.IsPickingDisabled(obj, true))
            svm.DisablePicking(obj, true);
    }
}