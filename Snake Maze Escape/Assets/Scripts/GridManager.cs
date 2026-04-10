using UnityEngine;

// GridManager is responsible for the entire grid/maze structure.
// It owns the grid data array, renders all cells as sprites,
// and provides public methods so other scripts can query or
// change cell types at runtime.
// Grid dimensions are set by GameManager via SetSize() before RebuildGrid().
// Grid is square to match the reference game aesthetic.

[DefaultExecutionOrder(-10)]
public class GridManager : MonoBehaviour
{
    // Grid dimensions — set by GameManager via SetSize() before RebuildGrid().
    // HideInInspector keeps them public (accessible by other scripts)
    // but removes them from the Unity Inspector panel.
    [HideInInspector] public int width;
    [HideInInspector] public int height;
    [HideInInspector] public float cellSize;

    [Header("Visuals")]
    public Color emptyColour = new Color(0.13f, 0.15f, 0.20f); // dark blue-grey cells
    public Color wallColour  = new Color(0.06f, 0.06f, 0.08f); // near-black walls

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
        cellSize = 0.4f;
    }

    // Awake() sets default dimensions only — BuildGrid/RenderGrid are
    // deferred to RebuildGrid(), which GameManager always calls in Start().
    // CentreCamera() is also in Start() as a safety fallback.
    void Awake()
    {
        if (width == 0 || height == 0)
        {
            width    = 8;
            height   = 8;
            cellSize = 0.4f;
        }
    }

    void Start()
    {
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

                // Gap between cells creates visible grid lines
                // 0.88 gives a 12% gap — visible at small cellSize (0.4f)
                cell.transform.localScale = new Vector3(
                    cellSize * 0.88f, cellSize * 0.88f, 1f);

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

    void CentreCamera()
    {
        if (Camera.main == null) return;

        float gridWorldWidth  = (width  - 1) * cellSize;
        float gridWorldHeight = (height - 1) * cellSize;

        float pad = cellSize * 1.2f;

        float screenAspect = (float)Screen.width / Screen.height;
        if (screenAspect <= 0.01f) screenAspect = 0.5625f;

        // The grid zone is 80% of screen height (top 8% = HUD, bottom 12% = ad)
        // Scale orthographic size to fit grid within this 80% zone
        float zoneScale = 1f / 0.80f;

        float sizeFromWidth  = (gridWorldWidth  / 2f + pad) / screenAspect;
        float sizeFromHeight = (gridWorldHeight / 2f + pad) * zoneScale;

        float orthoSize = Mathf.Max(sizeFromWidth, sizeFromHeight);

        // Shift camera DOWN to centre within the 80% zone
        // HUD takes top 8% so shift down by 4% of ortho height
        // Ad takes bottom 12% so shift up by 6% of ortho height
        // Net: shift up by 2% to centre in the middle zone
        float verticalShift = orthoSize * 0.02f;

        Camera.main.backgroundColor    = new Color(0.12f, 0.12f, 0.16f);
        Camera.main.orthographicSize   = orthoSize;
        Camera.main.transform.position = new Vector3(
            gridWorldWidth  / 2f,
            gridWorldHeight / 2f + verticalShift,
            -10f
        );

        Debug.Log($"[GridManager] CentreCamera — " +
                  $"grid={gridWorldWidth:F2}x{gridWorldHeight:F2} " +
                  $"ortho={orthoSize:F2} aspect={screenAspect:F2} " +
                  $"zone=80%");
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

    // Destroys all existing cell GameObjects and rebuilds the grid.
    // Called from GameManager.Start() after screen dims are valid.
    public void RebuildGrid()
    {
        foreach (Transform child in this.transform)
            Destroy(child.gameObject);

        BuildGrid();
        RenderGrid();
        CentreCamera();
    }

}