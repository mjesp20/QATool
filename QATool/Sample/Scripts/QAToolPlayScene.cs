using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QATool
{


    public class QAToolPlayScene : Editor
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
            Debug.Log($"{nameof(Play)} {scene.name}");
            if (Application.isPlaying)
            {
                SceneManager.LoadScene(scene.name);
            }
            else
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorPrefs.SetString(nameof(QAToolPlayScene), scene.name); //For some reason setting a variable was not working. Using EditorPrefs as a workaround.
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    EditorApplication.isPlaying = true;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void OnRuntimeMethodLoad()
        {
            string sceneName = EditorPrefs.GetString(nameof(QAToolPlayScene));
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                EditorPrefs.DeleteKey(nameof(QAToolPlayScene));
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
