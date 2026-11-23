#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// أداة بتبني شاشة اللودينج أوتوماتيك: Droppy > Build Loading Scene
public static class BuildLoadingScene
{
    const string ScenePath = "Assets/_Project/Scenes/Loading.unity";
    const string MainMenuPath = "Assets/_Project/Scenes/MainMenu.unity";
    const string ArtDir = "Assets/_Project/Art/Loading/";

    [MenuItem("Droppy/Build Loading Scene")]
    public static void Build()
    {
        AssetDatabase.Refresh();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // نفتح شاشة Loading (أو نعمل واحدة جديدة لو مش موجودة)
        Scene scene;
        if (System.IO.File.Exists(ScenePath))
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // نمسح أي Canvas قديم ونبدأ نضيف من جديد
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Canvas" || go.name == "LoadingManager") Object.DestroyImmediate(go);

        // الكاميرا بلون أزرق عشان مايظهرش السما
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.55f, 0.9f);
        }

        Sprite bgSprite = LoadSprite(ArtDir + "loading_bg.png");
        Sprite droppySprite = LoadSprite(ArtDir + "droppy.png");
        Sprite textSprite = LoadSprite(ArtDir + "loading_text.png");

        // ---------- Canvas ----------
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // ---------- الخلفية (تملا الشاشة من غير تشويه) ----------
        var bg = NewImage("Background", canvasGO.transform, bgSprite);
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 1920f / 1080f;

        // ---------- Droppy ----------
        var droppy = NewImage("Droppy", canvasGO.transform, droppySprite);
        droppy.preserveAspect = true;
        var dRt = droppy.rectTransform;
        dRt.anchorMin = dRt.anchorMax = new Vector2(0.5f, 0.5f);
        dRt.pivot = new Vector2(0.5f, 0.5f);
        dRt.sizeDelta = new Vector2(640, 640);
        dRt.anchoredPosition = new Vector2(0, 130);
        droppy.gameObject.AddComponent<DroppyFloat>();

        // ---------- نص "جاري التحميل..." (صورة عشان العربي يظهر صح) ----------
        var txt = NewImage("LoadingText", canvasGO.transform, textSprite);
        txt.preserveAspect = true;
        var tRt = txt.rectTransform;
        tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 0.5f);
        tRt.pivot = new Vector2(0.5f, 0.5f);
        tRt.sizeDelta = new Vector2(520, 110);
        tRt.anchoredPosition = new Vector2(0, -250);

        // ---------- شريط التحميل ----------
        var barGO = new GameObject("ProgressBar", typeof(RectTransform), typeof(Slider));
        barGO.transform.SetParent(canvasGO.transform, false);
        var barRt = (RectTransform)barGO.transform;
        barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 0.5f);
        barRt.pivot = new Vector2(0.5f, 0.5f);
        barRt.sizeDelta = new Vector2(900, 42);
        barRt.anchoredPosition = new Vector2(0, -345);

        var barBg = NewImage("Track", barGO.transform, null);
        barBg.color = new Color(1f, 1f, 1f, 0.45f);
        Stretch(barBg.rectTransform);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(barGO.transform, false);
        Stretch((RectTransform)fillArea.transform);

        var fill = NewImage("Fill", fillArea.transform, null);
        fill.color = new Color(0.1f, 0.6f, 1f, 1f);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = new Vector2(0, 1);
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

        var slider = barGO.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = null;
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;
        slider.value = 0f;

        // ---------- المدير ----------
        var mgr = new GameObject("LoadingManager");
        var ls = mgr.AddComponent<LoadingScreen>();
        ls.progressBar = slider;
        ls.nextSceneName = "MainMenu";
        ls.loadDuration = 10f;

        // ---------- Build Settings: Loading أول واحدة ثم MainMenu ----------
        SetupBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("تمام! شاشة Loading اتبنت. اضغط Play وجرّب.");
    }

    static Image NewImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Sprite LoadSprite(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { Debug.LogError("مش لاقي الصورة: " + path); return null; }
        if (imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void SetupBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == ScenePath || s.path == MainMenuPath);
        list.Insert(0, new EditorBuildSettingsScene(MainMenuPath, true));
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
