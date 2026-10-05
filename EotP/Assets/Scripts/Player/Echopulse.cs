using UnityEngine;
using UnityEngine.Rendering.Universal;

// Prototype echolocation pulse.
// Phase 1: a ring expands outward while a Light 2D grows with it, revealing the area.
// Phase 2: the ring is gone, the light holds at full size and fades out over lingerDuration.
[RequireComponent(typeof(LineRenderer))]
public class EchoPulse : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private float speed = 12f;          // world units per second
    [SerializeField] private float maxRadius = 10f;      // wave stops growing here
    [SerializeField] private int segments = 64;          // circle smoothness
    [SerializeField] private float lineWidth = 0.1f;

    [Header("Reveal")]
    [SerializeField] private Light2D pulseLight;         // child Point Light 2D
    [SerializeField] private float lightIntensity = 1f;
    [SerializeField] private float lingerDuration = 4f;  // seconds the reveal lasts after the wave finishes

    // Exposed for anything else that needs to react to the pulse later.
    public float Radius { get; private set; }
    public Vector2 Origin => transform.position;
    public bool IsExpanding => Radius < maxRadius;

    private LineRenderer line;
    private Color startColor;
    private float lingerTimer;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.startWidth = line.endWidth = lineWidth;
        startColor = line.startColor;
        Draw();

        if (pulseLight != null)
        {
            pulseLight.pointLightInnerRadius = 0f;
            pulseLight.pointLightOuterRadius = 0f;
            pulseLight.intensity = lightIntensity;
        }
    }

    private void Update()
    {
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
        {
            float angle = i * Mathf.PI * 2f / segments;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * Radius);
        }
    }
}