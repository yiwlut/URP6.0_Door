using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DoorPuzzle
{
    [ExecuteAlways]
    public sealed class LevelLightingRig : MonoBehaviour
    {
        [Header("Shared Environment")]
        [SerializeField] private Color ambientSky = new Color(0.11f, 0.15f, 0.24f);
        [SerializeField] private Color ambientEquator = new Color(0.05f, 0.08f, 0.13f);
        [SerializeField] private Color ambientGround = new Color(0.018f, 0.028f, 0.048f);
        [SerializeField] private Color fogColor = new Color(0.02f, 0.035f, 0.065f);
        [SerializeField, Min(0f)] private float fogDensity = 0.014f;

        [Header("Authored Light Levels")]
        [SerializeField, Min(0f)] private float moonIntensity = 1.9f;
        [SerializeField, Min(0f)] private float streetIntensity = 80f;
        [SerializeField, Min(0f)] private float streetRange = 10f;
        [SerializeField, Min(0f)] private float accentIntensity = 3f;
        [SerializeField, Min(0f)] private float accentRange = 9f;

        [Header("Wet Street Reflection")]
        [SerializeField] private bool enableRuntimeReflectionProbe = true;
        [SerializeField] private Vector3 reflectionProbeCenter = new Vector3(0f, 3.2f, 5f);
        [SerializeField] private Vector3 reflectionProbeSize = new Vector3(18f, 8f, 38f);
        [SerializeField, Range(0f, 2f)] private float reflectionIntensity = 1.35f;
        private ReflectionProbe runtimeReflectionProbe;

        public void ApplyEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            RenderSettings.reflectionBounces = 1;
            ApplyLightLevels();
        }

        private void ApplyLightLevels()
        {
            foreach (var light in GetComponentsInChildren<Light>(true))
            {
                if (light.type == LightType.Directional)
                {
                    light.intensity = moonIntensity;
                    light.shadows = LightShadows.Soft;
                    continue;
                }

                var lightName = light.name;
                if (lightName.IndexOf("Street", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    light.intensity = streetIntensity;
                    light.range = streetRange;
                }
                else
                {
                    light.intensity = accentIntensity;
                    light.range = accentRange;
                }

                // WebGL-friendly: local lights remain shadowless while the moon
                // directional light provides the scene's readable cast shadows.
                light.shadows = LightShadows.None;
            }
        }

        private IEnumerator Start()
        {
            if (!Application.isPlaying || !enableRuntimeReflectionProbe) yield break;
            // Capture the fully authored alley after its first rendered frame.
            yield return null;
            yield return new WaitForEndOfFrame();
            EnsureRuntimeReflectionProbe();
            if (runtimeReflectionProbe != null)
                runtimeReflectionProbe.RenderProbe();
        }

        private void EnsureRuntimeReflectionProbe()
        {
            if (runtimeReflectionProbe != null) return;
            var probeTransform = transform.Find("Night Street Reflection Probe");
            if (probeTransform == null)
            {
                Debug.LogWarning("[DoorPuzzle] The authored reflection probe is missing from Lighting Rig. Runtime creation is disabled.");
                return;
            }

            runtimeReflectionProbe = probeTransform.GetComponent<ReflectionProbe>();
            if (runtimeReflectionProbe == null)
            {
                Debug.LogWarning("[DoorPuzzle] Night Street Reflection Probe has no ReflectionProbe component.");
                return;
            }
            runtimeReflectionProbe.mode = ReflectionProbeMode.Realtime;
            runtimeReflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            runtimeReflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            runtimeReflectionProbe.center = reflectionProbeCenter;
            runtimeReflectionProbe.size = reflectionProbeSize;
            runtimeReflectionProbe.boxProjection = true;
            runtimeReflectionProbe.intensity = reflectionIntensity;
            runtimeReflectionProbe.hdr = true;
            runtimeReflectionProbe.nearClipPlane = 0.2f;
            runtimeReflectionProbe.farClipPlane = 48f;
            runtimeReflectionProbe.importance = 2;
#if UNITY_WEBGL && !UNITY_EDITOR
            runtimeReflectionProbe.resolution = 64;
#else
            runtimeReflectionProbe.resolution = 128;
#endif
        }

        private void OnEnable()
        {
            ApplyEnvironment();
        }

        private void OnValidate()
        {
            ApplyEnvironment();
        }
    }
}
