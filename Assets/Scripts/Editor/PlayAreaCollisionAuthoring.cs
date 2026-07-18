using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DoorPuzzle.Editor
{
    public static class PlayAreaCollisionAuthoring
    {
        private const string LevelPrefabPath = "Assets/@Prefabs/Level/Level Prototype.prefab";
        private const string BoundaryPrefabPath = "Assets/@Prefabs/Environment/Play Area Boundaries.prefab";
        private const string BoundaryRootName = "Play Area Boundaries";
        private const float WallThickness = 0.32f;
        private const float WallHeight = 4.5f;

        [MenuItem("Tools/Blues With You/Build Play Area Collision Boundaries", priority = 19)]
        public static void BuildManually()
        {
            var levelRoot = PrefabUtility.LoadPrefabContents(LevelPrefabPath);
            if (levelRoot == null)
            {
                Debug.LogError("[DoorPuzzle] Level Prototype prefab was not found.");
                return;
            }

            try
            {
                if (!TryGetFloorBounds(levelRoot, out var localBounds))
                {
                    Debug.LogError("[DoorPuzzle] No authored Floor renderer/collider was found for boundary fitting.");
                    return;
                }

                var boundaryPrefab = BuildBoundaryPrefab(localBounds);
                if (boundaryPrefab == null) return;

                var existing = levelRoot.transform.Find(BoundaryRootName);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

                var instance = PrefabUtility.InstantiatePrefab(boundaryPrefab, levelRoot.transform) as GameObject;
                if (instance == null) return;
                instance.name = BoundaryRootName;
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                PrefabUtility.SaveAsPrefabAsset(levelRoot, LevelPrefabPath);
                AssetDatabase.SaveAssets();

                Debug.Log($"[DoorPuzzle] Authored invisible play-area boundaries from floor bounds. " +
                          $"Center={localBounds.center}, Size={localBounds.size}. No runtime generation is used.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(levelRoot);
            }
        }

        private static bool TryGetFloorBounds(GameObject levelRoot, out Bounds localBounds)
        {
            var floorRenderers = levelRoot.GetComponentsInChildren<Renderer>(true)
                .Where(value => value.name.Equals("Floor", StringComparison.OrdinalIgnoreCase) ||
                                value.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            var floorColliders = levelRoot.GetComponentsInChildren<Collider>(true)
                .Where(value => value.name.Equals("Floor", StringComparison.OrdinalIgnoreCase) ||
                                value.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            var hasBounds = false;
            var worldBounds = new Bounds();
            foreach (var renderer in floorRenderers)
            {
                if (!hasBounds) { worldBounds = renderer.bounds; hasBounds = true; }
                else worldBounds.Encapsulate(renderer.bounds);
            }
            foreach (var collider in floorColliders)
            {
                if (!hasBounds) { worldBounds = collider.bounds; hasBounds = true; }
                else worldBounds.Encapsulate(collider.bounds);
            }

            if (!hasBounds)
            {
                localBounds = default;
                return false;
            }

            var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var worldCorner = worldBounds.center + Vector3.Scale(worldBounds.extents, new Vector3(x, y, z));
                var localCorner = levelRoot.transform.InverseTransformPoint(worldCorner);
                min = Vector3.Min(min, localCorner);
                max = Vector3.Max(max, localCorner);
            }
            localBounds = new Bounds((min + max) * 0.5f, max - min);
            return localBounds.size.x > 0.5f && localBounds.size.z > 0.5f;
        }

        private static GameObject BuildBoundaryPrefab(Bounds floorBounds)
        {
            EnsureFolder("Assets/@Prefabs/Environment");
            var root = new GameObject(BoundaryRootName);
            try
            {
                var floorTop = floorBounds.max.y;
                var centerY = floorTop + WallHeight * 0.5f;
                var width = floorBounds.size.x;
                var depth = floorBounds.size.z;

                AddWall(root.transform, "Left Boundary", new Vector3(floorBounds.min.x + WallThickness * 0.5f, centerY, floorBounds.center.z),
                    new Vector3(WallThickness, WallHeight, depth));
                AddWall(root.transform, "Right Boundary", new Vector3(floorBounds.max.x - WallThickness * 0.5f, centerY, floorBounds.center.z),
                    new Vector3(WallThickness, WallHeight, depth));
                AddWall(root.transform, "Rear Boundary", new Vector3(floorBounds.center.x, centerY, floorBounds.min.z + WallThickness * 0.5f),
                    new Vector3(width, WallHeight, WallThickness));
                AddWall(root.transform, "Far Boundary", new Vector3(floorBounds.center.x, centerY, floorBounds.max.z - WallThickness * 0.5f),
                    new Vector3(width, WallHeight, WallThickness));

                return PrefabUtility.SaveAsPrefabAsset(root, BoundaryPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AddWall(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = Vector3.zero;
            wall.transform.localRotation = Quaternion.identity;
            var collider = wall.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            collider.isTrigger = false;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
