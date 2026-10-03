using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Variables")]
    public float moveSpeed = 6f;

    [Header("Jump Variables")]
    public float tapJumpForce = 6f;
    public float heldJumpForce = 6f;
    public float holdThreshold = 0.2f;

    [Header("State")]
    public bool isGrounded;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Pulse Cooldown")]
    public float pulseCooldownDefault = 1f;
    public float pulseCooldownCombat = 5f;

    private bool isInCombat;
    private float pulseCooldownTimer;
    private float pulseCooldownDuration;
    private float jumpPressTime = -1f;

    private Rigidbody2D rb;
    private PlayerControls controls;
    private float moveInput;

    // Read-only values for other systems to use
    public float HealthNormalized => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsPulseReady => pulseCooldownTimer <= 0f;
    public float PulseCooldownNormalized => pulseCooldownDuration > 0f ? Mathf.Clamp01(pulseCooldownTimer / pulseCooldownDuration) : 0f;
    public bool IsInCombat => isInCombat;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        HandlePulseCooldownTimer();
    }

    void OnEnable()
    {
        controls.Player.Move.performed += OnMove;
        controls.Player.Move.canceled += OnMove;
        controls.Player.Jump.started += OnJump;
        controls.Player.Jump.canceled += OnJump;
        controls.Player.Pulse.performed += OnPulse;
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Move.performed -= OnMove;
        controls.Player.Move.canceled -= OnMove;
        controls.Player.Jump.started -= OnJump;
        controls.Player.Jump.canceled -= OnJump;
        controls.Player.Pulse.performed -= OnPulse;
        controls.Player.Disable();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

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

    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<float>();
    }

    void OnJump(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            if (isGrounded)
            {
                jumpPressTime = Time.time;

                Vector2 velocity = rb.linearVelocity;
                velocity.y = tapJumpForce;
                rb.linearVelocity = velocity;
            }
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            if (jumpPressTime < 0f) return;

            if (Time.time - jumpPressTime >= holdThreshold)
            {
                Vector2 velocity = rb.linearVelocity;
                velocity.y = heldJumpForce;
                rb.linearVelocity = velocity;
            }

            jumpPressTime = -1f;
        }
    }

    void OnPulse(InputAction.CallbackContext context)
    {
        if (!IsPulseReady)
        {
            Debug.Log("Pulse on cooldown. " + pulseCooldownTimer + " seconds left.");
            return;
        }

        FeaturePlaceholderLog("Pulse");

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

    void HandlePulseCooldownTimer()
    {
        if (pulseCooldownTimer > 0f)
        {
            pulseCooldownTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void SetInCombat(bool value)
    {
        isInCombat = value;
    }

    void FeaturePlaceholderLog(string feature)
    {
        Debug.Log(feature + " input detected");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    // Debug functions

    [ContextMenu("Debug: Take 10 Damage")]
    void DebugTakeDamage() => TakeDamage(10);

    [ContextMenu("Debug: Heal 10")]
    void DebugHeal() => Heal(10);

    [ContextMenu("Debug: Toggle Combat State")]
    void DebugToggleCombat() => SetInCombat(!isInCombat);
}
