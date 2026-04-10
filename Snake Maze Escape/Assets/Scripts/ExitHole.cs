using UnityEngine;
using System.Collections.Generic;

// ExitHole represents the destination cell for a specific snake.
// It renders as a coloured ring on the grid at a randomly chosen
// interior cell. Position is assigned automatically by GameManager.
// Only the snake whose colour matches can enter it —
// all other snakes treat it as passable (exit holes don't block other snakes
// at the grid data level — blocking is handled in SnakeRenderer).

public class ExitHole : MonoBehaviour
{
    [Header("Exit Hole Settings")]
    // Colour set by GameManager to match a specific snake
    public Color exitColour = new Color(0.91f, 0.36f, 0.29f);

    // Grid position — assigned automatically, not set in Inspector
    public Vector2Int gridPosition { get; private set; }

    // Cell size read from GridManager
    private float cellSize;

    // SpriteRenderer for the ring visual
    private SpriteRenderer sr;

    // Unity calls Start() once — Initialise() is called externally instead
    void Start() { }

    // Called by GameManager to place and render this exit hole
    // colour: must match the snake this exit belongs to
    // gm: reference to GridManager for valid cell selection
    // excludedPositions: cells already taken by other exits or snakes
    public void Initialise(Color colour, GridManager gm,
        List<Vector2Int> excludedPositions = null)
    {
        exitColour = colour;
        cellSize = gm.cellSize;

        // Pick a random valid interior cell not in the excluded list
        gridPosition = PickRandomCell(gm, excludedPositions ?? new List<Vector2Int>());

        // Position in world space — Z -0.5 sits between grid (0) and snakes (-1)
        transform.position = new Vector3(
            gridPosition.x * cellSize,
            gridPosition.y * cellSize,
            -0.5f
        );

        // Build and apply ring visual
        // sortingOrder 2 keeps exit holes on top of snakes (0) and grid (0)
        // so the ring is always visible even when a snake occupies the cell
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = CreateCircleSprite();
        sr.color        = exitColour;
        sr.sortingOrder = 3;

        // 70% of cell size so ring sits visibly inside the cell
        transform.localScale = new Vector3(cellSize * 0.7f, cellSize * 0.7f, 1f);

        Debug.Log($"Exit hole placed at {gridPosition}");
    }

    // Picks a random empty interior cell not in the excluded list
    Vector2Int PickRandomCell(GridManager gm, List<Vector2Int> excluded)
    {
        Vector2Int candidate;
        int attempts = 0;

        do
        {
            int x = Random.Range(1, gm.width - 1);
            int y = Random.Range(1, gm.height - 1);
            candidate = new Vector2Int(x, y);
            attempts++;

            if (attempts > 200)
            {
                Debug.LogWarning("ExitHole: fallback position used");
                candidate = new Vector2Int(gm.width - 2, gm.height - 2);
                break;
            }
        }
        while (excluded.Contains(candidate) ||
               gm.GetCell(candidate.x, candidate.y) == GridManager.CellType.Wall);

        return candidate;
    }

    // Generates a circle ring texture procedurally
    // Ring is white — colour applied via SpriteRenderer.color
    Sprite CreateCircleSprite()
    {
        int size = 64;
        // RGBA32 required — without it Unity 6 may drop the alpha channel,
        // making Color.clear pixels render as opaque black
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 centre = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int px = 0; px < size; px++)
        {
            for (int py = 0; py < size; py++)
            {
                float dist = Vector2.Distance(new Vector2(px, py), centre);
                float outerRing = radius;
                float innerVoid = radius * 0.55f;

                // Pixel is part of ring if between inner and outer radius
                if (dist <= outerRing && dist >= innerVoid)
                    tex.SetPixel(px, py, Color.white);
                else
                    tex.SetPixel(px, py, Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size);
    }

    // Called by GameManager when the generator has pre-computed the position.
    // Renders identically to Initialise() but skips random cell selection.
    public void InitialiseAt(Color colour, Vector2Int position, GridManager gm)
    {
        exitColour   = colour;
        cellSize     = gm.cellSize;
        gridPosition = position;

        transform.position = new Vector3(
            gridPosition.x * cellSize,
            gridPosition.y * cellSize,
            -0.5f);

        sr              = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = CreateCircleSprite();
        sr.color        = exitColour;
        sr.sortingOrder = 3;

        transform.localScale = new Vector3(cellSize * 0.7f, cellSize * 0.7f, 1f);
    }

    // Called by SnakeRenderer to verify colour match before allowing escape
    public bool MatchesSnake(Color snakeColour)
    {
        return Mathf.Approximately(exitColour.r, snakeColour.r) &&
               Mathf.Approximately(exitColour.g, snakeColour.g) &&
               Mathf.Approximately(exitColour.b, snakeColour.b);
    }
}