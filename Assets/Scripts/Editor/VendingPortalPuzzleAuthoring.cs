using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DoorPuzzle.Editor
{
    [InitializeOnLoad]
    public static class VendingPortalPuzzleAuthoring
    {
        private const string SessionKey = "DoorPuzzle.VendingPortalPuzzle.ColliderFitV2";
        private const string LevelPrefabPath = "Assets/@Prefabs/Level/Level Prototype.prefab";
        private const string PortalPrefabPath = "Assets/@Prefabs/Portal/Vending Portal.prefab";
        private const string RosePrefabPath = "Assets/@Prefabs/Environment/Center Rose.prefab";
        private const string AtmospherePrefabPath = "Assets/@Prefabs/Environment/Rainy Alley Atmosphere.prefab";
        private const string MaterialFolder = "Assets/@Materials/Portal";
        private const string EnvironmentMaterialFolder = "Assets/@Materials/Environment";
        private const string FrameMaterialPath = MaterialFolder + "/Portal Frame.mat";
        private const string DoorMaterialPath = MaterialFolder + "/Portal Door.mat";
        private const string SignalMaterialPath = MaterialFolder + "/Portal Signal.mat";
        private const string RosePetalMaterialPath = EnvironmentMaterialFolder + "/Rose Petals.mat";
        private const string RoseStemMaterialPath = EnvironmentMaterialFolder + "/Rose Stem.mat";
        private const string FogMaterialPath = EnvironmentMaterialFolder + "/World Fog Sprite.mat";
        private const string RainStreakMaterialPath = "Assets/@FX/Materials/Rain Streak.mat";

        static VendingPortalPuzzleAuthoring()
        {
            EditorApplication.delayCall += TryBindLoadedVendingMachinesOnce;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += TryBindLoadedVendingMachinesOnce;
            };
        }

        [MenuItem("Tools/Blues With You/Build Authored Vending Portal Puzzle", priority = 18)]
        public static void BuildManually()
        {
            SessionState.EraseBool(SessionKey);
            TryBuildOnce();
        }

        [MenuItem("Tools/Blues With You/Rebuild Rainy Alley Atmosphere", priority = 20)]
        public static void RebuildAtmosphereOnly()
        {
            var atmosphere = BuildAtmospherePrefab();
            if (atmosphere == null) return;
            AssetDatabase.SaveAssets();
            Debug.Log("[DoorPuzzle] Rebuilt authored rainy-alley atmosphere with visible depth-faded world fog.");
        }

        private static void TryBindLoadedVendingMachinesOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling) return;

            var vendingCount = BindLoadedVendingMachines();
            if (vendingCount == 0) return;

            AssetDatabase.SaveAssets();
            SessionState.SetBool(SessionKey, true);
            Debug.Log($"[DoorPuzzle] Bound authored vending-machine portal clue components: {vendingCount}. Existing level visuals were not rebuilt.");
        }

        private static void TryBuildOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling) return;

            var portalPrefab = BuildPortalPrefab();
            var rosePrefab = BuildRosePrefab();
            var atmospherePrefab = BuildAtmospherePrefab();
            if (portalPrefab == null || rosePrefab == null || atmospherePrefab == null ||
                !PlaceAuthoredContentInLevel(portalPrefab, rosePrefab, atmospherePrefab)) return;

            var vendingCount = BindLoadedVendingMachines();
            if (vendingCount == 0)
            {
                Debug.LogWarning("[DoorPuzzle] Authored portal is ready, but no loaded GameObject named 'VendingMachine' was found. Save/open the edited level and run the authoring menu once.");
                return;
            }

            AssetDatabase.SaveAssets();
            SessionState.SetBool(SessionKey, true);
            Debug.Log($"[DoorPuzzle] Authored vending portal puzzle complete. Bound vending machines: {vendingCount}. No runtime portal creation is used.");
        }

        private static GameObject BuildPortalPrefab()
        {
            EnsureFolder("Assets/@Prefabs/Portal");
            EnsureFolder(MaterialFolder);
            var frameMaterial = GetOrCreateLitMaterial(FrameMaterialPath,
                new Color(0.018f, 0.024f, 0.032f), Color.black, 0.78f, 0.64f);
            var doorMaterial = GetOrCreateLitMaterial(DoorMaterialPath,
                new Color(0.028f, 0.034f, 0.04f), Color.black, 0.68f, 0.48f);
            var signalMaterial = GetOrCreateLitMaterial(SignalMaterialPath,
                new Color(0.16f, 0.035f, 0.018f), new Color(0.42f, 0.08f, 0.035f) * 1.4f, 0.2f, 0.76f);
            if (frameMaterial == null || doorMaterial == null || signalMaterial == null) return null;

            var root = new GameObject("Vending Portal");
            try
            {
                var interaction = root.AddComponent<BoxCollider>();
                interaction.center = new Vector3(0f, 3f, -0.2f);
                interaction.size = new Vector3(4.35f, 6f, 1.15f);
                interaction.isTrigger = true;

                Primitive("Left Frame", new Vector3(-2.15f, 3f, 0f), new Vector3(0.42f, 6.2f, 0.7f), frameMaterial, root.transform, true);
                Primitive("Right Frame", new Vector3(2.15f, 3f, 0f), new Vector3(0.42f, 6.2f, 0.7f), frameMaterial, root.transform, true);
                Primitive("Top Frame", new Vector3(0f, 6.05f, 0f), new Vector3(4.7f, 0.42f, 0.7f), frameMaterial, root.transform, true);
                var barrier = Primitive("Locked Security Door", new Vector3(0f, 2.85f, 0.08f),
                    new Vector3(3.9f, 5.55f, 0.24f), doorMaterial, root.transform, true);
                var signal = Primitive("Portal Signal", new Vector3(0f, 5.72f, -0.39f),
                    new Vector3(1.05f, 0.11f, 0.08f), signalMaterial, root.transform, false);

                var lightObject = new GameObject("Portal Signal Light");
                lightObject.transform.SetParent(root.transform, false);
                lightObject.transform.localPosition = new Vector3(0f, 3.4f, -1.05f);
                var signalLight = lightObject.AddComponent<Light>();
                signalLight.type = LightType.Point;
                signalLight.color = new Color(0.42f, 0.08f, 0.035f);
                signalLight.intensity = 0.35f;
                signalLight.range = 4f;
                signalLight.shadows = LightShadows.None;

                root.AddComponent<VendingPortalLock>().Configure(
                    barrier.transform, signal.GetComponent<Renderer>(), signalLight);
                return PrefabUtility.SaveAsPrefabAsset(root, PortalPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildRosePrefab()
        {
            EnsureFolder("Assets/@Prefabs/Environment");
            EnsureFolder(EnvironmentMaterialFolder);
            var petalMaterial = GetOrCreateLitMaterial(RosePetalMaterialPath,
                new Color(0.24f, 0.004f, 0.012f), new Color(0.22f, 0.006f, 0.014f) * 0.16f, 0f, 0.31f);
            var stemMaterial = GetOrCreateLitMaterial(RoseStemMaterialPath,
                new Color(0.025f, 0.11f, 0.04f), Color.black, 0f, 0.28f);
            if (petalMaterial == null || stemMaterial == null) return null;

            var root = new GameObject("Center Rose");
            try
            {
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Stem";
                stem.transform.SetParent(root.transform, false);
                stem.transform.localPosition = new Vector3(0f, 0.48f, 0f);
                stem.transform.localScale = new Vector3(0.022f, 0.48f, 0.022f);
                stem.GetComponent<Renderer>().sharedMaterial = stemMaterial;
                UnityEngine.Object.DestroyImmediate(stem.GetComponent<Collider>());

                for (var index = 0; index < 14; index++)
                {
                    var angle = index * (360f / 14f) + (index % 2) * 8f;
                    var radius = index < 6 ? 0.075f : 0.13f;
                    var petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    petal.name = $"Petal {index + 1:00}";
                    petal.transform.SetParent(root.transform, false);
                    petal.transform.localPosition = new Vector3(
                        Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                        0.99f + (index < 6 ? 0.045f : 0f),
                        Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
                    petal.transform.localRotation = Quaternion.Euler(index < 6 ? -28f : -52f, -angle, 18f);
                    petal.transform.localScale = index < 6
                        ? new Vector3(0.13f, 0.035f, 0.18f)
                        : new Vector3(0.19f, 0.025f, 0.26f);
                    petal.GetComponent<Renderer>().sharedMaterial = petalMaterial;
                    UnityEngine.Object.DestroyImmediate(petal.GetComponent<Collider>());
                }

                for (var side = -1; side <= 1; side += 2)
                {
                    var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    leaf.name = side < 0 ? "Leaf Left" : "Leaf Right";
                    leaf.transform.SetParent(root.transform, false);
                    leaf.transform.localPosition = new Vector3(0.09f * side, side < 0 ? 0.48f : 0.64f, 0f);
                    leaf.transform.localRotation = Quaternion.Euler(0f, side * 28f, side * -24f);
                    leaf.transform.localScale = new Vector3(0.16f, 0.018f, 0.065f);
                    leaf.GetComponent<Renderer>().sharedMaterial = stemMaterial;
                    UnityEngine.Object.DestroyImmediate(leaf.GetComponent<Collider>());
                }
                return PrefabUtility.SaveAsPrefabAsset(root, RosePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildAtmospherePrefab()
        {
            EnsureFolder("Assets/@Prefabs/Environment");
            EnsureFolder(EnvironmentMaterialFolder);
            var fogShader = Shader.Find("Blues With You/Rain/World Fog Sprite");
            var fogMaterial = AssetDatabase.LoadAssetAtPath<Material>(FogMaterialPath);
            if (fogMaterial == null && fogShader != null)
            {
                fogMaterial = new Material(fogShader) { name = "World Fog Sprite" };
                AssetDatabase.CreateAsset(fogMaterial, FogMaterialPath);
            }
            if (fogMaterial == null)
            {
                Debug.LogWarning("[DoorPuzzle] World Fog Sprite shader is still importing. Authoring will retry.");
                return null;
            }
            fogMaterial.shader = fogShader;
            fogMaterial.SetColor("_BaseColor", new Color(0.27f, 0.36f, 0.46f, 0.58f));
            fogMaterial.SetFloat("_NoiseScale", 5f);
            fogMaterial.SetFloat("_NoiseStrength", 0.34f);
            fogMaterial.SetFloat("_EdgeSoftness", 0.39f);
            fogMaterial.SetFloat("_DepthFade", 1.35f);
            EditorUtility.SetDirty(fogMaterial);

            var rainMaterial = AssetDatabase.LoadAssetAtPath<Material>(RainStreakMaterialPath);
            if (rainMaterial == null)
            {
                Debug.LogError("[DoorPuzzle] Authored Rain Streak material is missing.");
                return null;
            }

            var root = new GameObject("Rainy Alley Atmosphere");
            try
            {
                BuildWorldFog(root.transform, fogMaterial);
                BuildAuthoredRain(root.transform, rainMaterial);
                BuildMistDroplets(root.transform, fogMaterial);
                return PrefabUtility.SaveAsPrefabAsset(root, AtmospherePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildWorldFog(Transform parent, Material material)
        {
            var value = new GameObject("Low World Fog");
            value.transform.SetParent(parent, false);
            value.transform.localPosition = new Vector3(0f, 0.72f, 4f);
            var system = value.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.maxParticles = 72;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 17f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(3.4f, 7.8f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.42f, 0.52f, 0.63f, 0.045f), new Color(0.62f, 0.7f, 0.78f, 0.115f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = 4.2f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(15f, 0.7f, 28f);
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.055f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0.018f);
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.sortingFudge = -2f;
        }

        private static void BuildAuthoredRain(Transform parent, Material material)
        {
            var value = new GameObject("Authored Rain Streaks");
            value.transform.SetParent(parent, false);
            value.transform.localPosition = new Vector3(0f, 9f, 4f);
            var system = value.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.maxParticles = 1200;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.15f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.038f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.56f, 0.68f, 0.8f, 0.1f), new Color(0.8f, 0.86f, 0.92f, 0.22f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = 520f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(15f, 1f, 28f);
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.25f);
            velocity.y = new ParticleSystem.MinMaxCurve(-13.5f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.1f);
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.075f;
            renderer.lengthScale = 4.6f;
            renderer.sharedMaterial = material;
        }

        private static void BuildMistDroplets(Transform parent, Material material)
        {
            var value = new GameObject("Near Camera Mist Particles");
            value.transform.SetParent(parent, false);
            value.transform.localPosition = new Vector3(0f, 1.2f, 1f);
            var system = value.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.maxParticles = 90;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.65f, 0.72f, 0.8f, 0.018f), new Color(0.82f, 0.86f, 0.9f, 0.055f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = 10f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 2.2f, 22f);
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.025f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.018f);
            velocity.z = new ParticleSystem.MinMaxCurve(0.012f);
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
        }

        private static bool PlaceAuthoredContentInLevel(GameObject portalPrefab, GameObject rosePrefab,
            GameObject atmospherePrefab)
        {
            var levelRoot = PrefabUtility.LoadPrefabContents(LevelPrefabPath);
            if (levelRoot == null)
            {
                Debug.LogError("[DoorPuzzle] Level Prototype prefab is missing.");
                return false;
            }
            try
            {
                var changed = false;
                if (levelRoot.GetComponentInChildren<VendingPortalLock>(true) == null)
                {
                    var instance = PrefabUtility.InstantiatePrefab(portalPrefab, levelRoot.transform) as GameObject;
                    if (instance == null) return false;
                    instance.name = "Vending Portal";
                    instance.transform.localPosition = new Vector3(0f, 0f, 9.35f);
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                    changed = true;
                }
                if (levelRoot.transform.Find("Center Rose") == null)
                {
                    var rose = PrefabUtility.InstantiatePrefab(rosePrefab, levelRoot.transform) as GameObject;
                    if (rose == null) return false;
                    rose.name = "Center Rose";
                    rose.transform.localPosition = new Vector3(0f, 0.02f, 1.15f);
                    rose.transform.localRotation = Quaternion.identity;
                    rose.transform.localScale = Vector3.one;
                    changed = true;
                }
                if (levelRoot.transform.Find("Rainy Alley Atmosphere") == null)
                {
                    var atmosphere = PrefabUtility.InstantiatePrefab(atmospherePrefab, levelRoot.transform) as GameObject;
                    if (atmosphere == null) return false;
                    atmosphere.name = "Rainy Alley Atmosphere";
                    atmosphere.transform.localPosition = Vector3.zero;
                    atmosphere.transform.localRotation = Quaternion.identity;
                    atmosphere.transform.localScale = Vector3.one;
                    changed = true;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(levelRoot, LevelPrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(levelRoot);
            }
        }

        private static int BindLoadedVendingMachines()
        {
            var count = 0;
            var vendingMachines = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(value => value.scene.IsValid() &&
                                value.name.Equals("VendingMachine", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (var vending in vendingMachines)
            {
                if (vending.GetComponent<VendingMachinePortalClue>() == null)
                    Undo.AddComponent<VendingMachinePortalClue>(vending);
                LiftVendingAboveFloor(vending);
                var solidCollider = vending.GetComponent<BoxCollider>();
                if (solidCollider == null)
                    solidCollider = Undo.AddComponent<BoxCollider>(vending);
                FitSolidColliderToRenderers(vending, solidCollider);
                EditorUtility.SetDirty(vending);
                count++;
            }

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && vendingMachines.Any(value => value.scene == prefabStage.scene))
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                PrefabUtility.SaveAsPrefabAsset(prefabStage.prefabContentsRoot, prefabStage.assetPath);
            }
            else
            {
                foreach (var scene in Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                             .Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt)
                             .Where(scene => scene.isLoaded && scene.isDirty))
                    EditorSceneManager.SaveScene(scene);
            }
            return count;
        }

        private static void LiftVendingAboveFloor(GameObject vending)
        {
            var vendingRenderers = vending.GetComponentsInChildren<Renderer>(true)
                .Where(value => value.enabled && !(value is ParticleSystemRenderer))
                .ToArray();
            if (vendingRenderers.Length == 0) return;

            var vendingBounds = vendingRenderers[0].bounds;
            foreach (var renderer in vendingRenderers.Skip(1)) vendingBounds.Encapsulate(renderer.bounds);

            var floorTop = Resources.FindObjectsOfTypeAll<Renderer>()
                .Where(value => value.gameObject.scene == vending.scene && value.enabled &&
                                value.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                value.bounds.min.x <= vendingBounds.center.x && value.bounds.max.x >= vendingBounds.center.x &&
                                value.bounds.min.z <= vendingBounds.center.z && value.bounds.max.z >= vendingBounds.center.z)
                .Select(value => value.bounds.max.y)
                .DefaultIfEmpty(float.NegativeInfinity)
                .Max();
            if (float.IsNegativeInfinity(floorTop)) return;

            const float visualGap = 0.012f;
            var lift = floorTop + visualGap - vendingBounds.min.y;
            if (lift <= 0.001f || lift > 0.12f) return;

            Undo.RecordObject(vending.transform, "Resolve Vending Machine Floor Z-Fighting");
            vending.transform.position += Vector3.up * lift;
            EditorUtility.SetDirty(vending.transform);
            Debug.Log($"[DoorPuzzle] Lifted VendingMachine by {lift:F3}m to remove floor z-fighting.");
        }

        private static void FitSolidColliderToRenderers(GameObject root, BoxCollider target)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(value => value.enabled && !(value is ParticleSystemRenderer))
                .ToArray();
            if (renderers.Length == 0) return;

            var localMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var localMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            foreach (var renderer in renderers)
            {
                var bounds = renderer.bounds;
                for (var x = -1; x <= 1; x += 2)
                for (var y = -1; y <= 1; y += 2)
                for (var z = -1; z <= 1; z += 2)
                {
                    var worldCorner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                    var localCorner = root.transform.InverseTransformPoint(worldCorner);
                    localMin = Vector3.Min(localMin, localCorner);
                    localMax = Vector3.Max(localMax, localCorner);
                }
            }

            var size = localMax - localMin;
            if (size.x <= 0.001f || size.y <= 0.001f || size.z <= 0.001f) return;

            Undo.RecordObject(target, "Fit Vending Machine Collider");
            target.isTrigger = false;
            target.center = (localMin + localMax) * 0.5f;
            target.size = size;
            EditorUtility.SetDirty(target);
        }

        private static GameObject Primitive(string name, Vector3 position, Vector3 scale,
            Material material, Transform parent, bool keepCollider)
        {
            var value = GameObject.CreatePrimitive(PrimitiveType.Cube);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localRotation = Quaternion.identity;
            value.transform.localScale = scale;
            value.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(value.GetComponent<Collider>());
            return value;
        }

        private static Material GetOrCreateLitMaterial(string path, Color baseColor,
            Color emission, float metallic, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return null;
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.SetColor("_EmissionColor", emission);
            if (emission.maxColorComponent > 0f) material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
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
