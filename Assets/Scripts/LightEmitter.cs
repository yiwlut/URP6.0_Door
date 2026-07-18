using System.Collections.Generic;
using UnityEngine;

namespace DoorPuzzle
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LightEmitter : MonoBehaviour
    {
        [SerializeField] private Color beamColor = new Color(0.25f, 0.85f, 1f, 1f);
        [SerializeField, Min(1f)] private float range = 24f;
        [SerializeField, Range(0, 4)] private int maxReflections = 2;
        [SerializeField] private LayerMask collisionMask = ~0;
        private LineRenderer beam;

        private void Awake()
        {
            beam = GetComponent<LineRenderer>();
            beam.useWorldSpace = true;
            beam.widthMultiplier = 0.045f;
            beam.numCapVertices = 3;
            beam.startColor = beamColor;
            beam.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.12f);
        }

        private void LateUpdate()
        {
            var points = new List<Vector3>(maxReflections + 2) { transform.position };
            var origin = transform.position;
            var direction = transform.forward;
            var remaining = range;
            for (var bounce = 0; bounce <= maxReflections && remaining > 0.05f; bounce++)
            {
                if (!Physics.Raycast(origin, direction, out var hit, remaining, collisionMask, QueryTriggerInteraction.Collide))
                {
                    points.Add(origin + direction * remaining);
                    break;
                }
                points.Add(hit.point);
                hit.collider.GetComponentInParent<LightReceiver>()?.ReceiveLight(beamColor);
                var mirror = hit.collider.GetComponentInParent<LightMirror>();
                if (mirror == null) break;
                remaining -= hit.distance;
                direction = Vector3.Reflect(direction, hit.normal).normalized;
                origin = hit.point + direction * 0.025f;
            }
            beam.positionCount = points.Count;
            beam.SetPositions(points.ToArray());
        }
    }
}
