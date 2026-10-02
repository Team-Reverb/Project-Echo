using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Variables")]
    public float moveSpeed = 6f;
    public float jumpForce = 6f;

    [Header("State")]
    public bool isGrounded;

    private Rigidbody2D rb;
    private PlayerControls controls;
    private float moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        controls.Player.Move.performed += OnMove;
        controls.Player.Move.canceled += OnMove;
        controls.Player.Jump.performed += OnJump;
        controls.Player.Pulse.performed += OnPulse;
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Move.performed -= OnMove;
        controls.Player.Move.canceled -= OnMove;
        controls.Player.Jump.performed -= OnJump;
        controls.Player.Pulse.performed -= OnPulse;
        controls.Player.Disable();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    // Update is called once per frame
    void Update()
    {

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
        if (isGrounded)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;
        }
    }

    void OnPulse(InputAction.CallbackContext context)
    {
        FeaturePlaceholderLog("Pulse");
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
}
