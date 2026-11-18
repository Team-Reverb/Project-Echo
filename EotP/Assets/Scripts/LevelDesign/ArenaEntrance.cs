using UnityEngine;

// The door into an arena. Interacting starts the fight; it stays shut until the arena is cleared.
public class ArenaEntrance : InteractableBase
{
    [Header("Arena")]
    [SerializeField] private ArenaEncounter encounter;
    [Tooltip("The solid part of the door. Disabled once the arena is cleared.")]
    [SerializeField] private GameObject doorBody;

    // Shown by InteractionPromptUI as "Press [E] to enter"
    public override string Prompt => "enter";

    // Only usable before the fight starts (or after a reset from dying)
    public override bool CanInteract(PlayerController player) => encounter != null && encounter.CanBegin;

    void Start()
    {
        if (encounter != null) encounter.Cleared += Open;
    }

    void OnDestroy()
    {
        if (encounter != null) encounter.Cleared -= Open;
    }

    public override void Interact(PlayerController player)
    {
        encounter.Begin(player);
    }

    void Open()
    {
        if (doorBody != null) doorBody.SetActive(false);
    }
}