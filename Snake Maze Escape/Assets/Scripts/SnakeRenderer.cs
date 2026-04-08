using UnityEngine;
using System.Collections.Generic;

// SnakeRenderer manages everything about a single snake:
// its position data, its visual segments on the grid,
// selection state, movement logic, and win detection
// when the snake's head reaches its matching exit hole.
// Cell size is read from GridManager at startup so the snake
// always scales correctly regardless of difficulty tier.

public class SnakeRenderer : MonoBehaviour
{
    [Header("Snake Settings")]
    public Color snakeColour = new Color(0.91f, 0.36f, 0.29f); // Coral red

    // Cell size read from GridManager at startup — never hardcoded
    // Ensures snake segments always match the current grid scale
    private float cellSize;

    // Ordered list of grid positions this snake occupies
    // Index 0 is always the head, last index is the tail
    private List<Vector2Int> cells = new List<Vector2Int>();

    // Matching list of SpriteRenderers — one per segment
    private List<SpriteRenderer> segmentRenderers = new List<SpriteRenderer>();

    // Tracks whether this snake is currently selected by the player
    private bool isSelected = false;

    // References to other managers — set in Start()
    private GridManager gridManager;
    private GameManager gameManager;
    private ExitHole exitHole;

    // Hardcoded starting positions for testing
    // Head at (0,0), body extending right
    // Will be replaced by JSON level loader in Step 8
    private List<Vector2Int> testSnakeCells = new List<Vector2Int>
    {
        new Vector2Int(0, 0), // head
        new Vector2Int(1, 0),
        new Vector2Int(2, 0), // tail
    };

    // Unity calls Start() once when the scene begins
    void Start()
    {
        // Cache references to other scripts in the scene
        gridManager = FindAnyObjectByType<GridManager>();
        gameManager = FindAnyObjectByType<GameManager>();
        exitHole = FindAnyObjectByType<ExitHole>();

        // Read cell size from GridManager so snake always matches grid scale
        // This must happen before RenderSnake() uses cellSize
        cellSize = gridManager.cellSize;

        // Initialise cell positions from test data
        cells = new List<Vector2Int>(testSnakeCells);

        // Draw the snake on screen
        RenderSnake();
    }

    // Destroys all existing segment GameObjects and rebuilds them
    // from the current cells list. Called after every move.
    void RenderSnake()
    {
        // Destroy old segment GameObjects
        foreach (SpriteRenderer sr in segmentRenderers)
            if (sr != null) Destroy(sr.gameObject);
        segmentRenderers.Clear();

        // Create a new segment for each cell position
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int pos = cells[i];

            GameObject seg = new GameObject($"Segment_{i}");
            seg.transform.parent = this.transform;

            // Z of -1 puts snake in front of grid cells (z=0)
            seg.transform.position = new Vector3(
                pos.x * cellSize, pos.y * cellSize, -1f);

            SpriteRenderer sr = seg.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = snakeColour;

            // Segments slightly smaller than cells so grid lines show through
            seg.transform.localScale = new Vector3(
                cellSize * 0.8f, cellSize * 0.8f, 1f);

            segmentRenderers.Add(sr);
        }

        // Apply selection highlight after rebuilding
        UpdateSelectionVisual();
    }

    // Updates segment colours to reflect selection state
    // When selected, the head segment lightens toward white
    void UpdateSelectionVisual()
    {
        for (int i = 0; i < segmentRenderers.Count; i++)
        {
            if (segmentRenderers[i] == null) continue;

            if (isSelected && i == 0)
                // Blend head 50% toward white as selection indicator
                segmentRenderers[i].color = Color.Lerp(snakeColour, Color.white, 0.5f);
            else
                segmentRenderers[i].color = snakeColour;
        }
    }

    // Called by InputManager to select or deselect this snake
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSelectionVisual();
    }

    // Returns true if any part of this snake occupies the given cell
    // Used by InputManager to detect which snake was clicked
    public bool OccupiesCell(int x, int y)
    {
        return cells.Contains(new Vector2Int(x, y));
    }

    // Returns the current head position (index 0 of cells list)
    public Vector2Int GetHeadCell()
    {
        return cells[0];
    }

    // Moves the snake in a straight line toward (targetX, targetY)
    // stopping when it hits a wall, another snake, its own body,
    // or the grid boundary. Snake slides automatically — player
    // clicks the destination, not each individual cell.
    public void MoveAlongLine(int targetX, int targetY, GridManager gridManager)
    {
        Vector2Int head = cells[0];

        // Calculate step direction — horizontal OR vertical only, never diagonal
        int dx = 0, dy = 0;
        if (targetX != head.x) dx = (targetX > head.x) ? 1 : -1;
        if (targetY != head.y) dy = (targetY > head.y) ? 1 : -1;

        Vector2Int current = head;

        // Step one cell at a time until blocked or target is reached
        while (true)
        {
            Vector2Int next = new Vector2Int(current.x + dx, current.y + dy);

            // Stop if we have stepped past the target cell
            if (dx != 0 && ((dx > 0 && next.x > targetX) || (dx < 0 && next.x < targetX))) break;
            if (dy != 0 && ((dy > 0 && next.y > targetY) || (dy < 0 && next.y < targetY))) break;

            // Stop at grid boundary or wall
            if (!gridManager.IsInBounds(next.x, next.y)) break;
            if (gridManager.GetCell(next.x, next.y) == GridManager.CellType.Wall) break;

            // Stop at own body — exclude tail since it vacates this frame
            bool selfBlock = false;
            for (int i = 0; i < cells.Count - 1; i++)
                if (cells[i] == next) { selfBlock = true; break; }
            if (selfBlock) break;

            // Stop at other snakes
            SnakeRenderer[] allSnakes = FindObjectsByType<SnakeRenderer>(FindObjectsInactive.Exclude);
            bool otherBlock = false;
            foreach (SnakeRenderer other in allSnakes)
            {
                if (other == this) continue;
                if (other.OccupiesCell(next.x, next.y)) { otherBlock = true; break; }
            }
            if (otherBlock) break;

            // Valid move — insert new head, remove tail to slide forward
            cells.Insert(0, next);
            cells.RemoveAt(cells.Count - 1);
            current = next;
        }

        // Rebuild visuals to match new positions
        RenderSnake();

        // Check if head has reached the matching exit hole
        CheckExitReached();
    }

    // Removes the head segment — moves the snake backward one cell
    // Used for the retraction mechanic
    public void Retract()
    {
        if (cells.Count <= 1) return;
        cells.RemoveAt(0);
        RenderSnake();
    }

    // Checks whether the snake's head is on its matching exit hole
    // If matched, destroys both snake and exit hole and notifies GameManager
    void CheckExitReached()
    {
        // Find exit hole lazily — it may not exist when Start() runs
        if (exitHole == null)
            exitHole = FindAnyObjectByType<ExitHole>();

        if (exitHole == null) return;

        Vector2Int head = cells[0];

        if (head == exitHole.gridPosition && exitHole.MatchesSnake(snakeColour))
        {
            Debug.Log("Snake escaped!");
            Destroy(exitHole.gameObject);
            Destroy(this.gameObject);
            gameManager.CheckWin();
        }
    }

    // Generates a minimal 1x1 white texture wrapped as a Sprite
    // Colour applied separately via SpriteRenderer.color
    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}