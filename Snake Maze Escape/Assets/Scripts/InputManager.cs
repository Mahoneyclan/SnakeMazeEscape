using UnityEngine;
using UnityEngine.InputSystem;

// InputManager handles all player input via press/drag.
// Works on both desktop (mouse) and mobile (touch) because it uses
// Pointer.current — the Input System base class shared by Mouse and Touchscreen.
// Press down on a snake to select it, then drag along its row or column
// to slide it — the snake follows the pointer in real time.
// Releasing the pointer finalises the position and deselects.

public class InputManager : MonoBehaviour
{
    private GridManager gridManager;

    // Snake currently being dragged — null when no drag is active
    private SnakeRenderer selectedSnake;

    // Last grid cell the pointer occupied during this drag
    private Vector2Int lastDragCell;

    // True while the pointer is held after selecting a snake
    private bool isDragging = false;

    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();
    }

    void Update()
    {
        Pointer ptr = Pointer.current;
        if (ptr == null) return;

        if (ptr.press.wasPressedThisFrame)
            HandlePressStart();
        else if (isDragging && ptr.press.IsPressed())
            HandleDrag();
        else if (ptr.press.wasReleasedThisFrame)
            HandleRelease();
    }

    // -------------------------------------------------------------------------
    // Input phases
    // -------------------------------------------------------------------------

    // Called on the frame the pointer goes down
    void HandlePressStart()
    {
        Vector2Int cell = PointerToGrid();

        if (!gridManager.IsInBounds(cell.x, cell.y))
        {
            Deselect();
            return;
        }

        // Only select a snake by its head — body/tail taps do nothing
        SnakeRenderer snake = GetSnakeAtHead(cell.x, cell.y);
        if (snake == null)
        {
            Deselect();
            return;
        }

        if (selectedSnake != null) Deselect();
        selectedSnake = snake;
        selectedSnake.SetSelected(true);
        lastDragCell = selectedSnake.GetHeadCell();
        isDragging   = true;
    }

    // Called every frame while the pointer is held and a snake is selected
    void HandleDrag()
    {
        // Snake may have been destroyed (escaped exit) mid-drag
        if (selectedSnake == null) { isDragging = false; return; }

        Vector2Int cell = PointerToGrid();
        if (cell == lastDragCell) return; // pointer hasn't crossed a cell boundary

        Vector2Int head = selectedSnake.GetHeadCell();

        // Only allow movement along the axis already aligned with the head
        bool sameRow = (cell.y == head.y);
        bool sameCol = (cell.x == head.x);
        if (!sameRow && !sameCol) return;

        if (!gridManager.IsInBounds(cell.x, cell.y)) return;
        if (gridManager.GetCell(cell.x, cell.y) == GridManager.CellType.Wall) return;

        selectedSnake.MoveAlongLine(cell.x, cell.y, gridManager);

        // Update lastDragCell to the snake's actual head after the move
        // (it may have stopped short of the pointer due to walls or other snakes)
        if (selectedSnake != null)
            lastDragCell = selectedSnake.GetHeadCell();
        else
            isDragging = false; // snake escaped — end drag
    }

    // Called on the frame the pointer is released
    void HandleRelease()
    {
        isDragging = false;
        Deselect();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    void Deselect()
    {
        if (selectedSnake != null)
        {
            selectedSnake.SetSelected(false);
            selectedSnake = null;
        }
    }

    // Converts the current pointer position to grid coordinates
    Vector2Int PointerToGrid()
    {
        Vector2 screen  = Pointer.current.position.ReadValue();
        Vector3 world   = Camera.main.ScreenToWorldPoint(
            new Vector3(screen.x, screen.y, 0f));
        int x = Mathf.RoundToInt(world.x / gridManager.cellSize);
        int y = Mathf.RoundToInt(world.y / gridManager.cellSize);
        return new Vector2Int(x, y);
    }

    // Returns the SnakeRenderer whose HEAD is at the given cell, or null
    SnakeRenderer GetSnakeAtHead(int x, int y)
    {
        SnakeRenderer[] all = FindObjectsByType<SnakeRenderer>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (SnakeRenderer s in all)
            if (s.IsHeadCell(x, y)) return s;
        return null;
    }
}
