using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// GameManager is the top-level controller for the game.
// It manages level progression across 25 levels (5 tiers x 5 sub-levels).
// Each tier increases snake length. Each sub-level increases snake count.
// Tier 1: 4-cell snakes, 1-5 snakes
// Tier 2: 5-cell snakes, 1-5 snakes
// ...up to Tier 5: 8-cell snakes, 1-5 snakes
//
// Level is persisted across scene reloads via PlayerPrefs.
// To reset progress from the Editor: Edit > Clear All PlayerPrefs.

public class GameManager : MonoBehaviour
{
    // PlayerPrefs key for saved level number
    private const string LevelKey = "SnakeMaze_Level";

    // Maximum level in the campaign
    private const int MaxLevel = 25;

    // Current level 1-25 — loaded from PlayerPrefs at Start()
    // The Inspector value is ignored at runtime (PlayerPrefs wins)
    [Header("Level (read-only at runtime — set via PlayerPrefs)")]
    public int currentLevel = 1;

    // References to other scripts
    private GridManager   gridManager;
    private LevelGenerator levelGenerator;
    private UIManager     uiManager;

    // Derived from currentLevel — calculated in Start()
    private int difficultyTier; // 1-5
    private int snakeCount;     // 1-5
    private int snakeLength;    // 4-8

    // Sub-levels per tier — 5 levels per tier (one per snake count)
    private const int subLevelsPerTier = 5;

    // Snake length per tier — grows by 2 per tier to match reference image
    private static readonly int[] snakeLengthByTier = { 0, 6, 8, 10, 12, 14 };

    // Snake colours — one unique colour per snake slot
    private Color[] snakeColours =
    {
        new Color(0.91f, 0.36f, 0.29f), // Coral red
        new Color(0.29f, 0.56f, 0.85f), // Ocean blue
        new Color(0.36f, 0.68f, 0.44f), // Leaf green
        new Color(0.94f, 0.71f, 0.16f), // Golden yellow
        new Color(0.61f, 0.43f, 0.82f)  // Purple
    };

    // Stores spawn positions for generator to protect
    private List<Vector2Int> snakeStartPositions = new List<Vector2Int>();

    // Stores exit hole positions for generator to protect
    private List<Vector2Int> exitHolePositions = new List<Vector2Int>();

    // Countdown of snakes still on the board — decremented by CheckWin()
    // Avoids FindObjectsByType after Destroy() which is deferred to end-of-frame
    private int snakesRemaining;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        // Load persisted level — defaults to 1 on first run
        currentLevel = PlayerPrefs.GetInt(LevelKey, 1);
        currentLevel = Mathf.Clamp(currentLevel, 1, MaxLevel);

        // Find or create UIManager
        uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager == null)
        {
            GameObject uiObj = new GameObject("UIManager");
            uiManager = uiObj.AddComponent<UIManager>();
        }
        uiManager.Initialise(this);

        // Find scene objects — auto-create LevelGenerator if missing from scene
        gridManager    = FindAnyObjectByType<GridManager>();
        levelGenerator = FindAnyObjectByType<LevelGenerator>();
        if (levelGenerator == null)
        {
            GameObject lgObj = new GameObject("LevelGenerator");
            levelGenerator = lgObj.AddComponent<LevelGenerator>();
        }

        // Derive parameters for this level
        CalculateLevelParameters();

        // Step 1 — resize grid for this tier
        gridManager.SetDifficulty(difficultyTier);
        gridManager.RebuildGrid();

        // Step 2 — spawn snakes first (no exits yet — starts with empty exclusions)
        SpawnAllSnakes(snakeCount, snakeLength);

        // Collect all snake cells now that bodies are placed
        List<Vector2Int> allSnakeCells = new List<Vector2Int>();
        List<Vector2Int> snakeHeads    = new List<Vector2Int>();

        SnakeRenderer[] spawnedSnakes = FindObjectsByType<SnakeRenderer>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (SnakeRenderer snake in spawnedSnakes)
        {
            allSnakeCells.AddRange(snake.GetAllCells());
            snakeHeads.Add(snake.GetHeadCell());
        }

        // Step 3 — place exit holes, avoiding all snake cells and each other
        PlaceAllExitHoles(snakeCount, allSnakeCells);

        // Step 4 — generate walls avoiding snakes and exits
        levelGenerator.Initialise(gridManager);
        levelGenerator.GenerateMulti(
            allSnakeCells, snakeHeads, exitHolePositions, difficultyTier);

        // Update the HUD level label now that everything is initialised
        uiManager.SetLevelLabel(currentLevel);

        Debug.Log($"Level {currentLevel} — " +
                  $"Tier {difficultyTier}, " +
                  $"{snakeCount} snake(s), " +
                  $"length {snakeLength}");
    }

    // -------------------------------------------------------------------------
    // Level parameter calculation
    // -------------------------------------------------------------------------

    // Derives tier, snake count, and snake length from currentLevel
    // Level 1-5   = Tier 1 (length 4), snakes 1-5
    // Level 6-10  = Tier 2 (length 5), snakes 1-5
    // Level 11-15 = Tier 3 (length 6), snakes 1-5
    // Level 16-20 = Tier 4 (length 7), snakes 1-5
    // Level 21-25 = Tier 5 (length 8), snakes 1-5
    void CalculateLevelParameters()
    {
        difficultyTier = Mathf.CeilToInt((float)currentLevel / subLevelsPerTier);
        difficultyTier = Mathf.Clamp(difficultyTier, 1, 5);

        snakeCount  = ((currentLevel - 1) % subLevelsPerTier) + 1;
        snakeLength = snakeLengthByTier[difficultyTier];
    }

    // -------------------------------------------------------------------------
    // Spawning
    // -------------------------------------------------------------------------

    // Places one exit hole per snake, avoiding all snake cells and each other
    void PlaceAllExitHoles(int count, List<Vector2Int> snakeCells)
    {
        exitHolePositions.Clear();
        List<Vector2Int> usedPositions = new List<Vector2Int>(snakeCells);

        for (int i = 0; i < count; i++)
        {
            GameObject exitObj  = new GameObject($"ExitHole_{i + 1}");
            ExitHole   exitHole = exitObj.AddComponent<ExitHole>();

            exitHole.Initialise(snakeColours[i], gridManager, usedPositions);

            exitHolePositions.Add(exitHole.gridPosition);
            usedPositions.Add(exitHole.gridPosition);
        }
    }

    // Spawns one snake per slot at a random valid position
    void SpawnAllSnakes(int count, int length)
    {
        snakeStartPositions.Clear();
        snakesRemaining = count;
        List<Vector2Int> usedPositions = new List<Vector2Int>(exitHolePositions);

        for (int i = 0; i < count; i++)
        {
            Vector2Int startPos = PickRandomEmptyCell(usedPositions);
            snakeStartPositions.Add(startPos);

            GameObject    snakeObj = new GameObject($"Snake_{i + 1}");
            SnakeRenderer snake    = snakeObj.AddComponent<SnakeRenderer>();

            // Pass usedPositions so the body avoids exits and other snake bodies
            snake.Initialise(snakeColours[i], startPos, length, gridManager, usedPositions);

            // Block all cells this snake now occupies before spawning the next one
            usedPositions.AddRange(snake.GetAllCells());
        }
    }

    // Returns a random interior cell not in the excluded list
    Vector2Int PickRandomEmptyCell(List<Vector2Int> excluded)
    {
        Vector2Int candidate;
        int attempts = 0;

        do
        {
            int x = Random.Range(1, gridManager.width  - 1);
            int y = Random.Range(1, gridManager.height - 1);
            candidate = new Vector2Int(x, y);
            attempts++;

            if (attempts > 200)
            {
                Debug.LogWarning("GameManager: fallback cell used — grid may be too crowded");
                break;
            }
        }
        while (excluded.Contains(candidate) ||
               gridManager.GetCell(candidate.x, candidate.y) == GridManager.CellType.Wall);

        return candidate;
    }

    // -------------------------------------------------------------------------
    // Win detection
    // -------------------------------------------------------------------------

    // Called by SnakeRenderer each time a snake escapes.
    // Uses snakesRemaining counter rather than FindObjectsByType — avoids the
    // deferred-Destroy race where the escaping snake is still found this frame.
    public void CheckWin()
    {
        snakesRemaining--;

        if (snakesRemaining <= 0)
        {
            Debug.Log($"Level {currentLevel} complete!");
            StartCoroutine(LevelCompleteSequence());
        }
        else
        {
            Debug.Log($"{snakesRemaining} snake(s) still on the board.");
        }
    }

    // Brief pause so the player can see the cleared board, then advances
    IEnumerator LevelCompleteSequence()
    {
        uiManager.ShowWinPanel(currentLevel);
        yield return new WaitForSeconds(2f);
        NextLevel();
    }

    // -------------------------------------------------------------------------
    // Level transitions
    // -------------------------------------------------------------------------

    // Saves the next level to PlayerPrefs then reloads the scene
    public void NextLevel()
    {
        int next = Mathf.Min(currentLevel + 1, MaxLevel);
        PlayerPrefs.SetInt(LevelKey, next);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Replays the current level — does not change the saved level
    public void ResetLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // -------------------------------------------------------------------------
    // Debug helpers (call from Editor context menu or Inspector button)
    // -------------------------------------------------------------------------

    // Resets all progress back to level 1
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        PlayerPrefs.SetInt(LevelKey, 1);
        PlayerPrefs.Save();
        Debug.Log("Progress reset to level 1.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Jumps directly to a specific level (for testing)
    [ContextMenu("Jump to Level 6 (Tier 2)")]
    public void DebugJumpToLevel6()
    {
        PlayerPrefs.SetInt(LevelKey, 6);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    [ContextMenu("Jump to Level 21 (Tier 5)")]
    public void DebugJumpToLevel21()
    {
        PlayerPrefs.SetInt(LevelKey, 21);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
