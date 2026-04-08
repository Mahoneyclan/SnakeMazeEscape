using UnityEngine;
using System.Collections.Generic;

public class SnakeRenderer : MonoBehaviour
{
    [Header("Snake Settings")]
    public Color snakeColour = new Color(0.91f, 0.36f, 0.29f); // Coral red
    public float cellSize = 1f;

    private List<Vector2Int> cells = new List<Vector2Int>();
    private List<GameObject> segments = new List<GameObject>();

    // Test snake — head at (0,0), body going right
    private List<Vector2Int> testSnakeCells = new List<Vector2Int>
    {
        new Vector2Int(0, 0), // head
        new Vector2Int(1, 0),
        new Vector2Int(2, 0), // tail
    };

    void Start()
    {
        cells = new List<Vector2Int>(testSnakeCells);
        RenderSnake();
    }

    void RenderSnake()
    {
        // Clear old segments
        foreach (GameObject seg in segments)
            Destroy(seg);
        segments.Clear();

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int pos = cells[i];

            GameObject seg = new GameObject($"Segment_{i}");
            seg.transform.parent = this.transform;
            seg.transform.position = new Vector3(pos.x * cellSize, pos.y * cellSize, -1f);

            SpriteRenderer sr = seg.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = snakeColour;

            // Head is slightly darker
            if (i == 0)
                sr.color = new Color(snakeColour.r * 0.8f, snakeColour.g * 0.8f, snakeColour.b * 0.8f);

            seg.transform.localScale = new Vector3(cellSize * 0.85f, cellSize * 0.85f, 1f);
            segments.Add(seg);
        }
    }

    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}