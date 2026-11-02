using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HunterEnvironmentBehaviour : MonoBehaviour
{
    public GameObject Platform;

    [Header("Arena")]
    public Vector2 ArenaHalfSize = new Vector2(15f, 7.5f); // half the inner width/height of the walls
    public float WallMargin = 1.5f;                        // keeps platforms away from the walls

    [Header("Platforms")]
    public int Columns = 3;
    public int MinPerColumn = 2;
    public int MaxPerColumn = 4;                           // inclusive
    public Vector2 WidthMultiplier = new Vector2(0.5f, 0.9f); // random width, times the prefab's width
    public float Thickness = 0.5f;
    public float JitterX = 1f;                             // random sideways offset
    public float JitterY = 0.5f;                           // random vertical offset

    private readonly List<GameObject> _platforms = new List<GameObject>();
    private Transform _walls;
    private int _groundLayer;

    void Awake()
    {
        _walls = transform.Find("Walls");
        _groundLayer = LayerMask.NameToLayer("Ground");

        if (_groundLayer == -1)
            Debug.LogWarning("HunterEnvironmentBehaviour: no layer named 'Ground' exists.");
    }

    void Start()
    {
        GenerateEnvironment();
    }

    // Clears the old platforms and builds a new random layout. Safe to call every episode.
    public void GenerateEnvironment()
    {
        ClearPlatforms();

        float usableX = ArenaHalfSize.x - WallMargin;
        float usableY = ArenaHalfSize.y - WallMargin;

        for (int c = 0; c < Columns; c++)
        {
            // Columns evenly spaced across the usable width (3 columns -> left, middle, right).
            float x = Mathf.Lerp(-usableX, usableX, (c + 0.5f) / Columns);

            int count = Random.Range(MinPerColumn, MaxPerColumn + 1);
            float spacing = (usableY * 2f) / count;

            for (int g = 0; g < count; g++)
            {
                float y = -usableY + spacing * (g + 0.5f);

                Vector3 localPos = new Vector3(
                    x + Random.Range(-JitterX, JitterX),
                    y + Random.Range(-JitterY, JitterY),
                    0f);

                GameObject platform = Instantiate(Platform, _walls);
                platform.transform.localPosition = localPos;

                Vector3 baseScale = Platform.transform.localScale;
                float width = baseScale.x * Random.Range(WidthMultiplier.x, WidthMultiplier.y);
                platform.transform.localScale = new Vector3(width, Thickness, 1f);

                // Ground layer blocks the pulse; Ground tag lets the player stand and jump on it.
                SetLayerRecursively(platform, _groundLayer);
                platform.tag = "Ground";

                _platforms.Add(platform);
            }
        }
    }

    void ClearPlatforms()
    {
        foreach (GameObject p in _platforms)
        {
            if (p != null)
                Destroy(p);
        }
        _platforms.Clear();
    }

    static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (layer < 0) return;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}