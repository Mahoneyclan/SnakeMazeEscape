using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 6;
    public int height = 6;
    public float cellSize = 1f;

    [Header("Visuals")]
    public Color emptyColour = new Color(1f, 1f, 1f);
    public Color wallColour = new Color(0.18f, 0.18f, 0.18f);
    public Color gridLineColour = new Color(0.88f, 0.88f, 0.88f);

    private GameObject[,] cells;

    void Start()
    {
        DrawGrid();
    }

    void DrawGrid()
    {
        cells = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Create a cell
                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cell.name = $"Cell_{x}_{y}";
                cell.transform.parent = this.transform;

                // Position it
                cell.transform.position = new Vector3(x * cellSize, y * cellSize, 0);
                cell.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 1f);

                // Colour it
                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                cell.GetComponent<Renderer>().material.color = emptyColour;

                cells[x, y] = cell;
            }
        }

        CentreCamera();
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
}