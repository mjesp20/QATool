using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayReadOnlySceneMenuOptions : Editor
{
    [MenuItem("CONTEXT/SceneAsset/Play")]
    private static void Play(MenuCommand menuCommand) => Play(menuCommand.context as SceneAsset);

    [MenuItem("Assets/Play")]
    private static void Play() => Play(Selection.activeObject as SceneAsset);

    [MenuItem("Assets/Play", true)]
    private static bool SceneAssetValidation()
    {
        return Selection.activeObject is SceneAsset && Selection.objects.Length == 1;
    }

    private static void Play(SceneAsset scene)
    {
        // Store the full asset path instead of just the name
        string scenePath = AssetDatabase.GetAssetPath(scene);
        Debug.Log($"{nameof(Play)} {scene.name} ({scenePath})");

        if (Application.isPlaying)
        {
            // Use LoadSceneInPlayMode to bypass build settings requirement
            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
        }
        else
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorPrefs.SetString(nameof(PlayReadOnlySceneMenuOptions), scenePath);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorApplication.isPlaying = true;
            }
        }
    }

    [RuntimeInitializeOnLoadMethod]
    private static void OnRuntimeMethodLoad()
    {
        string scenePath = EditorPrefs.GetString(nameof(PlayReadOnlySceneMenuOptions));
        if (!string.IsNullOrWhiteSpace(scenePath))
        {
            EditorPrefs.DeleteKey(nameof(PlayReadOnlySceneMenuOptions));
            // LoadSceneInPlayMode loads by asset path, no build settings needed
            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
        }
    }
}