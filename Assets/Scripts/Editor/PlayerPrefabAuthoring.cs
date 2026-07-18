using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DoorPuzzle.Editor
{
    public static class PlayerPrefabAuthoring
    {
        private const string SourceModelPath = "Assets/@Models/Player01/Player_01.fbx";
        private const string SourceWalkingPath = "Assets/@Models/Player01/Walking.fbx";
        private const string DerivedWalkingPath = "Assets/@Animations/Player01/Walking_Player01.fbx";
        private const string PlayerPrefabPath = "Assets/@Prefabs/Player/Player.prefab";
        private const string LevelPrefabPath = "Assets/@Prefabs/Level/Level Prototype.prefab";
        private const string PlayerMaterialFolder = "Assets/@Materials/Player01";
        private const string PlayerTextureFolder = PlayerMaterialFolder + "/Textures";
        private const string PlayerMaterialPath = PlayerMaterialFolder + "/Player01 URP Lit.mat";
        private const float PlayerVisualScale = 0.8f;

        [MenuItem("Tools/Blues With You/Rebuild Authored Player Prefab", priority = 17)]
        public static void BuildAuthoredPlayer()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[DoorPuzzle] Exit Play Mode before rebuilding the authored Player prefab.");
                return;
            }

            EnsureFolder("Assets/@Animations/Player01");
            EnsureFolder("Assets/@Prefabs/Player");
            EnsureFolder(PlayerTextureFolder);
            var walking = PrepareHumanoidWalking();
            if (walking == null) return;

            var playerMaterial = BuildPlayerMaterial();
            if (playerMaterial == null) return;

            var playerPrefab = BuildPlayerPrefab(walking, playerMaterial);
            if (playerPrefab == null) return;
            if (!PlacePlayerInLevel(playerPrefab)) return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DoorPuzzle] Authored Player prefab built and placed in Level Prototype. Runtime player creation is disabled.");
        }

        private static AnimationClip PrepareHumanoidWalking()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(SourceWalkingPath) == null)
            {
                Debug.LogError("[DoorPuzzle] Player01 source FBXs are missing. Source files were not modified.");
                return null;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(DerivedWalkingPath) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceWalkingPath, DerivedWalkingPath))
                {
                    Debug.LogError("[DoorPuzzle] Could not create the derived Humanoid Walking FBX.");
                    return null;
                }
                AssetDatabase.ImportAsset(DerivedWalkingPath, ImportAssetOptions.ForceSynchronousImport);
            }

            var importer = AssetImporter.GetAtPath(DerivedWalkingPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[DoorPuzzle] The derived Walking importer is missing.");
                return null;
            }

            if (importer.animationType != ModelImporterAnimationType.Human ||
                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                importer.sourceAvatar != null)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.sourceAvatar = null;
                importer.SaveAndReimport();
                EditorApplication.delayCall += BuildAuthoredPlayer;
                return null;
            }

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
            var needsClipImport = clips.Any(clip => !clip.loopTime || !clip.loopPose ||
                                                   !clip.lockRootRotation || !clip.lockRootHeightY ||
                                                   !clip.lockRootPositionXZ);
            if (!needsClipImport)
            {
                return AssetDatabase.LoadAllAssetsAtPath(DerivedWalkingPath)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
            }
            foreach (var clip in clips)
            {
                clip.loopTime = true;
                clip.loopPose = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            EditorApplication.delayCall += BuildAuthoredPlayer;
            return null;
        }

        private static Material BuildPlayerMaterial()
        {
            var albedo = LoadPlayerTexture("business_man_albedo.png");
            var normal = LoadPlayerTexture("business_man_normal.png");
            var occlusion = LoadPlayerTexture("business_man_ao.png");
            if (albedo == null)
            {
                Debug.LogError("[DoorPuzzle] Player albedo texture is missing from " + PlayerTextureFolder + ".");
                return null;
            }

            ConfigureTextureImporter(PlayerTextureFolder + "/business_man_albedo.png", false, true);
            ConfigureTextureImporter(PlayerTextureFolder + "/business_man_normal.png", true, false);
            ConfigureTextureImporter(PlayerTextureFolder + "/business_man_ao.png", false, false);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[DoorPuzzle] URP Lit shader was not found.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(PlayerMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Player01 URP Lit" };
                AssetDatabase.CreateAsset(material, PlayerMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", albedo);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.28f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Cull", 2f);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 1f);
                material.EnableKeyword("_NORMALMAP");
            }
            if (occlusion != null)
            {
                material.SetTexture("_OcclusionMap", occlusion);
                material.SetFloat("_OcclusionStrength", 1f);
                material.EnableKeyword("_OCCLUSIONMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D LoadPlayerTexture(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(PlayerTextureFolder + "/" + fileName);
        }

        private static void ConfigureTextureImporter(string path, bool normalMap, bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            var expectedType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType == expectedType && importer.sRGBTexture == sRgb) return;
            importer.textureType = expectedType;
            importer.sRGBTexture = sRgb;
            importer.SaveAndReimport();
        }

        private static GameObject BuildPlayerPrefab(AnimationClip walking, Material playerMaterial)
        {
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath);
            var idle = AssetDatabase.LoadAllAssetsAtPath(SourceModelPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal) &&
                                        clip.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0);
            if (modelPrefab == null || idle == null || walking == null)
            {
                Debug.LogError("[DoorPuzzle] Player model, embedded idle clip, or Humanoid Walking clip is missing.");
                return null;
            }

            var root = new GameObject("Player");
            try
            {
                var characterController = root.AddComponent<CharacterController>();
                characterController.height = 1.44f;
                characterController.radius = 0.28f;
                characterController.center = new Vector3(0f, 0.72f, 0f);
                root.AddComponent<ThirdPersonController>();

                var visual = PrefabUtility.InstantiatePrefab(modelPrefab, root.transform) as GameObject;
                if (visual == null)
                {
                    Debug.LogError("[DoorPuzzle] Could not instantiate Player_01.fbx into the Player prefab.");
                    return null;
                }
                visual.name = "Player Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * PlayerVisualScale;

                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (var index = 0; index < materials.Length; index++) materials[index] = playerMaterial;
                    renderer.sharedMaterials = materials;
                }

                foreach (var value in visual.GetComponentsInChildren<Collider>(true)) value.enabled = false;
                foreach (var value in visual.GetComponentsInChildren<Camera>(true)) value.enabled = false;
                foreach (var value in visual.GetComponentsInChildren<Light>(true)) value.enabled = false;
                foreach (var value in visual.GetComponentsInChildren<AudioListener>(true)) value.enabled = false;

                var animator = visual.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                {
                    Debug.LogError("[DoorPuzzle] Player_01.fbx does not expose a valid Humanoid Animator.");
                    return null;
                }
                animator.applyRootMotion = false;
                animator.runtimeAnimatorController = null;
                root.AddComponent<PlayerVisualAnimator>().Configure(
                    animator, characterController, idle, walking, playerMaterial);
                return PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static bool PlacePlayerInLevel(GameObject playerPrefab)
        {
            var levelRoot = PrefabUtility.LoadPrefabContents(LevelPrefabPath);
            if (levelRoot == null)
            {
                Debug.LogError("[DoorPuzzle] Level Prototype prefab is missing.");
                return false;
            }

            try
            {
                var existing = levelRoot.GetComponentInChildren<ThirdPersonController>(true);
                if (existing != null) return true;

                var instance = PrefabUtility.InstantiatePrefab(playerPrefab, levelRoot.transform) as GameObject;
                if (instance == null) return false;
                instance.name = "Player";
                instance.transform.localPosition = new Vector3(0f, 0.05f, -8.5f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                PrefabUtility.SaveAsPrefabAsset(levelRoot, LevelPrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(levelRoot);
            }
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
