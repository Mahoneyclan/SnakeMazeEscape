using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// GameManager is the top-level controller for the game.
// Supports 999 levels across 9 worlds. All difficulty parameters are
// computed continuously from the level number via LevelGenerator.CalculateParams().
// Level progress is persisted across sessions via PlayerPrefs.

[DefaultExecutionOrder(20)]
public class GameManager : MonoBehaviour
{
    private const string LevelKey  = "SnakeMaze_Level";
    private const int    MaxLevel  = 999;

    // Current level — loaded from PlayerPrefs at Start(); Inspector value ignored
    [Header("Level (read-only at runtime)")]
    public int currentLevel = 1;

    private GridManager   gridManager;
    private LevelGenerator levelGenerator;
    private UIManager     uiManager;
    private AudioManager  audioManager;

    // Fixed 5-slot colour palette — index matches snake slot, not count
    private static readonly Color[] SnakeColours =
    {
        new Color(0.91f, 0.36f, 0.29f), // Coral red
        new Color(0.29f, 0.56f, 0.85f), // Ocean blue
        new Color(0.36f, 0.68f, 0.44f), // Leaf green
        new Color(0.94f, 0.71f, 0.16f), // Golden yellow
        new Color(0.61f, 0.43f, 0.82f)  // Purple
    };

    // Decremented each time a snake escapes — avoids deferred-Destroy race
    private int snakesRemaining;

    // ─────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        currentLevel = Mathf.Clamp(PlayerPrefs.GetInt(LevelKey, 1), 1, MaxLevel);

        // Auto-create missing managers
        uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager == null)
        {
            uiManager = new GameObject("UIManager").AddComponent<UIManager>();
            Debug.LogWarning("[GameManager] UIManager not in scene — created");
        }
        uiManager.Initialise(this);

        gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager == null)
            gridManager = new GameObject("GridManager").AddComponent<GridManager>();

        levelGenerator = FindAnyObjectByType<LevelGenerator>();
        if (levelGenerator == null)
            levelGenerator = new GameObject("LevelGenerator").AddComponent<LevelGenerator>();

        audioManager = FindAnyObjectByType<AudioManager>();
        if (audioManager == null)
            audioManager = new GameObject("AudioManager").AddComponent<AudioManager>();

        // ── Calculate params for this level ──────────────────────────────────
        LevelParams p = LevelGenerator.CalculateParams(currentLevel);
        Debug.Log($"[GameManager] {p}");

        // ── Resize grid ───────────────────────────────────────────────────────
        gridManager.SetSize(p.gridWidth, p.gridHeight);
        gridManager.RebuildGrid();

        // ── Generate level layout ─────────────────────────────────────────────
        levelGenerator.Initialise(gridManager);
        GenerationResult gen = levelGenerator.Generate(p);

        if (!gen.success)
            Debug.LogWarning($"[GameManager] Level {currentLevel} generation failed — using whatever was produced");

        // ── Spawn snakes from pre-computed bodies ─────────────────────────────
        snakesRemaining = gen.snakeHeads.Count;

        for (int i = 0; i < gen.snakeHeads.Count; i++)
        {
            GameObject    obj   = new GameObject($"Snake_{i + 1}");
            SnakeRenderer snake = obj.AddComponent<SnakeRenderer>();
            snake.InitialiseWithBody(SnakeColours[i], gen.snakeBodies[i], gridManager);
        }

        // ── Spawn exit holes at pre-computed positions ────────────────────────
        for (int i = 0; i < gen.exitPositions.Count; i++)
        {
            GameObject exitObj  = new GameObject($"ExitHole_{i + 1}");
            ExitHole   exitHole = exitObj.AddComponent<ExitHole>();
            exitHole.InitialiseAt(SnakeColours[i], gen.exitPositions[i], gridManager);
        }

        Debug.Log($"[GameManager] Level {currentLevel} — World {p.world}, " +
                  $"{gen.snakeHeads.Count} snake(s), {p.snakeLength}-cell bodies");

        // Log key test levels once on startup for progression verification
        if (currentLevel == 1) LogTestLevels();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Win detection
    // ─────────────────────────────────────────────────────────────────────────

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

    IEnumerator LevelCompleteSequence()
    {
        audioManager?.PlayWin();

        yield return null;
        yield return null;

        uiManager?.ShowWinPanel(currentLevel);

        yield return new WaitForSeconds(2.5f);
        NextLevel();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Level transitions
    // ─────────────────────────────────────────────────────────────────────────

    public void NextLevel()
    {
        int next = Mathf.Min(currentLevel + 1, MaxLevel);
        PlayerPrefs.SetInt(LevelKey, next);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ResetLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Debug helpers
    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        PlayerPrefs.SetInt(LevelKey, 1);
        PlayerPrefs.Save();
        Debug.Log("Progress reset to level 1.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    [ContextMenu("Jump to Level 112 (World 2 Start)")]
    public void DebugJumpToLevel112()  => JumpToLevel(112);

    [ContextMenu("Jump to Level 223 (World 3 Start)")]
    public void DebugJumpToLevel223()  => JumpToLevel(223);

    [ContextMenu("Jump to Level 500 (Mid-game)")]
    public void DebugJumpToLevel500()  => JumpToLevel(500);

    [ContextMenu("Jump to Level 999 (Final)")]
    public void DebugJumpToLevel999()  => JumpToLevel(999);

    void JumpToLevel(int level)
    {
        PlayerPrefs.SetInt(LevelKey, level);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Logs computed parameters for 7 representative levels so the designer
    // can verify the progression curve in the Console on first play.
    static void LogTestLevels()
    {
        int[] testLevels = { 1, 5, 50, 111, 112, 500, 999 };
        Debug.Log("═══ Level Progression Snapshot ═══");
        foreach (int l in testLevels)
            Debug.Log($"  {LevelGenerator.CalculateParams(l)}");
        Debug.Log("══════════════════════════════════");
    }
}
