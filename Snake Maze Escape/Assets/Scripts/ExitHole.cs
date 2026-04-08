using UnityEngine;

public class ExitHole : MonoBehaviour
{
    public Color exitColour = new Color(0.91f, 0.36f, 0.29f);
    public Vector2Int gridPosition;
    public float cellSize = 1f;

    private SpriteRenderer sr;

    void Start()
    {
        transform.position = new Vector3(
            gridPosition.x * cellSize,
            gridPosition.y * cellSize,
            -0.5f
        );

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = exitColour;
        transform.localScale = new Vector3(cellSize * 0.7f, cellSize * 0.7f, 1f);
    }

    Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Vector2 centre = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int px = 0; px < size; px++)
        {
            for (int py = 0; py < size; py++)
            {
                float dist = Vector2.Distance(new Vector2(px, py), centre);
                float outerRing = radius;
                float innerVoid = radius * 0.55f;

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

    public bool MatchesSnake(Color snakeColour)
    {
        return Mathf.Approximately(exitColour.r, snakeColour.r) &&
               Mathf.Approximately(exitColour.g, snakeColour.g) &&
               Mathf.Approximately(exitColour.b, snakeColour.b);
    }
}