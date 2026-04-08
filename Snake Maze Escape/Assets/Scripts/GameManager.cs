using UnityEngine;
using UnityEngine.SceneManagement;

// GameManager is the top-level controller for a single level.
// It sets the difficulty tier, creates and places the exit hole
// automatically at startup, checks the win condition after each
// snake escape, and handles level reset.
// It does not control snake movement (SnakeRenderer)
// or player input (InputManager).

public class GameManager : MonoBehaviour
{
    [Header("Difficulty")]
    // Tier 1 = Beginner, Tier 5 = Expert
    // Controls grid size and wall density
    public int difficultyTier = 1;

    // Reference to GridManager — set in Start()
    private GridManager gridManager;

    // Unity calls Start() once when the scene begins
    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();

        // Set grid dimensions based on difficulty tier
        // Must be called before grid builds — GridManager.Awake()
        // uses fallback if this hasn't been called yet
        gridManager.SetDifficulty(difficultyTier);

        // Automatically place the exit hole — no Inspector config needed
        PlaceExitHole();

        Debug.Log($"Level started — Difficulty Tier {difficultyTier}");
    }

    // Creates the exit hole GameObject and initialises it
    // at a random valid interior cell matching the snake colour
    void PlaceExitHole()
    {
        // Create a new empty GameObject to hold the ExitHole script
        GameObject exitObj = new GameObject("ExitHole1");
        ExitHole exitHole = exitObj.AddComponent<ExitHole>();

        // Coral red — must match the snake colour exactly
        // In Step 8 this will be read from level data
        Color snakeColour = new Color(0.91f, 0.36f, 0.29f);

        // Initialise picks a random valid cell and draws the ring
        exitHole.Initialise(snakeColour, gridManager);
    }

    // Called by SnakeRenderer.CheckExitReached() every time a snake escapes
    // Checks whether any snakes remain on the board
    public void CheckWin()
    {
        // Count remaining snakes — escaped snakes destroy themselves
        SnakeRenderer[] remaining = FindObjectsByType<SnakeRenderer>(FindObjectsInactive.Exclude);

        if (remaining.Length == 0)
        {
            // All snakes have escaped — level is complete
            Debug.Log("Level Complete! All snakes escaped.");
            // Future: show win UI, play animation, load next level
        }
        else
        {
            // Some snakes still on the board
            Debug.Log($"{remaining.Length} snake(s) still on the board.");
        }
    }

    // Reloads the current scene from scratch
    // Restores all snakes, walls, and exit holes to starting state
    // Will be wired to the Reset button in Step 9
    public void ResetLevel()
    {
        Debug.Log("Resetting level...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}