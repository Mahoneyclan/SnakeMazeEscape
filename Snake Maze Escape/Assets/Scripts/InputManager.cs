using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private GridManager gridManager;
    private SnakeRenderer selectedSnake;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleClick();
    }

    void HandleClick()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, 0));

        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.y);

        // Outside grid — deselect
        if (!gridManager.IsInBounds(x, y))
        {
            Deselect();
            return;
        }

        // Clicked a wall — ignore
        if (gridManager.GetCell(x, y) == GridManager.CellType.Wall)
            return;

        // Clicked a snake — select it
        SnakeRenderer clickedSnake = GetSnakeAtCell(x, y);
        if (clickedSnake != null)
        {
            if (selectedSnake != null) Deselect();
            selectedSnake = clickedSnake;
            selectedSnake.SetSelected(true);
            return;
        }

        // No snake selected — nothing to do
        if (selectedSnake == null) return;

        Vector2Int head = selectedSnake.GetHeadCell();

        // Must be same row or same column
        bool sameRow = (y == head.y);
        bool sameCol = (x == head.x);

        if (!sameRow && !sameCol)
        {
            Deselect();
            return;
        }

        // Move along the line
        selectedSnake.MoveAlongLine(x, y, gridManager);
    }

    void Deselect()
    {
        if (selectedSnake != null)
        {
            selectedSnake.SetSelected(false);
            selectedSnake = null;
        }
    }

    SnakeRenderer GetSnakeAtCell(int x, int y)
    {
        SnakeRenderer[] allSnakes = FindObjectsByType<SnakeRenderer>(FindObjectsSortMode.None);
        foreach (SnakeRenderer snake in allSnakes)
            if (snake.OccupiesCell(x, y)) return snake;
        return null;
    }
}