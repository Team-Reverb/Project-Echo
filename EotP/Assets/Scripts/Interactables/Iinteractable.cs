using UnityEngine;

public interface Iinteractable
{
    string Prompt { get; } // action text in UI prompt

    Vector2 InteractionPoint { get; } // world position

    bool CanInteract(PlayerController player); // interactable by player

    void Interact(PlayerController player);

    void SetHighlighted(bool highlighted); // called by detector to highlight
}
