using UnityEngine;
using System.Collections.Generic;

// GridManager is responsible for the entire grid/maze structure.
// It owns the grid data array, renders all cells as sprites,
// and provides public methods so other scripts can query or
// change cell types (empty, wall) at runtime.

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 6;   // Number of columns in the grid
    public int height = 6;  // Number of rows in the grid
    public float cellSize = 1f; // Size of each cell in Unity units

    [Header("Visuals")]
    public Color emptyColour = new Color(1f, 1f, 1f);          // White for passable cells
    public Color wallColour = new Color(0.18f, 0.18f, 0.18f);  // Dark charcoal for walls

    // Defines what each cell in the grid can be
    public enum CellType { Empty, Wall }

    // 2D array storing the type of every cell in the grid
    private CellType[,] grid;

    // 2D array storing the SpriteRenderer for every cell
    // so we can update colours at runtime without recreating objects
    private SpriteRenderer[,] cellRenderers;

    // Hardcoded wall positions for testing
    // These will be replaced by JSON level data in Step 7
    private List<Vector2Int> testWalls = new List<Vector2Int>
    {
        new Vector2Int(1, 4),
        new Vector2Int(2, 4),
        new Vector2Int(3, 4),
        new Vector2Int(3, 3),
        new Vector2Int(3, 2),
        new Vector2Int(1, 1),
        new Vector2Int(2, 1),
    };


    // Awake() runs before Start() on all other scripts
    // This guarantees the grid data is ready before GameManager
    // tries to place exit holes in its own Start() method
    void Awake()
    {
        BuildGrid();
        RenderGrid();
        CentreCamera();
    }

    // Initialises the grid data array and applies wall positions
    void BuildGrid()
    {
        grid = new CellType[width, height];

        // Default every cell to empty
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = CellType.Empty;

        // Mark wall positions from the test list
        foreach (Vector2Int wall in testWalls)
            if (wall.x >= 0 && wall.x < width && wall.y >= 0 && wall.y < height)
                grid[wall.x, wall.y] = CellType.Wall;
    }

    // Creates a SpriteRenderer GameObject for every cell in the grid
    // and colours it based on its cell type
    void RenderGrid()
    {
        cellRenderers = new SpriteRenderer[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Create a new empty GameObject for this cell
                GameObject cell = new GameObject($"Cell_{x}_{y}");
                cell.transform.parent = this.transform;

                // Position it in world space based on grid coordinates
                cell.transform.position = new Vector3(x * cellSize, y * cellSize, 0);

                // Add a SpriteRenderer and assign a 1x1 white square sprite
                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSquareSprite();

                // Colour the cell based on its type
                sr.color = (grid[x, y] == CellType.Wall) ? wallColour : emptyColour;

                // Scale slightly below cellSize to create visible grid lines
                cell.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 1f);

                cellRenderers[x, y] = sr;
            }
        }
    }

    // Generates a minimal 1x1 white texture and wraps it in a Sprite
    // Used by both GridManager and SnakeRenderer to draw coloured squares
    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    // Positions the camera so the entire grid is centred and visible
    void CentreCamera()
    {
        Camera.main.transform.position = new Vector3(
            (width - 1) * cellSize / 2f,
            (height - 1) * cellSize / 2f,
            -10f
        );

        // Orthographic size controls zoom — add padding so edges aren't clipped
        Camera.main.orthographicSize = (Mathf.Max(width, height) * cellSize) / 2f + 1f;
    }

    // Returns the CellType at a given grid position
    // Returns Wall if the position is out of bounds (treats edges as solid)
    public CellType GetCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return CellType.Wall;
        return grid[x, y];
    }

    // Sets a cell to a new type and immediately updates its colour on screen
    public void SetCell(int x, int y, CellType type)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        grid[x, y] = type;
        cellRenderers[x, y].color = (type == CellType.Wall) ? wallColour : emptyColour;
    }

    // Returns true if the given coordinates are within the grid boundary
    // Used by SnakeRenderer and InputManager to validate moves
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}