using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class HunterAgent : Agent
{
    private SpriteRenderer _renderer;
    private Rigidbody2D _rigidBody;
    private Rigidbody2D _playerBody;
    private IPulseSource _pulseSource;
    private ITrainingBot _bot;

    public int CurrentEpisode = 0;
    public float CumulativeReward = 0f;

    [ContextMenu("Test Tag")]
    void TestTag() => OnTagged();

    [Header("Movement")]
    public float _moveSpeed = 5f;            // units per second

    [Header("Debug (turn off for training)")]
    public bool DebugLogs = true;

    [Header("References")]
    public GameObject Player;                // the bot during training, the real player later
    public HunterEnvironmentBehaviour Environment; // leave empty to find it in a parent

    [Header("Rewards")]
    public float EngagementRadius = 6f;      // outside this, small per-step penalty
    public float ScanReward = 0.2f;          // exposed during a pulse and not tagged
    public float ScanGrace = 0.5f;           // seconds after the pulse ends before paying out (>= projectile flight time)
    public float UnrevealedGrace = 11f;      // seconds unrevealed before the hiding penalty starts (~2 pulse cycles)
    public float UnrevealedPenaltyWeight = 1f; // per step: -weight / MaxStep

    [Header("Observation scaling")]
    public Vector2 ArenaHalfSize = new Vector2(15f, 7.5f); // copied from Environment in Initialize
    public float MaxPlayerSpeed = 15f;
    public float MaxPulseRadius = 20f;
    public float RevealMemory = 5f;
    public float MaxCooldownDuration = 10f;  // cooldown durations are divided by this

    [Header("Spawning")]
    public LayerMask GroundMask;             // Ground (platforms + walls)
    public float SpawnClearRadius = 0.6f;
    public float MinSpawnSeparation = 6f;    // min distance from the bot at spawn
    public int MaxSpawnAttempts = 30;

    // Reveal state
    private bool _revealed;
    private float _lastRevealedTime;
    private float _unrevealedSince;

    // Scan state
    private bool _wasPulseActive;
    private bool _exposedThisPulse;
    private float _scanPayoutTime = -1f;     // -1 = nothing pending

    private int _tagImmunitySteps;

    // MaxStep is 0 in heuristic/inference runs; -2f / 0 would be -Infinity
    private float StepScale => MaxStep > 0 ? 1f / MaxStep : 0f;

    public override void Initialize()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _rigidBody = GetComponent<Rigidbody2D>();
        _playerBody = Player.GetComponent<Rigidbody2D>();
        _pulseSource = Player.GetComponentInChildren<IPulseSource>();
        _bot = Player.GetComponent<ITrainingBot>();   // null outside training, that's fine

        if (_pulseSource == null)
            Debug.LogError($"{name}: Player has no IPulseSource (add a PulseEmitter).", this);

        if (Environment == null)
            Environment = GetComponentInParent<HunterEnvironmentBehaviour>();
        if (Environment != null)
            ArenaHalfSize = Environment.ArenaHalfSize;   // one source of truth

        CurrentEpisode = 0;
        CumulativeReward = 0f;
    }

    public void ResetPlayerInEnvironment()
    {
        if (_bot != null && _playerBody != null)
        {
            _playerBody.transform.position = Environment.GetRandomPlatform().transform.position + new Vector3(0, _playerBody.transform.localScale.y,0);
        }
    }

    // reset
    public override void OnEpisodeBegin()
    {
        CurrentEpisode++;
        CumulativeReward = 0f;
        _renderer.color = Color.blue;
        _tagImmunitySteps = 2;

        if (Environment != null)
        {
            Environment.GenerateEnvironment();
            Physics2D.SyncTransforms();   // new platform transforms -> physics, before OverlapCircle
        }

        if (_bot != null)
        {
            _bot.ResetBot();
            Physics2D.SyncTransforms();
        }

        transform.localPosition = FindClearSpawn();
        _rigidBody.position = transform.position;
        _rigidBody.linearVelocity = Vector2.zero;

        _revealed = false;
        _lastRevealedTime = float.NegativeInfinity;   // "never revealed" -> time-since obs = 1
        _unrevealedSince = Time.time;
        _wasPulseActive = false;
        _exposedThisPulse = false;
        _scanPayoutTime = -1f;

        ResetPlayerInEnvironment();
    }

    // Local position (relative to the Hunter's parent = arena centre) with no Ground nearby.
    private Vector2 FindClearSpawn()
    {
        Transform space = transform.parent;
        Vector2 playerLocal = space != null
            ? (Vector2)space.InverseTransformPoint(Player.transform.position)
            : (Vector2)Player.transform.position;

        Vector2 margin = Vector2.one * (SpawnClearRadius + 0.5f);
        Vector2 min = -ArenaHalfSize + margin;
        Vector2 max = ArenaHalfSize - margin;

        Vector2 fallback = Vector2.zero;
        bool haveFallback = false;

        for (int i = 0; i < MaxSpawnAttempts; i++)
        {
            Vector2 local = new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
            Vector2 world = space != null ? (Vector2)space.TransformPoint(local) : local;

            if (Physics2D.OverlapCircle(world, SpawnClearRadius, GroundMask))
                continue;

            if (!haveFallback) { fallback = local; haveFallback = true; }
            if (Vector2.Distance(local, playerLocal) >= MinSpawnSeparation)
                return local;
        }

        if (!haveFallback)
            Debug.LogWarning($"{name}: no clear spawn found, using arena centre.", this);
        return fallback;   // clear but close to the bot, or centre as last resort
    }

    // observations (11)
    public override void CollectObservations(VectorSensor sensor)
    {
        // 1-2: Hunter position inside the arena, roughly -1..1
        Vector2 local = transform.localPosition;
        sensor.AddObservation(local.x / ArenaHalfSize.x);
        sensor.AddObservation(local.y / ArenaHalfSize.y);

        // 3-4: player position relative to the Hunter, -1..1
        Vector2 toPlayer = Player.transform.position - transform.position;
        sensor.AddObservation(Mathf.Clamp(toPlayer.x / (ArenaHalfSize.x * 2f), -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(toPlayer.y / (ArenaHalfSize.y * 2f), -1f, 1f));

        // 5-6: player velocity, -1..1
        Vector2 playerVel = _playerBody != null ? _playerBody.linearVelocity : Vector2.zero;
        sensor.AddObservation(Mathf.Clamp(playerVel.x / MaxPlayerSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(playerVel.y / MaxPlayerSpeed, -1f, 1f));

        // 7: pulse cooldown, 1 = just pulsed, 0 = ready
        sensor.AddObservation(_pulseSource.CooldownNormalized);

        // 8: revealed right now (updated every physics step in FixedUpdate)
        sensor.AddObservation(_revealed);

        // 9: time since last revealed, 0 = just now, 1 = RevealMemory seconds or more / never
        sensor.AddObservation(Mathf.Clamp01((Time.time - _lastRevealedTime) / RevealMemory));

        // 10: pulse reach
        sensor.AddObservation(Mathf.Clamp01(_pulseSource.MaxRadius / MaxPulseRadius));

        // 11: cooldown length, so the policy knows how long the safe window is
        sensor.AddObservation(Mathf.Clamp01(_pulseSource.CooldownDuration / MaxCooldownDuration));
    }

    // Branch 0 (horizontal): 0 = stay, 1 = left, 2 = right
    // Branch 1 (vertical):   0 = stay, 1 = down, 2 = up
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;

        d[0] = 0;
        if (Input.GetKey(KeyCode.J)) d[0] = 1;
        else if (Input.GetKey(KeyCode.L)) d[0] = 2;

        d[1] = 0;
        if (Input.GetKey(KeyCode.K)) d[1] = 1;
        else if (Input.GetKey(KeyCode.I)) d[1] = 2;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveAgent(actions.DiscreteActions);

        if (Vector2.Distance(transform.position, Player.transform.position) > EngagementRadius)
            AddReward(-2f * StepScale);

        CumulativeReward = GetCumulativeReward();
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        float x = act[0] == 1 ? -1f : act[0] == 2 ? 1f : 0f;
        float y = act[1] == 1 ? -1f : act[1] == 2 ? 1f : 0f;

        Vector2 dir = new Vector2(x, y).normalized;   // diagonals not faster
        _rigidBody.linearVelocity = dir * _moveSpeed;
    }

    // scan + hiding
    // Runs every physics step, so short exposures aren't missed between decisions.
    private void FixedUpdate()
    {
        if (_pulseSource == null) return;
        if (_tagImmunitySteps > 0) _tagImmunitySteps--;

        bool pulseActive = _pulseSource.IsPulseActive;
        _revealed = pulseActive && _pulseSource.CanReveal(transform.position);
        if (_revealed)
        {
            _lastRevealedTime = Time.time;
            _unrevealedSince = Time.time;
        }

        if (DebugLogs)
            LogRevealDebug(pulseActive);

        // Exposed during a pulse -> pay out once the pulse is over and the shot had time to land
        if (pulseActive && !_wasPulseActive) _exposedThisPulse = false;   // new pulse
        if (_revealed) _exposedThisPulse = true;
        if (!pulseActive && _wasPulseActive && _exposedThisPulse)
            _scanPayoutTime = Time.time + ScanGrace;
        _wasPulseActive = pulseActive;

        if (_scanPayoutTime >= 0f && Time.time >= _scanPayoutTime)
        {
            AddReward(ScanReward);   // a tag ends the episode first, so tagged scans never pay
            _scanPayoutTime = -1f;
            _exposedThisPulse = false;
            if (DebugLogs) Debug.Log($"Scan reward +{ScanReward}");
        }

        // Hiding too long
        if (Time.time - _unrevealedSince > UnrevealedGrace)
            AddReward(-UnrevealedPenaltyWeight * StepScale);

        _renderer.color = _revealed ? Color.red : Color.blue;
    }

    // Must run BEFORE _wasPulseActive is updated, or the start/end log never fires.
    private void LogRevealDebug(bool pulseActive)
    {
        if (pulseActive != _wasPulseActive)
            Debug.Log($"Emitter pulse {(pulseActive ? "started" : "ended")}");

        if (pulseActive && !_revealed && Time.frameCount % 15 == 0)
        {
            var hit = Physics2D.Linecast(Player.transform.position, transform.position, GroundMask);
            Debug.Log($"Not revealed: dist {Vector2.Distance(Player.transform.position, transform.position):F1}, " +
                      $"blocked by {(hit ? hit.collider.name : "nothing")}");
        }
    }

    // tagged
    // Called by the bot's projectile on hit.
    public void OnTagged()
    {
        if (_tagImmunitySteps > 0) return;   // stale hit from last episode, or a double hit
        AddReward(-1f);

        ResetPlayerInEnvironment();

        //EndEpisode();                        // runs OnEpisodeBegin immediately
    }
}