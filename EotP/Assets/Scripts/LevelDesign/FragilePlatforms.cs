using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Put this on the Temporary tilemap.
// Each connected group of tiles is treated as one platform: after the player touches it
// for breakDelay seconds it disappears (no collision), then reappears after respawnDelay.
[RequireComponent(typeof(Tilemap))]
public class FragilePlatforms : MonoBehaviour
{
    [SerializeField] private float breakDelay = 3f;
    [SerializeField] private float respawnDelay = 3f;
    [Tooltip("On: timer only runs while the player is touching, and resets if they step off.\n" +
             "Off: first touch starts the timer and it breaks no matter what.")]
    [SerializeField] private bool requireContinuousContact = true;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("How faded the platform gets right before it breaks, as a warning.")]
    [SerializeField, Range(0f, 1f)] private float minAlphaBeforeBreak = 0.3f;

    private class Platform
    {
        public readonly List<Vector3Int> cells = new List<Vector3Int>();
        public readonly List<TileBase> tiles = new List<TileBase>();
        public bool broken;
        public bool touched;
        public float timer;
    }

    private static readonly Vector3Int[] Directions =
        { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

    private Tilemap tilemap;
    private readonly List<Platform> platforms = new List<Platform>();
    private readonly Dictionary<Vector3Int, Platform> cellToPlatform = new Dictionary<Vector3Int, Platform>();

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        BuildPlatforms();
    }

    // Groups touching tiles into platforms with a flood fill.
    private void BuildPlatforms()
    {
        tilemap.CompressBounds();
        foreach (Vector3Int start in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(start) || cellToPlatform.ContainsKey(start)) continue;

            var platform = new Platform();
            var stack = new Stack<Vector3Int>();
            stack.Push(start);
            cellToPlatform[start] = platform;

            while (stack.Count > 0)
            {
                Vector3Int cell = stack.Pop();
                platform.cells.Add(cell);
                platform.tiles.Add(tilemap.GetTile(cell));
                tilemap.SetTileFlags(cell, TileFlags.None); // allow per-tile color changes

                foreach (Vector3Int dir in Directions)
                {
                    Vector3Int next = cell + dir;
                    if (tilemap.HasTile(next) && !cellToPlatform.ContainsKey(next))
                    {
                        cellToPlatform[next] = platform;
                        stack.Push(next);
                    }
                }
            }
            platforms.Add(platform);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Debug.Log($"Touching: {collision.collider.name}, tag: {collision.collider.tag}");
        if (!collision.collider.CompareTag(playerTag)) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            // The contact point sits on the tile's edge, so nudge it into the tile both ways.
            if (!TryMarkTouched(contact.point + contact.normal * 0.05f))
                TryMarkTouched(contact.point - contact.normal * 0.05f);
        }
    }

    private bool TryMarkTouched(Vector2 worldPoint)
    {
        Vector3Int cell = tilemap.WorldToCell(worldPoint);
        if (cellToPlatform.TryGetValue(cell, out Platform platform) && !platform.broken)
        {
            platform.touched = true;
            return true;
        }
        return false;
    }

    // FixedUpdate runs in step with the physics callbacks above, so the touched flags are reliable.
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        foreach (Platform p in platforms)
        {
            if (p.broken)
            {
                p.timer += dt;
                if (p.timer >= respawnDelay && !PlayerOverlaps(p))
                    Restore(p);
            }
            else if (p.touched || (!requireContinuousContact && p.timer > 0f))
            {
                p.timer += dt;
                SetAlpha(p, Mathf.Lerp(1f, minAlphaBeforeBreak, p.timer / breakDelay));
                if (p.timer >= breakDelay)
                    Break(p);
            }
            else if (p.timer > 0f)
            {
                // Player stepped off before it broke.
                p.timer = 0f;
                SetAlpha(p, 1f);
            }

            p.touched = false;
        }
    }

    private void Break(Platform p)
    {
        foreach (Vector3Int cell in p.cells)
            tilemap.SetTile(cell, null);
        p.broken = true;
        p.timer = 0f;
    }

    private void Restore(Platform p)
    {
        for (int i = 0; i < p.cells.Count; i++)
        {
            tilemap.SetTile(p.cells[i], p.tiles[i]);
            tilemap.SetTileFlags(p.cells[i], TileFlags.None);
        }
        SetAlpha(p, 1f);
        p.broken = false;
        p.timer = 0f;
    }

    private void SetAlpha(Platform p, float alpha)
    {
        Color color = new Color(1f, 1f, 1f, alpha); // multiplies with the tilemap's own color
        foreach (Vector3Int cell in p.cells)
            tilemap.SetColor(cell, color);
    }

    // Don't respawn a platform inside the player.
    private bool PlayerOverlaps(Platform p)
    {
        Vector2 size = tilemap.cellSize * 0.9f;
        foreach (Vector3Int cell in p.cells)
        {
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(tilemap.GetCellCenterWorld(cell), size, 0f))
            {
                if (hit.CompareTag(playerTag)) return true;
            }
        }
        return false;
    }
}