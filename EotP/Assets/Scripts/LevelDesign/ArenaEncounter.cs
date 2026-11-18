using System;
using System.Collections.Generic;
using UnityEngine;

// Souls-style arena: the player enters through an ArenaEntrance and the exits stay sealed
// until every enemy is dead. Dying resets the fight so it has to be entered again.
public class ArenaEncounter : MonoBehaviour
{
    [Serializable]
    public class EnemySpawn
    {
        public GameObject prefab;
        public Transform point;
    }

    public enum State { Waiting, Fighting, Cleared }

    [Tooltip("Where the player is placed when they enter the arena.")]
    [SerializeField] private Transform entryPoint;
    [Tooltip("Gates that block the way forward until the arena is cleared.")]
    [SerializeField] private GameObject[] gates;
    [Tooltip("Enemies spawned each time the fight starts. Counted as dead once destroyed or disabled.")]
    [SerializeField] private EnemySpawn[] enemySpawns;

    public event Action Cleared;
    public State CurrentState { get; private set; } = State.Waiting;
    public bool CanBegin => CurrentState == State.Waiting;

    private readonly List<GameObject> spawned = new List<GameObject>();

    // Start (not Awake) so CheckpointManager.Instance is already set
    void Start()
    {
        SetGates(true);
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.PlayerRespawned += ResetArena;
    }

    void OnDestroy()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.PlayerRespawned -= ResetArena;
    }

    // Called by ArenaEntrance
    public void Begin(PlayerController player)
    {
        if (!CanBegin) return;

        MovePlayer(player);
        CurrentState = State.Fighting;
        SpawnEnemies();
    }

    void Update()
    {
        if (CurrentState != State.Fighting) return;

        foreach (GameObject enemy in spawned)
            if (enemy != null && enemy.activeInHierarchy) return; // someone's still alive

        Clear();
    }

    void Clear()
    {
        CurrentState = State.Cleared;
        SetGates(false);
        Cleared?.Invoke();
    }

    // Player died during the fight: remove leftover enemies so it can be entered again
    void ResetArena()
    {
        if (CurrentState != State.Fighting) return;

        DespawnEnemies();
        CurrentState = State.Waiting;
    }

    void SpawnEnemies()
    {
        foreach (EnemySpawn spawn in enemySpawns)
        {
            if (spawn.prefab == null || spawn.point == null) continue;
            spawned.Add(Instantiate(spawn.prefab, spawn.point.position, Quaternion.identity));
        }
    }

    void DespawnEnemies()
    {
        foreach (GameObject enemy in spawned)
            if (enemy != null) Destroy(enemy);
        spawned.Clear();
    }

    void MovePlayer(PlayerController player)
    {
        if (entryPoint == null) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = entryPoint.position;
        }
        player.transform.position = entryPoint.position;
    }

    void SetGates(bool closed)
    {
        foreach (GameObject gate in gates)
            if (gate != null) gate.SetActive(closed);
    }

    [ContextMenu("Debug: Clear Arena")]
    void DebugClear()
    {
        if (CurrentState != State.Fighting) return;
        DespawnEnemies();
        Clear();
    }
}