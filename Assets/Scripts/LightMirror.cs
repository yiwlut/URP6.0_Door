using UnityEngine;

namespace DoorPuzzle
{
    public sealed class LightMirror : MonoBehaviour, ITargetable
    {
        [SerializeField] private float rotationStep = 45f;
        [SerializeField] private Renderer highlightRenderer;
        private MaterialPropertyBlock properties;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public bool CanTarget => enabled && gameObject.activeInHierarchy;
        public Transform TargetTransform => transform;

        private void Awake()
        {
            if (highlightRenderer == null) highlightRenderer = GetComponentInChildren<Renderer>();
            properties = new MaterialPropertyBlock();
        }

        public void SetTargeted(bool targeted)
        {
            if (highlightRenderer == null) return;
            highlightRenderer.GetPropertyBlock(properties);
            properties.SetColor(EmissionColor, targeted ? new Color(0.15f, 0.85f, 1f) * 3f : Color.black);
            highlightRenderer.SetPropertyBlock(properties);
        }

        public void Activate(ThirdPersonController player) => transform.Rotate(Vector3.up, rotationStep, Space.World);
    }
}
