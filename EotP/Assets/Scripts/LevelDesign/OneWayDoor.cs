using UnityEngine;

// Souls-style shortcut door: can only be opened from one side, then stays open.
// Setup: put this on an empty "Door" object with a BoxCollider2D set to Is Trigger,
// sized a few tiles wider than the door so the player can reach it from either side.
// Add a child "DoorBody" with the sprite and a solid BoxCollider2D, and drag it into doorBody.
public class OneWayDoor : InteractableBase
{
    public enum Side { Left, Right }

    [Header("Door")]
    [Tooltip("The side the player has to be standing on to open the door.")]
    public Side opensFrom = Side.Right;

    [Tooltip("The solid part that blocks the player. Disabled when the door opens.")]
    public GameObject doorBody;

    private bool isOpen;

    // Shown by InteractionPromptUI as "Press [E] to open"
    public override string Prompt => "open";

    // Once open, the door stops showing up as an interactable
    public override bool CanInteract(PlayerController player) => !isOpen;

    public override void Interact(PlayerController player)
    {
        bool playerOnRight = player.transform.position.x > transform.position.x;
        bool correctSide = (opensFrom == Side.Right) == playerOnRight;

        if (correctSide)
            Open();
        else
            Debug.Log("It won't open from this side."); // swap for a UI message later
    }

    public void Open()
    {
        isOpen = true;
        doorBody.SetActive(false); // swap for an animation later
    }
}