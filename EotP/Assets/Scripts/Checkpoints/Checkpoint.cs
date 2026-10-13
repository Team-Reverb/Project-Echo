using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional. Where the player respawns. Uses this object's position if empty.")]
    [SerializeField] private Transform respawnPoint;
    [Tooltip("Optional. Shown while the player is in range, e.g. a 'Press E' text object.")]
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Visuals")]
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = new Color(1f, 0.6f, 0.2f);

    private PlayerControls controls;
    private PlayerController playerInRange;
    private bool isActive;

    public bool IsActive => isActive;
    public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position;

    // Sets up defaults when the script is first added
    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Awake()
    {
        controls = new PlayerControls();
        UpdateVisual();
        ShowPrompt(false);
    }

    void OnEnable()
    {
        controls.Player.Interact.performed += OnInteract;
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Interact.performed -= OnInteract;
        controls.Player.Disable();
    }

    void OnDestroy()
    {
        controls.Dispose();
    }

    // Input

    void OnInteract(InputAction.CallbackContext context)
    {
        if (playerInRange == null) return;

        if (CheckpointManager.Instance == null)
        {
            Debug.LogWarning("Checkpoint: No CheckpointManager in the scene.");
            return;
        }

        CheckpointManager.Instance.ActivateCheckpoint(this, playerInRange);
    }

    // Called by CheckpointManager

    public void SetActiveState(bool active)
    {
        isActive = active;
        UpdateVisual();
    }

    // Range Detection

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        playerInRange = player;
        ShowPrompt(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player != playerInRange) return;

        playerInRange = null;
        ShowPrompt(false);
    }

    // Visuals

    void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActive ? activeColor : inactiveColor;
        }
    }

    void ShowPrompt(bool show)
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(show);
        }
    }
}