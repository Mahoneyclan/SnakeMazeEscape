# Snake Escape — Game Design Document
**Version 1.1 | April 2026**

---

## 1. Concept Statement

Snake Escape is a single-player grid-based puzzle game. The player is presented with a maze containing one or more coloured snakes. Each snake must be guided to its matching coloured exit hole — positioned anywhere within the maze — by tracing a path through the grid using tap/click interactions. Snakes cannot cross walls, themselves, or each other. An exit hole blocks all snakes except its matching colour. All snakes must reach their exit hole for the level to be complete. The game scales in difficulty by increasing the number of snakes, their length, and the complexity of how they block each other's routes.

---

## 2. Core Mechanics

### The Grid
- The play area is a rectangular grid of cells (e.g. 5×5 on easy, up to 10×10 on hard)
- Each cell is one of:
  - **Empty** — passable by any snake
  - **Wall** — impassable, forms the maze structure
  - **Snake body** — occupied, impassable to other snakes
  - **Exit hole** — positioned anywhere inside the maze; passable only by its matching coloured snake, acts as a wall to all others

### Snake Anatomy
- Each snake has a **head**, a **body** of one or more segments, and a **tail**
- A snake occupies a connected sequence of grid cells
- Each snake has a unique flat colour
- Each snake has one matching exit hole placed **anywhere within the maze grid** — not restricted to the border
- Snakes and their exit holes are **pre-placed** when the level loads — the player reads the maze and starts moving

### Path Tracing (Player Input)
- Player **clicks/taps the head** of a snake to select it
- Player then **clicks/taps adjacent cells** in sequence to trace the snake's intended path
- The snake moves to follow the traced path — head first, body follows, tail vacates
- Player can **click/tap backward** along the snake's own body to retract it (undo movement)
- The path trace is cancelled if the player taps a wall, another snake, or a non-adjacent cell
- Only one snake can be moved at a time — tap another snake head to switch

### Movement Rules
- Snakes move **one cell at a time** along the player's traced path
- A snake **cannot** move into:
  - A wall cell
  - A cell occupied by any other snake
  - A cell occupied by its own body (cannot cross itself)
- A snake **can** move into cells its own tail is about to vacate (tail chasing is allowed)
- Movement is **not turn-based** — snakes do not move simultaneously

### Winning a Level
- A snake escapes when its **head enters its matching exit hole**
- On escape the snake **disappears** from the grid, freeing all cells it occupied
- The level is **won** when all snakes have escaped
- No time limit — this is a pure logic puzzle

---

## 3. Win / Lose Conditions

| Condition | Result |
|---|---|
| All snakes reach their matching exit hole | Level complete — advance to next |
| Player gets stuck (no valid moves) | No lose state — player retracts moves to recover |
| Player taps Reset | Level restarts from initial state |

There is **no lose state**. The game is non-punishing — the player can always retract moves or reset. The challenge is purely intellectual.

---

## 4. Snake Behaviour — Full Rules

| Rule | Detail |
|---|---|
| Forward movement | Head moves to next traced cell, body follows, tail vacates |
| Backward movement | Player taps back along snake's own body — snake retracts one cell at a time |
| Self-crossing | Forbidden — snake cannot enter any cell currently occupied by its own body |
| Cross-snake collision | Forbidden — snake cannot enter any cell occupied by another snake |
| Wall collision | Forbidden — snake cannot enter wall cells |
| Exit hole | Only the matching coloured snake can enter — all other snakes treat it as a wall and cannot pass through it |
| Tail vacating | The cell a tail is leaving is considered free the moment the move is committed |
| Snake length | Fixed for each level — snakes do not grow or shrink except by moving |

---

## 5. Grid & Map Rules

### Cell Types
```
[ ] = Empty cell (passable by any snake)
[#] = Wall cell (impassable)
[R] = Red snake segment
[B] = Blue snake segment
[r] = Red exit hole (passable by red snake only — wall to all others)
[b] = Blue exit hole (passable by blue snake only — wall to all others)
```

### Grid Constraints
- Grid is always rectangular
- Exit holes are placed **anywhere inside the maze** — including interior cells, not just the border
- Exit holes act as walls to non-matching snakes — this is a core puzzle mechanic, not just a visual marker
- Each exit hole has exactly one matching snake
- Every puzzle is **guaranteed solvable** — the generator validates before saving
- Every puzzle has a **unique solution path order** at higher difficulties (snake A must move before snake B)

### Walls
- Walls form the maze — they are fixed and cannot be moved
- Walls can create corridors as narrow as one cell wide
- Dead ends are valid and intentional at higher difficulties

---

## 6. Difficulty Escalation

Difficulty is controlled by four levers applied in combination:

### Tier 1 — Beginner
- Grid: 5×5
- Snakes: 1
- Snake length: 2–3 cells
- Walls: minimal, open paths
- Blocking: none — snake has clear route to exit
- Goal: teach the tap-to-trace mechanic

### Tier 2 — Easy
- Grid: 6×6
- Snakes: 2
- Snake length: 3–4 cells
- Walls: simple corridors
- Blocking: snakes may share corridor space but don't actively block

### Tier 3 — Medium
- Grid: 7×7
- Snakes: 2–3
- Snake length: 4–6 cells
- Walls: maze-like with dead ends
- Blocking: one snake must move to free another's path

### Tier 4 — Hard
- Grid: 8×8
- Snakes: 3–4
- Snake length: 5–8 cells
- Walls: tight corridors, limited passing space
- Blocking: strict move ordering required — wrong order = deadlock

### Tier 5 — Expert
- Grid: 9×9 to 10×10
- Snakes: 4–5
- Snake length: 6–10 cells
- Walls: complex maze with multiple dead ends
- Blocking: multiple snakes must move in precise sequence; some moves must be partially retracted

### Escalation Parameters Table

| Tier | Grid | Snakes | Length | Blocking Complexity |
|---|---|---|---|---|
| Beginner | 5×5 | 1 | 2–3 | None |
| Easy | 6×6 | 2 | 3–4 | Passive |
| Medium | 7×7 | 2–3 | 4–6 | One dependency |
| Hard | 8×8 | 3–4 | 5–8 | Chain dependencies |
| Expert | 10×10 | 4–5 | 6–10 | Full sequence + retraction |

---

## 7. Visual Style

### Principles
- Flat colours only — no gradients, no textures
- Clean, minimal, geometric
- High contrast between snakes, walls, background, and exit holes
- Colourblind-aware palette — snakes are distinguishable by both colour and shape marker if needed

### Colour Palette

| Element | Colour | Hex |
|---|---|---|
| Background | Off-white | #F5F5F0 |
| Grid lines | Light grey | #E0E0DC |
| Wall cells | Dark charcoal | #2D2D2D |
| Empty cells | White | #FFFFFF |
| Exit holes | Match snake colour, darker ring | — |
| Snake 1 | Coral red | #E85D4A |
| Snake 2 | Ocean blue | #4A90D9 |
| Snake 3 | Leaf green | #5BAD6F |
| Snake 4 | Golden yellow | #F0B429 |
| Snake 5 | Purple | #9B6DD1 |
| UI background | Near-white | #FAFAF8 |
| UI text | Charcoal | #1A1A1A |

### Snake Visual Design
- Snake body = rounded rectangles filling each cell
- Head cell = slightly larger rounded square with two dot eyes
- Tail cell = slightly tapered
- Exit hole = a coloured circle or ring rendered on the cell floor, matching its snake's colour, with a subtle dark inner void to suggest depth — clearly distinguishable from an empty cell
- Exit holes sit flush within the maze grid — they look like destinations, not borders
- Selected snake = subtle white glow outline to indicate active selection

### UI
- Level number top-left (plain text, small)
- Reset button top-right (icon only — circular arrow)
- Move counter below level number (optional, for self-challenge)
- No score, no timer display

---

## 8. Level Generator Rules

The generator creates guaranteed-solvable puzzles algorithmically. It works in reverse — place the solution first, then build the puzzle around it.

### Generation Algorithm (High Level)

The generator works in reverse — place the solution first, then build the puzzle around it.

**Step 1 — Place exit holes inside the maze**
Randomly select N interior cells as exit holes (N = number of snakes for this tier).
Each hole gets a unique colour. Exit holes must not be adjacent to each other.
Exit holes can be anywhere in the grid — corners, centre, edges — but not outside the grid boundary.

**Step 2 — Place snake starting positions**
For each exit hole, randomly select a separate interior cell as the snake's starting head position.
Head positions must not overlap exit holes or each other.
Head position should be separated from its exit hole by at least (tier difficulty × 2) cells of path distance.

**Step 3 — Grow snake bodies**
From each head, randomly walk through the grid to place body segments.
Snake length is randomly chosen within the tier's range.
Snakes cannot overlap each other or any exit hole during placement.

**Step 4 — Carve solution paths**
Ensure a valid passable path exists from each snake head to its matching exit hole.
These paths become the corridor structure of the maze.
At higher tiers, paths are deliberately routed through shared corridors to create blocking dependencies.

**Step 5 — Fill remaining cells with walls**
All cells not used by snakes, exit holes, or solution paths are candidates for walls.
Wall density is tuned per tier — lower tiers leave more empty cells, higher tiers fill aggressively.
Constraint: solution paths for every snake must remain passable after wall placement.

**Step 6 — Validate**
Run a solver (BFS/DFS) against the generated puzzle.
If unsolvable → discard and regenerate.
If solvable → check solution requires intended move order at higher tiers.
If all checks pass → save the level.

**Step 7 — Difficulty validation**
Check the minimum number of moves to solve.
Check how many snake interdependencies exist (snake A must move before snake B).
Check whether any non-matching snake's exit hole acts as a meaningful blocker in the solution path.
If the puzzle is too easy for its intended tier → add wall cells to tighten corridors and re-validate.

### Generator Constraints by Tier

| Tier | Min solution moves | Min interdependencies |
|---|---|---|
| Beginner | 3 | 0 |
| Easy | 6 | 0–1 |
| Medium | 10 | 1–2 |
| Hard | 16 | 2–3 |
| Expert | 24 | 3+ |

---

## 9. Technical Architecture (Unity)

### Core Components
```
GridManager       — owns the grid array, cell state, renders the board
Snake             — owns position array, handles movement logic
InputManager      — handles tap/click, path tracing, snake selection
LevelLoader       — reads level data from JSON, initialises GridManager + Snakes
LevelGenerator    — procedural level creation (see Section 8)
GameManager       — win condition checking, level progression, reset
UIManager         — level number, reset button, move counter
```

### Data Structure — Level JSON
```json
{
  "tier": 2,
  "grid": { "width": 6, "height": 6 },
  "walls": [[0,1],[1,1],[2,1],[3,1],[5,2],[5,3]],
  "snakes": [
    {
      "id": 1,
      "colour": "#E85D4A",
      "cells": [[1,4],[1,3],[1,2]],
      "head": [1,4],
      "exit": [4,4]
    },
    {
      "id": 2,
      "colour": "#4A90D9",
      "cells": [[3,3],[3,4],[3,5]],
      "head": [3,3],
      "exit": [1,0]
    }
  ]
}
```
Note: `exit` coordinates are interior maze cells, not border positions. The exit hole for snake 1 sits at grid cell [4,4] — inside the maze — and is impassable to snake 2.
```

### Scene Structure
```
Main Scene
├── GameManager (script)
├── Grid
│   ├── GridManager (script)
│   └── Cell prefabs (instantiated at runtime)
├── Snakes
│   └── Snake prefabs (instantiated per level)
├── UI
│   ├── LevelLabel
│   ├── ResetButton
│   └── MoveCounter
└── Camera (orthographic, fixed)
```

---

## 10. Build Sequence

Build in this order — each step is playable before the next begins:

1. **Static grid renderer** — draw a grid on screen, nothing moves
2. **Wall placement** — render walls correctly from JSON data
3. **Snake renderer** — draw snakes on the grid from JSON data
4. **Input system** — tap to select snake, tap cells to trace path
5. **Movement logic** — snake follows traced path, collision detection
6. **Retraction** — tap back along body to undo movement
7. **Win detection** — snake exits, disappears, level complete on all escaped
8. **Level loader** — load levels from JSON files
9. **Level generator** — procedural generation + validation
10. **UI + polish** — reset button, move counter, transitions

---

*End of GDD v1.0*
