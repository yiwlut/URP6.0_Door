using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DoorPuzzle
{
    public static class WebQuality
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            QualitySettings.vSyncCount = 0;
            QualitySettings.lodBias = 1f;
            QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 28f);
            Application.targetFrameRate = 60;
#endif
        }

        public static void ConfigureCamera(Camera camera)
        {
            if (camera == null) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.farClipPlane = Mathf.Min(camera.farClipPlane, 72f);
            if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
            {
                data.requiresColorOption = CameraOverrideOption.Off;
                data.requiresDepthOption = CameraOverrideOption.Off;
            }
#endif
        }
    }
}
