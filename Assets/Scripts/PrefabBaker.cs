#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DoorPuzzle.Editor
{
    public static class PrefabBaker
    {
        private const string Root = "Assets/@Prefabs/PuzzleChunks";
        private const string CatalogPath = "Assets/Resources/PuzzleChunkCatalog.asset";

        [MenuItem("Tools/Blues With You/Rebuild Puzzle Chunk Prefabs", priority = 15)]
        public static void Rebuild()
        {
            EnsureFolder("Assets/@Prefabs");
            EnsureFolder(Root);
            EnsureFolder("Assets/Resources");
            var stone = GetOrCreateMaterial(Root + "/ChunkStone.mat", new Color(0.07f, 0.1f, 0.15f), Color.black);
            var glow = GetOrCreateMaterial(Root + "/LightPuzzleGlow.mat", new Color(0.04f, 0.12f, 0.16f), new Color(0.1f, 0.75f, 1f) * 2f);
            var beam = GetOrCreateMaterial(Root + "/LightBeam.mat", new Color(0.15f, 0.75f, 1f), new Color(0.15f, 0.75f, 1f) * 3f);

            var start = BuildStartChunk(stone, glow);
            var relay = BuildRelayChunk(stone, glow, beam);
            var threshold = BuildThresholdChunk(stone, glow);
            var placements = new[]
            {
                new PuzzleChunkCatalog.Placement { prefab = start, position = new Vector3(0f, 0f, -7f) },
                new PuzzleChunkCatalog.Placement { prefab = relay, position = new Vector3(0f, 0f, 2f) },
                new PuzzleChunkCatalog.Placement { prefab = threshold, position = new Vector3(0f, 0f, 10f) }
            };

            var catalog = AssetDatabase.LoadAssetAtPath<PuzzleChunkCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PuzzleChunkCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.SetPlacements(placements);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[DoorPuzzle] Rebuilt three reusable puzzle chunk prefabs and the runtime chunk catalog.");
        }

        public static bool EnsureGenerated()
        {
            var exists = AssetDatabase.LoadAssetAtPath<PuzzleChunkCatalog>(CatalogPath) != null;
            if (!exists)
            {
                Debug.LogWarning(
                    "[DoorPuzzle] Puzzle chunk catalog is missing. Automatic recreation is disabled; " +
                    "the authored deletion was preserved.");
            }
            return exists;
        }

        private static GameObject BuildStartChunk(Material stone, Material glow)
        {
            var root = NewChunk("Chunk_00_Start", "start", 10f);
            Primitive("Floor", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.35f, 0f), new Vector3(9f, 0.7f, 10f), stone);
            Primitive("Guide", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.04f, 1.5f), new Vector3(1.8f, 0.04f, 1.8f), glow);
            return Save(root, Root + "/Chunk_00_Start.prefab");
        }

        private static GameObject BuildRelayChunk(Material stone, Material glow, Material beamMaterial)
        {
            var root = NewChunk("Chunk_01_LightRelay", "light-relay", 10f);
            Primitive("Floor", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.35f, 0f), new Vector3(9f, 0.7f, 10f), stone);
            var emitter = new GameObject("Light Emitter");
            emitter.transform.SetParent(root.transform, false);
            emitter.transform.localPosition = new Vector3(-3.2f, 1.25f, -3.2f);
            emitter.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            var line = emitter.AddComponent<LineRenderer>();
            line.sharedMaterial = beamMaterial;
            emitter.AddComponent<LightEmitter>();
            Primitive("Emitter Core", PrimitiveType.Sphere, emitter.transform, Vector3.zero, Vector3.one * 0.38f, glow);

            var mirror = Primitive("Targetable Mirror", PrimitiveType.Cube, root.transform,
                new Vector3(0f, 1.35f, 0f), new Vector3(1.5f, 2.2f, 0.18f), glow);
            mirror.transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
            mirror.AddComponent<LightMirror>();

            var receiver = Primitive("Light Receiver", PrimitiveType.Sphere, root.transform,
                new Vector3(3.2f, 1.25f, 3.2f), Vector3.one * 0.7f, glow);
            receiver.AddComponent<LightReceiver>();
            return Save(root, Root + "/Chunk_01_LightRelay.prefab");
        }

        private static GameObject BuildThresholdChunk(Material stone, Material glow)
        {
            var root = NewChunk("Chunk_02_Threshold", "threshold", 8f);
            Primitive("Floor", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.35f, 0f), new Vector3(9f, 0.7f, 8f), stone);
            Primitive("Arch Left", PrimitiveType.Cube, root.transform, new Vector3(-2.8f, 3f, 1f), new Vector3(0.7f, 6f, 0.8f), stone);
            Primitive("Arch Right", PrimitiveType.Cube, root.transform, new Vector3(2.8f, 3f, 1f), new Vector3(0.7f, 6f, 0.8f), stone);
            Primitive("Arch Top", PrimitiveType.Cube, root.transform, new Vector3(0f, 5.7f, 1f), new Vector3(6.2f, 0.7f, 0.8f), glow);
            var gate = Primitive("Light Gate", PrimitiveType.Cube, root.transform, new Vector3(0f, 2.6f, 1f),
                new Vector3(4.8f, 5.2f, 0.35f), glow);
            gate.AddComponent<LightGate>();
            return Save(root, Root + "/Chunk_02_Threshold.prefab");
        }

        private static GameObject NewChunk(string name, string id, float length)
        {
            var root = new GameObject(name);
            root.AddComponent<PuzzleChunk>().Configure(id, length);
            return root;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            var value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            value.transform.localScale = scale;
            value.GetComponent<Renderer>().sharedMaterial = material;
            return value;
        }

        private static GameObject Save(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Material GetOrCreateMaterial(string path, Color baseColor, Color emission)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", emission);
            if (emission.maxColorComponent > 0f) material.EnableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(material, path);
            return material;
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
#endif
