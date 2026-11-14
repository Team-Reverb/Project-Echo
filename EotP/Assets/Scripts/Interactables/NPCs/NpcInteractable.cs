using UnityEngine;

public class NpcInteractable : InteractableBase
{
    [Header("NPC Details")]
    [SerializeField] private string npcName;
    [SerializeField] private Sprite sprite; // Optional

    [Header("Dialogue")]
    [TextArea(2, 5)]
    [SerializeField] private string[] lines;

    [Header("Camera")]
    [SerializeField] private NpcInteractionCamera npcCamera;

    public override string Prompt => "talk to " + npcName;

    public override void Interact(PlayerController player)
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("NpcInteractable: No DialogueManager in the scene.");
            return;
        }

        DialogueManager.Instance.StartDialogue(npcName, lines, npcCamera);
    }

    protected override void Awake()
    {
        base.Awake();
        ApplySprite();
    }

    void OnValidate()
    {
        ApplySprite();
    }

    void ApplySprite()
    {
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }
}
