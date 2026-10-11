using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Inspector Variables

    [Header("References")]
    [SerializeField] private EchoPulse echoPulsePrefab;

    [Header("Movement Variables")]
    public float moveSpeed = 6f;

    [Header("Jump Variables")]
    public float tapJumpForce = 15f;
    public float heldJumpForce = 20f;
    public float holdThreshold = 0.3f;
    public float coyoteTime = 0.1f;

    [Header("State")]
    public bool isGrounded;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Pulse Cooldown")]
    public float pulseCooldownDefault = 1f;
    public float pulseCooldownCombat = 5f;

    [Header("State Machine")]
    public float actionStateDuration = 0.3f;

    // Private Variables

    private bool isInCombat;
    private float moveInput;
    private float jumpPressTime = -1f;
    private float lastGroundedTime = -1f;
    private float pulseCooldownTimer;
    private float pulseCooldownDuration;
    private float actionStateTimer;
    private CharacterState currentState = CharacterState.Idle;

    private Rigidbody2D rb;
    private PlayerControls controls;

    // Read-only Values for Other Systems

    public float HealthNormalized => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsPulseReady => pulseCooldownTimer <= 0f;
    public float PulseCooldownNormalized => pulseCooldownDuration > 0f ? Mathf.Clamp01(pulseCooldownTimer / pulseCooldownDuration) : 0f;
    public bool IsInCombat => isInCombat;
    public CharacterState CurrentState => currentState;
    private bool CanJump => Time.time - lastGroundedTime <= coyoteTime;

    // Unity Lifecycle

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
        currentHealth = maxHealth;
    }

    void OnEnable()
    {
        controls.Player.Move.performed += OnMove;
        controls.Player.Move.canceled += OnMove;
        controls.Player.Jump.started += OnJump;
        controls.Player.Jump.canceled += OnJump;
        controls.Player.Pulse.performed += OnPulse;
        controls.Player.Interact.performed += OnInteract;
        controls.Player.Attack.performed += OnAttack;
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Move.performed -= OnMove;
        controls.Player.Move.canceled -= OnMove;
        controls.Player.Jump.started -= OnJump;
        controls.Player.Jump.canceled -= OnJump;
        controls.Player.Pulse.performed -= OnPulse;
        controls.Player.Interact.performed -= OnInteract;
        controls.Player.Attack.performed -= OnAttack;
        controls.Player.Disable();
    }

    void Update()
    {
        HandleCoyoteTimer();
        HandlePulseCooldownTimer();
        HandleActionStateTimer();

        if (actionStateTimer <= 0f)
        {
            UpdateMovementState();
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    // Input Callbacks

    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<float>();
    }

    void OnJump(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            if (CanJump)
            {
                jumpPressTime = Time.time;
            }
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            if (jumpPressTime < 0f) return;

            float heldDuration = Time.time - jumpPressTime;
            float appliedForce = heldDuration >= holdThreshold ? heldJumpForce : tapJumpForce;

            Vector2 velocity = rb.linearVelocity;
            velocity.y = appliedForce;
            rb.linearVelocity = velocity;

            jumpPressTime = -1f;
            lastGroundedTime = -1f;
        }
    }

    void OnPulse(InputAction.CallbackContext context)
    {
        if (echoPulsePrefab == null)
        {
            Debug.LogWarning("PlayerController: Echo Pulse Prefab is not assigned.");
            return;
        }

        Instantiate(echoPulsePrefab, transform.position, Quaternion.identity);

        if (isInCombat)
        {
            pulseCooldownDuration = pulseCooldownCombat;
        }
        else
        {
            pulseCooldownDuration = pulseCooldownDefault;
        }
        pulseCooldownTimer = pulseCooldownDuration;

        EnterActionState(CharacterState.Pulsing);
    }

    void OnInteract(InputAction.CallbackContext context)
    {
        FeaturePlaceholderLog("Interact");
        EnterActionState(CharacterState.Interacting);
    }

    void OnAttack(InputAction.CallbackContext context)
    {
        FeaturePlaceholderLog("Attack");
        EnterActionState(CharacterState.Attacking);
    }

    // Movement Handling

    void HandleMovement()
    {
        Vector2 velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        rb.linearVelocity = velocity;

        if (moveInput != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveInput);
            transform.localScale = scale;
        }
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

    // Character State Handling

    void HandleActionStateTimer()
    {
        if (actionStateTimer > 0f)
        {
            actionStateTimer -= Time.deltaTime;
        }
    }

    void UpdateMovementState()
    {
        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0f)
            {
                currentState = CharacterState.Jumping;
            }
            else
            {
                currentState = CharacterState.Falling;
            }
        }
        else if (Mathf.Abs(moveInput) > 0.01f)
        {
            currentState = CharacterState.Walking;
        }
        else
        {
            currentState = CharacterState.Idle;
        }
    }

    void EnterActionState(CharacterState state)
    {
        currentState = state;
        actionStateTimer = actionStateDuration;
    }

    // Health and Combat

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void SetInCombat(bool value)
    {
        isInCombat = value;
    }

    // Ground Detection

    void OnCollisionEnter2D(Collision2D collision)
    {
        CheckForGround(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        CheckForGround(collision);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    void CheckForGround(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ground")) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    // Debugging

    void FeaturePlaceholderLog(string feature)
    {
        Debug.Log(feature + " input detected");
    }

    [ContextMenu("Debug: Take 10 Damage")]
    void DebugTakeDamage() => TakeDamage(10);

    [ContextMenu("Debug: Heal 10")]
    void DebugHeal() => Heal(10);

    [ContextMenu("Debug: Toggle Combat State")]
    void DebugToggleCombat() => SetInCombat(!isInCombat);
}
