using UnityEngine;

public class Checkpoint : InteractableBase
{
    [Header("Checkpoint")]
    [Tooltip("Optional. Where the player respawns. Uses this object's position if empty.")]
    [SerializeField] private Transform respawnPoint;

    [Header("Visuals")]
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = new Color(1f, 0.6f, 0.2f);

    private bool isActive;
    private bool isHighlighted;

    public bool IsActive => isActive;
    public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position;

    // Shown by InteractionPromptUI as "Press [E] to rest"
    public override string Prompt => "rest";

    protected override void Awake()
    {
        base.Awake();
        UpdateVisual();
    }

    // Called by InteractionDetector when the player presses Interact
    public override void Interact(PlayerController player)
    {
        if (CheckpointManager.Instance == null)
        {
            Debug.LogWarning("Checkpoint: No CheckpointManager in the scene.");
            return;
        }

        CheckpointManager.Instance.ActivateCheckpoint(this, player);
    }

    // Called by CheckpointManager
    public void SetActiveState(bool active)
    {
        isActive = active;
        UpdateVisual();
    }

    // The base class resets the sprite colour when un-highlighting, so reapply ours
    public override void SetHighlighted(bool highlighted)
    {
        base.SetHighlighted(highlighted);
        isHighlighted = highlighted;
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        Color color = isActive ? activeColor : inactiveColor;
        if (isHighlighted)
        {
            color = Color.Lerp(color, Color.white, 0.35f); // brighter while targeted
        }
        spriteRenderer.color = color;
    }
}