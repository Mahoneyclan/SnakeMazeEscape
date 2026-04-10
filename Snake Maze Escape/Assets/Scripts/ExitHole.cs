using UnityEngine;
using System.Collections.Generic;

// ExitHole represents the destination cell for a specific snake.
// It renders as a coloured ring on the grid at a randomly chosen
// interior cell. Position is assigned automatically by GameManager.
// Only the snake whose colour matches can enter it —
// all other snakes treat it as passable (exit holes don't block other snakes
// at the grid data level — blocking is handled in SnakeRenderer).

public class ExitHole : MonoBehaviour
{
    [Header("Exit Hole Settings")]
    // Colour set by GameManager to match a specific snake
    public Color exitColour = new Color(0.91f, 0.36f, 0.29f);

    // Grid position — assigned automatically, not set in Inspector
    public Vector2Int gridPosition { get; private set; }

    // Cell size read from GridManager
    private float cellSize;

    // SpriteRenderer for the ring visual
    private SpriteRenderer sr;

    // Generates a circle ring texture procedurally
    // Ring is white — colour applied via SpriteRenderer.color
    Sprite CreateCircleSprite()
    {
        int size = 64;
        // RGBA32 required — without it Unity 6 may drop the alpha channel,
        // making Color.clear pixels render as opaque black
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 centre = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int px = 0; px < size; px++)
        {
            for (int py = 0; py < size; py++)
            {
                float dist = Vector2.Distance(new Vector2(px, py), centre);
                float outerRing = radius;
                float innerVoid = radius * 0.55f;

                // Pixel is part of ring if between inner and outer radius
                if (dist <= outerRing && dist >= innerVoid)
                    tex.SetPixel(px, py, Color.white);
                else
                    tex.SetPixel(px, py, Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size);
    }

    // Called by GameManager when the generator has pre-computed the position.
    // Renders identically to Initialise() but skips random cell selection.
    public void InitialiseAt(Color colour, Vector2Int position, GridManager gm)
    {
        exitColour   = colour;
        cellSize     = gm.cellSize;
        gridPosition = position;

        transform.position = new Vector3(
            gridPosition.x * cellSize,
            gridPosition.y * cellSize,
            -0.5f);

        sr              = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = CreateCircleSprite();
        sr.color        = exitColour;
        sr.sortingOrder = 3;

        transform.localScale = new Vector3(cellSize * 0.7f, cellSize * 0.7f, 1f);
    }

    // Called by SnakeRenderer to verify colour match before allowing escape
    public bool MatchesSnake(Color snakeColour)
    {
        return Mathf.Approximately(exitColour.r, snakeColour.r) &&
               Mathf.Approximately(exitColour.g, snakeColour.g) &&
               Mathf.Approximately(exitColour.b, snakeColour.b);
    }
}