// AD INTEGRATION NOTE:
// Banner ad zone is reserved at bottom 12% of screen (anchorMin.y=0, anchorMax.y=0.12)
// To add Google AdMob:
//   1. Import Google Mobile Ads Unity Plugin from:
//      https://github.com/googleads/googleads-mobile-unity/releases
//   2. Replace the BannerAdZone placeholder Image with AdMob banner
//   3. Add interstitial ad call in GameManager.NextLevel() before scene reload
//   4. iOS requires ATT permission prompt — add via iOS post-process build script
// Interstitial: call between levels in GameManager.NextLevel()
// Show every 3-5 levels to avoid user fatigue
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

[DefaultExecutionOrder(-20)]
public class UIManager : MonoBehaviour
{
    private GameManager         gameManager;
    private TextMeshProUGUI     levelLabel;
    private GameObject          winPanel;
    private TextMeshProUGUI     winLabel;
    private Canvas              uiCanvas;
    private TMP_FontAsset       defaultFont;

    public bool IsReady => uiCanvas != null && winPanel != null && levelLabel != null;

    // -------------------------------------------------------------------------
    // Lifecycle — canvas built once in Awake, never rebuilt
    // -------------------------------------------------------------------------

    void Awake()
    {
        defaultFont = LoadFont();
        EnsureEventSystem();
        BuildCanvas();
        Debug.Log($"[UIManager] Awake complete — IsReady={IsReady}");
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void Initialise(GameManager gm)
    {
        gameManager        = gm;
        levelLabel.text    = $"Level {gm.currentLevel}";
        Debug.Log($"[UIManager] Initialise — Level {gm.currentLevel}");
    }

    public void SetLevelLabel(int level)
    {
        levelLabel.text = $"Level {level}";
        Debug.Log($"[UIManager] SetLevelLabel({level})");
    }

    public void ShowWinPanel(int levelJustCompleted)
    {
        if (winPanel == null)
        {
            Debug.LogError("[UIManager] ShowWinPanel — winPanel is null");
            return;
        }

        winLabel.text = levelJustCompleted >= 999
            ? "You Win!\nAll 999 Levels Complete!"
            : $"Level {levelJustCompleted}\nComplete!";

        winPanel.SetActive(true);
        Debug.Log($"[UIManager] ShowWinPanel({levelJustCompleted}) — shown");
    }

    // -------------------------------------------------------------------------
    // Font loading
    // -------------------------------------------------------------------------

    TMP_FontAsset LoadFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(
            "Fonts & Materials/LiberationSans SDF");

        if (font == null)
        {
            TMP_FontAsset[] all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (all.Length > 0) font = all[0];
        }

        if (font == null)
            Debug.LogError("[UIManager] No TMP font — " +
                "Window > TextMeshPro > Import TMP Essential Resources");

        return font;
    }

    // -------------------------------------------------------------------------
    // Event system
    // -------------------------------------------------------------------------

    void EnsureEventSystem()
    {
        EventSystem es = FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject obj = new GameObject("EventSystem");
            obj.AddComponent<EventSystem>();
            obj.AddComponent<InputSystemUIInputModule>();
            return;
        }
        StandaloneInputModule legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null)
        {
            Destroy(legacy);
            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    // -------------------------------------------------------------------------
    // Canvas — built once in Awake, never destroyed or rebuilt
    // -------------------------------------------------------------------------

    void BuildCanvas()
    {
        GameObject canvasObj = new GameObject("UICanvas");
        canvasObj.transform.SetParent(transform, false);

        uiCanvas              = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 999;

        CanvasScaler scaler        = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(750f, 1334f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Transform root = canvasObj.transform;
        BuildHUD(root);
        winPanel = BuildWinPanel(root);
        winPanel.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // HUD
    // -------------------------------------------------------------------------

    void BuildHUD(Transform root)
    {
        // Dark strip — top 8% of screen
        GameObject hudBg   = NewRect("HUDBackground", root);
        RectTransform bgRt = hudBg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.92f);
        bgRt.anchorMax = new Vector2(1f, 1.00f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image hudImg         = hudBg.AddComponent<Image>();
        hudImg.color         = new Color(0.08f, 0.10f, 0.14f, 1f);
        hudImg.raycastTarget = false;

        // Level label — left half of HUD strip
        GameObject labelObj   = NewRect("LevelLabel", root);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.02f, 0.92f);
        labelRt.anchorMax = new Vector2(0.50f, 1.00f);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        levelLabel               = MakeTMP(labelObj);
        levelLabel.text          = "Level 1";
        levelLabel.fontSize      = 34f;
        levelLabel.fontStyle     = FontStyles.Bold;
        levelLabel.alignment     = TextAlignmentOptions.MidlineLeft;
        levelLabel.color         = Color.white;
        levelLabel.raycastTarget = false;

        // Replay button
        MakeHUDButton(root, "Replay",
            new Vector2(0.52f, 0.925f), new Vector2(0.74f, 0.995f),
            new Color(0.25f, 0.28f, 0.35f, 1f), 24f,
            () => gameManager?.ResetLevel());

        // Reset button
        MakeHUDButton(root, "Reset",
            new Vector2(0.76f, 0.925f), new Vector2(0.98f, 0.995f),
            new Color(0.55f, 0.20f, 0.20f, 1f), 22f,
            () => gameManager?.ResetAllProgress());

        // Dark strip — bottom 12% reserved for banner ad
        GameObject adBg   = NewRect("BannerAdZone", root);
        RectTransform adRt = adBg.GetComponent<RectTransform>();
        adRt.anchorMin = new Vector2(0f, 0.00f);
        adRt.anchorMax = new Vector2(1f, 0.12f);
        adRt.offsetMin = Vector2.zero;
        adRt.offsetMax = Vector2.zero;
        Image adImg         = adBg.AddComponent<Image>();
        adImg.color         = new Color(0.08f, 0.08f, 0.10f, 1f);
        adImg.raycastTarget = false;

        GameObject adLabel = NewRect("AdPlaceholder", adBg.transform);
        Stretch(adLabel.GetComponent<RectTransform>());
        TextMeshProUGUI adTmp = MakeTMP(adLabel);
        adTmp.text           = "ADVERTISEMENT";
        adTmp.fontSize       = 18f;
        adTmp.alignment      = TextAlignmentOptions.Center;
        adTmp.color          = new Color(0.4f, 0.4f, 0.4f, 1f);
        adTmp.raycastTarget  = false;
    }

    void MakeHUDButton(Transform root, string label,
        Vector2 anchorMin, Vector2 anchorMax,
        Color colour, float fontSize,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btn    = NewRect(label + "Btn", root);
        RectTransform rt  = btn.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img        = btn.AddComponent<Image>();
        img.color        = colour;
        Button button    = btn.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        GameObject textObj = NewRect("Label", btn.transform);
        Stretch(textObj.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = MakeTMP(textObj);
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
    }

    // -------------------------------------------------------------------------
    // Win panel
    // -------------------------------------------------------------------------

    GameObject BuildWinPanel(Transform root)
    {
        // Full-screen dark overlay
        GameObject panel = NewRect("WinPanel", root);
        Stretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

        // Centred card
        GameObject card    = NewRect("Card", panel.transform);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin        = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax        = new Vector2(0.5f, 0.5f);
        cardRt.pivot            = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta        = new Vector2(650f, 480f);
        cardRt.anchoredPosition = Vector2.zero;
        card.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.13f, 1f);

        // Win text — top 60% of card
        GameObject textObj   = NewRect("WinText", card.transform);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.05f, 0.40f);
        textRt.anchorMax = new Vector2(0.95f, 0.95f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        winLabel           = MakeTMP(textObj);
        winLabel.text      = "Level Complete!";
        winLabel.fontSize  = 58f;
        winLabel.fontStyle = FontStyles.Bold;
        winLabel.alignment = TextAlignmentOptions.Center;
        winLabel.color     = Color.white;

        // Next Level button — bottom 25% of card
        GameObject btn    = NewRect("NextBtn", card.transform);
        RectTransform btnRt = btn.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.12f, 0.06f);
        btnRt.anchorMax = new Vector2(0.88f, 0.30f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;
        Image btnImg         = btn.AddComponent<Image>();
        btnImg.color         = new Color(0.29f, 0.56f, 0.85f);
        Button button        = btn.AddComponent<Button>();
        button.targetGraphic = btnImg;
        button.onClick.AddListener(() => gameManager?.NextLevel());

        GameObject btnTextObj = NewRect("Label", btn.transform);
        Stretch(btnTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI btnTmp = MakeTMP(btnTextObj);
        btnTmp.text      = "Next Level";
        btnTmp.fontSize  = 46f;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color     = Color.white;

        return panel;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    TextMeshProUGUI MakeTMP(GameObject obj)
    {
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) tmp.font = defaultFont;
        return tmp;
    }

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
}
