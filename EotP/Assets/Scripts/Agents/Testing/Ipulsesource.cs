using UnityEngine;

// Anything that emits echo pulses the Hunter can be revealed by.
// PulseEmitter implements this, so the bot and (later) the real player both just use a PulseEmitter.
public interface IPulseSource
{
    bool IsPulseActive { get; }        // a pulse from this source is out (expanding or lingering)
    float CooldownNormalized { get; }  // 1 = just pulsed, 0 = ready
    float CooldownDuration { get; }    // seconds
    float MaxRadius { get; }
    bool CanReveal(Vector2 point);     // false when no pulse is out
}

// Implemented by the scripted training bot (teammate).
public interface ITrainingBot
{
    // Respawn, forget last-seen position, destroy in-flight projectiles,
    // and call PulseEmitter.ResetEmitter().
    void ResetBot();
}