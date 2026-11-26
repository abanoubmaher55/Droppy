using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SaveWaterEG
{
    // Quick pop-in animation (scale 0.9 -> 1 + fade) each time the object is enabled.
    public class PopIn : MonoBehaviour
    {
        float _t;
        void OnEnable() { _t = 0f; }
        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_t / 0.28f);
            float e = 1f - Mathf.Pow(1f - k, 3f);
            transform.localScale = Vector3.one * (0.9f + 0.1f * e);
            var cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = e;
            if (k >= 1f) { transform.localScale = Vector3.one; if (cg != null) cg.alpha = 1f; enabled = false; }
        }
    }

    // Gentle continuous scale pulse (for map markers and highlights).
    public class UIPulse : MonoBehaviour
    {
        public float amp = 0.08f;
        public float speed = 3f;
        Vector3 _base;
        void Start() { _base = transform.localScale; }
        void Update() { transform.localScale = _base * (1f + Mathf.Sin(Time.time * speed) * amp); }
    }

    // Small helpers to build uGUI screens from code (no prefabs needed).
    public static class UIFactory
    {
        static Font _font;
        public static Font GetFont()
        {
            if (_font == null)
            {
                // 1) Arabic font bundled by the setup script (Assets/Resources/Fonts/arabic)
                _font = Resources.Load<Font>("Fonts/arabic");
                if (_font == null)
                {
                    // 2) OS Arabic-capable font
                    try { _font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Noto Naskh Arabic", "Tahoma" }, 16); } catch { }
                }
                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                }
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return _font;
        }

        public static Sprite _white;
        public static Sprite WhiteSprite()
        {
            if (_white == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color32[16];
                for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(px); tex.Apply();
                _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                _white.name = "SWG_White";
            }
            return _white;
        }

        public static RectTransform Root(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var rt = Root(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = color;
            img.type = Image.Type.Sliced;
            return rt;
        }

        public static RectTransform Sub(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        public static Text Label(Transform parent, string name, string content, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rt = Sub(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = GetFont();
            t.text = ArabicText.Fix(content);
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;
            return t;
        }

        public static Button MakeButton(Transform parent, string name, string label, int fontSize, Color bg, Color fg, UnityAction onClick)
        {
            var rt = Sub(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = WhiteSprite(); img.color = bg; img.type = Image.Type.Sliced;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = bg * 1.1f;
            colors.pressedColor = bg * 0.85f;
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);
            if (!string.IsNullOrEmpty(label))
                Label(rt, "Txt", label, fontSize, fg);
            return btn;
        }

        // Positions a child with explicit normalized anchor rect inside its parent.
        public static RectTransform Place(Transform parent, string name, float xMin, float yMin, float xMax, float yMax)
        {
            return Sub(parent, name, new Vector2(xMin, yMin), new Vector2(xMax, yMax), Vector2.zero, Vector2.zero);
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, bool preserveAspect = true)
        {
            var rt = Sub(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color;
            img.preserveAspect = preserveAspect;
            return img;
        }

        // Adds a quick pop-in (scale + fade) that plays when the object is enabled.
        public static void AddPopIn(GameObject go)
        {
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
            if (go.GetComponent<PopIn>() == null) go.AddComponent<PopIn>();
        }

        public static void AddPulse(GameObject go, float amp = 0.08f, float speed = 3f)
        {
            var p = go.AddComponent<UIPulse>();
            p.amp = amp; p.speed = speed;
        }

        // Creates a camera with an AudioListener if the scene has none.
        // (UI scenes use overlay canvases, but sounds and world rendering need this.)
        public static Camera EnsureCamera(Color? background = null)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
                cam.transform.position = new Vector3(0, 0, -10);
            }
            if (go_hasListener(cam) == false) cam.gameObject.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background ?? new Color(0.35f, 0.72f, 0.95f);
            return cam;
        }

        static bool go_hasListener(Camera cam) { return cam.GetComponent<AudioListener>() != null; }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        public static Canvas EnsureCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            return c;
        }

        // Invisible-but-clickable overlay button (for transparent buttons over artwork).
        public static Button GhostButton(Transform parent, string name, float xMin, float yMin, float xMax, float yMax, UnityAction onClick)
        {
            var rt = Place(parent, name, xMin, yMin, xMax, yMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = new Color(1, 1, 1, 0.001f); // ~invisible, still raycastable
            var btn = rt.gameObject.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        // Wrap long Arabic-ish text roughly by characters (simple word wrap for chat bubbles).
        public static string Wrap(string s, int maxChars)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= maxChars) return s;
            var words = s.Split(' ');
            var sb = new System.Text.StringBuilder();
            int line = 0;
            foreach (var w in words)
            {
                if (line + w.Length > maxChars && line > 0) { sb.Append('\n'); line = 0; }
                else if (line > 0) { sb.Append(' '); line += 1; }
                sb.Append(w); line += w.Length;
            }
            return sb.ToString();
        }
    }
}
