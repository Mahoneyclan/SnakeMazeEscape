using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

// UIManager builds all UI entirely in code — no prefabs or scene setup required.
// Uses TextMeshProUGUI for reliable text rendering in Unity 6.
//
// HUD (always visible):
//   Top-centre: "Level X" label
//
// Win panel (shown on level complete):
//   Dark overlay, centred card, level-complete text, Next Level + Replay buttons

public class UIManager : MonoBehaviour
{
    // Set by GameManager before the win panel can be shown
    private GameManager gameManager;

    // HUD elements
    private TextMeshProUGUI levelLabel;

    // Win panel root — toggled when all snakes escape
    private GameObject winPanel;
    private TextMeshProUGUI winLabel;

    // Stored canvas references for late camera binding and panel rebuilding
    private Transform canvasRoot;
    private Canvas    uiCanvas;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        try { EnsureEventSystem(); }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] EnsureEventSystem failed: {e.Message}");
        }
        BuildCanvas();
    }

    // Called by GameManager immediately after finding/creating this component
    public void Initialise(GameManager gm)
    {
        gameManager = gm;
    }

    // Updates the HUD level counter — called by GameManager after level loads
    public void SetLevelLabel(int level)
    {
        if (levelLabel != null)
            levelLabel.text = $"Level {level}";
    }

    // -------------------------------------------------------------------------
    // Public win panel API
    // -------------------------------------------------------------------------

    public void ShowWinPanel(int levelJustCompleted)
    {
        // winPanel can be destroyed if the scene is mid-reload — rebuild it
        if (winPanel == null && canvasRoot != null)
        {
            winPanel = BuildWinPanel(canvasRoot);
            winPanel.SetActive(false);
        }

        if (winPanel == null)
        {
            Debug.LogWarning("[UIManager] Cannot show win panel — canvas not ready");
            return;
        }

        bool lastLevel = levelJustCompleted >= 999;

        winLabel.text = lastLevel
            ? "You Win!\nAll Levels Complete!"
            : $"Level {levelJustCompleted}\nComplete!";

        winPanel.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Canvas construction
    // -------------------------------------------------------------------------

    void EnsureEventSystem()
    {
        EventSystem es = FindAnyObjectByType<EventSystem>();

        if (es == null)
        {
            // No EventSystem in scene — create one with the correct module
            GameObject obj = new GameObject("EventSystem");
            obj.AddComponent<EventSystem>();
            obj.AddComponent<InputSystemUIInputModule>();
            return;
        }

        // EventSystem exists — swap out StandaloneInputModule if present
        StandaloneInputModule legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null)
        {
            Destroy(legacy);
            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    // Stored canvas reference so we can bind the camera in Start()
    void BuildCanvas()
    {
        GameObject canvasObj = new GameObject("UICanvas");
        canvasObj.transform.SetParent(transform, false);

        // ScreenSpaceCamera is required for URP 2D renderer.
        // ScreenSpaceOverlay is drawn before the URP 2D post-processing pass
        // and gets overwritten, making it invisible. ScreenSpaceCamera renders
        // after the scene and stays on top correctly.
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode    = RenderMode.ScreenSpaceCamera;
        uiCanvas.planeDistance = 1f;
        uiCanvas.sortingOrder  = 100;
        // Camera.main should be available since the Camera scene object's
        // Awake() runs before UIManager.Awake() — bind it now if possible,
        // otherwise fall back to Start().
        if (Camera.main != null)
            uiCanvas.worldCamera = Camera.main;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(750f, 1334f); // iOS Classic baseline
        scaler.matchWidthOrHeight  = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Transform root = canvasObj.transform;
        canvasRoot = root;

        BuildHUD(root);

        winPanel = BuildWinPanel(root);
        winPanel.SetActive(false);
    }

    void Start()
    {
        // Fallback: bind the camera if it wasn't available during Awake()
        if (uiCanvas != null && uiCanvas.worldCamera == null && Camera.main != null)
            uiCanvas.worldCamera = Camera.main;
    }

    // -------------------------------------------------------------------------
    // HUD — always visible
    // -------------------------------------------------------------------------

    void BuildHUD(Transform root)
    {
        // Dark background strip across the full top — guarantees text is readable
        GameObject hudBg = NewRect("HUDBackground", root);
        RectTransform hudBgRt = hudBg.GetComponent<RectTransform>();
        hudBgRt.anchorMin = new Vector2(0f, 0.91f);
        hudBgRt.anchorMax = new Vector2(1f, 1.00f);
        hudBgRt.offsetMin = Vector2.zero;
        hudBgRt.offsetMax = Vector2.zero;
        hudBg.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.92f);

        // Level label — top-left of screen
        GameObject labelObj = NewRect("LevelLabel", root);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.91f);
        labelRt.anchorMax = new Vector2(0.52f, 1.00f);
        labelRt.offsetMin = new Vector2(20f, 0f);
        labelRt.offsetMax = Vector2.zero;

        levelLabel = labelObj.AddComponent<TextMeshProUGUI>();
        levelLabel.text      = "Level 1";
        levelLabel.fontSize   = 58f;
        levelLabel.fontStyle  = FontStyles.Bold;
        levelLabel.alignment  = TextAlignmentOptions.MidlineLeft;
        levelLabel.color      = Color.white;

        // Replay button — top-right, resets current level
        GameObject replayBtn = NewRect("HUDReplayBtn", root);
        RectTransform replayRt = replayBtn.GetComponent<RectTransform>();
        replayRt.anchorMin = new Vector2(0.54f, 0.91f);
        replayRt.anchorMax = new Vector2(0.76f, 0.99f);
        replayRt.offsetMin = Vector2.zero;
        replayRt.offsetMax = Vector2.zero;

        Image replayImg  = replayBtn.AddComponent<Image>();
        replayImg.color  = new Color(0.25f, 0.25f, 0.30f, 0.90f);

        Button replayButton = replayBtn.AddComponent<Button>();
        replayButton.targetGraphic = replayImg;
        replayButton.onClick.AddListener(() => gameManager.ResetLevel());

        GameObject replayTextObj = NewRect("Label", replayBtn.transform);
        Stretch(replayTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI replayTmp = replayTextObj.AddComponent<TextMeshProUGUI>();
        replayTmp.text     = "Replay";
        replayTmp.fontSize  = 42f;
        replayTmp.fontStyle = FontStyles.Bold;
        replayTmp.alignment = TextAlignmentOptions.Center;
        replayTmp.color     = Color.white;

        // Reset to Level 1 button — top-far-right
        GameObject resetBtn = NewRect("HUDResetBtn", root);
        RectTransform resetRt = resetBtn.GetComponent<RectTransform>();
        resetRt.anchorMin = new Vector2(0.78f, 0.91f);
        resetRt.anchorMax = new Vector2(0.97f, 0.99f);
        resetRt.offsetMin = Vector2.zero;
        resetRt.offsetMax = Vector2.zero;

        Image resetImg  = resetBtn.AddComponent<Image>();
        resetImg.color  = new Color(0.55f, 0.20f, 0.20f, 0.90f);

        Button resetButton = resetBtn.AddComponent<Button>();
        resetButton.targetGraphic = resetImg;
        resetButton.onClick.AddListener(() => gameManager.ResetAllProgress());

        GameObject resetTextObj = NewRect("Label", resetBtn.transform);
        Stretch(resetTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI resetTmp = resetTextObj.AddComponent<TextMeshProUGUI>();
        resetTmp.text     = "↺ L1";
        resetTmp.fontSize  = 38f;
        resetTmp.fontStyle = FontStyles.Bold;
        resetTmp.alignment = TextAlignmentOptions.Center;
        resetTmp.color     = Color.white;
    }

    // -------------------------------------------------------------------------
    // Win panel
    // -------------------------------------------------------------------------

    GameObject BuildWinPanel(Transform root)
    {
        // Full-screen dark overlay
        GameObject panel = NewRect("WinPanel", root);
        Stretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);

        // Centred card — fixed size in reference pixels
        GameObject card = NewRect("Card", panel.transform);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin        = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax        = new Vector2(0.5f, 0.5f);
        cardRt.pivot            = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta        = new Vector2(700f, 500f);
        cardRt.anchoredPosition = Vector2.zero;
        card.AddComponent<Image>().color = new Vector4(0.10f, 0.10f, 0.13f, 1f);

        // Win text — top 60% of card, using stretch anchors
        GameObject textObj = NewRect("WinText", card.transform);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.05f, 0.38f);
        textRt.anchorMax = new Vector2(0.95f, 0.95f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        winLabel = textObj.AddComponent<TextMeshProUGUI>();
        winLabel.text      = "Level Complete!";
        winLabel.fontSize   = 64f;
        winLabel.fontStyle  = FontStyles.Bold;
        winLabel.alignment  = TextAlignmentOptions.Center;
        winLabel.color      = Color.white;

        // Replay button — bottom 28% of card, using stretch anchors
        // Stretch anchors are more reliable than pixel offsets across resolutions
        GameObject btn = NewRect("ReplayBtn", card.transform);
        RectTransform btnRt = btn.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.15f, 0.06f);
        btnRt.anchorMax = new Vector2(0.85f, 0.28f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;

        Image btnImg   = btn.AddComponent<Image>();
        btnImg.color   = new Color(0.29f, 0.56f, 0.85f);

        Button button  = btn.AddComponent<Button>();
        button.targetGraphic = btnImg;
        button.onClick.AddListener(() => gameManager.ResetLevel());

        GameObject btnTextObj = NewRect("Label", btn.transform);
        Stretch(btnTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI btnTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnTmp.text      = "Replay";
        btnTmp.fontSize   = 52f;
        btnTmp.fontStyle  = FontStyles.Bold;
        btnTmp.alignment  = TextAlignmentOptions.Center;
        btnTmp.color      = Color.white;

        return panel;
    }

    // -------------------------------------------------------------------------
    // Widget helpers
    // -------------------------------------------------------------------------

    GameObject NewRect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // anchorPos: offset in pixels from the centre of the parent card
    GameObject MakeButton(Transform parent, string labelText,
        Vector2 anchorPos, Vector2 size, Color colour,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btn = NewRect(labelText + "Btn", parent);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = anchorPos;

        Image img = btn.AddComponent<Image>();
        img.color = colour;

        Button button = btn.AddComponent<Button>();
        button.targetGraphic = img;

        ColorBlock cb = button.colors;
        cb.highlightedColor = Color.Lerp(colour, Color.white, 0.2f);
        cb.pressedColor     = Color.Lerp(colour, Color.black, 0.2f);
        button.colors       = cb;

        button.onClick.AddListener(onClick);

        // Text label inside button
        GameObject textObj = NewRect("Label", btn.transform);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        Stretch(textRt);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = labelText;
        tmp.fontSize   = 44f;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.color      = Color.white;

        return btn;
    }
}
