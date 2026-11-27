using UnityEngine;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // Main menu. Two states:
    //  - Home: the user's menu art full-screen, three big REAL buttons
    //    (المعلومات / الشات بوت / الشخصيات) over the art's button row, animated Droppy.
    //  - Tab view: top bar (home + 3 tabs) with the tab content below.
    public class MainMenuController : MonoBehaviour
    {
        RectTransform _homePanel, _tabBar, _contentWrap;
        RectTransform _infoPanel, _chatPanel, _charsPanel, _extraPanel;
        Button _tabInfo, _tabChat, _tabChars, _tabExtra;

        // ===== 4th tab (empty for now). Change the name here, that's all. =====
        const string ExtraTabName = "المستويات";
        const string ExtraTabIcon = "🏆";
        static readonly Color ExtraOrange = new Color(0.98f, 0.55f, 0.12f);
        int _tab = -1;

        static readonly Color InfoBlue = new Color(0.10f, 0.55f, 0.95f);
        static readonly Color ChatGreen = new Color(0.20f, 0.75f, 0.30f);
        static readonly Color CharsPurple = new Color(0.55f, 0.30f, 0.85f);

        void Start()
        {
            UIFactory.EnsureCamera(new Color(0.42f, 0.76f, 0.96f));
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.EnsureCanvas("MenuCanvas");
            canvas.gameObject.AddComponent<UISoundManager>();
            canvas.gameObject.AddComponent<TransparentButtonFix>();
            var root = canvas.transform;

            // ---------- tab content (under the top bar) ----------
            _contentWrap = UIFactory.Place(root, "Content", 0.025f, 0.015f, 0.975f, 0.845f);
            UIFactory.Panel(_contentWrap, "sky", new Color(0.42f, 0.76f, 0.96f));
            _infoPanel = UIFactory.Root(_contentWrap, "InfoPanel");
            _infoPanel.gameObject.AddComponent<InfoTabController>();
            _chatPanel = UIFactory.Root(_contentWrap, "ChatPanel");
            _chatPanel.gameObject.AddComponent<ChatbotController>();
            _extraPanel = UIFactory.Root(_contentWrap, "ExtraPanel"); // empty on purpose, add content later

            // ---------- top bar ----------
            _tabBar = UIFactory.Place(root, "TopBar", 0, 0.855f, 1, 1);
            var homeRt = UIFactory.Place(_tabBar, "Home", 0.012f, 0.08f, 0.068f, 0.92f);
            var homeB = UIFactory.MakeButton(homeRt, "b", "🏠", 34, InfoBlue, Color.white, ShowHome);
            homeB.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;

            var logoRt = UIFactory.Place(_tabBar, "Logo", 0.08f, 0.1f, 0.32f, 0.9f);
            var logo = UIFactory.Label(logoRt, "t", "SaveWaterEG 💧", 44, Color.white, TextAnchor.MiddleLeft);
            logo.fontStyle = FontStyle.Bold;
            var lo = logo.gameObject.AddComponent<Outline>();
            lo.effectColor = new Color(0.05f, 0.3f, 0.6f); lo.effectDistance = new Vector2(2, -2);

            _tabInfo = MakeTab(_tabBar, "TabInfo", "🗺 المعلومات", 0.365f, 0.515f, InfoBlue, () => ShowTab(0));
            _tabChat = MakeTab(_tabBar, "TabChat", "💬 الشات بوت", 0.522f, 0.668f, ChatGreen, () => ShowTab(1));
            _tabChars = MakeTab(_tabBar, "TabChars", "👨‍👩‍👧 الشخصيات", 0.675f, 0.825f, CharsPurple, () => ShowTab(2));
            _tabExtra = MakeTab(_tabBar, "TabExtra", ExtraTabIcon + " " + ExtraTabName, 0.832f, 0.982f, ExtraOrange, () => ShowTab(3));

            // ---------- home panel (menu art + real buttons + animated Droppy) ----------
            _homePanel = UIFactory.Root(root, "HomePanel");
            // Background drawn as a RawImage straight from the texture (no sprite mesh),
            // stretched over the whole screen. Falls back to a flat sky colour.
            var bgTex = Resources.Load<Texture2D>("Art/main_menu/menu_bg") ?? Resources.Load<Texture2D>("Art/menu_bg");
            if (bgTex != null)
            {
                var bgRt = UIFactory.Root(_homePanel, "BG");
                var raw = bgRt.gameObject.AddComponent<RawImage>();
                raw.texture = bgTex;
                raw.color = Color.white;
                raw.raycastTarget = false;
                Debug.Log("[SaveWaterEG] menu_bg loaded: " + bgTex.width + "x" + bgTex.height);
            }
            else
            {
                Debug.LogWarning("[SaveWaterEG] Art/menu_bg NOT found in Resources/Art");
                UIFactory.Panel(_homePanel, "BG", new Color(0.42f, 0.76f, 0.96f));
            }

            // animated Droppy where the art's Droppy stood
            var droppyRt = UIFactory.Place(_homePanel, "Droppy", 0.109f, 0.115f, 0.379f, 0.644f);
            var droppySprite = LoadDroppySprite();
            if (droppySprite != null)
            {
                UIFactory.Img(droppyRt, "img", droppySprite, Color.white);
                droppyRt.gameObject.AddComponent<DroppyAnimator>();
            }

            // The art has the three buttons drawn on it. Each one is copied on top of itself as a
            // live button (cut from the art) that breathes, grows on hover, dips on press and
            // bounces when clicked. Falls back to an invisible click area if the cut-out is missing.
            LiveButton("info", "BtnInfo", 0.391f, 0.250f, 0.518f, 0.465f, 0f, () => ShowTab(0));
            LiveButton("chat", "BtnChat", 0.529f, 0.250f, 0.656f, 0.465f, 1.3f, () => ShowTab(1));
            LiveButton("levels", "BtnLevels", 0.668f, 0.250f, 0.797f, 0.465f, 2.6f, () => ShowTab(3));
            LiveButton("chars", "BtnChars", 0.809f, 0.250f, 0.939f, 0.465f, 3.9f, () => ShowTab(2));

            // Characters tab: full screen, on top of everything (has its own top tabs and back arrow)
            _charsPanel = UIFactory.Root(root, "CharsPanel");
            var charsCtl = _charsPanel.gameObject.AddComponent<CharactersTabController>();
            charsCtl.onTab = ShowTab;
            charsCtl.onHome = ShowHome;

            ShowHome();
        }

        void BigMenuButton(Transform parent, string name, string icon, string label, float xMin, Color color, UnityEngine.Events.UnityAction act)
        {
            var rt = UIFactory.Place(parent, name, xMin, 0.05f, xMin + 0.165f, 0.33f);
            var b = UIFactory.MakeButton(rt, "b", "", 30, color, Color.white, act);
            var ol = b.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.25f); ol.effectDistance = new Vector2(0, -4);
            var iconRt = UIFactory.Place(rt, "icon", 0.15f, 0.42f, 0.85f, 0.97f);
            UIFactory.Label(iconRt, "t", icon, 64, Color.white);
            var labRt = UIFactory.Place(rt, "label", 0.03f, 0.05f, 0.97f, 0.4f);
            var t = UIFactory.Label(labRt, "t", label, 34, Color.white);
            t.fontStyle = FontStyle.Bold;
            var tol = t.gameObject.AddComponent<Outline>();
            tol.effectColor = new Color(0, 0, 0, 0.4f);
            UIFactory.AddPopIn(rt.gameObject);
        }


        // Loads a sprite from several paths. Works even if the PNG is imported as "Default" texture (not Sprite).
        public static Sprite LoadDroppySprite()
        {
            string[] paths = { "Art/main_menu/droppy", "Art/droppy", "Art/Characters/droppy", "Art/Info/droppy" };
            foreach (var p in paths)
            {
                var sp = Resources.Load<Sprite>(p);
                if (sp != null) return sp;
                var tex = Resources.Load<Texture2D>(p);
                if (tex != null)
                {
                    Debug.Log("[SaveWaterEG] droppy loaded as Texture2D from " + p + " (building sprite)");
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            Debug.LogWarning("[SaveWaterEG] droppy NOT found. Tried: " + string.Join(", ", paths) + " (under Assets/Resources)");
            return null;
        }

        void LiveButton(string art, string name, float xMin, float yMin, float xMax, float yMax, float phase, UnityEngine.Events.UnityAction act)
        {
            string folder = art == "info" ? "Info" : art == "chat" ? "Chat" : art == "levels" ? "Levels" : "Characters";
            var tex = Resources.Load<Texture2D>("Art/main_menu/btn_" + art);
            if (tex == null) tex = Resources.Load<Texture2D>("Art/" + folder + "/btn_" + art);
            if (tex == null) tex = Resources.Load<Texture2D>("Art/btn_" + art); // old flat path fallback
            if (tex == null) { UIFactory.GhostButton(_homePanel, name, xMin, yMin, xMax, yMax, act); return; }
            var rt = UIFactory.Place(_homePanel, name, xMin, yMin, xMax, yMax);
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(act);
            var fx = rt.gameObject.AddComponent<MenuButtonFX>();
            fx.phase = phase;
        }

        Button MakeTab(Transform parent, string name, string label, float xMin, float xMax, Color color, UnityEngine.Events.UnityAction act)
        {
            var rt = UIFactory.Place(parent, name, xMin, 0.1f, xMax, 0.9f);
            var b = UIFactory.MakeButton(rt, "b", label, 30, color, Color.white, act);
            var t = b.GetComponentInChildren<Text>();
            t.fontStyle = FontStyle.Bold;
            var ol = t.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.35f);
            return b;
        }

        void ShowHome()
        {
            _tab = -1;
            _homePanel.gameObject.SetActive(true);
            _tabBar.gameObject.SetActive(false);
            _contentWrap.gameObject.SetActive(false);
            _charsPanel.gameObject.SetActive(false);
            UIFactory.AddPopIn(_homePanel.gameObject);
            _homePanel.gameObject.SetActive(false);
            _homePanel.gameObject.SetActive(true);
        }

        void ShowTab(int tab)
        {
            if (tab == _tab) return;
            _tab = tab;
            _homePanel.gameObject.SetActive(false);
            _tabBar.gameObject.SetActive(tab != 2);       // Characters tab has its own top bar
            _contentWrap.gameObject.SetActive(tab != 2);
            _infoPanel.gameObject.SetActive(tab == 0);
            _chatPanel.gameObject.SetActive(tab == 1);
            _charsPanel.gameObject.SetActive(tab == 2);
            _extraPanel.gameObject.SetActive(tab == 3);
            var active = tab == 0 ? _infoPanel : tab == 1 ? _chatPanel : tab == 2 ? _charsPanel : _extraPanel;
            UIFactory.AddPopIn(active.gameObject);
            active.gameObject.SetActive(false);
            active.gameObject.SetActive(true);
            StyleTab(_tabInfo, tab == 0, InfoBlue);
            StyleTab(_tabChat, tab == 1, ChatGreen);
            StyleTab(_tabChars, tab == 2, CharsPurple);
            StyleTab(_tabExtra, tab == 3, ExtraOrange);
        }

        void StyleTab(Button b, bool on, Color onColor)
        {
            b.GetComponent<Image>().color = on ? onColor : onColor * 0.8f;
            b.transform.localScale = on ? Vector3.one * 1.07f : Vector3.one;
        }
    }
}
