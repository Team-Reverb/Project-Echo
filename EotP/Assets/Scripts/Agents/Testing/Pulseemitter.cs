using System.Collections.Generic;
using UnityEngine;

// Spawns EchoPulses and tracks only its OWN pulses, so arenas running in parallel
// don't reveal each other's Hunters (EchoPulse.Active is shared by the whole scene).
public class PulseEmitter : MonoBehaviour, IPulseSource
{
    [SerializeField] private EchoPulse pulsePrefab;
    [SerializeField] private float cooldownDuration = 5f;
    [Tooltip("Overrides the prefab's linger time. 1.5 for training, negative keeps the prefab value.")]
    [SerializeField] private float lingerOverride = 1.5f;
    [Tooltip("Testing only: press this key to fire. Set to None once the bot drives the emitter.")]
    [SerializeField] private KeyCode testKey = KeyCode.P;

    private readonly List<EchoPulse> _pulses = new List<EchoPulse>();
    private float _cooldownTimer;

    public float CooldownDuration => cooldownDuration;
    public float CooldownNormalized => cooldownDuration > 0f ? Mathf.Clamp01(_cooldownTimer / cooldownDuration) : 0f;
    public bool CanPulse => _cooldownTimer <= 0f;
    public float MaxRadius => pulsePrefab != null ? pulsePrefab.MaxRadius : 0f;

    public bool IsPulseActive
    {
        get { Prune(); return _pulses.Count > 0; }
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (testKey != KeyCode.None && Input.GetKeyDown(testKey))
            TryPulse();
    }

    // Returns false if still on cooldown.
    public bool TryPulse()
    {
        if (!CanPulse || pulsePrefab == null)
            return false;

        // No parent: the ring must stay where it was fired, not follow the emitter.
        EchoPulse pulse = Instantiate(pulsePrefab, transform.position, Quaternion.identity);
        if (lingerOverride >= 0f)
            pulse.LingerDuration = lingerOverride;

        _pulses.Add(pulse);
        _cooldownTimer = cooldownDuration;
        return true;
    }

    public bool CanReveal(Vector2 point)
    {
        Prune();
        foreach (EchoPulse pulse in _pulses)
        {
            if (pulse.CanReveal(point))
                return true;
        }
        return false;
    }

    // Call from the bot's ResetBot().
    public void ResetEmitter()
    {
        foreach (EchoPulse pulse in _pulses)
        {
            if (pulse != null)
            {
                pulse.gameObject.SetActive(false);   // gone immediately, Destroy alone waits a frame
                Destroy(pulse.gameObject);
            }
        }
        _pulses.Clear();
        _cooldownTimer = 0f;
    }

    private void Prune()
    {
        _pulses.RemoveAll(p => p == null || !p.gameObject.activeInHierarchy);
    }
}