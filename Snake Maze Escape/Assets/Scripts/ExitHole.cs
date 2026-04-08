using UnityEngine;

// ExitHole represents the destination cell for a specific snake.
// It renders as a coloured ring on the grid at a randomly chosen
// interior cell position. It does not require manual configuration
// in the Inspector — position is assigned automatically by
// GameManager when the level is initialised.
// Only the snake whose colour matches the exit hole can enter it.

public class ExitHole : MonoBehaviour
{
    [Header("Exit Hole Settings")]
    // Colour must match the snake this exit belongs to exactly
    // Set by GameManager.PlaceExitHole() at runtime
    public Color exitColour = new Color(0.91f, 0.36f, 0.29f);

    // Grid position assigned automatically — not set in Inspector
    public Vector2Int gridPosition { get; private set; }

    // Must match GridManager.cellSize so positioning is consistent
    public float cellSize = 1f;

    // Reference to GridManager for bounds and cell type checking
    private GridManager gridManager;

    // The SpriteRenderer that draws the ring visual
    private SpriteRenderer sr;

    // Unity calls Start() once when the scene begins
    void Start()
    {
        // Nothing here — Initialise() is called externally by GameManager
    }

    // Called by GameManager after the exit hole is created
    // Finds a random valid empty interior cell and places the hole there
    public void Initialise(Color colour, GridManager gm)
    {
        exitColour = colour;
        gridManager = gm;
        cellSize = gm.cellSize;

        // Pick a random valid grid position
        gridPosition = PickRandomCell();

        // Position in world space — Z of -0.5 sits between grid (0) and snakes (-1)
        transform.position = new Vector3(
            gridPosition.x * cellSize,
            gridPosition.y * cellSize,
            -0.5f
        );

        // Build and apply the ring visual
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = exitColour;

        // Scale ring to 70% of cell size so it sits visibly inside the cell
        transform.localScale = new Vector3(cellSize * 0.7f, cellSize * 0.7f, 1f);

        Debug.Log($"Exit hole placed at ({gridPosition.x}, {gridPosition.y})");
    }

    // Finds a random empty interior cell that is not occupied by a wall
    // Keeps trying until a valid cell is found
    // Interior = not on the very edge row/column of the grid
    Vector2Int PickRandomCell()
    {
        Vector2Int candidate;
        int attempts = 0;

        do
        {
            // Pick a random cell inside the grid boundary (not on outer edge)
            int x = Random.Range(1, gridManager.width - 1);
            int y = Random.Range(1, gridManager.height - 1);
            candidate = new Vector2Int(x, y);
            attempts++;

            // Safety exit after 100 attempts to avoid infinite loop
            if (attempts > 100)
            {
                Debug.LogWarning("ExitHole: could not find valid cell after 100 attempts. Using fallback.");
                candidate = new Vector2Int(gridManager.width - 2, gridManager.height - 2);
                break;
            }
        }
        // Keep trying if the cell is a wall or already has something on it
        while (gridManager.GetCell(candidate.x, candidate.y) == GridManager.CellType.Wall);

        return candidate;
    }

    // Generates a circle ring texture procedurally
    // Pixels between innerVoid and outerRing radius are filled; others transparent
    Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Vector2 centre = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int px = 0; px < size; px++)
        {
            for (int py = 0; py < size; py++)
            {
                float dist = Vector2.Distance(new Vector2(px, py), centre);
                float outerRing = radius;
                float innerVoid = radius * 0.55f;

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

    // Called by SnakeRenderer.CheckExitReached() to verify colour match
    // Uses Approximately() instead of == to avoid floating point mismatch
    public bool MatchesSnake(Color snakeColour)
    {
        return Mathf.Approximately(exitColour.r, snakeColour.r) &&
               Mathf.Approximately(exitColour.g, snakeColour.g) &&
               Mathf.Approximately(exitColour.b, snakeColour.b);
    }
}