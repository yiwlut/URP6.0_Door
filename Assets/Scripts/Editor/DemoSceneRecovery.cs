using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoorPuzzle.Editor
{
    [InitializeOnLoad]
    public static class DemoSceneRecovery
    {
        private const string RestoredScenePath = "Assets/Scenes/Scene - 3 Demo.unity";
        private const string ExpectedGuid = "9a25ca9cfed6006428797d4e71c52171";
        private const string SessionKey = "DoorPuzzle.DemoSceneRecovery.Completed";

        static DemoSceneRecovery()
        {
            EditorApplication.delayCall += Recover;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += Recover;
            };
        }

        private static void Recover()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (SessionState.GetBool(SessionKey, false)) return;

            var active = SceneManager.GetActiveScene();
            if (active.isDirty)
            {
                Debug.LogError("[DoorPuzzle] Demo recovery paused because the active scene has unsaved changes.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RestoredScenePath) == null ||
                AssetDatabase.AssetPathToGUID(RestoredScenePath) != ExpectedGuid)
            {
                Debug.LogError("[DoorPuzzle] Restored Scene - 3 Demo asset or GUID is not ready.");
                return;
            }

            EditorSceneManager.OpenScene(RestoredScenePath, OpenSceneMode.Single);
            SessionState.SetBool(SessionKey, true);
            Debug.Log("[DoorPuzzle] Reopened the exact pre-title-change Scene - 3 Demo with its authored lighting, rain and fog.");
        }
    }
}
