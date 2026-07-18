using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DoorPuzzle
{
    /// <summary>
    /// Binds the authored alley, player, camera and lighting. Level geometry,
    /// puzzle visuals, portals and VFX are never generated at runtime.
    /// </summary>
    public sealed class WorldBuilder : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip rainyAtmo;
        [SerializeField, Range(0f, 1f)] private float rainyAtmoVolume = 0.62f;
        [SerializeField] private AudioClip streetPianoBgm;
        [SerializeField, Range(0f, 1f)] private float streetPianoVolume = 0.46f;

        private void Awake()
        {
            var level = GameObject.Find("Level Prototype");
            if (level == null)
            {
                Debug.LogError("[DoorPuzzle] The authored Level Prototype is missing. Runtime level creation is disabled.");
                enabled = false;
                return;
            }

            var lightingRig = FindFirstObjectByType<LevelLightingRig>(FindObjectsInactive.Include);
            if (lightingRig != null)
                lightingRig.ApplyEnvironment();
            else
                ConfigureRenderEnvironment();

            if (!BindAuthoredPlayerAndCamera())
            {
                enabled = false;
                return;
            }

            PlayRainAmbience();
            PlayStreetPianoBgm();
            Debug.Log("[DoorPuzzle] Authored alley and vending-machine portal puzzle bound. Runtime fantasy VFX and level generation are disabled.");
        }

        private bool BindAuthoredPlayerAndCamera()
        {
            var player = FindFirstObjectByType<ThirdPersonController>(FindObjectsInactive.Include);
            if (player == null)
            {
                Debug.LogError("[DoorPuzzle] The authored Player prefab is missing. Runtime player creation is disabled.");
                return false;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("[DoorPuzzle] The authored Main Camera is missing. Runtime camera creation is disabled.");
                return false;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.008f, 0.014f, 0.028f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 90f;
            camera.fieldOfView = 64f;
            camera.allowHDR = true;

            var additionalData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (additionalData != null) additionalData.renderPostProcessing = true;

            player.Initialize(camera);
            WebQuality.ConfigureCamera(camera);
            return true;
        }

        private void PlayRainAmbience()
        {
            if (rainyAtmo == null || transform.Find("Rain Ambience") != null) return;
            CreateLoopingAudio("Rain Ambience", rainyAtmo, rainyAtmoVolume);
        }

        private void PlayStreetPianoBgm()
        {
            if (transform.Find("Street Piano BGM") != null) return;
            if (streetPianoBgm == null)
                streetPianoBgm = Resources.Load<AudioClip>("Audio/bgm_StreetPiano02");
            if (streetPianoBgm == null)
            {
                Debug.LogWarning("[DoorPuzzle] In-game street piano BGM is missing.");
                return;
            }
            CreateLoopingAudio("Street Piano BGM", streetPianoBgm, streetPianoVolume);
        }

        private void CreateLoopingAudio(string objectName, AudioClip clip, float volume)
        {
            var audioObject = new GameObject(objectName);
            audioObject.transform.SetParent(transform, false);
            var source = audioObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.dopplerLevel = 0f;
            source.Play();
        }

        private static void ConfigureRenderEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.075f, 0.105f, 0.17f);
            RenderSettings.ambientEquatorColor = new Color(0.035f, 0.055f, 0.085f);
            RenderSettings.ambientGroundColor = new Color(0.012f, 0.018f, 0.03f);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}
