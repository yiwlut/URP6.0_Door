using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DoorPuzzle
{
    /// <summary>Blends the prefab-authored idle and walk clips from controller velocity.</summary>
    public sealed class PlayerVisualAnimator : MonoBehaviour
    {
        private const float MovingThreshold = 0.04f;

        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip walkingClip;
        [SerializeField] private Material playerMaterial;
        [SerializeField, Min(0.01f)] private float transitionSpeed = 8f;

        private AnimationClipPlayable idlePlayable;
        private AnimationClipPlayable walkPlayable;
        private AnimationMixerPlayable mixer;
        private PlayableGraph graph;
        private float walkWeight;

        public void Configure(Animator targetAnimator, CharacterController controller,
            AnimationClip idle, AnimationClip walking, Material material)
        {
            animator = targetAnimator;
            characterController = controller;
            idleClip = idle;
            walkingClip = walking;
            playerMaterial = material;
            ApplyMaterial();
            if (Application.isPlaying) BuildGraph();
        }

        private void Awake()
        {
            ApplyMaterial();
            BuildGraph();
        }

        private void ApplyMaterial()
        {
            if (playerMaterial == null || animator == null) return;
            foreach (var renderer in animator.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index++) materials[index] = playerMaterial;
                renderer.sharedMaterials = materials;
            }
        }

        private void BuildGraph()
        {
            if (graph.IsValid() || animator == null || characterController == null ||
                idleClip == null || walkingClip == null) return;

            animator.applyRootMotion = false;
            graph = PlayableGraph.Create("Player Locomotion");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
            idlePlayable.SetApplyFootIK(true);
            walkPlayable = AnimationClipPlayable.Create(graph, walkingClip);
            walkPlayable.SetApplyFootIK(true);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            graph.Connect(idlePlayable, 0, mixer, 0);
            graph.Connect(walkPlayable, 0, mixer, 1);
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);
            AnimationPlayableOutput.Create(graph, "Player Locomotion", animator).SetSourcePlayable(mixer);
            graph.Play();
        }

        private void Update()
        {
            if (!graph.IsValid() || characterController == null) return;

            var planarSpeed = Vector3.ProjectOnPlane(characterController.velocity, Vector3.up).magnitude;
            var targetWeight = planarSpeed > MovingThreshold ? 1f : 0f;
            walkWeight = Mathf.MoveTowards(walkWeight, targetWeight, transitionSpeed * Time.deltaTime);
            mixer.SetInputWeight(0, 1f - walkWeight);
            mixer.SetInputWeight(1, walkWeight);
            idlePlayable.SetSpeed(1d);
            walkPlayable.SetSpeed(planarSpeed > MovingThreshold
                ? Mathf.Clamp(planarSpeed / 4.2f, 0.72f, 1.55f)
                : 0d);
        }

        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
