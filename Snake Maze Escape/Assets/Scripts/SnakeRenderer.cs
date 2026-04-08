using UnityEngine;
using System.Collections.Generic;

// SnakeRenderer manages a single snake: position data, visuals, movement,
// selection state, and win detection.
//
// Visual layers per segment (sortingOrder):
//   1 — drop shadow  (slightly larger, semi-transparent, offset down-right)
//   2 — body sprite  (head larger/lighter, body normal, tail smaller/darker)
//
// Both ends are moveable: clicking the tail reverses the cells list so the
// tail becomes the active head, and movement proceeds from there.

public class SnakeRenderer : MonoBehaviour
{
    [Header("Snake Settings")]
    public Color snakeColour = new Color(0.91f, 0.36f, 0.29f);

    private float cellSize;

    // Index 0 = active head, last index = active tail
    private List<Vector2Int> cells = new List<Vector2Int>();

    // Body SpriteRenderers only (not shadows) — used by UpdateSelectionVisual
    private List<SpriteRenderer> segmentRenderers = new List<SpriteRenderer>();

    private bool isSelected  = false;
    private bool initialised = false;

    private GridManager gridManager;
    private GameManager gameManager;

    // -------------------------------------------------------------------------
    // Initialisation
    // -------------------------------------------------------------------------

    public void Initialise(Color colour, Vector2Int startPos, int length,
        GridManager gm, List<Vector2Int> occupiedCells = null)
    {
        snakeColour = colour;
        gridManager = gm;
        gameManager = FindAnyObjectByType<GameManager>();
        cellSize    = gridManager.cellSize;

        cells = BuildSnakeBody(startPos, length,
            occupiedCells ?? new List<Vector2Int>());

        initialised = true;
        RenderSnake();
    }

    // Tries all four directions; picks the first that fits without overlapping
    List<Vector2Int> BuildSnakeBody(Vector2Int head, int length,
        List<Vector2Int> occupiedCells)
    {
        Vector2Int[] directions = {
            Vector2Int.right, Vector2Int.up,
            Vector2Int.left,  Vector2Int.down
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int next = head;
            List<Vector2Int> attempt = new List<Vector2Int> { head };
            bool blocked = false;

            for (int i = 1; i < length; i++)
            {
                next += dir;
                if (!gridManager.IsInBounds(next.x, next.y)) { blocked = true; break; }
                if (occupiedCells.Contains(next))             { blocked = true; break; }
                attempt.Add(next);
            }

            if (!blocked && attempt.Count == length)
                return attempt;
        }

        // Fallback — stack at head (generator will handle any overlap)
        List<Vector2Int> fallback = new List<Vector2Int>();
        for (int i = 0; i < length; i++) fallback.Add(head);
        return fallback;
    }

    void Start()
    {
        if (initialised) return;
        gridManager = FindAnyObjectByType<GridManager>();
        gameManager = FindAnyObjectByType<GameManager>();
        cellSize    = gridManager.cellSize;
        cells       = new List<Vector2Int> { Vector2Int.zero };
        RenderSnake();
    }

    // -------------------------------------------------------------------------
    // Rendering
    // -------------------------------------------------------------------------

    void RenderSnake()
    {
        // Destroy all previous child GameObjects (segments + shadows)
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
        segmentRenderers.Clear();

        int last = cells.Count - 1;

        for (int i = 0; i <= last; i++)
        {
            Vector3 worldPos = new Vector3(
                cells[i].x * cellSize,
                cells[i].y * cellSize,
                -1f);

            float segScale = SegmentScale(i, last);

            // Drop shadow — slightly larger, dark, offset down-right
            GameObject shadow    = new GameObject($"Shadow_{i}");
            shadow.transform.SetParent(transform, false);
            shadow.transform.position = worldPos + new Vector3(
                cellSize * 0.07f, -cellSize * 0.07f, 0f);

            SpriteRenderer shadowSr = shadow.AddComponent<SpriteRenderer>();
            shadowSr.sprite       = CreateSquareSprite();
            shadowSr.color        = new Color(0f, 0f, 0f, 0.28f);
            shadowSr.sortingOrder = 1;
            float shadowScale     = segScale * 1.18f;
            shadow.transform.localScale = new Vector3(shadowScale, shadowScale, 1f);

            // Body sprite
            GameObject seg    = new GameObject($"Segment_{i}");
            seg.transform.SetParent(transform, false);
            seg.transform.position = worldPos;

            SpriteRenderer sr = seg.AddComponent<SpriteRenderer>();
            sr.sprite         = CreateSquareSprite();
            sr.color          = SegmentBaseColour(i, last);
            sr.sortingOrder   = 2;
            seg.transform.localScale = new Vector3(segScale, segScale, 1f);

            segmentRenderers.Add(sr);
        }

        UpdateSelectionVisual();
    }

    // Head is larger, tail is smaller, body uniform
    float SegmentScale(int i, int last)
    {
        if (i == 0)    return cellSize * 0.88f; // head
        if (i == last) return cellSize * 0.55f; // tail
        return             cellSize * 0.80f;    // body
    }

    // Head slightly lighter, tail slightly darker, body normal
    Color SegmentBaseColour(int i, int last)
    {
        if (i == 0)    return Color.Lerp(snakeColour, Color.white, 0.18f);
        if (i == last) return Color.Lerp(snakeColour, Color.black, 0.25f);
        return snakeColour;
    }

    void UpdateSelectionVisual()
    {
        int last = cells.Count - 1;
        for (int i = 0; i < segmentRenderers.Count; i++)
        {
            if (segmentRenderers[i] == null) continue;

            if (isSelected && i == 0)
                // Active head: bright highlight
                segmentRenderers[i].color = Color.Lerp(snakeColour, Color.white, 0.55f);
            else if (isSelected && i == last)
                // Inactive tail: soft highlight shows it is also tappable
                segmentRenderers[i].color = Color.Lerp(snakeColour, Color.white, 0.30f);
            else
                segmentRenderers[i].color = SegmentBaseColour(i, last);
        }
    }

    // -------------------------------------------------------------------------
    // Public selection API
    // -------------------------------------------------------------------------

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSelectionVisual();
    }

    // Reverses the cells list so the tail becomes the active head.
    // Called by InputManager when the player taps the tail end.
    public void SetActiveEndToTail()
    {
        cells.Reverse();
        RenderSnake();
    }

    public bool IsHeadCell(int x, int y) =>
        cells.Count > 0 && cells[0] == new Vector2Int(x, y);

    // IsTailCell is only true when the tail is a distinct cell from the head
    public bool IsTailCell(int x, int y) =>
        cells.Count > 1 && cells[cells.Count - 1] == new Vector2Int(x, y);

    // -------------------------------------------------------------------------
    // Public query API
    // -------------------------------------------------------------------------

    public bool OccupiesCell(int x, int y) =>
        cells.Contains(new Vector2Int(x, y));

    public List<Vector2Int> GetAllCells()  => new List<Vector2Int>(cells);
    public Vector2Int       GetHeadCell()  => cells[0];
    public Color            GetColour()    => snakeColour;

    // -------------------------------------------------------------------------
    // Movement
    // -------------------------------------------------------------------------

    public void MoveAlongLine(int targetX, int targetY, GridManager gm)
    {
        Vector2Int head = cells[0];

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

            if (!gm.IsInBounds(next.x, next.y)) break;
            if (gm.GetCell(next.x, next.y) == GridManager.CellType.Wall) break;

            // Stop at own body (tail vacates this frame so exclude it)
            bool selfBlock = false;
            for (int i = 0; i < cells.Count - 1; i++)
                if (cells[i] == next) { selfBlock = true; break; }
            if (selfBlock) break;

            // Stop at other snakes
            SnakeRenderer[] allSnakes = FindObjectsByType<SnakeRenderer>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            bool otherBlock = false;
            foreach (SnakeRenderer other in allSnakes)
            {
                if (other == this) continue;
                if (other.OccupiesCell(next.x, next.y)) { otherBlock = true; break; }
            }
            if (otherBlock) break;

            cells.Insert(0, next);
            cells.RemoveAt(cells.Count - 1);
            current = next;
        }

        RenderSnake();
        CheckExitReached();
    }

    public void Retract()
    {
        if (cells.Count <= 1) return;
        cells.RemoveAt(0);
        RenderSnake();
    }

    void CheckExitReached()
    {
        ExitHole[] allExits = FindObjectsByType<ExitHole>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (ExitHole exit in allExits)
        {
            if (!exit.MatchesSnake(snakeColour)) continue;
            if (cells[0] != exit.gridPosition)   continue;

            Debug.Log("Snake escaped!");
            Destroy(exit.gameObject);
            Destroy(this.gameObject);
            gameManager.CheckWin();
            return;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
