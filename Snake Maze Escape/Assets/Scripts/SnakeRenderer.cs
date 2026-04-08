using UnityEngine;
using System.Collections.Generic;

public class SnakeRenderer : MonoBehaviour
{
    [Header("Snake Settings")]
    public Color snakeColour = new Color(0.91f, 0.36f, 0.29f);
    public float cellSize = 1f;

    private List<Vector2Int> cells = new List<Vector2Int>();
    private List<SpriteRenderer> segmentRenderers = new List<SpriteRenderer>();
    private bool isSelected = false;
    private GridManager gridManager;

    private List<Vector2Int> testSnakeCells = new List<Vector2Int>
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(2, 0),
    };

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        cells = new List<Vector2Int>(testSnakeCells);
        RenderSnake();
    }

    void RenderSnake()
    {
        foreach (SpriteRenderer sr in segmentRenderers)
            if (sr != null) Destroy(sr.gameObject);
        segmentRenderers.Clear();

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int pos = cells[i];

            GameObject seg = new GameObject($"Segment_{i}");
            seg.transform.parent = this.transform;
            seg.transform.position = new Vector3(pos.x * cellSize, pos.y * cellSize, -1f);

            SpriteRenderer sr = seg.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = snakeColour;
            seg.transform.localScale = new Vector3(cellSize * 0.85f, cellSize * 0.85f, 1f);

            segmentRenderers.Add(sr);
        }

        UpdateSelectionVisual();
    }

    void UpdateSelectionVisual()
    {
        for (int i = 0; i < segmentRenderers.Count; i++)
        {
            if (segmentRenderers[i] == null) continue;
            if (isSelected && i == 0)
                segmentRenderers[i].color = Color.Lerp(snakeColour, Color.white, 0.5f);
            else
                segmentRenderers[i].color = snakeColour;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSelectionVisual();
    }

    public bool OccupiesCell(int x, int y)
    {
        return cells.Contains(new Vector2Int(x, y));
    }

    public Vector2Int GetHeadCell()
    {
        return cells[0];
    }

    // Move snake along a straight line toward target, stopping at obstacles
    public void MoveAlongLine(int targetX, int targetY, GridManager gridManager)
    {
        Vector2Int head = cells[0];

        // Determine direction
        int dx = 0, dy = 0;
        if (targetX != head.x) dx = (targetX > head.x) ? 1 : -1;
        if (targetY != head.y) dy = (targetY > head.y) ? 1 : -1;

        Vector2Int current = head;

        while (true)
        {
            Vector2Int next = new Vector2Int(current.x + dx, current.y + dy);

            // Stop if past target
            if (dx != 0 && ((dx > 0 && next.x > targetX) || (dx < 0 && next.x < targetX))) break;
            if (dy != 0 && ((dy > 0 && next.y > targetY) || (dy < 0 && next.y < targetY))) break;

            // Stop at grid edge or wall
            if (!gridManager.IsInBounds(next.x, next.y)) break;
            if (gridManager.GetCell(next.x, next.y) == GridManager.CellType.Wall) break;

            // Stop at own body (ignore tail — it vacates)
            bool selfBlock = false;
            for (int i = 0; i < cells.Count - 1; i++)
                if (cells[i] == next) { selfBlock = true; break; }
            if (selfBlock) break;

            // Stop at other snakes
            SnakeRenderer[] allSnakes = FindObjectsByType<SnakeRenderer>(FindObjectsSortMode.None);
            bool otherBlock = false;
            foreach (SnakeRenderer other in allSnakes)
            {
                if (other == this) continue;
                if (other.OccupiesCell(next.x, next.y)) { otherBlock = true; break; }
            }
            if (otherBlock) break;

            // Valid — shift snake one step
            cells.Insert(0, next);
            cells.RemoveAt(cells.Count - 1);
            current = next;
        }

        RenderSnake();
    }

    // Retract snake one cell (move backward)
    public void Retract()
    {
        if (cells.Count <= 1) return;
        cells.RemoveAt(0);
        RenderSnake();
    }

    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}