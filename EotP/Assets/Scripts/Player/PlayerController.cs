using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Inspector Variables

    [Header("References")]
    public EchoPulse echoPulsePrefab;

    [Header("Movement Variables")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Jump Variables")]
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private float minJumpTime = 0.1f;
    [SerializeField] private float maxJumpTime = 0.4f;
    [SerializeField] private float jumpCutMultiplier = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;

    [Header("State")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float groundedGraceTime = 0.08f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Pulse Cooldown")]
    [SerializeField] private float pulseCooldownDefault = 1f;
    [SerializeField] private float pulseCooldownCombat = 5f;

    [Header("Standard Sword")]
    [SerializeField] private float swordReach = 1f;
    [SerializeField] private float swordHeight = 0.4f;
    [SerializeField] private float swordHeightOffset = 0.1f;
    [SerializeField] private float swordDamage = 10f;
    [SerializeField] private float comboCooldown = 0.5f;
    [SerializeField] private LayerMask enemyLayers;

    // Events

    public event System.Action AttackRequested;
    public event System.Action Damaged;

    // Private Variables

    private bool isInCombat;
    private float moveInput;
    private bool isJumping;
    private float jumpTimer;
    private float lastGroundedTime = -1f;
    private float pulseCooldownTimer;
    private float pulseCooldownDuration;
    private float attackLockedUntil;
    private float lastGroundContactTime = -1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private InteractionDetector interactionDetector;
    private PlayerControls controls;

    // Read-only Values for Other Systems

    public bool IsGrounded => isGrounded;
    public bool IsInCombat => isInCombat;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsPulseReady => pulseCooldownTimer <= 0f;
    public float PulseCooldownNormalized => pulseCooldownDuration > 0f ? Mathf.Clamp01(pulseCooldownTimer / pulseCooldownDuration) : 0f;
    private bool CanJump => Time.time - lastGroundedTime <= coyoteTime;

    // Unity

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        interactionDetector = GetComponentInChildren<InteractionDetector>();
        controls = new PlayerControls();
        currentHealth = maxHealth;
    }

    void OnEnable()
    {
        controls.Player.Move.performed += OnMove;
        controls.Player.Move.canceled += OnMove;
        controls.Player.Jump.started += OnJumpPressed;
        controls.Player.Pulse.performed += OnPulse;
        controls.Player.Interact.performed += OnInteract;
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Move.performed -= OnMove;
        controls.Player.Move.canceled -= OnMove;
        controls.Player.Jump.started -= OnJumpPressed;
        controls.Player.Pulse.performed -= OnPulse;
        controls.Player.Interact.performed -= OnInteract;
        controls.Player.Disable();

        isGrounded = false;
    }

    void Update()
    {
        HandleCoyoteTimer();
        HandlePulseCooldownTimer();
        HandleAttackInput();
    }

    void FixedUpdate()
    {
        UpdateGroundCheck();
        HandleJump();
        HandleMovement();
    }

    // Input Callbacks

    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<float>();
    }

    void OnJumpPressed(InputAction.CallbackContext context)
    {
        if (!CanJump) return;

        isJumping = true;
        jumpTimer = 0f;
        lastGroundedTime = -1f;
        lastGroundContactTime = -1f;

        Vector2 velocity = rb.linearVelocity;
        velocity.y = jumpSpeed;
        rb.linearVelocity = velocity;
    }

    void OnPulse(InputAction.CallbackContext context)
    {
        if (!IsPulseReady)
        {
            Debug.Log("Pulse on cooldown. " + pulseCooldownTimer + " seconds left.");
            return;
        }

        if (echoPulsePrefab == null)
        {
            Debug.LogWarning("PlayerController: Echo Pulse Prefab is not assigned.");
            return;
        }

        Instantiate(echoPulsePrefab, transform.position, Quaternion.identity);
        StartPulseCooldown();
    }

    void OnInteract(InputAction.CallbackContext context)
    {
        if (interactionDetector == null)
        {
            Debug.LogWarning("PlayerController: No InteractionDetector found.");
            return;
        }

        interactionDetector.TryInteract();
    }

    // Movement Handling

    void HandleMovement()
    {
        Vector2 velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        rb.linearVelocity = velocity;

        if (moveInput != 0f)
        {
            FaceDirection(moveInput);
        }
    }

    void FaceDirection(float direction)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
        transform.localScale = scale;
    }

    void HandleJump()
    {
        if (!isJumping) return;

        jumpTimer += Time.fixedDeltaTime;

        bool held = controls.Player.Jump.IsPressed();
        bool reachedMax = jumpTimer >= maxJumpTime;
        bool letGo = !held && jumpTimer >= minJumpTime;
        bool lostSpeed = rb.linearVelocity.y <= 0.1f;

        if (reachedMax || letGo || lostSpeed)
        {
            isJumping = false;

            if (letGo && !reachedMax && rb.linearVelocity.y > 0f)
            {
                Vector2 velocity = rb.linearVelocity;
                velocity.y *= jumpCutMultiplier;
                rb.linearVelocity = velocity;
            }
            return;
        }

        Vector2 rising = rb.linearVelocity;
        rising.y = jumpSpeed;
        rb.linearVelocity = rising;
    }

    void HandleCoyoteTimer()
    {
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            lastGroundedTime = Time.time;
        }
    }

    // Pulse Cooldown

    void HandlePulseCooldownTimer()
    {
        if (pulseCooldownTimer > 0f)
        {
            pulseCooldownTimer -= Time.deltaTime;
        }
    }

    void StartPulseCooldown()
    {
        if (isInCombat)
        {
            pulseCooldownDuration = pulseCooldownCombat;
        }
        else
        {
            pulseCooldownDuration = pulseCooldownDefault;
        }
        pulseCooldownTimer = pulseCooldownDuration;
    }

    // Standard Sword

    void HandleAttackInput()
    {
        if (controls.Player.Attack.IsPressed() && Time.time >= attackLockedUntil)
        {
            AttackRequested?.Invoke();
        }
    }

    public void SwordHit()
    {
        GetSwordArea(out Vector2 center, out Vector2 size);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayers);
        HashSet<GameObject> struck = new HashSet<GameObject>();

        foreach (Collider2D hit in hits)
        {
            GameObject target = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
            if (!struck.Add(target)) continue;


            Debug.Log("Sword hit " + target.name);
            // public void TakeDamage(float amount) method in enemy will receive a hit
            target.SendMessage("TakeDamage", swordDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void StartAttackCooldown()
    {
        attackLockedUntil = Time.time + comboCooldown;
    }

    void GetSwordArea(out Vector2 center, out Vector2 size)
    {
        Collider2D body = col != null ? col : GetComponent<Collider2D>();
        Bounds bounds = body.bounds;

        float facing = Mathf.Sign(transform.localScale.x);
        float frontEdge = facing > 0f ? bounds.max.x : bounds.min.x;

        center = new Vector2(frontEdge + facing * swordReach / 2f, bounds.center.y + swordHeightOffset);
        size = new Vector2(swordReach, swordHeight);
    }

    // Health and Combat

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        Damaged?.Invoke();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void SetInCombat(bool value)
    {
        isInCombat = value;
    }

    // Input Locking

    public void SetInputEnabled(bool value)
    {
        if (value)
        {
            controls.Player.Enable();
        }
        else
        {
            controls.Player.Disable();
            moveInput = 0f;
            isJumping = false;
        }
    }

    // Ground Detection

    void UpdateGroundCheck()
    {
        GetGroundCheckArea(out Vector2 center, out Vector2 size);

        bool touching = false;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Ground"))
            {
                touching = true;
                break;
            }
        }

        if (touching && !isJumping) lastGroundContactTime = Time.time;

        isGrounded = Time.time - lastGroundContactTime <= groundedGraceTime;
    }

    void GetGroundCheckArea(out Vector2 center, out Vector2 size)
    {
        Collider2D body = col != null ? col : GetComponent<Collider2D>();
        Bounds bounds = body.bounds;

        center = new Vector2(bounds.center.x, bounds.min.y - groundCheckDistance / 2f);
        size = new Vector2(bounds.size.x * 0.9f, groundCheckDistance);
    }

    // Debugging

    void OnDrawGizmosSelected()
    {
        GetSwordArea(out Vector2 center, out Vector2 size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);

        GetGroundCheckArea(out Vector2 groundCenter, out Vector2 groundSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCenter, groundSize);
    }

    // Unused at the moment
    //
    // void FeaturePlaceholderLog(string feature)
    // {
    //     Debug.Log(feature + " input detected");
    // }

    [ContextMenu("Debug: Take 10 Damage")]
    void DebugTakeDamage() => TakeDamage(10);

    [ContextMenu("Debug: Heal 10")]
    void DebugHeal() => Heal(10);

    [ContextMenu("Debug: Toggle Combat State")]
    void DebugToggleCombat() => SetInCombat(!isInCombat);
}
