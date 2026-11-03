using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TagProjectile : MonoBehaviour
{
    [SerializeField] private string agentTag = "Agent";
    [SerializeField] private float lifetime = 3f;

    private Rigidbody2D rb;
    private PlayerBotController owner;
    private readonly HashSet<Collider2D> ignored = new HashSet<Collider2D>();
    private bool consumed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }


    public void Initialize(Vector2 direction, float speed, Collider2D[] ignoreColliders, PlayerBotController shooter)
    {
        owner = shooter;

        if (ignoreColliders != null)
        {
            foreach (Collider2D c in ignoreColliders)
            {
                if (c != null) ignored.Add(c);
            }
        }

        if (owner != null) owner.RegisterProjectile(this);

        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        if (other.isTrigger) return;
        if (ignored.Contains(other)) return;

        consumed = true;

        bool isAgent = other.CompareTag(agentTag) || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(agentTag));

        if (isAgent) other.SendMessageUpwards("OnTagged", SendMessageOptions.DontRequireReceiver);

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (owner != null) owner.UnregisterProjectile(this);
    }
}
