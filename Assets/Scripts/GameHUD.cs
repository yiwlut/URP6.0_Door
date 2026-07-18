using UnityEngine;

namespace DoorPuzzle
{
    /// <summary>
    /// Keeps only non-verbal targeting and cinematic framing.
    /// The playable scene never explains itself with text.
    /// </summary>
    public sealed class GameHUD : MonoBehaviour
    {
        private float alignment;
        private float cinematicBars = 1f;
        private bool solved;

        public void SetPuzzleState(float alignmentValue, float progressValue, bool onPad, bool isSolved)
        {
            alignment = alignmentValue;
            solved = isSolved;
        }

        public void SetCinematic(bool active) => cinematicBars = active ? 1f : 0f;

        private void OnGUI()
        {
            var scale = Mathf.Clamp(Screen.height / 900f, 0.78f, 1.35f);
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var width = Screen.width / scale;
            var height = Screen.height / scale;

            if (!solved)
            {
                var crossColor = Color.Lerp(
                    new Color(0.65f, 0.8f, 1f, 0.7f),
                    new Color(1f, 0.72f, 0.25f),
                    alignment);
                DrawRect(new Rect(width * 0.5f - 8f, height * 0.5f - 1f, 16f, 2f), crossColor);
                DrawRect(new Rect(width * 0.5f - 1f, height * 0.5f - 8f, 2f, 16f), crossColor);
            }

            if (cinematicBars > 0.01f)
            {
                DrawRect(new Rect(0f, 0f, width, 54f * cinematicBars), Color.black);
                DrawRect(new Rect(0f, height - 54f * cinematicBars, width, 54f * cinematicBars), Color.black);
            }

            GUI.matrix = oldMatrix;
        }

        private static void DrawRect(Rect rect, Color color)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }
}
