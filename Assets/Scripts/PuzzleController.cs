using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace DoorPuzzle
{
    public sealed class PuzzleController : MonoBehaviour
    {
        private ThirdPersonController player;
        private GameHUD hud;
        private Camera puzzleCamera;
        private Transform portalCenter;
        private Transform alignmentPad;
        private Renderer maskRenderer;
        private Renderer[] sigilRenderers;
        private Renderer[] pastRenderers;
        private GameObject physicalBarrier;
        private Material portalVfxMaterial;
        private Material frameMaterial;
        private Material realizedPastMaterial;
        private ParticleSystem solveBurst;
        private Light portalLight;
        private Vignette vignette;
        private ChromaticAberration chromaticAberration;
        private readonly List<Transform> sigilCenters = new List<Transform>();

        private float alignment;
        private float focusProgress;
        private bool onPad;
        private bool solved;

        public bool Solved => solved;
        public float Alignment => alignment;

        public void DebugSolve()
        {
            if (!solved && player != null)
            {
                StartCoroutine(SolveSequence());
            }
        }

        public void Initialize(
            ThirdPersonController thirdPersonPlayer,
            GameHUD puzzleHud,
            Transform center,
            Transform pad,
            Renderer portalMask,
            Renderer[] sigils,
            Renderer[] pastWorldRenderers,
            GameObject barrier,
            Material vfxMaterial,
            Material doorFrameMaterial,
            Material pastMaterial,
            ParticleSystem burst,
            Light doorLight,
            Vignette volumeVignette,
            ChromaticAberration volumeChromaticAberration)
        {
            player = thirdPersonPlayer;
            hud = puzzleHud;
            puzzleCamera = player.PlayerCamera;
            portalCenter = center;
            alignmentPad = pad;
            maskRenderer = portalMask;
            sigilRenderers = sigils;
            pastRenderers = pastWorldRenderers;
            physicalBarrier = barrier;
            portalVfxMaterial = vfxMaterial;
            frameMaterial = doorFrameMaterial;
            realizedPastMaterial = pastMaterial;
            solveBurst = burst;
            portalLight = doorLight;
            vignette = volumeVignette;
            chromaticAberration = volumeChromaticAberration;

            sigilCenters.Clear();
            foreach (var renderer in sigilRenderers)
            {
                sigilCenters.Add(renderer.transform);
            }
        }

        private void Update()
        {
            if (player == null || player.IsCinematic)
            {
                hud?.SetCinematic(true);
                return;
            }

            hud?.SetCinematic(false);
            if (solved)
            {
                hud?.SetPuzzleState(1f, 1f, true, true);
                player.SetPuzzleFocus(0f, 0f);
                return;
            }

#if UNITY_EDITOR
            // Editor-only smoke-test hook. It is intentionally excluded from builds
            // and lets automated QA exercise the entire realization/camera sequence.
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            {
                DebugSolve();
                return;
            }
#endif

            alignment = CalculateAlignment(out onPad);
            var holdingFocus = Keyboard.current != null && Keyboard.current.eKey.isPressed;
            if (onPad && alignment > 0.82f && holdingFocus)
            {
                focusProgress += Time.deltaTime / 2.65f * Mathf.InverseLerp(0.82f, 1f, alignment);
            }
            else
            {
                focusProgress -= Time.deltaTime * (holdingFocus ? 0.12f : 0.32f);
            }

            focusProgress = Mathf.Clamp01(focusProgress);
            player.SetPuzzleFocus(alignment, focusProgress);
            hud?.SetPuzzleState(alignment, focusProgress, onPad, false);

            if (portalVfxMaterial != null)
            {
                if (portalVfxMaterial.HasProperty("_Intensity"))
                    portalVfxMaterial.SetFloat("_Intensity", Mathf.Lerp(1.45f, 6.2f, Mathf.Max(alignment * 0.55f, focusProgress)));
                if (portalVfxMaterial.HasProperty("_VertexWave"))
                    portalVfxMaterial.SetFloat("_VertexWave", Mathf.Lerp(0.055f, 0.006f, alignment));
            }

            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(0.28f, 0.43f, focusProgress);
            }

            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Mathf.Lerp(0.025f, 0.18f, focusProgress);
            }

            if (focusProgress >= 0.999f)
            {
                StartCoroutine(SolveSequence());
            }
        }

        private float CalculateAlignment(out bool standingOnPad)
        {
            var playerPosition = player.transform.position;
            var horizontalDistance = Vector2.Distance(
                new Vector2(playerPosition.x, playerPosition.z),
                new Vector2(alignmentPad.position.x, alignmentPad.position.z));
            standingOnPad = horizontalDistance < 2.05f;
            if (!standingOnPad || sigilCenters.Count == 0)
            {
                return 0f;
            }

            var points = new Vector3[sigilCenters.Count];
            var average = Vector2.zero;
            for (var i = 0; i < sigilCenters.Count; i++)
            {
                points[i] = puzzleCamera.WorldToViewportPoint(sigilCenters[i].position);
                if (points[i].z <= 0f) return 0f;
                average += new Vector2(points[i].x, points[i].y);
            }
            average /= points.Length;

            var spread = 0f;
            for (var i = 0; i < points.Length; i++)
            {
                spread = Mathf.Max(spread, Vector2.Distance(average, new Vector2(points[i].x, points[i].y)));
            }

            var lookError = Vector2.Distance(average, new Vector2(0.5f, 0.5f));
            var positionScore = Mathf.InverseLerp(2.05f, 0.12f, horizontalDistance);
            var overlapScore = Mathf.InverseLerp(0.042f, 0.004f, spread);
            var lookScore = Mathf.InverseLerp(0.23f, 0.025f, lookError);
            return Mathf.Clamp01(positionScore * overlapScore * lookScore);
        }

        private IEnumerator SolveSequence()
        {
            solved = true;
            hud?.SetPuzzleState(1f, 1f, true, true);
            hud?.SetCinematic(true);
            solveBurst?.Play(true);

            if (portalLight != null)
            {
                portalLight.color = new Color(1f, 0.67f, 0.28f);
            }

            StartCoroutine(player.PlaySolveSequence(portalCenter.position));

            const float revealDuration = 2.15f;
            for (var t = 0f; t < revealDuration; t += Time.deltaTime)
            {
                var p = Mathf.SmoothStep(0f, 1f, t / revealDuration);
                if (portalVfxMaterial != null)
                {
                    if (portalVfxMaterial.HasProperty("_Intensity"))
                        portalVfxMaterial.SetFloat("_Intensity", Mathf.Lerp(6f, 11f, Mathf.Sin(p * Mathf.PI)));
                }
                if (frameMaterial != null)
                {
                    frameMaterial.SetColor("_EmissionColor", Color.Lerp(new Color(0.08f, 0.38f, 0.75f) * 2f, new Color(1f, 0.48f, 0.12f) * 5f, p));
                }
                if (portalLight != null)
                {
                    portalLight.intensity = Mathf.Lerp(8.5f, 24f, Mathf.Sin(p * Mathf.PI));
                }
                if (chromaticAberration != null)
                {
                    chromaticAberration.intensity.value = Mathf.Sin(p * Mathf.PI) * 0.55f;
                }
                yield return null;
            }

            if (maskRenderer != null) maskRenderer.enabled = false;
            if (physicalBarrier != null) physicalBarrier.SetActive(false);
            if (portalVfxMaterial != null)
            {
                // The echo has become real, so its particles are no longer clipped
                // by the portal stencil. The alignment arcs themselves are hidden below.
            }
            foreach (var renderer in sigilRenderers)
            {
                if (renderer != null) renderer.enabled = false;
            }
            foreach (var renderer in pastRenderers)
            {
                if (renderer == null) continue;
                renderer.gameObject.layer = 0;
                renderer.sharedMaterial = realizedPastMaterial;
            }

            if (portalLight != null) portalLight.intensity = 9f;
            if (vignette != null) vignette.intensity.value = 0.24f;
            if (chromaticAberration != null) chromaticAberration.intensity.value = 0.02f;
            if (portalVfxMaterial != null && portalVfxMaterial.HasProperty("_Intensity")) portalVfxMaterial.SetFloat("_Intensity", 2.4f);

            yield return new WaitForSeconds(2.1f);
            hud?.SetCinematic(false);
            hud?.SetPuzzleState(1f, 1f, true, true);
        }
    }
}
