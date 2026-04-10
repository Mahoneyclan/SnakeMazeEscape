using UnityEngine;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// Data types shared between LevelGenerator and GameManager
// ─────────────────────────────────────────────────────────────────────────────

public struct LevelParams
{
    public int   level;
    public int   world;
    public float worldProgress;
    public int   gridWidth;
    public int   gridHeight;
    public int   snakeCount;
    public int   snakeLength;
    public float wallDensity;
    public int   minSolveMoves;
    public int   interdependency;
    public int   exitDistance;

    public override string ToString() =>
        $"L{level} W{world}(wp={worldProgress:F2}) " +
        $"Grid:{gridWidth}x{gridHeight} Snakes:{snakeCount}x{snakeLength} " +
        $"Walls:{wallDensity:P0} MinMoves:{minSolveMoves} " +
        $"Inter:{interdependency} ExitDist:{exitDistance}";
}

public class GenerationResult
{
    public List<Vector2Int>       snakeHeads    = new List<Vector2Int>();
    public List<List<Vector2Int>> snakeBodies   = new List<List<Vector2Int>>();
    public List<Vector2Int>       exitPositions = new List<Vector2Int>();
    public bool                   success;
}

// ─────────────────────────────────────────────────────────────────────────────
// LevelGenerator
// ─────────────────────────────────────────────────────────────────────────────

public class LevelGenerator : MonoBehaviour
{
    private GridManager gridManager;

    private static readonly Vector2Int[] Dirs = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    // Failure sentinel — returned when no valid cell can be found
    private static readonly Vector2Int Invalid = new Vector2Int(-1, -1);

    public void Initialise(GridManager gm) => gridManager = gm;

    // ─────────────────────────────────────────────────────────────────────────
    // Parameter calculation  (static — callable before any instance exists)
    // ─────────────────────────────────────────────────────────────────────────

    public static LevelParams CalculateParams(int level)
    {
        level = Mathf.Clamp(level, 1, 999);

        int   world         = Mathf.CeilToInt(level / 111f);
        float worldProgress = ((level - 1) % 111) / 110f;

        int gridWidth  = Mathf.Clamp(7  + (level / 111), 7,  14);
        int gridHeight = Mathf.Clamp(10 + (level / 83),  10, 20);

        // Minimum 2 snakes; cycles 2→5 across each world's progress
        int snakeCount = Mathf.Clamp(
            2 + Mathf.RoundToInt(worldProgress * 3f),
            2, 5);

        // Minimum 6 segments; grows to 14 by level 999
        int snakeLength = Mathf.Clamp(6 + (level / 90), 6, 14);

        float wallDensity    = 0.08f + (level / 999f) * 0.44f;
        int   minSolveMoves  = 3 + (level / 10);
        int   interdependency = Mathf.Clamp(level / 200, 0, 4);
        int   exitDistance   = Mathf.Clamp(3 + (level / 100), 3, 15);

        // World-start breathing room — first 10 % of each world after world 1
        if (world > 1 && worldProgress < 0.1f)
        {
            wallDensity    = wallDensity   * 0.70f;
            minSolveMoves  = Mathf.Max(3,  Mathf.RoundToInt(minSolveMoves * 0.70f));
            exitDistance   = Mathf.Max(3,  exitDistance - 2);
            snakeCount     = Mathf.Max(2,  snakeCount   - 1); // floor stays at 2
        }

        return new LevelParams
        {
            level          = level,
            world          = world,
            worldProgress  = worldProgress,
            gridWidth      = gridWidth,
            gridHeight     = gridHeight,
            snakeCount     = snakeCount,
            snakeLength    = snakeLength,
            wallDensity    = wallDensity,
            minSolveMoves  = minSolveMoves,
            interdependency = interdependency,
            exitDistance   = exitDistance
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Main entry point — retry loop with progressive relaxation
    // ─────────────────────────────────────────────────────────────────────────

    public GenerationResult Generate(LevelParams p)
    {
        float density  = p.wallDensity;
        int   minMoves = p.minSolveMoves;

        for (int relaxPass = 0; relaxPass < 6; relaxPass++)
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                GenerationResult r = TryGenerate(p, density, minMoves);
                if (r.success)
                {
                    if (relaxPass > 0)
                        Debug.Log($"[LevelGen] L{p.level} succeeded after " +
                                  $"{relaxPass} relaxation pass(es)");
                    return r;
                }
            }

            // Relax constraints by 5 % density / 20 % minMoves each pass
            density  = Mathf.Max(0f, density - 0.05f);
            minMoves = Mathf.Max(3,  Mathf.RoundToInt(minMoves * 0.80f));
            Debug.Log($"[LevelGen] L{p.level} relaxing — " +
                      $"density={density:F2} minMoves={minMoves}");
        }

        // Final fallback: no walls, no move requirement, guarantee solvability
        Debug.LogWarning($"[LevelGen] L{p.level} using fallback (no walls)");
        GenerationResult fallback = TryGenerate(p, 0f, 0);
        fallback.success = true;
        return fallback;
    }

    // Single generation attempt. Returns success=false immediately on any
    // unrecoverable failure so the caller can retry cheaply.
    GenerationResult TryGenerate(LevelParams p, float density, int minMoves)
    {
        var result = new GenerationResult();

        // Clear grid to all-empty before each attempt
        ClearGrid();

        // ── 1. Place snakes ──────────────────────────────────────────────────
        var usedCells = new List<Vector2Int>();

        for (int i = 0; i < p.snakeCount; i++)
        {
            Vector2Int head = PickInteriorCell(usedCells);
            if (head == Invalid) return result;

            List<Vector2Int> body = BuildBody(head, p.snakeLength, usedCells);
            result.snakeHeads.Add(head);
            result.snakeBodies.Add(body);
            usedCells.AddRange(body);
        }

        // ── 2. Place exits at minimum BFS distance ───────────────────────────
        for (int i = 0; i < p.snakeCount; i++)
        {
            Vector2Int exit = PlaceExit(result.snakeHeads[i], p.exitDistance, usedCells);
            if (exit == Invalid) return result;
            result.exitPositions.Add(exit);
            usedCells.Add(exit);
        }

        // ── 3. Place walls ───────────────────────────────────────────────────
        var protectedCells = new List<Vector2Int>(usedCells); // snakes + exits
        PlaceWalls(density, protectedCells);

        // ── 4. Validate: all paths exist (removes walls if needed) ───────────
        if (!EnsureAllPathsExist(result.snakeHeads, result.exitPositions))
            return result;

        // ── 5. Validate: minimum solve moves ────────────────────────────────
        if (minMoves > 0 &&
            !AllPathsLongEnough(result.snakeHeads, result.exitPositions, minMoves))
            return result;

        // ── 6. Validate: interdependency ─────────────────────────────────────
        if (p.interdependency > 0)
        {
            int pairs = CountInterdependentPairs(
                result.snakeHeads, result.exitPositions, result.snakeBodies);
            if (pairs < p.interdependency) return result;
        }

        result.success = true;
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Snake body building
    // ─────────────────────────────────────────────────────────────────────────

    Vector2Int PickInteriorCell(List<Vector2Int> excluded)
    {
        for (int attempt = 0; attempt < 500; attempt++)
        {
            int x = Random.Range(1, gridManager.width  - 1);
            int y = Random.Range(1, gridManager.height - 1);
            var c = new Vector2Int(x, y);
            if (!excluded.Contains(c)) return c;
        }
        return Invalid;
    }

    List<Vector2Int> BuildBody(Vector2Int head, int length, List<Vector2Int> occupied)
    {
        // Pass 1 — four straight directions
        foreach (Vector2Int dir in Dirs)
        {
            var attempt = new List<Vector2Int> { head };
            Vector2Int next = head;
            bool blocked = false;

            for (int i = 1; i < length; i++)
            {
                next += dir;
                if (!gridManager.IsInBounds(next.x, next.y)) { blocked = true; break; }
                if (occupied.Contains(next))                  { blocked = true; break; }
                attempt.Add(next);
            }

            if (!blocked) return attempt;
        }

        // Pass 2 — DFS winding
        var winding = new List<Vector2Int> { head };
        if (BuildWindingPath(winding, length, occupied)) return winding;

        // Fallback — stack (rare; only if grid is completely packed)
        Debug.LogWarning($"[LevelGen] BuildBody stacking at {head} len={length}");
        var fallback = new List<Vector2Int>();
        for (int i = 0; i < length; i++) fallback.Add(head);
        return fallback;
    }

    bool BuildWindingPath(List<Vector2Int> body, int target, List<Vector2Int> occupied)
    {
        if (body.Count == target) return true;
        Vector2Int tip = body[body.Count - 1];

        foreach (Vector2Int dir in Dirs)
        {
            Vector2Int next = tip + dir;
            if (!gridManager.IsInBounds(next.x, next.y)) continue;
            if (occupied.Contains(next))                  continue;
            if (body.Contains(next))                      continue;

            body.Add(next);
            if (BuildWindingPath(body, target, occupied)) return true;
            body.RemoveAt(body.Count - 1);
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Exit placement
    // ─────────────────────────────────────────────────────────────────────────

    // Finds a cell >= minDist BFS steps from snakeHead on the empty grid.
    // Tries up to 100 candidates at the desired distance; falls back to any
    // reachable cell if none found.
    Vector2Int PlaceExit(Vector2Int snakeHead, int minDist, List<Vector2Int> occupied)
    {
        var candidates = new List<Vector2Int>();
        for (int x = 1; x < gridManager.width  - 1; x++)
        for (int y = 1; y < gridManager.height - 1; y++)
        {
            var c = new Vector2Int(x, y);
            if (!occupied.Contains(c)) candidates.Add(c);
        }
        Shuffle(candidates);

        // First pass: honour minDist (check up to 100)
        int tries = 0;
        foreach (Vector2Int c in candidates)
        {
            if (tries++ >= 100) break;
            if (BfsDistance(snakeHead, c, null) >= minDist) return c;
        }

        // Second pass: any reachable cell
        foreach (Vector2Int c in candidates)
            if (BfsDistance(snakeHead, c, null) > 0) return c;

        return Invalid;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Wall management
    // ─────────────────────────────────────────────────────────────────────────

    void ClearGrid()
    {
        for (int x = 0; x < gridManager.width;  x++)
        for (int y = 0; y < gridManager.height; y++)
            gridManager.SetCell(x, y, GridManager.CellType.Empty);
    }

    void PlaceWalls(float density, List<Vector2Int> protectedCells)
    {
        var candidates = new List<Vector2Int>();
        for (int x = 1; x < gridManager.width  - 1; x++)
        for (int y = 1; y < gridManager.height - 1; y++)
        {
            var c = new Vector2Int(x, y);
            if (!protectedCells.Contains(c)) candidates.Add(c);
        }
        Shuffle(candidates);

        int wallTarget = Mathf.RoundToInt(
            gridManager.width * gridManager.height * density);
        wallTarget = Mathf.Min(wallTarget, candidates.Count);

        for (int i = 0; i < wallTarget; i++)
            gridManager.SetCell(candidates[i].x, candidates[i].y,
                                GridManager.CellType.Wall);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Path validation
    // ─────────────────────────────────────────────────────────────────────────

    // Returns true when every snake can reach its exit.
    // Non-matching exits count as walls during each snake's BFS.
    // Removes walls one-by-one until all paths exist.
    bool EnsureAllPathsExist(List<Vector2Int> heads, List<Vector2Int> exits)
    {
        for (int i = 0; i < heads.Count; i++)
        {
            var otherExits = OtherExits(exits, i);
            if (BfsDistance(heads[i], exits[i], otherExits) >= 0) continue;

            if (!ClearPathForSnake(heads[i], exits[i], otherExits))
                return false;
        }
        return true;
    }

    bool ClearPathForSnake(Vector2Int head, Vector2Int exit,
                           List<Vector2Int> extraWalls)
    {
        var walls = new List<Vector2Int>();
        for (int x = 0; x < gridManager.width;  x++)
        for (int y = 0; y < gridManager.height; y++)
            if (gridManager.GetCell(x, y) == GridManager.CellType.Wall)
                walls.Add(new Vector2Int(x, y));

        Shuffle(walls);

        foreach (Vector2Int w in walls)
        {
            gridManager.SetCell(w.x, w.y, GridManager.CellType.Empty);
            if (BfsDistance(head, exit, extraWalls) >= 0) return true;
        }
        return false;
    }

    // Returns true when every snake's shortest path is >= minMoves.
    bool AllPathsLongEnough(List<Vector2Int> heads, List<Vector2Int> exits,
                            int minMoves)
    {
        for (int i = 0; i < heads.Count; i++)
        {
            int dist = BfsDistance(heads[i], exits[i], OtherExits(exits, i));
            if (dist < minMoves) return false;
        }
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Interdependency
    // ─────────────────────────────────────────────────────────────────────────

    // Counts snake pairs (a,b) where snake B's initial body cells appear on
    // snake A's ideal shortest path — meaning B must move before A can escape.
    int CountInterdependentPairs(List<Vector2Int> heads, List<Vector2Int> exits,
                                 List<List<Vector2Int>> bodies)
    {
        int count = 0;
        for (int a = 0; a < heads.Count; a++)
        {
            List<Vector2Int> pathA = BfsPath(heads[a], exits[a], null);
            if (pathA == null) continue;

            for (int b = 0; b < heads.Count; b++)
            {
                if (a == b) continue;
                foreach (Vector2Int cell in pathA)
                {
                    if (bodies[b].Contains(cell)) { count++; break; }
                }
            }
        }
        return count;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BFS utilities
    // ─────────────────────────────────────────────────────────────────────────

    // Returns Manhattan-BFS shortest path length from start to goal,
    // or -1 if goal is unreachable. extraWalls are treated as impassable.
    int BfsDistance(Vector2Int start, Vector2Int goal, List<Vector2Int> extraWalls)
    {
        if (start == goal) return 0;

        var queue   = new Queue<Vector2Int>();
        var dist    = new Dictionary<Vector2Int, int>();

        queue.Enqueue(start);
        dist[start] = 0;

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            int        d   = dist[cur];

            foreach (Vector2Int dir in Dirs)
            {
                Vector2Int nb = cur + dir;
                if (!gridManager.IsInBounds(nb.x, nb.y))           continue;
                if (dist.ContainsKey(nb))                           continue;
                if (gridManager.GetCell(nb.x, nb.y) ==
                    GridManager.CellType.Wall)                       continue;
                if (extraWalls != null && extraWalls.Contains(nb))  continue;

                if (nb == goal) return d + 1;
                dist[nb] = d + 1;
                queue.Enqueue(nb);
            }
        }
        return -1;
    }

    // Returns the actual path cells from start to goal, or null if unreachable.
    List<Vector2Int> BfsPath(Vector2Int start, Vector2Int goal,
                             List<Vector2Int> extraWalls)
    {
        if (start == goal) return new List<Vector2Int> { start };

        var queue  = new Queue<Vector2Int>();
        var parent = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        parent[start] = start;

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();

            foreach (Vector2Int dir in Dirs)
            {
                Vector2Int nb = cur + dir;
                if (!gridManager.IsInBounds(nb.x, nb.y))           continue;
                if (parent.ContainsKey(nb))                         continue;
                if (gridManager.GetCell(nb.x, nb.y) ==
                    GridManager.CellType.Wall)                       continue;
                if (extraWalls != null && extraWalls.Contains(nb))  continue;

                parent[nb] = cur;

                if (nb == goal)
                {
                    var path = new List<Vector2Int>();
                    Vector2Int c = goal;
                    while (c != start) { path.Add(c); c = parent[c]; }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                queue.Enqueue(nb);
            }
        }
        return null;
    }

    // Returns a list of all exits except the one at index i
    List<Vector2Int> OtherExits(List<Vector2Int> exits, int skipIndex)
    {
        var result = new List<Vector2Int>();
        for (int j = 0; j < exits.Count; j++)
            if (j != skipIndex) result.Add(exits[j]);
        return result;
    }

    // Fisher-Yates in-place shuffle
    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
