using UnityEngine;

public class TestNPC : InteractableBase
{
    [SerializeField] private string npcName = "Test NPC";
    [SerializeField] private string text = "Hello!";

    public override string Prompt => "talk to " + npcName;

    public override void Interact(PlayerController player)
    {
        Debug.Log(npcName + ": " + text);
    }
}
