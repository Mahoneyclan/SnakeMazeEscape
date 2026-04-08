using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 6;
    public int height = 6;
    public float cellSize = 1f;

    [Header("Visuals")]
    public Color emptyColour = new Color(1f, 1f, 1f);
    public Color wallColour = new Color(0.18f, 0.18f, 0.18f);

    public enum CellType { Empty, Wall }
    private CellType[,] grid;
    private SpriteRenderer[,] cellRenderers;

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

    void Start()
    {
        BuildGrid();
        RenderGrid();
        CentreCamera();
    }

    void BuildGrid()
    {
        grid = new CellType[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = CellType.Empty;

        foreach (Vector2Int wall in testWalls)
            if (wall.x >= 0 && wall.x < width && wall.y >= 0 && wall.y < height)
                grid[wall.x, wall.y] = CellType.Wall;
    }

    void RenderGrid()
    {
        cellRenderers = new SpriteRenderer[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject cell = new GameObject($"Cell_{x}_{y}");
                cell.transform.parent = this.transform;
                cell.transform.position = new Vector3(x * cellSize, y * cellSize, 0);

                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSquareSprite();
                sr.color = (grid[x, y] == CellType.Wall) ? wallColour : emptyColour;
                cell.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 1f);

                cellRenderers[x, y] = sr;
            }
        }
    }

    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    void CentreCamera()
    {
        Camera.main.transform.position = new Vector3(
            (width - 1) * cellSize / 2f,
            (height - 1) * cellSize / 2f,
            -10f
        );
        Camera.main.orthographicSize = (Mathf.Max(width, height) * cellSize) / 2f + 1f;
    }

    public CellType GetCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return CellType.Wall;
        return grid[x, y];
    }

    public void SetCell(int x, int y, CellType type)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        grid[x, y] = type;
        cellRenderers[x, y].color = (type == CellType.Wall) ? wallColour : emptyColour;
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}