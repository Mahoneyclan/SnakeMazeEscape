using UnityEngine;
using System.Collections.Generic;

// SnakeRenderer manages a single snake: position data, visuals, movement,
// selection state, and win detection.
//
// Visual approach: two LineRenderers (shadow + body) form a continuous tube.
// Eyes (white + dark pupil) sit on top of the head and rotate to face the
// movement direction every frame. Tail tapers to a near-point.

public class SnakeRenderer : MonoBehaviour
{
    [Header("Snake Settings")]
    public Color snakeColour = new Color(0.91f, 0.36f, 0.29f);

    [Tooltip("Cells per second at which the snake slides to its target")]
    [SerializeField] private float segmentMoveSpeed = 18f;

    private float cellSize;

    // Index 0 = active head, last = active tail — pure data, no GameObjects
    private List<Vector2Int> cells = new List<Vector2Int>();

    // Visual positions that smoothly chase cells[] each frame
    private List<Vector3> visualPositions = new List<Vector3>();

    private LineRenderer bodyLR;
    private LineRenderer shadowLR;

    // Eyes: two whites + two pupils
    private Transform eyeL, eyeR, pupilL, pupilR;
    private Vector2   lastHeadDir = Vector2.right;

    private bool isSelected  = false;
    private bool initialised = false;

    public bool IsAnimating { get; private set; }

    private GridManager  gridManager;
    private GameManager  gameManager;
    private AudioManager audioManager;

    // -------------------------------------------------------------------------
    // Initialisation
    // -------------------------------------------------------------------------

    // Called by GameManager when LevelGenerator has pre-computed the full body.
    // Skips BuildSnakeBody entirely — uses the supplied cells directly.
    public void InitialiseWithBody(Color colour, List<Vector2Int> body, GridManager gm)
    {
        snakeColour  = colour;
        gridManager  = gm;
        gameManager  = FindAnyObjectByType<GameManager>();
        audioManager = FindAnyObjectByType<AudioManager>();
        cellSize     = gridManager.cellSize;

        cells = new List<Vector2Int>(body);

        visualPositions.Clear();
        foreach (Vector2Int c in cells)
            visualPositions.Add(CellToWorld(c));

        if (cells.Count >= 2)
        {
            Vector2Int d = cells[0] - cells[1];
            if (d != Vector2Int.zero)
                lastHeadDir = new Vector2(d.x, d.y).normalized;
        }

        initialised = true;
        SpawnVisuals();
    }

    public void Initialise(Color colour, Vector2Int startPos, int length,
        GridManager gm, List<Vector2Int> occupiedCells = null)
    {
        snakeColour  = colour;
        gridManager  = gm;
        gameManager  = FindAnyObjectByType<GameManager>();
        audioManager = FindAnyObjectByType<AudioManager>();
        cellSize     = gridManager.cellSize;

        cells = BuildSnakeBody(startPos, length,
            occupiedCells ?? new List<Vector2Int>());

        visualPositions.Clear();
        foreach (Vector2Int c in cells)
            visualPositions.Add(CellToWorld(c));

        // Seed initial head direction from body layout
        if (cells.Count >= 2)
        {
            Vector2Int d = cells[0] - cells[1];
            if (d != Vector2Int.zero)
                lastHeadDir = new Vector2(d.x, d.y).normalized;
        }

        initialised = true;
        SpawnVisuals();
    }

    List<Vector2Int> BuildSnakeBody(Vector2Int head, int length,
        List<Vector2Int> occupied)
    {
        Vector2Int[] dirs = {
            Vector2Int.right, Vector2Int.up,
            Vector2Int.left,  Vector2Int.down
        };

        foreach (Vector2Int dir in dirs)
        {
            List<Vector2Int> attempt = new List<Vector2Int> { head };
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

        List<Vector2Int> winding = new List<Vector2Int> { head };
        if (BuildWindingPath(winding, length, occupied))
            return winding;

        Debug.LogWarning($"SnakeRenderer: no room for snake of length {length} at {head}");
        List<Vector2Int> fallback = new List<Vector2Int>();
        for (int i = 0; i < length; i++) fallback.Add(head);
        return fallback;
    }

    bool BuildWindingPath(List<Vector2Int> body, int targetLength,
        List<Vector2Int> occupied)
    {
        if (body.Count == targetLength) return true;

        Vector2Int[] dirs = {
            Vector2Int.right, Vector2Int.up,
            Vector2Int.left,  Vector2Int.down
        };

        Vector2Int tip = body[body.Count - 1];

        foreach (Vector2Int dir in dirs)
        {
            Vector2Int next = tip + dir;
            if (!gridManager.IsInBounds(next.x, next.y)) continue;
            if (occupied.Contains(next))                  continue;
            if (body.Contains(next))                      continue;

            body.Add(next);
            if (BuildWindingPath(body, targetLength, occupied)) return true;
            body.RemoveAt(body.Count - 1);
        }

        return false;
    }

    void Start()
    {
        if (initialised) return;
        gridManager     = FindAnyObjectByType<GridManager>();
        gameManager     = FindAnyObjectByType<GameManager>();
        audioManager    = FindAnyObjectByType<AudioManager>();
        cellSize        = gridManager.cellSize;
        cells           = new List<Vector2Int> { Vector2Int.zero };
        visualPositions = new List<Vector3> { CellToWorld(Vector2Int.zero) };
        SpawnVisuals();
    }

    // -------------------------------------------------------------------------
    // Per-frame smooth animation + eye tracking
    // -------------------------------------------------------------------------

    void Update()
    {
        if (!initialised || cells.Count == 0) return;

        bool anyMoving = false;
        float step     = cellSize * segmentMoveSpeed * Time.deltaTime;

        for (int i = 0; i < cells.Count && i < visualPositions.Count; i++)
        {
            Vector3 target = CellToWorld(cells[i]);
            Vector3 newPos = Vector3.MoveTowards(visualPositions[i], target, step);
            visualPositions[i] = newPos;

            if (Vector3.Distance(newPos, target) > 0.001f) anyMoving = true;
        }

        IsAnimating = anyMoving;
        UpdateLineRendererPositions();
        UpdateEyes();
    }

    // -------------------------------------------------------------------------
    // Visual construction
    // -------------------------------------------------------------------------

    void SpawnVisuals()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        eyeL = eyeR = pupilL = pupilR = null;

        // Shadow
        GameObject shadowObj = new GameObject("SnakeShadow");
        shadowObj.transform.SetParent(transform, false);
        shadowLR = shadowObj.AddComponent<LineRenderer>();
        ApplyLineRendererSettings(shadowLR, sortingOrder: 1);
        ApplyShadowStyle();

        // Body
        GameObject bodyObj = new GameObject("SnakeBody");
        bodyObj.transform.SetParent(transform, false);
        bodyLR = bodyObj.AddComponent<LineRenderer>();
        ApplyLineRendererSettings(bodyLR, sortingOrder: 2);
        ApplyBodyStyle();

        SpawnEyes();
        UpdateLineRendererPositions();
        UpdateEyes();
    }

    // Creates two eyes (white circle + dark pupil) as persistent child objects
    void SpawnEyes()
    {
        float eyeSize   = cellSize * 0.16f;
        float pupilSize = cellSize * 0.09f;

        eyeL   = MakeEyePart("EyeL",   eyeSize,   Color.white,                    sortingOrder: 3);
        eyeR   = MakeEyePart("EyeR",   eyeSize,   Color.white,                    sortingOrder: 3);
        pupilL = MakeEyePart("PupilL", pupilSize, new Color(0.1f, 0.1f, 0.1f),   sortingOrder: 4);
        pupilR = MakeEyePart("PupilR", pupilSize, new Color(0.1f, 0.1f, 0.1f),   sortingOrder: 4);
    }

    Transform MakeEyePart(string partName, float size, Color colour, int sortingOrder)
    {
        GameObject obj = new GameObject(partName);
        obj.transform.SetParent(transform, false);
        obj.transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = CreateFilledCircleSprite();
        sr.color        = colour;
        sr.sortingOrder = sortingOrder;

        return obj.transform;
    }

    // Positions eyes at the head, perpendicular to the movement direction
    void UpdateEyes()
    {
        if (eyeL == null || visualPositions.Count == 0) return;

        // Direction: derived from visual positions for smooth rotation
        if (visualPositions.Count >= 2)
        {
            Vector3 h = visualPositions[0];
            Vector3 n = visualPositions[1];
            Vector2 d = new Vector2(h.x - n.x, h.y - n.y);
            if (d.magnitude > cellSize * 0.05f)
                lastHeadDir = d.normalized;
        }

        Vector2 fwd  = lastHeadDir;
        Vector2 perp = new Vector2(-fwd.y, fwd.x);

        Vector3 headPos = visualPositions[0];
        float   eyeZ    = headPos.z - 0.2f;

        // Eye whites: offset forward and to each side
        float fwdOff  = cellSize * 0.14f;
        float sideOff = cellSize * 0.21f;

        Vector3 eyeCentre = headPos + new Vector3(fwd.x * fwdOff, fwd.y * fwdOff, 0f);
        eyeCentre.z = eyeZ;

        eyeL.position = eyeCentre + new Vector3( perp.x * sideOff,  perp.y * sideOff, 0f);
        eyeR.position = eyeCentre + new Vector3(-perp.x * sideOff, -perp.y * sideOff, 0f);

        // Pupils: slightly forward of eye centre for a "looking ahead" feel
        float pupilFwd = cellSize * 0.03f;
        Vector3 pOff   = new Vector3(fwd.x * pupilFwd, fwd.y * pupilFwd, -0.05f);
        pupilL.position = eyeL.position + pOff;
        pupilR.position = eyeR.position + pOff;
    }

    // -------------------------------------------------------------------------
    // LineRenderer setup
    // -------------------------------------------------------------------------

    void ApplyLineRendererSettings(LineRenderer lr, int sortingOrder)
    {
        lr.useWorldSpace     = true;
        lr.numCapVertices    = 10;
        lr.numCornerVertices = 8;
        lr.alignment         = LineAlignment.TransformZ;
        lr.positionCount     = cells.Count;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.sortingOrder      = sortingOrder;
        lr.material          = new Material(Shader.Find("Sprites/Default"));
    }

    void ApplyBodyStyle()
    {
        // Uniform body width, then pronounced taper over the last ~30% to near-point
        AnimationCurve w = new AnimationCurve();
        w.AddKey(Flat(0f,    cellSize * 0.84f)); // head — slight bump
        w.AddKey(Flat(0.08f, cellSize * 0.80f)); // body starts
        w.AddKey(Flat(0.70f, cellSize * 0.80f)); // body ends — taper begins
        w.AddKey(Flat(0.88f, cellSize * 0.42f)); // mid-taper
        w.AddKey(Flat(1f,    cellSize * 0.08f)); // tail tip — near-point
        bodyLR.widthCurve = w;

        ApplyBodyGradient();
    }

    void ApplyShadowStyle()
    {
        AnimationCurve w = new AnimationCurve();
        w.AddKey(Flat(0f,    cellSize * 0.84f * 1.20f));
        w.AddKey(Flat(0.70f, cellSize * 0.80f * 1.20f));
        w.AddKey(Flat(1f,    cellSize * 0.08f * 1.20f)); // taper matches body
        shadowLR.widthCurve = w;

        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.black, 0f),
                    new GradientColorKey(Color.black, 1f) },
            new[] { new GradientAlphaKey(0.28f, 0f),
                    new GradientAlphaKey(0.10f, 0.85f), // fade out toward tail tip
                    new GradientAlphaKey(0f,    1f) }
        );
        shadowLR.colorGradient = g;
    }

    void ApplyBodyGradient()
    {
        if (bodyLR == null) return;

        Color headCol = isSelected
            ? Color.Lerp(snakeColour, Color.white, 0.55f)
            : Color.Lerp(snakeColour, Color.white, 0.18f);

        Color tailCol = isSelected
            ? Color.Lerp(snakeColour, Color.white, 0.30f)
            : Color.Lerp(snakeColour, Color.black, 0.30f);

        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(headCol,     0f),
                new GradientColorKey(snakeColour, 0.18f),
                new GradientColorKey(snakeColour, 0.70f),
                new GradientColorKey(tailCol,     1f)
            },
            new[] {
                new GradientAlphaKey(1f,    0f),
                new GradientAlphaKey(1f,    0.80f),
                new GradientAlphaKey(0.85f, 1f)   // slight fade at tip
            }
        );
        bodyLR.colorGradient = g;
    }

    void UpdateLineRendererPositions()
    {
        if (bodyLR == null || shadowLR == null) return;
        Vector3 sOff = ShadowOffset();
        for (int i = 0; i < visualPositions.Count; i++)
        {
            bodyLR.SetPosition(i,   visualPositions[i]);
            shadowLR.SetPosition(i, visualPositions[i] + sOff);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    Vector3 CellToWorld(Vector2Int cell) =>
        new Vector3(cell.x * cellSize, cell.y * cellSize, -1f);

    Vector3 ShadowOffset() =>
        new Vector3(cellSize * 0.07f, -cellSize * 0.07f, 0.5f);

    static Keyframe Flat(float t, float v) => new Keyframe(t, v, 0f, 0f);

    // Procedural filled circle sprite (64 px, anti-aliased edge)
    Sprite CreateFilledCircleSprite()
    {
        const int size = 32;
        Texture2D tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float half = size / 2f;
        float r    = half - 0.5f;

        for (int px = 0; px < size; px++)
        {
            for (int py = 0; py < size; py++)
            {
                float dx    = px - half + 0.5f;
                float dy    = py - half + 0.5f;
                float dist  = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(r - dist + 0.5f);
                tex.SetPixel(px, py, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size);
    }

    // -------------------------------------------------------------------------
    // Public selection API
    // -------------------------------------------------------------------------

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyBodyGradient();
    }

    public void SetActiveEndToTail()
    {
        cells.Reverse();
        visualPositions.Reverse();
        lastHeadDir = -lastHeadDir;
        ApplyBodyGradient();
    }

    public bool IsHeadCell(int x, int y) =>
        cells.Count > 0 && cells[0] == new Vector2Int(x, y);

    public bool IsTailCell(int x, int y) =>
        cells.Count > 1 && cells[cells.Count - 1] == new Vector2Int(x, y);

    // -------------------------------------------------------------------------
    // Public query API
    // -------------------------------------------------------------------------

    public bool OccupiesCell(int x, int y) =>
        cells.Contains(new Vector2Int(x, y));

    public List<Vector2Int> GetAllCells()  => new List<Vector2Int>(cells);
    public Vector2Int       GetHeadCell()  => cells[0];
    public Color            GetColour()    => snakeColour;

    // -------------------------------------------------------------------------
    // Movement
    // -------------------------------------------------------------------------

    public void MoveAlongLine(int targetX, int targetY, GridManager gm)
    {
        Vector2Int head = cells[0];

        int dx = 0, dy = 0;
        if (targetX != head.x) dx = (targetX > head.x) ? 1 : -1;
        if (targetY != head.y) dy = (targetY > head.y) ? 1 : -1;

        Vector2Int current = head;
        bool moved = false;

        while (true)
        {
            Vector2Int next = new Vector2Int(current.x + dx, current.y + dy);

            if (dx != 0 && ((dx > 0 && next.x > targetX) || (dx < 0 && next.x < targetX))) break;
            if (dy != 0 && ((dy > 0 && next.y > targetY) || (dy < 0 && next.y < targetY))) break;

            if (!gm.IsInBounds(next.x, next.y)) break;
            if (gm.GetCell(next.x, next.y) == GridManager.CellType.Wall) break;

            bool selfBlock = false;
            for (int i = 0; i < cells.Count - 1; i++)
                if (cells[i] == next) { selfBlock = true; break; }
            if (selfBlock) break;

            SnakeRenderer[] allSnakes = FindObjectsByType<SnakeRenderer>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            bool otherBlock = false;
            foreach (SnakeRenderer other in allSnakes)
            {
                if (other == this) continue;
                if (other.OccupiesCell(next.x, next.y)) { otherBlock = true; break; }
            }
            if (otherBlock) break;

            cells.Insert(0, next);
            cells.RemoveAt(cells.Count - 1);

            visualPositions.Insert(0, visualPositions[0]);
            visualPositions.RemoveAt(visualPositions.Count - 1);

            current = next;
            moved   = true;
        }

        if (moved) audioManager?.PlayMove();
        CheckExitReached();
    }

    public void Retract()
    {
        if (cells.Count <= 1) return;
        cells.RemoveAt(0);
        visualPositions.RemoveAt(0);
        if (bodyLR)   bodyLR.positionCount   = cells.Count;
        if (shadowLR) shadowLR.positionCount = cells.Count;
    }

    void CheckExitReached()
    {
        ExitHole[] allExits = FindObjectsByType<ExitHole>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (ExitHole exit in allExits)
        {
            if (!exit.MatchesSnake(snakeColour)) continue;
            if (cells[0] != exit.gridPosition)   continue;

            SpawnEscapeParticles(exit.gridPosition);
            audioManager?.PlayEscape();
            Destroy(exit.gameObject);
            Destroy(gameObject);
            gameManager.CheckWin();
            return;
        }
    }

    // -------------------------------------------------------------------------
    // Particle burst on escape
    // -------------------------------------------------------------------------

    void SpawnEscapeParticles(Vector2Int gridPos)
    {
        Vector3 worldPos = new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, -2f);

        GameObject psObj = new GameObject("EscapeParticles");
        psObj.transform.position = worldPos;

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        // Stop immediately — AddComponent triggers playOnAwake by default,
        // and Unity won't allow changing duration on a playing system.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main           = ps.main;
        main.playOnAwake   = false;
        main.duration      = 0.4f;
        main.loop          = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(2.5f, 6f);
        main.startSize     = new ParticleSystem.MinMaxCurve(
            cellSize * 0.10f, cellSize * 0.28f);
        main.startColor    = new ParticleSystem.MinMaxGradient(
            Color.Lerp(snakeColour, Color.white,  0.4f),
            Color.Lerp(snakeColour, Color.yellow, 0.5f));
        main.gravityModifier = 0.4f;
        main.maxParticles    = 30;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)25) });

        var shape       = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = cellSize * 0.3f;

        var psr          = psObj.GetComponent<ParticleSystemRenderer>();
        psr.material     = new Material(Shader.Find("Sprites/Default"));
        psr.sortingOrder = 5;

        ps.Play();
        Destroy(psObj, 2f);
    }
}
