using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HazardZone : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damagePerSecond = 20f;

    private PlayerController playerInside;

    // Sets the collider to trigger automatically when the script is first added
    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // Damage is applied in Update rather than OnTriggerStay2D,
    // because a stationary Rigidbody2D can fall asleep and stop Stay callbacks
    void Update()
    {
        if (playerInside != null)
        {
            playerInside.TakeDamage(damagePerSecond * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            playerInside = player;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && player == playerInside)
        {
            playerInside = null;
        }
    }
}