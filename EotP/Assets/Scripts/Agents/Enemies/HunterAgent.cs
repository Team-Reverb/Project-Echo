using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class HunterAgent : Agent
{
    private SpriteRenderer _renderer;
    private Rigidbody2D _rigidBody;
    private Rigidbody2D _playerBody;
    private PlayerController _playerController;
    private EchoPulse _pulsePrefab;

    public int CurrentEpisode = 0;
    public float CumulativeReward = 0f;

    [Header("Movement")]
    public float _moveSpeed = 5f;            // units per second

    [Header("References")]
    public GameObject Player;

    [Header("Rewards")]
    public float EngagementRadius = 6f;      // outside this, small per-step penalty

    [Header("Observation scaling")]
    public Vector2 ArenaHalfSize = new Vector2(30f, 15f); // half the arena's width/height
    public float MaxPlayerSpeed = 15f;       // velocities are divided by this
    public float MaxPulseRadius = 20f;       // largest radius the pulse can be randomized/upgraded to
    public float RevealMemory = 5f;          // seconds until "time since revealed" maxes out at 1

    private bool _revealed;
    private float _lastRevealedTime;

    public override void Initialize()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _rigidBody = GetComponent<Rigidbody2D>();
        _playerBody = Player.GetComponent<Rigidbody2D>();
        _playerController = Player.GetComponent<PlayerController>();
        _pulsePrefab = _playerController.echoPulsePrefab.GetComponent<EchoPulse>();

        CurrentEpisode = 0;
        CumulativeReward = 0f;
    }

    public override void OnEpisodeBegin()
    {
        CurrentEpisode++;
        CumulativeReward = 0f;
        _renderer.color = Color.blue;
        _rigidBody.linearVelocity = Vector2.zero;
        _revealed = false;
        _lastRevealedTime = float.NegativeInfinity;   // "never revealed" -> time-since obs = 1
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1-2: Hunter position inside the arena, roughly -1..1
        Vector2 local = transform.localPosition;
        sensor.AddObservation(local.x / ArenaHalfSize.x);
        sensor.AddObservation(local.y / ArenaHalfSize.y);

        // 3-4: player position relative to the Hunter, -1..1 (max gap is the full arena size)
        Vector2 toPlayer = Player.transform.position - transform.position;
        sensor.AddObservation(Mathf.Clamp(toPlayer.x / (ArenaHalfSize.x * 2f), -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(toPlayer.y / (ArenaHalfSize.y * 2f), -1f, 1f));

        // 5-6: player velocity, -1..1
        Vector2 playerVel = _playerBody != null ? _playerBody.linearVelocity : Vector2.zero;
        sensor.AddObservation(Mathf.Clamp(playerVel.x / MaxPlayerSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(playerVel.y / MaxPlayerSpeed, -1f, 1f));

        // 7: pulse cooldown, 0..1 (check which end means "ready" in PlayerController)
        sensor.AddObservation(Mathf.Clamp01(_playerController.PulseCooldownNormalized));

        // 8: revealed right now (an active pulse reaches us with no wall in between)
        _revealed = IsRevealed();
        if (_revealed)
            _lastRevealedTime = Time.time;
        sensor.AddObservation(_revealed);

        // 9: time since last revealed, 0 = just now, 1 = RevealMemory seconds or more / never
        sensor.AddObservation(Mathf.Clamp01((Time.time - _lastRevealedTime) / RevealMemory));

        // 10: pulse reach, so the policy adapts to radius changes/upgrades
        sensor.AddObservation(Mathf.Clamp01(_pulsePrefab.MaxRadius / MaxPulseRadius));
    }

    // Branch 0 (horizontal): 0 = stay, 1 = left, 2 = right
    // Branch 1 (vertical):   0 = stay, 1 = down, 2 = up
    // IJKL so it doesn't fight the player's arrow keys while testing.
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
        {
            AddReward(-2f / MaxStep);
        }

        // Scan-survival and tag rewards come next.

        CumulativeReward = GetCumulativeReward();
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        float x = act[0] == 1 ? -1f : act[0] == 2 ? 1f : 0f;
        float y = act[1] == 1 ? -1f : act[1] == 2 ? 1f : 0f;

        Vector2 dir = new Vector2(x, y).normalized;   // diagonals not faster
        _rigidBody.linearVelocity = dir * _moveSpeed;
    }

    bool IsRevealed()
    {
        foreach (EchoPulse pulse in EchoPulse.Active)
        {
            if (pulse.CanReveal(transform.position))
                return true;
        }
        return false;
    }
}