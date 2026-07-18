using UnityEngine;

namespace DoorPuzzle
{
    public sealed class PuzzleChunk : MonoBehaviour
    {
        [SerializeField] private string chunkId = "chunk";
        [SerializeField, Min(1f)] private float length = 10f;

        public string ChunkId => chunkId;
        public float Length => length;

        public void Configure(string id, float chunkLength)
        {
            chunkId = id;
            length = Mathf.Max(1f, chunkLength);
        }
    }
}
