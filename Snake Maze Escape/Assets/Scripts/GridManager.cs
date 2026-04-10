using UnityEngine;

// GridManager is responsible for the entire grid/maze structure.
// It owns the grid data array, renders all cells as sprites,
// and provides public methods so other scripts can query or
// change cell types at runtime.
// Grid dimensions are set by difficulty tier via SetDifficulty()
// and are never exposed in the Inspector — prevents manual override conflicts.
// Grid is square to match the reference game aesthetic.

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


    // Sets grid dimensions directly — used by the 999-level system
    public void SetSize(int w, int h)
    {
        width    = Mathf.Max(4, w);
        height   = Mathf.Max(4, h);
        cellSize = 0.9f;
    }

    // Sets grid dimensions based on difficulty tier (kept for compatibility)
    public void SetDifficulty(int tier)
    {
        // Square grids — match reference game aesthetic
        // cellSize is uniform; camera auto-fits to screen width
        switch (tier)
        {
            case 1: width =  8; height =  8; cellSize = 0.9f; break; // Beginner
            case 2: width = 10; height = 10; cellSize = 0.9f; break; // Easy
            case 3: width = 12; height = 12; cellSize = 0.9f; break; // Medium
            case 4: width = 13; height = 13; cellSize = 0.9f; break; // Hard
            case 5: width = 14; height = 14; cellSize = 0.9f; break; // Expert
            default: width = 8; height =  8; cellSize = 0.9f; break; // Fallback
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

    // Initialises the grid data array — all cells start empty
    // Walls are added later by LevelGenerator
    void BuildGrid()
    {
        grid = new CellType[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = CellType.Empty;
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

    // Positions the orthographic camera to frame the square grid.
    // On a portrait screen the grid is always width-constrained, so orthographic
    // size is derived from the horizontal span. A small vertical offset shifts
    // the grid downward to leave room for the HUD strip at the top.
    void CentreCamera()
    {
        float gridWorldWidth  = (width  - 1) * cellSize;
        float gridWorldHeight = (height - 1) * cellSize;

        // Padding: 1.5 cells on every side
        float pad = cellSize * 1.5f;

        float screenAspect = (float)Screen.width / Screen.height;

        // Half-height needed to show full grid width + padding
        float sizeFromWidth  = (gridWorldWidth  / 2f + pad) / screenAspect;
        // Half-height needed to show full grid height + padding
        float sizeFromHeight = gridWorldHeight / 2f + pad;

        float orthoSize = Mathf.Max(sizeFromWidth, sizeFromHeight);

        // Shift grid centre down by 4% of the ortho height to give HUD space at top
        float verticalOffset = orthoSize * 0.04f;

        Camera.main.orthographicSize = orthoSize;
        Camera.main.transform.position = new Vector3(
            gridWorldWidth  / 2f,
            gridWorldHeight / 2f - verticalOffset,
            -10f
        );
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

// Destroys all existing cell GameObjects and rebuilds the grid
    // Called by GameManager after SetDifficulty() changes dimensions
    public void RebuildGrid()
    {
        // Destroy all existing cell GameObjects
        foreach (Transform child in this.transform)
            Destroy(child.gameObject);

        // Rebuild from scratch with new dimensions
        BuildGrid();
        RenderGrid();
        CentreCamera();
    }

}