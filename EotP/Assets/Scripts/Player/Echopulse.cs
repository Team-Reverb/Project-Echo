using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Prototype echolocation pulse.
// Phase 1: a ring expands outward while a Light 2D grows with it, revealing the area.
//          Each ring segment stops at the first wall/ground in its direction.
// Phase 2: the ring is gone, the light holds at full size and fades out over lingerDuration.
// Light occlusion comes from a Shadow Caster 2D on the Solid tilemap (no code needed).
[RequireComponent(typeof(LineRenderer))]
public class EchoPulse : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private float speed = 12f;          // world units per second
    [SerializeField] private float maxRadius = 10f;      // wave stops growing here
    [SerializeField] private int segments = 64;          // circle smoothness (also number of wall rays)
    [SerializeField] private float lineWidth = 0.1f;

    [Header("Reveal")]
    [SerializeField] private Light2D pulseLight;         // child Point Light 2D, Shadows enabled
    [SerializeField] private float lightIntensity = 1f;
    [SerializeField] private float lingerDuration = 4f;  // seconds the reveal lasts after the wave finishes

    [Header("Occlusion")]
    [SerializeField] private LayerMask blockingLayers;   // layer(s) of the Solid tilemap. Must not be Nothing
    [SerializeField] private float wallInset = 0.02f;    // keeps the ring just in front of the wall surface

    // Exposed for anything else that needs to react to the pulse later.
    public float Radius { get; private set; }
    public Vector2 Origin => transform.position;
    public bool IsExpanding => Radius < maxRadius;

    private LineRenderer line;
    private CircleCollider2D circle;
    private Color startColor;
    private float lingerTimer;

    private Vector2[] dirs;   // direction of each ring segment
    private float[] reach;    // distance to the first wall in that direction

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        circle = GetComponent<CircleCollider2D>();

        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.startWidth = line.endWidth = lineWidth;
        startColor = line.startColor;

        CacheReach();
        Draw();

        if (pulseLight != null)
        {
            pulseLight.pointLightInnerRadius = 0f;
            pulseLight.pointLightOuterRadius = 0f;
            pulseLight.intensity = lightIntensity;
        }
    }

    // Level geometry is static, so wall distances are measured once when the pulse spawns.
    private void CacheReach()
    {
        dirs = new Vector2[segments];
        reach = new float[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            dirs[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            RaycastHit2D hit = Physics2D.Raycast(Origin, dirs[i], maxRadius, blockingLayers);
            reach[i] = hit.collider != null ? Mathf.Max(0f, hit.distance - wallInset) : maxRadius;
        }
    }

    private void Update()
    {
        UpdateColliderShape();
        if (IsExpanding)
            UpdateWave();
        else
            UpdateLinger();
    }

    private void UpdateWave()
    {
        Radius = Mathf.Min(Radius + speed * Time.deltaTime, maxRadius);
        Draw();

        // Ring fades as it travels.
        float t = Radius / maxRadius;
        Color c = startColor;
        c.a *= 1f - t;
        line.startColor = line.endColor = c;

        // Light grows with the wave.
        if (pulseLight != null)
            pulseLight.pointLightOuterRadius = Radius;

        if (!IsExpanding)
            line.enabled = false; // wave done, only the reveal remains
    }

    private void UpdateLinger()
    {
        lingerTimer += Time.deltaTime;
        float t = lingerTimer / lingerDuration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Stays bright for most of the linger, then drops off at the end.
        if (pulseLight != null)
            pulseLight.intensity = lightIntensity * (1f - t * t);
    }

    private void Draw()
    {
        for (int i = 0; i < segments; i++)
            line.SetPosition(i, (Vector3)(dirs[i] * Mathf.Min(Radius, reach[i])));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Agent"))
            return;

        // The trigger passes through walls, so only alert agents the pulse can actually see.
        // Use the agent's centre so the ground under its feet doesn't block the line.
        if (!HasLineOfSight(collision.bounds.center))
            return;

        collision.GetComponent<BiterAgent>().Alert(Origin);
    }

    public void UpdateColliderShape()
    {
        if (circle == null)
            return;

        circle.radius = Radius;
        if (Radius > 1 && IsExpanding == false) { circle.radius = 0; } // disable if climax reached
    }

    // True if no wall/ground is between the pulse origin and `point`.
    public bool HasLineOfSight(Vector2 point)
    {
        return !Physics2D.Linecast(Origin, point, blockingLayers);
    }

    // True if the wave has reached `point` AND nothing blocks it. Use this for polling
    // checks (e.g. the ML arena's "revealed" observation).
    public bool CanReveal(Vector2 point)
    {
        return Vector2.Distance(Origin, point) <= Radius && HasLineOfSight(point);
    }
}