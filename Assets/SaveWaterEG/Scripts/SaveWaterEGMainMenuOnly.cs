using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SaveWaterEGMainMenuOnly : MonoBehaviour
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    // Hit-button size (matches the buttons drawn in the background image)
    private static readonly Vector2 HitButtonSize = new Vector2(320f, 280f);

    // Set to true to see the clickable areas as red boxes (for adjusting)
    private const bool ShowHitAreas = false;

    private Canvas canvas;
    private GameObject activePage;
    private TMP_FontAsset arabicFont;
    private Sprite roundedSprite;

    private void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        arabicFont = Resources.Load<TMP_FontAsset>("CairoSDF");
        if (arabicFont == null)
            Debug.LogError("SaveWaterEG: Missing Resources/CairoSDF font asset");

        Texture2D bgTexture = Resources.Load<Texture2D>("SaveWaterEG_Menu_Scenery");
        if (bgTexture == null)
        {
            Debug.LogError("SaveWaterEG: Missing Resources/SaveWaterEG_Menu_Scenery");
            return;
        }

        Texture2D buttonTexture = Resources.Load<Texture2D>("RoundedButton");
        if (buttonTexture != null)
        {
            roundedSprite = Sprite.Create(buttonTexture,
                new Rect(0, 0, buttonTexture.width, buttonTexture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(48, 48, 48, 48));
        }

        EnsureEventSystem();
        BuildMenu(bgTexture);
    }

    private void BuildMenu(Texture2D bgTexture)
    {
        GameObject canvasObject = new GameObject("SaveWaterEG_Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        Sprite bgSprite = Sprite.Create(bgTexture,
            new Rect(0, 0, bgTexture.width, bgTexture.height),
            new Vector2(0.5f, 0.5f), 100f);

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(canvas.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        Image bg = background.GetComponent<Image>();
        bg.sprite = bgSprite;
        bg.preserveAspect = false;
        bg.raycastTarget = false;

        // Transparent buttons placed over the buttons drawn in the background image.
        CreateHitButton("InformationButton", new Vector2(-70f, -125f),
            () => OpenPage("المعلومات", "صفحة المعلومات والخريطة"));
        CreateHitButton("ChatbotButton", new Vector2(290f, -125f),
            () => OpenPage("الشات بوت", "صفحة التفاعل مع المساعد دروبي"));
        CreateHitButton("CharactersButton", new Vector2(650f, -125f),
            () => OpenPage("الشخصيات", "صفحة شخصيات اللعبة"));
    }

    private void CreateHitButton(string objectName, Vector2 position,
        UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = HitButtonSize;
        rt.anchoredPosition = position;

        Image image = go.GetComponent<Image>();
        image.color = ShowHitAreas
            ? new Color(1f, 0f, 0f, 0.4f)
            : new Color(1f, 1f, 1f, 0f); // invisible but clickable
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
    }

    private void OpenPage(string title, string description)
    {
        ClosePage();

        activePage = new GameObject(title + "Panel", typeof(RectTransform), typeof(Image));
        activePage.transform.SetParent(canvas.transform, false);
        Stretch(activePage.GetComponent<RectTransform>());
        activePage.GetComponent<Image>().color = new Color(0.01f, 0.08f, 0.18f, 0.82f);

        GameObject panel = CreatePanel("ContentPanel", activePage.transform,
            Vector2.zero, new Vector2(1050f, 650f), new Color(0.88f, 0.97f, 1f, 1f));

        AddLabel(panel.transform, title, 60, new Color(0.02f, 0.24f, 0.52f),
            new Vector2(0f, 165f), new Vector2(900f, 110f));
        AddLabel(panel.transform, description, 38, new Color(0.08f, 0.22f, 0.32f),
            new Vector2(0f, 10f), new Vector2(900f, 170f));

        CreateActionButton(panel.transform, "BackButton", "رجوع",
            new Vector2(0f, -215f), new Vector2(300f, 95f),
            new Color(0.02f, 0.52f, 0.82f), ClosePage);
    }

    private GameObject CreatePanel(string name, Transform parent,
        Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private void CreateActionButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;

        Image image = go.GetComponent<Image>();
        image.sprite = roundedSprite;
        image.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        AddLabel(go.transform, label, 36, Color.white, Vector2.zero, size);
    }

    private void AddLabel(Transform parent, string value, int fontSize,
        Color color, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (arabicFont != null) text.font = arabicFont;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.isRightToLeftText = true;
        text.text = value;
    }

    private void ClosePage()
    {
        if (activePage != null) Destroy(activePage);
        activePage = null;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        var module = eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }
}