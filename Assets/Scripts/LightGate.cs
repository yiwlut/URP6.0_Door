using UnityEngine;

namespace DoorPuzzle
{
    public sealed class LightGate : MonoBehaviour
    {
        [SerializeField] private LightReceiver[] receivers;
        [SerializeField] private string channelId = "main";
        [SerializeField] private Transform movingPart;
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 4.5f, 0f);
        [SerializeField] private float speed = 3f;
        private Vector3 closedPosition;

        private void Awake()
        {
            if (movingPart == null) movingPart = transform;
            if (receivers == null || receivers.Length == 0)
            {
                var all = FindObjectsByType<LightReceiver>(FindObjectsSortMode.None);
                receivers = System.Array.FindAll(all, receiver => receiver != null && receiver.ChannelId == channelId);
            }
            closedPosition = movingPart.localPosition;
        }

        private void Update()
        {
            var open = receivers != null && receivers.Length > 0;
            if (receivers != null)
            {
                foreach (var receiver in receivers) open &= receiver != null && receiver.IsLit;
            }
            var destination = closedPosition + (open ? openOffset : Vector3.zero);
            movingPart.localPosition = Vector3.MoveTowards(movingPart.localPosition, destination, speed * Time.deltaTime);
        }
    }
}
