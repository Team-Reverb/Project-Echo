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

    [Header("Jumping Behaviour Toggle")]
    [SerializeField] private bool jumpingEnabled = false;

    [Header("Jump Physics")]
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private float maxJumpTime = 0.4f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float groundedGraceTime = 0.08f;

    [Header("Platform Scan")]
    [SerializeField] private float maxJumpHeight = 3f;
    [SerializeField] private float maxJumpReach = 3f;
    [SerializeField] private float minStepHeight = 0.5f;
    [SerializeField] private float scanInterval = 0.5f;
    [SerializeField] private float landingInset = 0.3f;

    [Header("Jump Decision Making")]
    [Range(0f, 1f)]
    [SerializeField] private float jumpChance = 0.3f;
    [SerializeField] private float jumpCooldown = 3f;

    [Header("Jump Movement")]
    [SerializeField] private float takeoffDistance = 1f;
    [SerializeField] private float approachTimeout = 4f;
    [SerializeField] private float maxAirTime = 2f;

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

    private enum JumpState { None, Approaching, Airborne }

    private struct JumpTarget
    {
        public Collider2D platform;
        public Bounds bounds;
        public float topY;
        public float nearEdgeX;
        public float landingX;
        public float direction;
        public float gap;
        public float height;
    }

    private const float LandingTolerance = 0.2f;

    private JumpState jumpState;
    private JumpTarget currentTarget;
    private bool isJumping;
    private float jumpTimer;
    private float jumpStateTimer;
    private float jumpCooldownTimer;
    private float scanTimer;
    private float lastGroundContactTime = -1f;
    private Collider2D lastRolledPlatform;
    private Collider2D col;

    private readonly List<JumpTarget> validTargets = new List<JumpTarget>();
    private readonly List<Bounds> rejectedPlatforms = new List<Bounds>();

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

        col = GetComponent<Collider2D>();
        if (jumpingEnabled && col == null) Debug.LogError($"{name}: Jumping needs a Collider2D on the root object.", this);

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
        UpdateJumpDecision();
    }

    void FixedUpdate()
    {
        if (jumpingEnabled) UpdateGroundCheck();
        UpdateMovement();
    }

    // Movement

    private void UpdateMovement()
    {
        if (jumpingEnabled && jumpState != JumpState.None)
        {
            UpdateJumpMovement();
            return;
        }

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

    // Platform Scan

    void ScanForPlatforms()
    {
        validTargets.Clear();
        rejectedPlatforms.Clear();

        GetScanArea(out Vector2 center, out Vector2 size, out float facing);

        Bounds body = col.bounds;
        float feetY = body.min.y;
        float frontEdge = facing > 0f ? body.max.x : body.min.x;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Ground")) continue;

            Bounds b = hit.bounds;
            float topY = b.max.y;
            float height = topY - feetY;
            float nearEdgeX = facing > 0f ? b.min.x : b.max.x;
            float gap = Mathf.Max(0f, (nearEdgeX - frontEdge) * facing);
            bool alreadyPastEdge = (nearEdgeX - transform.position.x) * facing <= 0f; // Bot is under or on this platform

            float landingX = nearEdgeX + facing * (body.extents.x + landingInset);
            landingX = Mathf.Clamp(landingX, b.min.x, b.max.x);
            float landingLocalX = WorldToLocalX(landingX);

            bool valid = !alreadyPastEdge && height >= minStepHeight && height <= maxJumpHeight && gap <= maxJumpReach && landingLocalX >= minLocalX && landingLocalX <= maxLocalX;

            if (!valid)
            {
                rejectedPlatforms.Add(b);
                continue;
            }

            JumpTarget target = new JumpTarget();
            target.platform = hit;
            target.bounds = b;
            target.topY = topY;
            target.nearEdgeX = nearEdgeX;
            target.landingX = landingX;
            target.direction = facing;
            target.gap = gap;
            target.height = height;
            validTargets.Add(target);
        }
    }

    void GetScanArea(out Vector2 center, out Vector2 size, out float facing)
    {
        Collider2D body = col != null ? col : GetComponent<Collider2D>();
        Bounds bounds = body.bounds;

        facing = Mathf.Sign(transform.localScale.x);
        float frontEdge = facing > 0f ? bounds.max.x : bounds.min.x;
        float bandHeight = Mathf.Max(0.01f, maxJumpHeight - minStepHeight);

        center = new Vector2(frontEdge + facing * maxJumpReach / 2f, bounds.min.y + minStepHeight + bandHeight / 2f);
        size = new Vector2(maxJumpReach, bandHeight);
    }

    float WorldToLocalX(float worldX)
    {
        if (transform.parent == null) return worldX;

        return transform.parent.InverseTransformPoint(new Vector3(worldX, transform.position.y, 0f)).x;
    }

    // Jump Decision

    void UpdateJumpDecision()
    {
        if (!jumpingEnabled) return;

        if (jumpCooldownTimer > 0f) jumpCooldownTimer -= Time.deltaTime;

        if (jumpState != JumpState.None || !isGrounded) return;

        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = scanInterval;

        ScanForPlatforms();

        if (validTargets.Count == 0)
        {
            lastRolledPlatform = null;
            return;
        }

        JumpTarget best = validTargets[0];
        foreach (JumpTarget candidate in validTargets)
        {
            if (candidate.gap < best.gap) best = candidate;
        }

        if (jumpCooldownTimer > 0f) return;
        if (best.platform == lastRolledPlatform) return;
        lastRolledPlatform = best.platform;

        if (Random.value > jumpChance) return;

        BeginJump(best);
    }

    void BeginJump(JumpTarget target)
    {
        currentTarget = target;
        jumpState = JumpState.Approaching;
        jumpStateTimer = 0f;
        pauseTimer = 0f;
        stuckTimer = 0f;
    }

    // Jump Movement

    void UpdateJumpMovement()
    {
        jumpStateTimer += Time.fixedDeltaTime;

        if (currentTarget.platform == null || !currentTarget.platform.gameObject.activeInHierarchy)
        {
            FinishJump(false);
            return;
        }

        if (jumpState == JumpState.Approaching) UpdateApproach();
        else UpdateAirborne();
    }

    void UpdateApproach()
    {
        if (!isGrounded || jumpStateTimer > approachTimeout)
        {
            FinishJump(false);
            return;
        }

        float direction = currentTarget.direction;
        float remaining = (currentTarget.nearEdgeX - transform.position.x) * direction;

        if (remaining <= takeoffDistance)
        {
            StartJump();
            return;
        }

        if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckTime)
            {
                FinishJump(false);
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

    void StartJump()
    {
        jumpState = JumpState.Airborne;
        jumpStateTimer = 0f;
        isJumping = true;
        jumpTimer = 0f;
        isGrounded = false;
        lastGroundContactTime = -1f;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
    }

    void UpdateAirborne()
    {
        if (jumpStateTimer > maxAirTime)
        {
            FinishJump(false);
            return;
        }

        if (isJumping)
        {
            jumpTimer += Time.fixedDeltaTime;

            if (jumpTimer >= maxJumpTime || rb.linearVelocity.y <= 0.1f)
            {
                isJumping = false;
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
            }
        }

        float velocityX;
        bool aboveTop = col.bounds.min.y >= currentTarget.topY - 0.05f;

        if (aboveTop)
        {
            float dx = currentTarget.landingX - transform.position.x;
            velocityX = Mathf.Abs(dx) > arrivalDistance ? Mathf.Sign(dx) * moveSpeed : 0f;
        }
        else
        {
            float stopShort = col.bounds.extents.x + 0.1f;
            float remaining = (currentTarget.nearEdgeX - transform.position.x) * currentTarget.direction;
            velocityX = remaining > stopShort ? currentTarget.direction * moveSpeed : 0f;
        }

        rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);

        if (!isJumping && isGrounded)
        {
            bool onPlatform = col.bounds.min.y >= currentTarget.topY - LandingTolerance;
            FinishJump(onPlatform);
        }
    }

    void FinishJump(bool success)
    {
        jumpState = JumpState.None;
        isJumping = false;
        stuckTimer = 0f;
        jumpCooldownTimer = jumpCooldown;
        currentTarget = default;

        BeginPause(0f);
    }

    void ResetJump()
    {
        jumpState = JumpState.None;
        currentTarget = default;
        isJumping = false;
        jumpTimer = 0f;
        jumpStateTimer = 0f;
        jumpCooldownTimer = 0f;
        scanTimer = 0f;
        lastGroundContactTime = -1f;
        lastRolledPlatform = null;
        validTargets.Clear();
        rejectedPlatforms.Clear();
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

        ResetJump();
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
        if (jumpingEnabled) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    void CheckForGround(Collision2D collision)
    {
        if (jumpingEnabled) return;

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

        if (jumpingEnabled) DrawJumpGizmos();
    }

    void DrawJumpGizmos()
    {
        // Ground check box
        GetGroundCheckArea(out Vector2 groundCenter, out Vector2 groundSize);
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(groundCenter, groundSize);

        // Scan area
        GetScanArea(out Vector2 scanCenter, out Vector2 scanSize, out float facing);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(scanCenter, scanSize);

        if (!Application.isPlaying) return;

        // Rejected platform tops (red)
        Gizmos.color = Color.red;
        foreach (Bounds b in rejectedPlatforms)
        {
            Gizmos.DrawLine(new Vector3(b.min.x, b.max.y, 0f), new Vector3(b.max.x, b.max.y, 0f));
        }

        // Valid platform tops and landing points (green)
        Gizmos.color = Color.green;
        foreach (JumpTarget t in validTargets)
        {
            Gizmos.DrawLine(new Vector3(t.bounds.min.x, t.topY, 0f), new Vector3(t.bounds.max.x, t.topY, 0f));
            Gizmos.DrawWireSphere(new Vector3(t.landingX, t.topY, 0f), 0.15f);
        }

        // Current jump target (magenta line from the bot)
        if (jumpState != JumpState.None)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, new Vector3(currentTarget.landingX, currentTarget.topY, 0f));
        }
    }
}
