using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Variables")]
    public float moveSpeed = 6f;
    public float jumpForce = 6f;

    [Header("State")]
    public bool isGrounded;

    [Header("Input Bindings")]
    public KeyCode pulseKey = KeyCode.E;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
        HandleJump();
        HandlePulseInput();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");

        Vector3 velocity = rb.linearVelocity;
        velocity.x = horizontal * moveSpeed;
        rb.linearVelocity = velocity;

        if (horizontal != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(horizontal);
            transform.localScale = scale;
        }
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;
        }
    }

    void HandlePulseInput()
    {
        if (Input.GetKeyDown(pulseKey))
        {
            FeaturePlaceholderLog("Pulse");
        }
    }

    void FeaturePlaceholderLog(string feature)
    {
        Debug.Log(feature + " input detected");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
