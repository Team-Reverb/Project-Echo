using System.Collections.Generic;
using UnityEngine;

public class PlayerBotController : MonoBehaviour, ITrainingBot
{
    // Inspector Variables

    [Header("References")]
    [SerializeField] private TagProjectile projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint; // optional, defaults to bot's position

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float minLocalX = -10f;
    [SerializeField] private float maxLocalX = 10f;
    [SerializeField] private float arrivalDistance = 0.25f;
    [SerializeField] private float minPauseTime = 0.5f;
    [SerializeField] private float maxPauseTime = 1.5f;
    [SerializeField] private float stuckTime = 0.5f;

    [Header("Pulse")]
    [SerializeField] private float maxRandomPulseDelay = 4.5f;

    [Header("Spotting")]
    [SerializeField] private string agentTag = "Agent";


    [Header("Shooting")]
    [SerializeField] private float minShotDelay = 0.25f;
    [SerializeField] private float maxShotDelay = 0.75f;
    [SerializeField] private float projectileSpeed = 12f;

    // Private Variables

    private float destinationX;
    private float pauseTimer;
    private float stuckTimer;
    private float pulseTimer;

    private Vector2 lastSeenPosition;
    private float lastSeenTime = -1f;
    private bool hasSeenAgent;
    private bool shotQueuedThisPulse;
    private bool shotPending;
    private float shotTimer;

    private bool isGrounded;
    private Vector3 startingLocalPosition;
    private Collider2D agentCollider;

    private readonly List<TagProjectile> activeProjectiles = new List<TagProjectile>();

    private Rigidbody2D rb;
    private Collider2D[] ownColliders;
    private PulseEmitter pulseEmitter;
    private IPulseSource pulseSource;

    // Read-only Values for Other Systems

    public bool IsGrounded => isGrounded;
    public bool IsInCombat => true;
    public bool HasSeenAgent => hasSeenAgent;
    public Vector2 LastSeenPosition => lastSeenPosition;
    public float LastSeenTime => lastSeenTime;


    // Unity

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ownColliders = GetComponentsInChildren<Collider2D>();

        pulseEmitter = GetComponentInChildren<PulseEmitter>();
        pulseSource = pulseEmitter;

        if (pulseEmitter == null) Debug.LogError($"{name}: PlayerBot needs a PulseEmitter on itself or a child.", this);

        startingLocalPosition = transform.localPosition;

        ResetBehaviour();
    }

    void Update()
    {
        UpdatePulse();
        UpdateDetection();
        UpdateShooting();
    }

    void FixedUpdate()
    {
        UpdateMovement();
    }

    // Movement

    private void UpdateMovement()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            StopHorizontal();
            return;
        }

        float difference = GetWorldXFromLocal(destinationX) - transform.position.x;

        if (Mathf.Abs(difference) <= arrivalDistance)
        {
            StopHorizontal();
            BeginPause(0f);
            return;
        }

        float direction = Mathf.Sign(difference);

        if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckTime)
            {
                StopHorizontal();
                BeginPause(direction);
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        FaceDirection(direction);
    }


    private void StopHorizontal()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void BeginPause(float blockedDirection)
    {
        pauseTimer = Random.Range(minPauseTime, maxPauseTime);
        stuckTimer = 0f;
        ChooseNewDestination(blockedDirection);
    }

    private void ChooseNewDestination(float blockedDirection)
    {
        float min = minLocalX;
        float max = maxLocalX;

        if (blockedDirection != 0f)
        {
            float currentLocalX = GetLocalX();
            float margin = arrivalDistance * 2f;

            if (blockedDirection > 0f) max = Mathf.Max(min, currentLocalX - margin);
            else min = Mathf.Min(max, currentLocalX + margin);
        }

        destinationX = Random.Range(min, max);
    }

    private void FaceDirection(float direction)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
        transform.localScale = scale;
    }

    private float GetLocalX()
    {
        return transform.parent != null
            ? transform.parent.InverseTransformPoint(transform.position).x
            : transform.position.x;
    }

    private float GetWorldXFromLocal(float localX)
    {
        if (transform.parent == null) return localX;

        Vector3 localPosition = new Vector3(localX, transform.localPosition.y, transform.localPosition.z);
        return transform.parent.TransformPoint(localPosition).x;
    }

    // Pulse

    private void UpdatePulse()
    {
        if (pulseEmitter == null) return;

        pulseTimer -= Time.deltaTime;
        if (pulseTimer > 0f) return;

        if (pulseEmitter.TryPulse())
        {
            pulseTimer = pulseSource.CooldownDuration + Random.Range(0f, maxRandomPulseDelay);
        }
        else
        {
            pulseTimer = 0.1f;
        }
    }

    // Spotting

    private void UpdateDetection()
    {
        if (pulseSource == null) return;

        if (!pulseSource.IsPulseActive)
        {
            shotQueuedThisPulse = false;
            return;
        }

        Collider2D agent = FindAgent();
        if (agent == null) return;

        Vector2 agentPosition = agent.bounds.center;
        if (!pulseSource.CanReveal(agentPosition)) return;
        lastSeenPosition = agentPosition;
        lastSeenTime = Time.time;
        hasSeenAgent = true;

        if (!shotQueuedThisPulse)
        {
            shotPending = true;
            shotTimer = Random.Range(minShotDelay, maxShotDelay);
            shotQueuedThisPulse = true;
        }
    }
    private Collider2D FindAgent()
    {
        if (agentCollider != null && agentCollider.gameObject.activeInHierarchy) return agentCollider;

        agentCollider = null;

        if (transform.parent != null)
        {
            foreach (Collider2D c in transform.parent.GetComponentsInChildren<Collider2D>())
            {
                if (c.CompareTag(agentTag))
                {
                    agentCollider = c;
                    break;
                }
            }
        }
        else
        {
            GameObject go = GameObject.FindWithTag(agentTag);
            if (go != null) agentCollider = go.GetComponent<Collider2D>();
        }

        return agentCollider;
    }


    // Shooting

    private void UpdateShooting()
    {
        if (!shotPending) return;

        shotTimer -= Time.deltaTime;
        if (shotTimer > 0f) return;

        shotPending = false;
        FireProjectile();
    }


    private void FireProjectile()
    {
        if (!hasSeenAgent || projectilePrefab == null) return;

        Vector2 spawnPosition = projectileSpawnPoint != null
            ? (Vector2)projectileSpawnPoint.position
            : (Vector2)transform.position;

        Vector2 direction = lastSeenPosition - spawnPosition;
        if (direction.sqrMagnitude < 0.001f) return;
        direction.Normalize();

        if (Mathf.Abs(direction.x) > 0.01f) FaceDirection(direction.x);

        TagProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        projectile.Initialize(direction, projectileSpeed, ownColliders, this);
    }

    public void RegisterProjectile(TagProjectile projectile)
    {
        if (projectile != null && !activeProjectiles.Contains(projectile)) activeProjectiles.Add(projectile);
    }

    public void UnregisterProjectile(TagProjectile projectile)
    {
        activeProjectiles.Remove(projectile);
    }

    private void ClearProjectiles()
    {
        TagProjectile[] projectiles = activeProjectiles.ToArray();
        activeProjectiles.Clear();

        foreach (TagProjectile projectile in projectiles)
        {
            if (projectile != null)
            {
                projectile.gameObject.SetActive(false);
                Destroy(projectile.gameObject);
            }
        }
    }

    // Reset

    public void ResetBot()
    {
        ClearProjectiles();

        if (pulseEmitter != null) pulseEmitter.ResetEmitter();

        Vector3 resetPosition = transform.parent != null
            ? transform.parent.TransformPoint(startingLocalPosition)
            : startingLocalPosition;

        rb.position = resetPosition;
        transform.position = resetPosition;
        rb.linearVelocity = Vector2.zero;
        isGrounded = false;

        ResetBehaviour();
    }

    public void SetSpawnLocalPosition(Vector3 localPosition)
    {
        startingLocalPosition = localPosition;
    }

    private void ResetBehaviour()
    {
        ChooseNewDestination(0f);

        pauseTimer = Random.Range(minPauseTime, maxPauseTime);
        stuckTimer = 0f;
        pulseTimer = Random.Range(0f, maxRandomPulseDelay);

        lastSeenPosition = Vector2.zero;
        lastSeenTime = -1f;
        hasSeenAgent = false;
        shotQueuedThisPulse = false;
        shotPending = false;
        shotTimer = 0f;
        agentCollider = null;
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
            isGrounded = false;
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

    void OnDrawGizmosSelected()
    {
        float y = transform.position.y;
        Vector3 a = new Vector3(minLocalX, transform.localPosition.y, 0f);
        Vector3 b = new Vector3(maxLocalX, transform.localPosition.y, 0f);

        if (transform.parent != null)
        {
            a = transform.parent.TransformPoint(a);
            b = transform.parent.TransformPoint(b);
        }
        else
        {
            a.y = b.y = y;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, b);

        if (Application.isPlaying && hasSeenAgent)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastSeenPosition, 0.3f);
        }
    }

}
