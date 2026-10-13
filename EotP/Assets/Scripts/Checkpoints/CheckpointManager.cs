using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerController player;

    private Checkpoint currentCheckpoint;
    private Vector3 respawnPosition;
    private Rigidbody2D playerRb;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (player == null)
        {
            Debug.LogWarning("CheckpointManager: Player is not assigned.");
            return;
        }

        playerRb = player.GetComponent<Rigidbody2D>();

        // Before any checkpoint is lit, respawn where the player started
        respawnPosition = player.transform.position;
    }

    void Update()
    {
        if (player != null && player.CurrentHealth <= 0f)
        {
            Respawn();
        }
    }

    public void ActivateCheckpoint(Checkpoint checkpoint, PlayerController activatingPlayer)
    {
        // Only one checkpoint is lit at a time
        if (currentCheckpoint != null && currentCheckpoint != checkpoint)
        {
            currentCheckpoint.SetActiveState(false);
        }

        currentCheckpoint = checkpoint;
        currentCheckpoint.SetActiveState(true);
        respawnPosition = checkpoint.RespawnPosition;

        // Resting refills health, like a bonfire
        activatingPlayer.Heal(activatingPlayer.MaxHealth);
        Debug.Log("Checkpoint activated: " + checkpoint.name);
    }

    void Respawn()
    {
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.position = respawnPosition;
        }
        player.transform.position = respawnPosition;
        player.Heal(player.MaxHealth);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}