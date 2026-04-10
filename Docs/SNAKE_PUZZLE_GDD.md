# Snake Maze Escape — Game Design Document
**Version 2.0 | April 2026**

---

## 1. Concept Statement

Snake Maze Escape is a single-player grid-based puzzle game for iPhone. The player is presented with a maze containing two or more coloured snakes. Each snake must be guided to its matching coloured exit hole by dragging from the snake's head through the grid. Snakes cannot cross walls, themselves, or each other. An exit hole blocks all snakes except its matching colour. All snakes must reach their exit holes for the level to be complete. The game scales across 999 levels by increasing grid size, snake count, snake length, wall density, and how tightly snakes block each other's routes.

---

## 2. Core Mechanics

### The Grid
- The play area is a rectangular grid of cells (7×10 at level 1, up to 14×20 at level 999)
- Each cell is one of:
  - **Empty** — passable by any snake
  - **Wall** — impassable, forms the maze structure
  - **Snake body** — occupied, impassable to other snakes
  - **Exit hole** — placed anywhere inside the maze; passable only by its matching coloured snake, acts as a wall to all others

### Snake Anatomy
- Each snake has a **head**, a **body** of connected segments, and a **tail**
- A snake occupies a connected sequence of grid cells
- Each snake has a unique flat colour
- Each snake has one matching exit hole placed **anywhere within the maze grid** — not restricted to the border
- Snakes and their exit holes are **pre-placed** when the level loads

### Movement — Drag from Head Only
- Player **taps and holds the head** of a snake to select it
- Player **drags** along a row or column to slide the snake in that direction
- The snake moves cell by cell as the drag crosses cell boundaries — head first, body follows, tail vacates
- Movement is locked to one axis per drag (row or column aligned with the head)
- Only the **head** is a valid selection point — tapping the body or tail does nothing
- Releasing the pointer deselects the snake
- Only one snake can be moved at a time

### Movement Rules
- Snakes move **one cell at a time** along the drag direction
- A snake **cannot** move into:
  - A wall cell
  - A cell occupied by any other snake
  - A cell occupied by its own body (no self-crossing)
  - An exit hole belonging to a different snake
- A snake **can** move into cells its own tail is about to vacate

### Winning a Level
- A snake escapes when its **head enters its matching exit hole**
- On escape the snake **disappears** from the grid, freeing all cells it occupied
- A particle burst and sound effect play on escape
- The level is **won** when all snakes have escaped
- After 2 seconds the game automatically advances to the next level

---

## 3. Win / Lose Conditions

| Condition | Result |
|---|---|
| All snakes reach their matching exit hole | Level complete — auto-advance to next level after 2 s |
| Player gets stuck | No lose state — player can tap Replay to reset the current level |
| Player taps Replay | Current level restarts |
| Player taps ↺ L1 | All progress reset — returns to Level 1 |

There is **no lose state**. The game is non-punishing — the player can always reset. The challenge is purely intellectual.

---

## 4. Snake Behaviour — Full Rules

| Rule | Detail |
|---|---|
| Selection | Tap the snake HEAD to select; drag along a row or column to move |
| Body/tail tap | Does nothing — only the head is selectable |
| Forward movement | Head moves to next cell in drag direction; body follows; tail vacates |
| Self-crossing | Forbidden — snake cannot enter any cell currently occupied by its own body |
| Cross-snake collision | Forbidden — snake cannot enter any cell occupied by another snake |
| Wall collision | Forbidden — snake cannot enter wall cells |
| Exit hole (own) | Snake escapes; disappears from grid |
| Exit hole (other) | Treated as a wall — all non-matching snakes are blocked |
| Tail vacating | The cell a tail is leaving is free the moment the move commits |
| Snake length | Fixed for each level — snakes do not grow or shrink |

---

## 5. Grid & Map Rules

### Cell Types
```
[ ] = Empty cell (passable by any snake)
[#] = Wall cell (impassable)
[R] = Red snake segment
[B] = Blue snake segment
[r] = Red exit hole (passable by red only — wall to all others)
[b] = Blue exit hole (passable by blue only — wall to all others)
```

### Grid Constraints
- Grid grows from 7×10 (level 1) to 14×20 (level 999) via continuous formula
- Exit holes are placed **anywhere inside the maze** — including interior cells
- Exit holes act as walls to non-matching snakes — a core puzzle mechanic
- Each exit hole has exactly one matching snake
- Every puzzle is **guaranteed solvable** — BFS validation before accepting generation
- Every puzzle has at least one valid solution path; higher difficulties require specific move ordering

### Walls
- Walls form the maze — fixed and cannot be moved
- Wall density grows from 8% (level 1) to 52% (level 999)
- Walls can create corridors as narrow as one cell wide

---

## 6. Level Progression — 999-Level System

All difficulty parameters are computed continuously from the level number. There are no discrete tiers.

### Parameter Formulas

| Parameter | Formula | Range |
|---|---|---|
| World | ceil(level / 111) | 1–9 |
| Grid width | 7 + floor(level / 111) | 7–14 |
| Grid height | 10 + floor(level / 83) | 10–20 |
| Snake count | 2 + round(worldProgress × 3) | 2–5 |
| Snake length | 6 + floor(level / 90) | 6–14 |
| Wall density | 0.08 + (level/999) × 0.44 | 8%–52% |
| Min solve moves | 3 + floor(level / 10) | 3–103 |
| Exit distance | 3 + floor(level / 100) | 3–15 |
| Interdependency | floor(level / 200) | 0–4 |

### World-Start Breathing Room
At the start of each world after World 1 (first 10% of each world's levels):
- Wall density reduced by 30%
- Min solve moves reduced by 30%
- Exit distance reduced by 2
- Snake count reduced by 1 (minimum 2)

### Colour Palette (5 snakes)
| Slot | Colour | Description |
|---|---|---|
| 1 | Coral red `#E85D4A` | Always present |
| 2 | Ocean blue `#4A90D9` | Always present |
| 3 | Leaf green `#5BAD6F` | Levels with ≥ 3 snakes |
| 4 | Golden yellow `#F0B429` | Levels with ≥ 4 snakes |
| 5 | Purple `#9B6DD1` | Levels with 5 snakes |

---

## 7. Level Generator Algorithm

### Generation Pipeline (TryGenerate)

**Step 1 — Place snake bodies**
For each snake slot: pick a random interior cell as the head; extend a body of `snakeLength` cells using:
1. Straight-line pass in 4 directions
2. DFS winding path if no straight line fits
3. Stacking fallback (rare — only if grid is completely packed)

**Step 2 — Place exit holes at minimum BFS distance**
For each snake head, find a free interior cell with BFS distance ≥ `exitDistance` from the head. Falls back to nearest reachable cell if needed.

**Step 3 — Place walls**
Randomly fill cells that are not snake bodies or exits, up to the target wall density.

**Step 4 — Validate: all paths exist**
BFS from each snake head to its exit, treating other exits as walls. If any path is blocked, remove walls one by one until all paths are clear.

**Step 5 — Validate: minimum solve moves**
Every snake's shortest path must be ≥ `minSolveMoves`. Fail attempt if not met.

**Step 6 — Validate: interdependency**
At levels requiring interdependency, count snake-pair blocking relationships. Fail if fewer than required.

### Retry Loop
6 relaxation passes × 50 attempts each = up to 300 tries. Each pass reduces wall density by 5% and min-moves by 20%. Final fallback uses zero walls and zero move requirement (always solvable).

---

## 8. Visual Style

### Principles
- Clean, minimal, mobile-first portrait layout
- High contrast between snakes, walls, background, and exit holes
- Colourblind-aware: 5 visually distinct snake colours

### Colour Reference

| Element | Colour |
|---|---|
| Empty cells | White `#FFFFFF` |
| Wall cells | Dark charcoal `#2D2D2D` |
| Grid gap | ~5% cell size gap between cells |
| HUD background | Near-black `#141A24` at 92% opacity |
| HUD text | White |

### Snake Visual Design (LineRenderer)
- Snake body = continuous rounded tube (two LineRenderers: body + shadow)
- Width curve: 84% at head → 80% uniform body → tapers to 8% at tail tip
- Head = slightly brighter colour; eyes (white circles + dark pupils) track movement direction
- Selected snake = full-body gradient brightens by 55%
- Escape = particle burst in snake colour + sound effect

### Exit Hole Visual
- Coloured ring (70% of cell size) rendered at sortingOrder 3
- Procedural circle sprite with 55% inner void
- Colour matches the snake it belongs to

### HUD (always visible)
- Dark background strip at top of screen
- Level number — top-left, white bold text
- Replay button — top-center-right, dark background
- ↺ L1 button — top-right, red background (resets all progress to level 1)

### Win Panel (shown on level complete)
- Dark overlay (80% opacity)
- Centred card: level-complete text
- Replay button (restarts same level)
- Auto-advances to next level after 2 seconds

---

## 9. Technical Architecture

### Scripts (`Assets/Scripts/`)

| Script | Responsibility |
|---|---|
| `GameManager.cs` | Level lifecycle, win detection, PlayerPrefs persistence, scene reload |
| `GridManager.cs` | Grid data array, cell sprite rendering, camera auto-fit |
| `LevelGenerator.cs` | Procedural generation — LevelParams, GenerationResult, BFS validation |
| `SnakeRenderer.cs` | LineRenderer snake, smooth animation, eye tracking, movement, escape particles |
| `ExitHole.cs` | Exit hole placement, ring sprite, colour matching |
| `InputManager.cs` | Pointer input (mouse + touch via Pointer.current), head-only drag movement |
| `UIManager.cs` | All UI built in code — HUD, win panel, buttons |
| `AudioManager.cs` | Procedural PCM audio — move blip, escape sweep, win chord |

### Persistence
- Current level stored in `PlayerPrefs` key `"SnakeMaze_Level"`
- Scene reloads on level complete / reset (single-scene architecture)
- All state reconstructed from level number on each load

### Input System
- Unity Input System package (`Pointer.current`) — works on both mouse and touchscreen
- `InputSystemUIInputModule` replaces legacy `StandaloneInputModule`

### Scene Objects
All managers are scene GameObjects. If any are missing, `GameManager.Start()` auto-creates them:
- `GridManager` — grid + camera
- `LevelGenerator` — procedural generation
- `UIManager` — all UI
- `AudioManager` — sound

---

## 10. Build Sequence (Completed)

| Step | Status | Description |
|---|---|---|
| 1 | ✅ | Static grid renderer |
| 2 | ✅ | Wall placement and rendering |
| 3 | ✅ | Snake renderer (LineRenderer, tapered tail, eyes) |
| 4 | ✅ | Input — head-only drag movement |
| 5 | ✅ | Movement logic — collision detection |
| 6 | ✅ | Win detection — snake escape, level complete |
| 7 | ✅ | 999-level progression system |
| 8 | ✅ | HUD — level label, replay, reset-to-L1 |
| 9 | ✅ | Audio — procedural PCM synthesis |
| 10 | 🔧 | Polish — particle burst, win panel auto-advance |

---

*End of GDD v2.0*
