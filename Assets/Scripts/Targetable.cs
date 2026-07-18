using UnityEngine;

namespace DoorPuzzle
{
    public interface ITargetable
    {
        bool CanTarget { get; }
        Transform TargetTransform { get; }
        void SetTargeted(bool targeted);
        void Activate(ThirdPersonController player);
    }

    public interface IInteractable
    {
        bool CanInteract { get; }
        void SetInteractionFocused(bool focused);
        void Interact(ThirdPersonController player);
    }
}
