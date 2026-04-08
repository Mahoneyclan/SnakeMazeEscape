using UnityEngine;
using UnityEngine.SceneManagement;

// GameManager is the top-level controller for a single level.
// It creates and places the exit hole automatically at startup,
// checks the win condition after each snake escape,
// and handles level reset.
// It does not control snake movement (SnakeRenderer)
// or player input (InputManager).

public class GameManager : MonoBehaviour
{
    // Reference to GridManager for level setup
    private GridManager gridManager;

    // Unity calls Start() once when the scene begins
    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();

        // Automatically place the exit hole — no Inspector config needed
        PlaceExitHole();

        Debug.Log("Level started");
    }

    // Creates the exit hole GameObject and initialises it at a random valid cell
    // The exit hole colour matches the snake colour (coral red for now)
    // In Step 7 (level loader) this will read colour from JSON level data
    void PlaceExitHole()
    {
        // Create a new empty GameObject to hold the ExitHole script
        GameObject exitObj = new GameObject("ExitHole1");

        // Add the ExitHole component
        ExitHole exitHole = exitObj.AddComponent<ExitHole>();

        // Coral red — must match the snake colour exactly
        Color snakeColour = new Color(0.91f, 0.36f, 0.29f);

        // Initialise picks a random valid cell and draws the ring
        exitHole.Initialise(snakeColour, gridManager);
    }

    // Called by SnakeRenderer.CheckExitReached() every time a snake escapes
    // Checks whether any snakes remain on the board
    public void CheckWin()
    {
        // Count remaining snakes — escaped snakes destroy themselves
        SnakeRenderer[] remaining = FindObjectsByType<SnakeRenderer>(FindObjectsSortMode.None);

        if (remaining.Length == 0)
        {
            // All snakes have escaped — level is complete
            Debug.Log("Level Complete! All snakes escaped.");
            // Future: show win UI, play animation, load next level
        }
        else
        {
            Debug.Log($"{remaining.Length} snake(s) still on the board.");
        }
    }

    // Reloads the current scene from scratch
    // Restores all snakes, walls, and exit holes to their starting state
    public void ResetLevel()
    {
        Debug.Log("Resetting level...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}