using UnityEngine;
using System.Collections.Generic;

// SnakeRenderer manages everything about a single snake:
// its position data, its visual segments on the grid,
// selection state, movement logic, and win detection
// when the snake's head reaches its matching exit hole.

public class SnakeRenderer : MonoBehaviour
{
    [Header("Snake Settings")]
    public Color snakeColour = new Color(0.91f, 0.36f, 0.29f); // Coral red — unique per snake
    public float cellSize = 1f; // Must match GridManager.cellSize

    // Ordered list of grid positions this snake occupies
    // Index 0 is always the head, last index is the tail
    private List<Vector2Int> cells = new List<Vector2Int>();

    // Matching list of SpriteRenderers — one per segment
    // Kept in sync with cells so we can update colours without rebuilding
    private List<SpriteRenderer> segmentRenderers = new List<SpriteRenderer>();

    // Tracks whether this snake is currently selected by the player
    private bool isSelected = false;

    // References to other managers set in Start()
    private GridManager gridManager;
    private GameManager gameManager;
    private ExitHole exitHole;

    // Hardcoded starting positions for testing
    // Head at (0,0), body extending right to (2,0)
    // Will be replaced by JSON level loader in Step 7
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
        gridManager = FindFirstObjectByType<GridManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        exitHole = FindFirstObjectByType<ExitHole>();

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

            // Z position of -1 puts the snake in front of the grid cells (z = 0)
            seg.transform.position = new Vector3(pos.x * cellSize, pos.y * cellSize, -1f);

            SpriteRenderer sr = seg.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = snakeColour;

            // Segments are slightly smaller than cells so the grid shows through
            seg.transform.localScale = new Vector3(cellSize * 0.85f, cellSize * 0.85f, 1f);

            segmentRenderers.Add(sr);
        }

        // Apply selection highlight after rebuilding
        UpdateSelectionVisual();
    }

    // Updates segment colours to show selection state
    // When selected, the head segment lightens toward white
    void UpdateSelectionVisual()
    {
        for (int i = 0; i < segmentRenderers.Count; i++)
        {
            if (segmentRenderers[i] == null) continue;

            if (isSelected && i == 0)
                // Blend head colour 50% toward white as a selection indicator
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
    // stopping as soon as it hits a wall, another snake, its own body,
    // or the grid boundary. The snake slides one cell at a time
    // until blocked — the player does not need to click each individual cell.
    public void MoveAlongLine(int targetX, int targetY, GridManager gridManager)
    {
        Vector2Int head = cells[0];

        // Calculate the step direction — only horizontal OR vertical, never diagonal
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

            // Stop at grid boundary or wall cell
            if (!gridManager.IsInBounds(next.x, next.y)) break;
            if (gridManager.GetCell(next.x, next.y) == GridManager.CellType.Wall) break;

            // Stop if the next cell is part of own body
            // We exclude the tail (last index) because it will vacate this frame
            bool selfBlock = false;
            for (int i = 0; i < cells.Count - 1; i++)
                if (cells[i] == next) { selfBlock = true; break; }
            if (selfBlock) break;

            // Stop if the next cell is occupied by a different snake
            SnakeRenderer[] allSnakes = FindObjectsByType<SnakeRenderer>(FindObjectsSortMode.None);
            bool otherBlock = false;
            foreach (SnakeRenderer other in allSnakes)
            {
                if (other == this) continue;
                if (other.OccupiesCell(next.x, next.y)) { otherBlock = true; break; }
            }
            if (otherBlock) break;

            // Move is valid — insert new head position, remove tail
            // This shifts the entire snake forward by one cell
            cells.Insert(0, next);
            cells.RemoveAt(cells.Count - 1);
            current = next;
        }

        // Rebuild visuals to match new cell positions
        RenderSnake();

        // Check if the snake has reached its exit hole after moving
        CheckExitReached();
    }

    // Removes the head segment, effectively moving the snake backward
    // Used for future retraction mechanic
    public void Retract()
    {
        if (cells.Count <= 1) return;
        cells.RemoveAt(0);
        RenderSnake();
    }

    // Checks whether the snake's head is now on its matching exit hole
    // If it matches, the snake and exit hole are destroyed and GameManager
    // is notified to check if all snakes have escaped (win condition)
    void CheckExitReached()
    {
        if (exitHole == null) return;

        Vector2Int head = cells[0];

        if (head == exitHole.gridPosition && exitHole.MatchesSnake(snakeColour))
        {
            Debug.Log("Snake escaped!");

            // Remove the exit hole and this snake from the scene
            Destroy(exitHole.gameObject);
            Destroy(this.gameObject);

            // Tell GameManager to check if the level is complete
            gameManager.CheckWin();
        }
    }

    // Generates a minimal 1x1 white texture wrapped as a Sprite
    // Colour is applied separately via SpriteRenderer.color
    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}