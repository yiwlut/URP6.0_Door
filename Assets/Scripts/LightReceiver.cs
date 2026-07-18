using UnityEngine;

namespace DoorPuzzle
{
    public sealed class LightReceiver : MonoBehaviour
    {
        [SerializeField] private Renderer indicator;
        [SerializeField] private string channelId = "main";
        [SerializeField, Min(0.02f)] private float holdTime = 0.12f;
        private float lastLitTime = -10f;
        private Color receivedColor = Color.black;
        private MaterialPropertyBlock properties;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        public bool IsLit => Time.time - lastLitTime <= holdTime;
        public string ChannelId => channelId;

        private void Awake()
        {
            if (indicator == null) indicator = GetComponentInChildren<Renderer>();
            properties = new MaterialPropertyBlock();
        }

        public void ReceiveLight(Color color)
        {
            lastLitTime = Time.time;
            receivedColor = color;
        }

        private void LateUpdate()
        {
            if (indicator == null) return;
            indicator.GetPropertyBlock(properties);
            properties.SetColor(EmissionColor, IsLit ? receivedColor * 3f : Color.black);
            indicator.SetPropertyBlock(properties);
        }
    }
}
