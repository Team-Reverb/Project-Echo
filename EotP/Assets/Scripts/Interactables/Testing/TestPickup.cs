using UnityEngine;

public class TestPickup : InteractableBase
{
    [SerializeField] private string itemName = "Test Item";

    public override string Prompt => "pick up " + itemName;

    public override void Interact(PlayerController player)
    {
        Debug.Log("Picked up " + itemName);
        Destroy(gameObject);
    }

}
