using UnityEngine;
using UnityEngine.InputSystem;

// Souls-style shortcut door: can only be opened from one side, then stays open.
// Setup: put this on an empty "Door" object with a BoxCollider2D set to Is Trigger,
// sized a few tiles wider than the door so the player can reach it from either side.
// Add a child "DoorBody" with the sprite and a solid BoxCollider2D, and drag it into doorBody.
[RequireComponent(typeof(BoxCollider2D))]
public class OneWayDoor : MonoBehaviour
{
    public enum Side { Left, Right }

    [Tooltip("The side the player has to be standing on to open the door.")]
    public Side opensFrom = Side.Right;

    [Tooltip("The solid part that blocks the player. Disabled when the door opens.")]
    public GameObject doorBody;

    bool playerInRange;
    Transform player;
    bool isOpen;

    void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void Update()
    {
        if (isOpen || !playerInRange || Keyboard.current == null) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        bool playerOnRight = player.position.x > transform.position.x;
        bool correctSide = (opensFrom == Side.Right) == playerOnRight;

        if (correctSide)
            Open();
        else
            Debug.Log("It won't open from this side."); // swap for a UI prompt later
    }

    public void Open()
    {
        isOpen = true;
        doorBody.SetActive(false); // swap for an animation later
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        player = other.transform;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}