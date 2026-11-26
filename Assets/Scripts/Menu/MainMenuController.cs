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
        RectTransform _infoPanel, _chatPanel, _charsPanel;
        Button _tabInfo, _tabChat, _tabChars;
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
            _charsPanel = UIFactory.Root(_contentWrap, "CharsPanel");
            _charsPanel.gameObject.AddComponent<CharactersTabController>();

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

            // ---------- home panel (menu art + real buttons + animated Droppy) ----------
            _homePanel = UIFactory.Root(root, "HomePanel");
            var bgSprite = Resources.Load<Sprite>("Art/menu_bg");
            if (bgSprite != null)
            {
                var bg = UIFactory.Img(_homePanel, "BG", bgSprite, Color.white, false);
                bg.type = Image.Type.Simple;
            }
            else
            {
                UIFactory.Panel(_homePanel, "BG", new Color(0.42f, 0.76f, 0.96f));
            }

            // animated Droppy where the art's Droppy stood
            var droppyRt = UIFactory.Place(_homePanel, "Droppy", 0.02f, 0.03f, 0.42f, 0.52f);
            var droppySprite = Resources.Load<Sprite>("Art/droppy");
            if (droppySprite != null)
            {
                UIFactory.Img(droppyRt, "img", droppySprite, Color.white);
                droppyRt.gameObject.AddComponent<DroppyAnimator>();
            }

            BigMenuButton(_homePanel, "BtnInfo", "🗺", "المعلومات", 0.455f, InfoBlue, () => ShowTab(0));
            BigMenuButton(_homePanel, "BtnChat", "💬", "الشات بوت", 0.637f, ChatGreen, () => ShowTab(1));
            BigMenuButton(_homePanel, "BtnChars", "👨‍👩‍👧", "الشخصيات", 0.819f, CharsPurple, () => ShowTab(2));

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
            UIFactory.AddPopIn(_homePanel.gameObject);
            _homePanel.gameObject.SetActive(false);
            _homePanel.gameObject.SetActive(true);
        }

        void ShowTab(int tab)
        {
            if (tab == _tab) return;
            _tab = tab;
            _homePanel.gameObject.SetActive(false);
            _tabBar.gameObject.SetActive(true);
            _contentWrap.gameObject.SetActive(true);
            _infoPanel.gameObject.SetActive(tab == 0);
            _chatPanel.gameObject.SetActive(tab == 1);
            _charsPanel.gameObject.SetActive(tab == 2);
            var active = tab == 0 ? _infoPanel : tab == 1 ? _chatPanel : _charsPanel;
            UIFactory.AddPopIn(active.gameObject);
            active.gameObject.SetActive(false);
            active.gameObject.SetActive(true);
            StyleTab(_tabInfo, tab == 0, InfoBlue);
            StyleTab(_tabChat, tab == 1, ChatGreen);
            StyleTab(_tabChars, tab == 2, CharsPurple);
        }

        void StyleTab(Button b, bool on, Color onColor)
        {
            b.GetComponent<Image>().color = on ? onColor : onColor * 0.8f;
            b.transform.localScale = on ? Vector3.one * 1.07f : Vector3.one;
        }
    }
}
