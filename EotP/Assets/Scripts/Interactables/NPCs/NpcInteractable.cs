using UnityEngine;

public class NpcInteractable : InteractableBase
{
    [Header("NPC Details")]
    [SerializeField] private string npcName;
    [SerializeField] private Sprite sprite;

    [Header("Dialogue")]
    [TextArea(2, 8)]
    [SerializeField] private string[] lines;

    [Header("Camera")]
    [SerializeField] private NpcInteractionCamera npcCamera;

    [Header("Visibility During Dialogue")]
    [SerializeField] private Material talkingMaterial;

    private Material originalMaterial;
    private bool isTalking;

    public override string Prompt => "talk to " + npcName;

    public override void Interact(PlayerController player)
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("NpcInteractable: No DialogueManager in the scene.");
            return;
        }

        DialogueManager.Instance.StartDialogue(npcName, lines, npcCamera);

        if (DialogueManager.Instance.IsActive) SetTalking(true);
    }

    protected override void Awake()
    {
        base.Awake();
        ApplySprite();

        if (spriteRenderer != null) originalMaterial = spriteRenderer.sharedMaterial;
    }

    void OnDestroy()
    {
        if (isTalking && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.DialogueEnded -= OnDialogueEnded;
        }
    }

    void SetTalking(bool talking)
    {
        if (talking == isTalking) return;
        isTalking = talking;

        if (spriteRenderer != null && talkingMaterial != null)
        {
            spriteRenderer.sharedMaterial = talking ? talkingMaterial : originalMaterial;
        }

        DialogueManager manager = DialogueManager.Instance;
        if (manager == null) return;

        if (talking) manager.DialogueEnded += OnDialogueEnded;
        else manager.DialogueEnded -= OnDialogueEnded;
    }

    void OnDialogueEnded()
    {
        SetTalking(false);
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
