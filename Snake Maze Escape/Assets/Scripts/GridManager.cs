using UnityEngine;
using System.Collections.Generic;

// GridManager is responsible for the entire grid/maze structure.
// It owns the grid data array, renders all cells as sprites,
// and provides public methods so other scripts can query or
// change cell types at runtime.
// Grid dimensions are set by difficulty tier via SetDifficulty()
// and are never exposed in the Inspector — prevents manual override conflicts.
// Grid is rectangular (taller than wide) to suit iPhone portrait layout.

public class GridManager : MonoBehaviour
{
    // Grid dimensions are hidden from Inspector — controlled by SetDifficulty()
    // HideInInspector keeps them public (accessible by other scripts)
    // but removes them from the Unity Inspector panel
    [HideInInspector] public int width;
    [HideInInspector] public int height;
    [HideInInspector] public float cellSize;

    [Header("Visuals")]
    public Color emptyColour = new Color(1f, 1f, 1f);
    public Color wallColour = new Color(0.18f, 0.18f, 0.18f);

    // Camera padding — adds breathing room so grid doesn't touch screen edges
    [Header("Camera")]
    public float cameraPaddingX = 0.15f; // 15% padding left and right
    public float cameraPaddingY = 0.10f; // 10% padding top and bottom

    // Defines what each cell in the grid can be
    public enum CellType { Empty, Wall }

    // 2D array storing the type of every cell in the grid
    private CellType[,] grid;

    // 2D array storing the SpriteRenderer for every cell
    // so we can update colours at runtime without recreating objects
    private SpriteRenderer[,] cellRenderers;

    // Hardcoded wall positions for testing at 10x14 grid scale
    // These will be replaced by the level generator in Step 8
    private List<Vector2Int> testWalls = new List<Vector2Int>
    {
        // Horizontal barrier across middle
        new Vector2Int(2, 7),
        new Vector2Int(3, 7),
        new Vector2Int(4, 7),
        new Vector2Int(5, 7),
        // Vertical barrier on right
        new Vector2Int(7, 4),
        new Vector2Int(7, 5),
        new Vector2Int(7, 6),
        new Vector2Int(7, 7),
        // Small cluster bottom left
        new Vector2Int(2, 2),
        new Vector2Int(3, 2),
        new Vector2Int(2, 3),
        // Small cluster top right
        new Vector2Int(7, 10),
        new Vector2Int(8, 10),
        new Vector2Int(8, 11),
    };

    // Sets grid dimensions based on difficulty tier
    // Must be called by GameManager before Awake() builds the grid
    public void SetDifficulty(int tier)
    {
        switch (tier)
        {
            case 1: width = 8;  height = 12; cellSize = 0.5f; break; // Beginner
            case 2: width = 9;  height = 13; cellSize = 0.5f; break; // Easy
            case 3: width = 10; height = 14; cellSize = 0.5f; break; // Medium
            case 4: width = 11; height = 15; cellSize = 0.5f; break; // Hard
            case 5: width = 12; height = 16; cellSize = 0.5f; break; // Expert
            default: width = 8; height = 12; cellSize = 0.5f; break; // Fallback
        }
    }

    // Awake() runs before Start() on all other scripts
    // Guarantees the grid is fully built before GameManager
    // tries to place snakes and exit holes in its Start()
    void Awake()
    {
        // Default to tier 1 if SetDifficulty() was never called
        if (width == 0 || height == 0)
            SetDifficulty(1);

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
        // Only apply walls that fit within the current grid dimensions
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
                GameObject cell = new GameObject($"Cell_{x}_{y}");
                cell.transform.parent = this.transform;

                // Position in world space based on grid coordinates and cell size
                cell.transform.position = new Vector3(x * cellSize, y * cellSize, 0);

                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSquareSprite();
                sr.color = (grid[x, y] == CellType.Wall) ? wallColour : emptyColour;

                // Slight gap between cells creates visible grid lines
                cell.transform.localScale = new Vector3(
                    cellSize * 0.95f, cellSize * 0.95f, 1f);

                cellRenderers[x, y] = sr;
            }
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

    // Positions the orthographic camera to frame the full grid
    // with padding on all sides — suits iPhone portrait layout
    void CentreCamera()
    {
        // Total world dimensions of the grid
        float gridWorldWidth = (width - 1) * cellSize;
        float gridWorldHeight = (height - 1) * cellSize;

        // Centre camera over the grid
        Camera.main.transform.position = new Vector3(
            gridWorldWidth / 2f,
            gridWorldHeight / 2f,
            -10f
        );

        // Add two cell widths of padding on each axis
        float verticalSize = (gridWorldHeight / 2f) + (cellSize * 2f);
        float screenAspect = (float)Screen.width / Screen.height;
        float horizontalSize = ((gridWorldWidth / 2f) + (cellSize * 2f)) / screenAspect;

        // Use whichever is larger to ensure full grid fits on screen
        Camera.main.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }

    // Returns the CellType at a given grid position
    // Returns Wall if position is out of bounds — treats edges as solid
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

    // Returns true if the coordinates are within the grid boundary
    // Used by SnakeRenderer and InputManager to validate moves
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}