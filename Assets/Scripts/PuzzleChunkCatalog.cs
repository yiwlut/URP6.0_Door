using System;
using UnityEngine;

namespace DoorPuzzle
{
    [CreateAssetMenu(menuName = "Blues With You/Puzzle Chunk Catalog", fileName = "PuzzleChunkCatalog")]
    public sealed class PuzzleChunkCatalog : ScriptableObject
    {
        [Serializable]
        public struct Placement
        {
            public GameObject prefab;
            public Vector3 position;
            public Vector3 rotation;
        }

        [SerializeField] private Placement[] placements = Array.Empty<Placement>();
        public Placement[] Placements => placements;

        public void SetPlacements(Placement[] value)
        {
            placements = value ?? Array.Empty<Placement>();
        }
    }
}
