using UnityEngine;

// Prototype echolocation pulse: an expanding ring that fades out and destroys itself.
// Put this on a prefab with a LineRenderer, then Instantiate it where the pulse fires.
[RequireComponent(typeof(LineRenderer))]
public class EchoPulse : MonoBehaviour
{
    [SerializeField] private float speed = 12f;      // world units per second
    [SerializeField] private float maxRadius = 10f;  // pulse dies at this radius
    [SerializeField] private int segments = 64;      // circle smoothness
    [SerializeField] private float lineWidth = 0.1f;

    // Exposed for the future reveal system to read.
    public float Radius { get; private set; }
    public Vector2 Origin => transform.position;

    private LineRenderer line;
    private Color startColor;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.startWidth = line.endWidth = lineWidth;
        startColor = line.startColor;
        Draw();
    }

    private void Update()
    {
        Radius += speed * Time.deltaTime;

        if (Radius >= maxRadius)
        {
            Destroy(gameObject);
            return;
        }

        Draw();

        // Fade out as the pulse travels.
        float t = Radius / maxRadius;
        Color c = startColor;
        c.a *= 1f - t;
        line.startColor = line.endColor = c;
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