using UnityEngine;
using UnityEngine.InputSystem;

// InputManager handles all player input.
// It detects mouse clicks, converts screen coordinates to grid coordinates,
// determines whether the player clicked a snake or a target cell,
// and delegates movement to the selected SnakeRenderer.
// Only one snake can be selected at a time.

public class InputManager : MonoBehaviour
{
    // Reference to GridManager for bounds checking and cell type queries
    private GridManager gridManager;

    // The currently selected snake — null if none is selected
    private SnakeRenderer selectedSnake;

    // Unity calls Start() once when the scene begins
    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();
    }

    // Unity calls Update() every frame
    // We only act on the frame when the left mouse button is first pressed
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleClick();
    }

    // Main click handler — called once per left mouse button press
    // Converts the click to a grid position and decides what to do
    void HandleClick()
    {
        // Read mouse position in screen pixels
        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        // Convert screen pixels to world space coordinates
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, 0));

        // Round to nearest integer to get grid cell coordinates
        int x = Mathf.RoundToInt(worldPos.x / gridManager.cellSize);
        int y = Mathf.RoundToInt(worldPos.y / gridManager.cellSize);

        // If the click landed outside the grid, deselect and stop
        if (!gridManager.IsInBounds(x, y))
        {
            Deselect();
            return;
        }

        // If the click landed on a wall, ignore it
        if (gridManager.GetCell(x, y) == GridManager.CellType.Wall)
            return;

        // Check whether a snake occupies the clicked cell
        SnakeRenderer clickedSnake = GetSnakeAtCell(x, y);

        if (clickedSnake != null)
        {
            if (selectedSnake != null) Deselect();

            // If the player tapped the tail, reverse the snake so the tail
            // becomes the active head — both ends are moveable
            if (clickedSnake.IsTailCell(x, y))
                clickedSnake.SetActiveEndToTail();

            selectedSnake = clickedSnake;
            selectedSnake.SetSelected(true);
            return;
        }

        // If no snake is selected there is nothing else to do
        if (selectedSnake == null) return;

        // A snake is selected and the player clicked an empty cell
        // Target must be in the same row or column as the snake's head
        Vector2Int head = selectedSnake.GetHeadCell();
        bool sameRow = (y == head.y);
        bool sameCol = (x == head.x);

        if (!sameRow && !sameCol)
        {
            // Diagonal or unaligned click — deselect the snake
            Deselect();
            return;
        }

        // Valid target — move snake along the line toward the clicked cell
        // Snake stops early if blocked by wall, another snake, or grid edge
        selectedSnake.MoveAlongLine(x, y, gridManager);
    }

    // Deselects the current snake and clears the reference
    void Deselect()
    {
        if (selectedSnake != null)
        {
            selectedSnake.SetSelected(false);
            selectedSnake = null;
        }
    }

    // Searches all active SnakeRenderers to find one occupying the given cell
    // Returns null if no snake is found at that position
    SnakeRenderer GetSnakeAtCell(int x, int y)
    {
        SnakeRenderer[] allSnakes = FindObjectsByType<SnakeRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (SnakeRenderer snake in allSnakes)
            if (snake.OccupiesCell(x, y)) return snake;
        return null;
    }
}