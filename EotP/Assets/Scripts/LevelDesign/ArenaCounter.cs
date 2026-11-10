using UnityEngine;

// Locks the player in until every enemy in the list is dead, then opens the gates.
// Setup: put this on an empty "Arena" object with a BoxCollider2D set to Is Trigger.
// Keep the trigger a few tiles INSIDE the gates so the player is fully in before they close.
// Gates: objects with a sprite and solid BoxCollider2D placed in the arena openings.
// Enemies: place them in the arena; this script hides them until the fight starts.
[RequireComponent(typeof(BoxCollider2D))]
public class ArenaEncounter : MonoBehaviour
{
    [Tooltip("Gates that seal the arena while the fight is on.")]
    public GameObject[] gates;

    [Tooltip("Enemies for this fight. Counted as dead once destroyed or disabled.")]
    public GameObject[] enemies;

    enum State { Waiting, Fighting, Cleared }
    State state = State.Waiting;

    void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void Start()
    {
        SetGates(false);
        foreach (var e in enemies)
            if (e) e.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (state != State.Waiting || !other.CompareTag("Player")) return;

        state = State.Fighting;
        SetGates(true);
        foreach (var e in enemies)
            if (e) e.SetActive(true);
    }

    void Update()
    {
        if (state != State.Fighting) return;

        foreach (var e in enemies)
            if (e && e.activeInHierarchy) return; // someone's still alive

        state = State.Cleared;
        SetGates(false);
    }

    void SetGates(bool closed)
    {
        foreach (var g in gates)
            if (g) g.SetActive(closed);
    }
}