using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DoorPuzzle.Editor
{
    /// <summary>
    /// Read-only validation for the hand-authored alley.
    /// This class intentionally never creates, saves, or restores scene/prefab objects.
    /// </summary>
    public static class StaticAlleyBaker
    {
        private const string LightingPrefabPath = "Assets/@Prefabs/Level/Lighting Rig.prefab";
        private const string LevelPrefabPath = "Assets/@Prefabs/Level/Level Prototype.prefab";

        [MenuItem("Tools/Blues With You/Validate Authored Alley", priority = 14)]
        public static void ValidateAuthoredAlley()
        {
            var lighting = AssetDatabase.LoadAssetAtPath<GameObject>(LightingPrefabPath);
            var level = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabPath);
            if (lighting == null || level == null)
            {
                Debug.LogWarning(
                    "[DoorPuzzle] Authored alley validation found a missing prefab. " +
                    "Nothing was recreated. Missing objects stay deleted until they are restored manually.");
                return;
            }

            var streetLights = lighting.GetComponentsInChildren<Light>(true)
                .Count(light => light.name.IndexOf("Street Light", StringComparison.OrdinalIgnoreCase) >= 0);
            var hasLightingRig = level.GetComponentInChildren<LevelLightingRig>(true) != null;
            Debug.Log(
                $"[DoorPuzzle] Read-only alley validation: {streetLights} authored street lights, " +
                $"lighting rig reference present: {hasLightingRig}. No assets were changed.");
        }
    }
}
