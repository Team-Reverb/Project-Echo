using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Inspector Variables

    [Header("References")]
    [SerializeField] private EchoPulse echoPulsePrefab;

    [Header("Movement Variables")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Jump Variables")]
    [SerializeField] private float tapJumpForce = 15f;
    [SerializeField] private float heldJumpForce = 20f;
    [SerializeField] private float holdThreshold = 0.3f;
    [SerializeField] private float coyoteTime = 0.1f;

    [Header("State")]
    [SerializeField] private bool isGrounded;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Pulse Cooldown")]
    [SerializeField] private float pulseCooldownDefault = 1f;
    [SerializeField] private float pulseCooldownCombat = 5f;

    [Header("Attack Combo")]
    [SerializeField] private float[] comboHitDurations = { 0.4f, 0.4f, 0.5f };
    [SerializeField] private float comboWindow = 0.3f;   
    [SerializeField] private float comboCooldown = 0.5f;

    [Header("Hurt")]
    [SerializeField] private float hurtDuration = 0.3f;


    [Header("State Machine")]
    [SerializeField] private float actionStateDuration = 0.3f;

    // Events

    public event System.Action<int> AttackStarted;
    public event System.Action Damaged;

    // Private Variables

    private bool isInCombat;
    private float moveInput;
    private float jumpPressTime = -1f;
    private float lastGroundedTime = -1f;
    private float pulseCooldownTimer;
    private float pulseCooldownDuration;
    private float actionStateTimer;
    private CharacterState actionState = CharacterState.Idle;
    private CharacterState movementState = CharacterState.Idle;
    private CharacterState currentState = CharacterState.Idle;
    private int comboIndex;
    private bool attackQueued;
    private float lastAttackEndTime = -1f;
    private float attackLockedUntil;

    private Rigidbody2D rb;
    private PlayerControls controls;

    // Read-only Values for Other Systems

    public bool IsGrounded => isGrounded;
    public bool IsInCombat => isInCombat;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsPulseReady => pulseCooldownTimer <= 0f;
    public float PulseCooldownNormalized => pulseCooldownDuration > 0f ? Mathf.Clamp01(pulseCooldownTimer / pulseCooldownDuration) : 0f;
    public CharacterState CurrentState => currentState;
    public CharacterState MovementState => movementState;
    private bool CanJump => Time.time - lastGroundedTime <= coyoteTime;
    private bool IsAttacking => actionStateTimer > 0f && actionState == CharacterState.Attacking;
    private bool IsHurt => actionStateTimer > 0f && actionState == CharacterState.Hurt;


    // Unity

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
        controls.Player.Jump.started += OnJumpPressed;
        controls.Player.Jump.canceled += OnJumpReleased;
        controls.Player.Pulse.performed += OnPulse;
        controls.Player.Interact.performed += OnInteract;
        controls.Player.Attack.performed += OnAttack;
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Move.performed -= OnMove;
        controls.Player.Move.canceled -= OnMove;
        controls.Player.Jump.started -= OnJumpPressed;
        controls.Player.Jump.canceled -= OnJumpReleased;
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
        HandleHeldAttack();
        UpdateMovementState();
        UpdateCurrentState();
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

    void OnJumpPressed(InputAction.CallbackContext context)
    {
        if (CanJump)
        {
            jumpPressTime = Time.time;
        }
    }

    void OnJumpReleased(InputAction.CallbackContext context)
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
        EnterActionState(CharacterState.Pulsing);
    }

    void OnInteract(InputAction.CallbackContext context)
    {
        FeaturePlaceholderLog("Interact");
        EnterActionState(CharacterState.Interacting);
    }

    void OnAttack(InputAction.CallbackContext context)
    {
        if (comboHitDurations.Length == 0) return;
        if (IsHurt) return;
        if (Time.time < attackLockedUntil) return;

        if (IsAttacking) 
        {
            attackQueued = true;
            return;
        }

        if (Time.time - lastAttackEndTime > comboWindow)
        {
            comboIndex = 0;
        }

        StartAttack();
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

    // Attack Combo

    void StartAttack()
    {
        attackQueued = false;
        FeaturePlaceholderLog("Attack " + (comboIndex + 1));
        EnterActionState(CharacterState.Attacking, comboHitDurations[comboIndex]);
        AttackStarted?.Invoke(comboIndex);
    }

    void OnAttackFinished()
    {
        lastAttackEndTime = Time.time;
        comboIndex++;

        if (comboIndex >= comboHitDurations.Length)
        {
            ResetCombo();
            attackLockedUntil = Time.time + comboCooldown;
            return;
        }

        if (attackQueued)
        {
            StartAttack();
        }
    }

    void HandleHeldAttack()
    {
        if (IsAttacking && controls.Player.Attack.IsPressed())
        {
            attackQueued = true;
        }
    }

    void ResetCombo()
    {
        comboIndex = 0;
        attackQueued = false;
    }

    // Character State Handling

    void HandleActionStateTimer()
    {
        if (actionStateTimer <= 0f) return;

        actionStateTimer -= Time.deltaTime;

        if (actionStateTimer <= 0f && actionState == CharacterState.Attacking)
        {
            OnAttackFinished();
        }

    }

    void UpdateMovementState()
    {
        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0f)
            {
                movementState = CharacterState.Jumping;
            }
            else
            {
                movementState = CharacterState.Falling;
            }
        }
        else if (Mathf.Abs(moveInput) > 0.01f)
        {
            movementState = CharacterState.Walking;
        }
        else
        {
            movementState = CharacterState.Idle;
        }
    }

    void UpdateCurrentState()
    {
        currentState = actionStateTimer > 0f ? actionState : movementState;
    }


    void EnterActionState(CharacterState state)
    {
        EnterActionState(state, actionStateDuration);
    }

    void EnterActionState(CharacterState state, float duration)
    {
        bool busy = IsAttacking || IsHurt;
        if (busy && (state == CharacterState.Pulsing || state == CharacterState.Interacting)) return;

        actionState = state;
        actionStateTimer = duration;
        currentState = state;
    }


    // Health and Combat

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        ResetCombo();
        EnterActionState(CharacterState.Hurt, hurtDuration);
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
