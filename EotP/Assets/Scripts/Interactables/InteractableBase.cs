using UnityEngine;

// Optional base class that handles the basic parts of IInteractable (highlight, position).
[RequireComponent(typeof(Collider2D))]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interactable")]
    [SerializeField] private Transform interactionPoint; // Optional. Defaults to object's position.
    [SerializeField] private GameObject highlightObject; // Optional. Outline object that is switched on while targeted.
    [SerializeField] protected SpriteRenderer spriteRenderer; // Optional. Tinted if no highlight object is set.
    [SerializeField] private Color highlightTint = new Color(1f, 0.95f, 0.6f);

    private Color originalColor = Color.white;

    public abstract string Prompt { get; }
    public Vector2 InteractionPoint => interactionPoint != null ? (Vector2)interactionPoint.position : (Vector2)transform.position;

    public virtual bool CanInteract(PlayerController player) => true;
    public abstract void Interact(PlayerController player);

    void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void Awake()
    {
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        if (highlightObject != null) highlightObject.SetActive(false);
    }

    public virtual void SetHighlighted(bool highlighted)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(highlighted);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.color = highlighted ? highlightTint : originalColor;
        }
    }
}
