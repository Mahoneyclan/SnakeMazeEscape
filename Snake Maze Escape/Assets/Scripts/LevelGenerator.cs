using UnityEngine;
using System.Collections.Generic;

// LevelGenerator creates randomised maze layouts linked to difficulty tier.
// It places walls randomly based on a density percentage then validates
// that a clear path exists from every snake head to its exit hole using BFS.
// Protects all snake body cells and exit hole cells from wall placement.
// Guarantees every generated level is solvable.

public class LevelGenerator : MonoBehaviour
{
    // Reference to GridManager — set by GameManager before Generate() is called
    private GridManager gridManager;

    // Wall density per difficulty tier — percentage of cells that become walls
    // Index 0 unused — tiers are 1-based
    private float[] wallDensityByTier = { 0f, 0.10f, 0.20f, 0.30f, 0.40f, 0.50f };

    // Initialise with a reference to the grid
    public void Initialise(GridManager gm)
    {
        gridManager = gm;
    }

    // Multi-snake generation
    // allSnakeCells: every cell occupied by every snake body
    // snakeHeads: head position of each snake (for path validation)
    // exitPositions: grid position of every exit hole
    // tier: difficulty level 1-5
    public void GenerateMulti(List<Vector2Int> allSnakeCells,
        List<Vector2Int> snakeHeads,
        List<Vector2Int> exitPositions, int tier)
    {
        tier = Mathf.Clamp(tier, 1, 5);
        float density = wallDensityByTier[tier];

        int totalCells = gridManager.width * gridManager.height;
        int wallCount = Mathf.RoundToInt(totalCells * density);

        // Build combined protected cells — all snake cells and all exit holes
        List<Vector2Int> protectedCells = new List<Vector2Int>();
        protectedCells.AddRange(allSnakeCells);
        protectedCells.AddRange(exitPositions);

        // Get all candidate cells excluding protected cells and borders
        List<Vector2Int> candidates = GetWallCandidates(protectedCells);
        Shuffle(candidates);

        // Place walls up to the calculated wall count
        List<Vector2Int> placedWalls = new List<Vector2Int>();
        int placed = 0;

        foreach (Vector2Int candidate in candidates)
        {
            if (placed >= wallCount) break;
            gridManager.SetCell(candidate.x, candidate.y, GridManager.CellType.Wall);
            placedWalls.Add(candidate);
            placed++;
        }

        // Validate a path exists from every snake head to its matching exit hole
        for (int i = 0; i < snakeHeads.Count; i++)
        {
            if (i >= exitPositions.Count) break;
            EnsurePathExists(snakeHeads[i], exitPositions[i], placedWalls);
        }

        Debug.Log($"Generated — Tier {tier}, {placed} walls, " +
                  $"{snakeHeads.Count} snake(s)");
    }

    // Builds a list of all cells safe for wall placement
    // Excludes protected cells and the outer border
    List<Vector2Int> GetWallCandidates(List<Vector2Int> protectedCells)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                // Never wall protected cells
                if (protectedCells.Contains(cell)) continue;

                // Never wall the outer border — keeps edges navigable
                if (x == 0 || x == gridManager.width - 1) continue;
                if (y == 0 || y == gridManager.height - 1) continue;

                candidates.Add(cell);
            }
        }

        return candidates;
    }

    // BFS pathfinding — returns true if a clear path exists from start to goal
    bool PathExists(Vector2Int start, Vector2Int goal)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Found the goal — path exists
            if (current == goal) return true;

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbour = current + dir;

                if (!gridManager.IsInBounds(neighbour.x, neighbour.y)) continue;
                if (visited.Contains(neighbour)) continue;
                if (gridManager.GetCell(neighbour.x, neighbour.y) ==
                    GridManager.CellType.Wall) continue;

                visited.Add(neighbour);
                queue.Enqueue(neighbour);
            }
        }

        // No path found
        return false;
    }

    // Removes walls one by one until a valid path opens from snake to exit
    void EnsurePathExists(Vector2Int snakeHead, Vector2Int exitPos,
        List<Vector2Int> placedWalls)
    {
        if (PathExists(snakeHead, exitPos)) return;

        Shuffle(placedWalls);

        foreach (Vector2Int wall in placedWalls)
        {
            gridManager.SetCell(wall.x, wall.y, GridManager.CellType.Empty);

            if (PathExists(snakeHead, exitPos))
            {
                Debug.Log($"Path cleared for snake at {snakeHead}");
                return;
            }
        }

        Debug.LogWarning($"Could not clear path for snake at {snakeHead}");
    }

    // Fisher-Yates shuffle — randomises a list in place
    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
