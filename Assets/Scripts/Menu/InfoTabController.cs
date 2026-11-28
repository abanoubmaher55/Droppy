using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SaveWaterEG
{
    public class InfoTabController : MonoBehaviour
    {
        public System.Action<int> onTab;
        public System.Action onHome;

        static readonly string[] Types = { "fresh", "salty", "well", "spring" };
        static readonly string[] TypeIcons = { "💧", "🌊", "🚰", "👁" };

        readonly Dictionary<string, bool> _enabled = new Dictionary<string, bool>();
        readonly Dictionary<string, Image> _checks = new Dictionary<string, Image>();
        readonly Dictionary<string, Text> _checkMarks = new Dictionary<string, Text>();
        readonly List<GameObject> _pins = new List<GameObject>();
        readonly List<MapLocation> _pinLocs = new List<MapLocation>();
        readonly HashSet<string> _discovered = new HashSet<string>();

        Text _cardTitle, _cardType, _cardBody, _counter;
        Image _cardIcon;
        RectTransform _cardIconBg;
        List<MapLocation> _locations;
        RectTransform _card;
        Text _chipIconText;

        void Start()
        {
            _locations = GameData.LoadLocations();
            foreach (var t in Types) _enabled[t] = true;
            var root = (RectTransform)transform;

            // 1. الخلفية الكاملة (يبحث عن صورة الخلفية في مجلد الشخصيات أو المعلومات)
            var bgSprite = Resources.Load<Sprite>("Art/characters_tab/bg") 
                        ?? Resources.Load<Sprite>("Art/characters_tab/background")
                        ?? Resources.Load<Sprite>("Art/information_tab/bg");
            if (bgSprite != null)
            {
                var bgRt = UIFactory.Place(root, "PageBg", 0f, 0f, 1f, 1f);
                UIFactory.Img(bgRt, "img", bgSprite, Color.white).raycastTarget = false;
            }
            else
            {
                UIFactory.Panel(root, "PageBg", new Color(0.42f, 0.76f, 0.96f));
            }

            // 2. الشريط العلوي بالزراير الجديدة وزرار الباك
            BuildTopBar(root);

            // 3. محتوى الصفحة (الفلاتر + الخريطة + الكارت الجانبي)
            var body = UIFactory.Place(root, "Body", 0.025f, 0.015f, 0.975f, 0.845f);
            BuildFilters(body);
            BuildMap(body);
            BuildRightColumn(body);
            RefreshPins();
            ShowWelcome();
        }

        // ---------------- top bar (buttons + back arrow) ----------------
        void BuildTopBar(RectTransform root)
        {
            var bar = UIFactory.Place(root, "TopBar", 0f, 0.86f, 1f, 0.98f);

            var backRt = UIFactory.Place(bar, "BtnBack", 0.02f, 0.05f, 0.08f, 0.95f);
            var backSp = Resources.Load<Sprite>("Art/information_tab/btn_back");
            var backBtn = UIFactory.MakeButton(backRt, "b", "", 20, Color.white, Color.white, () => onHome?.Invoke());
            if (backSp != null)
            {
                var img = backBtn.GetComponent<Image>();
                img.sprite = backSp;
                img.preserveAspect = true;
            }

            MakeImageTab(bar, "TabInfo", "Art/information_tab/btn_info", 0.36f, 0.50f, true, () => onTab?.Invoke(0));
            MakeImageTab(bar, "TabChat", "Art/information_tab/btn_chat", 0.51f, 0.65f, false, () => onTab?.Invoke(1));
            MakeImageTab(bar, "TabChars", "Art/information_tab/btn_chars", 0.66f, 0.80f, false, () => onTab?.Invoke(2));
            MakeImageTab(bar, "TabLevels", "Art/information_tab/btn_levels", 0.81f, 0.95f, false, () => onTab?.Invoke(3));
        }

        void MakeImageTab(RectTransform parent, string name, string path, float xMin, float xMax, bool isActive, UnityEngine.Events.UnityAction act)
        {
            var rt = UIFactory.Place(parent, name, xMin, 0.08f, xMax, 0.92f);
            var sp = Resources.Load<Sprite>(path);
            var btn = UIFactory.MakeButton(rt, "b", "", 20, Color.white, Color.white, act);
            if (sp != null)
            {
                var img = btn.GetComponent<Image>();
                img.sprite = sp;
                img.preserveAspect = true;
            }
            if (isActive) rt.localScale = Vector3.one * 1.06f;
        }

        // ---------------- left: filter panel ----------------
        void BuildFilters(RectTransform root)
        {
            var panel = UIFactory.Place(root, "Filters", 0f, 0f, 0.185f, 1f);
            UIFactory.Panel(panel, "bg", new Color(1f, 1f, 1f, 0.94f));

            var headRt = UIFactory.Place(panel, "Head", 0.03f, 0.90f, 0.97f, 0.995f);
            var head = UIFactory.Label(headRt, "t", "🔽 فلتر أنواع المياه", 26, new Color(0.08f, 0.4f, 0.75f));
            head.fontStyle = FontStyle.Bold;

            for (int i = 0; i < Types.Length; i++)
            {
                string type = Types[i];
                float y1 = 0.885f - i * 0.115f, y0 = y1 - 0.10f;
                var rowRt = UIFactory.Place(panel, "row_" + type, 0.03f, y0, 0.97f, y1);
                var row = UIFactory.MakeButton(rowRt, "b", "", 20, new Color(0.93f, 0.96f, 1f), Color.white, () => Toggle(type));

                var dotRt = UIFactory.Place(rowRt, "dot", 0.78f, 0.18f, 0.97f, 0.82f);
                var dot = UIFactory.Img(dotRt, "d", Resources.Load<Sprite>("Art/circle"), GameData.TypeColor(type));
                dot.raycastTarget = false;
                var iconRt = UIFactory.Place(rowRt, "icon", 0.78f, 0.18f, 0.97f, 0.82f);
                var ic = UIFactory.Label(iconRt, "i", TypeIcons[i], 22, Color.white);
                ic.raycastTarget = false;

                var labRt = UIFactory.Place(rowRt, "lab", 0.16f, 0.1f, 0.76f, 0.9f);
                var lab = UIFactory.Label(labRt, "t", GameData.TypeLabel(type), 23, new Color(0.12f, 0.25f, 0.4f), TextAnchor.MiddleRight);
                lab.raycastTarget = false;

                var chkRt = UIFactory.Place(rowRt, "chk", 0.02f, 0.22f, 0.14f, 0.78f);
                var chk = UIFactory.Img(chkRt, "c", Resources.Load<Sprite>("Art/circle"), new Color(0.25f, 0.8f, 0.3f));
                chk.raycastTarget = false;
                var markRt = UIFactory.Place(rowRt, "mark", 0.02f, 0.22f, 0.14f, 0.78f);
                var mark = UIFactory.Label(markRt, "m", "✔", 20, Color.white);
                mark.raycastTarget = false;
                _checks[type] = chk;
                _checkMarks[type] = mark;
                UIFactory.AddPopIn(rowRt.gameObject);
            }

            var allRt = UIFactory.Place(panel, "All", 0.06f, 0.30f, 0.94f, 0.40f);
            var all = UIFactory.MakeButton(allRt, "b", "🔄 عرض الكل", 24, new Color(0.1f, 0.55f, 0.95f), Color.white, ShowAll);
            all.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;

            var signRt = UIFactory.Place(panel, "Sign", 0.04f, 0.03f, 0.96f, 0.24f);
            UIFactory.Panel(signRt, "bg", new Color(0.55f, 0.35f, 0.16f));
            var signT = UIFactory.Place(signRt, "t1", 0.05f, 0.5f, 0.95f, 0.95f);
            var st = UIFactory.Label(signT, "t", "💧 اكتشف مصادر المياه", 20, new Color(1f, 0.95f, 0.8f));
            st.fontStyle = FontStyle.Bold;
            var cntRt = UIFactory.Place(signRt, "t2", 0.05f, 0.05f, 0.95f, 0.5f);
            _counter = UIFactory.Label(cntRt, "t", "", 24, Color.white);
            _counter.fontStyle = FontStyle.Bold;
        }

        // ---------------- center: map ----------------
        void BuildMap(RectTransform root)
        {
            var frame = UIFactory.Place(root, "MapFrame", 0.195f, 0f, 0.80f, 1f);
            UIFactory.Panel(frame, "bg", new Color(0f, 0f, 0f, 0f));

            var holder = UIFactory.Sub(frame, "Map", Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
            var fit = holder.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 1.2f;
            var mapSprite = Resources.Load<Sprite>("Art/information_tab/egypt_map") ?? Resources.Load<Sprite>("Art/egypt_map");
            var mapImg = UIFactory.Img(holder, "img", mapSprite != null ? mapSprite : UIFactory.WhiteSprite(),
                mapSprite != null ? Color.white : new Color(0.9f, 0.8f, 0.6f), false);
            mapImg.type = Image.Type.Simple;
            mapImg.raycastTarget = true;
            var mapBtn = mapImg.gameObject.AddComponent<Button>();
            mapBtn.transition = Selectable.Transition.None;
            mapBtn.onClick.AddListener(ShowWelcome);

            SeaLabel(holder, "البحر الأبيض المتوسط", 0.22f, 0.93f);
            SeaLabel(holder, "البحر الأحمر", 0.80f, 0.34f);

            foreach (var loc in _locations)
            {
                float x = Mathf.Clamp01(loc.x), y = Mathf.Clamp01(loc.y);
                var pin = UIFactory.Place(holder, "pin_" + loc.id, x - 0.055f, y - 0.075f, x + 0.055f, y + 0.075f);

                var circleRt = UIFactory.Place(pin, "c", 0.18f, 0.42f, 0.82f, 1f);
                var circ = UIFactory.Img(circleRt, "ci", Resources.Load<Sprite>("Art/circle"), GameData.TypeColor(loc.type));
                circ.raycastTarget = false;
                var icRt = UIFactory.Place(pin, "i", 0.18f, 0.42f, 0.82f, 1f);
                var ic = UIFactory.Label(icRt, "t", TypeIcon(loc.type), 22, Color.white);
                ic.raycastTarget = false;
                UIFactory.AddPulse(circleRt.gameObject, 0.10f, 2.5f);

                var pillRt = UIFactory.Place(pin, "pill", 0f, 0f, 1f, 0.36f);
                UIFactory.Panel(pillRt, "bg", new Color(0.05f, 0.25f, 0.5f, 0.92f))
                    .GetComponent<Image>().raycastTarget = false;
                var nameT = UIFactory.Label(pillRt, "t", loc.name, 17, Color.white);
                nameT.raycastTarget = false;

                var b = UIFactory.GhostButton(pin, "hit", 0, 0, 1, 1, null);
                var captured = loc;
                b.onClick.AddListener(() => OpenCard(captured));
                _pins.Add(pin.gameObject);
                _pinLocs.Add(loc);
            }
        }

        void SeaLabel(Transform parent, string text, float x, float y)
        {
            var rt = UIFactory.Place(parent, "sea", x - 0.14f, y - 0.03f, x + 0.14f, y + 0.03f);
            var t = UIFactory.Label(rt, "t", text, 22, Color.white);
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            var ol = t.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.05f, 0.35f, 0.6f, 0.8f);
        }

        // ---------------- right: card + bubble + Droppy ----------------
        void BuildRightColumn(RectTransform root)
        {
            var col = UIFactory.Place(root, "Right", 0.81f, 0f, 1f, 1f);

            var card = UIFactory.Place(col, "Card", 0f, 0.44f, 1f, 1f);
            UIFactory.Panel(card, "bg", new Color(1f, 1f, 1f, 0.97f));

            var titleRt = UIFactory.Place(card, "title", 0.04f, 0.85f, 0.96f, 0.99f);
            UIFactory.Panel(titleRt, "bg", new Color(0.98f, 0.72f, 0.12f));
            _cardTitle = UIFactory.Label(titleRt, "t", "", 27, Color.white);
            _cardTitle.fontStyle = FontStyle.Bold;

           var chipRt = UIFactory.Place(card, "chip", 0.04f, 0.70f, 0.96f, 0.83f);
            _cardIconBg = UIFactory.Place(chipRt, "cbg", 0.84f, 0.12f, 0.99f, 0.88f);
            _cardIcon = UIFactory.Img(_cardIconBg, "c", Resources.Load<Sprite>("Art/circle"), Color.white);
            _cardIcon.raycastTarget = false;
            var chipIcon = UIFactory.Place(chipRt, "ci", 0.84f, 0.12f, 0.99f, 0.88f);
            var chipIconT = UIFactory.Label(chipIcon, "t", "", 22, Color.white);
            chipIconT.raycastTarget = false;

            _cardType = UIFactory.Label(chipRt, "t", "", 21, new Color(0.9f, 0.4f, 0.1f), TextAnchor.MiddleRight);
            var typeRt = _cardType.rectTransform;
            typeRt.anchorMin = new Vector2(0.02f, 0.05f);
            typeRt.anchorMax = new Vector2(0.81f, 0.95f);
            typeRt.offsetMin = Vector2.zero;
            typeRt.offsetMax = Vector2.zero;
            _cardType.raycastTarget = false;

            _cardType.gameObject.name = "chipText";
            _cardIconBg.gameObject.name = "chipBg";
            _chipIconText = chipIconT;

            var bodyRt = UIFactory.Place(card, "body", 0.06f, 0.04f, 0.94f, 0.67f);
            _cardBody = UIFactory.Label(bodyRt, "t", "", 21, new Color(0.15f, 0.25f, 0.4f), TextAnchor.UpperRight);
            _card = card;

            var bubble = UIFactory.Place(col, "Bubble", 0.03f, 0.24f, 0.97f, 0.41f);
            UIFactory.Panel(bubble, "bg", new Color(1f, 1f, 1f, 0.95f));
            var bt = UIFactory.Label(bubble, "t", "اضغط على أي علامة على الخريطة لتتعرف على نوع المياه! 💧", 19, new Color(0.1f, 0.35f, 0.6f));
            bt.fontStyle = FontStyle.Bold;
            UIFactory.AddPulse(bubble.gameObject, 0.02f, 2f);

            var droppyRt = UIFactory.Place(col, "Droppy", 0.12f, 0f, 0.88f, 0.23f);
            var droppySprite = MainMenuController.LoadDroppySprite();
            if (droppySprite != null)
            {
                UIFactory.Img(droppyRt, "img", droppySprite, Color.white);
                droppyRt.gameObject.AddComponent<DroppyAnimator>();
            }
        }

        // ---------------- behavior ----------------
        void Toggle(string type)
        {
            _enabled[type] = !_enabled[type];
            UpdateChecks();
            RefreshPins();
        }

        void ShowAll()
        {
            foreach (var t in Types) _enabled[t] = true;
            UpdateChecks();
            RefreshPins();
        }

        void UpdateChecks()
        {
            foreach (var t in Types)
            {
                bool on = _enabled[t];
                _checks[t].color = on ? new Color(0.25f, 0.8f, 0.3f) : new Color(0.75f, 0.78f, 0.82f);
                _checkMarks[t].text = on ? "✔" : "";
            }
        }

        void RefreshPins()
        {
            for (int i = 0; i < _pins.Count; i++)
                _pins[i].SetActive(_enabled[_pinLocs[i].type]);
            UpdateCounter();
        }

        void OpenCard(MapLocation loc)
        {
            ArabicText.Set(_cardTitle, loc.name);
            ArabicText.Set(_cardType, GameData.TypeLabel(loc.type));
            _cardType.color = GameData.TypeColor(loc.type) * 0.9f;
            _cardIcon.color = GameData.TypeColor(loc.type);
            _chipIconText.text = TypeIcon(loc.type);
            ArabicText.Set(_cardBody, loc.description);
            _discovered.Add(loc.id);
            UpdateCounter();
            UIFactory.AddPopIn(_card.gameObject);
            _card.gameObject.SetActive(false);
            _card.gameObject.SetActive(true);
        }

        void ShowWelcome()
        {
            ArabicText.Set(_cardTitle, "أهلاً بك! 🌍");
            ArabicText.Set(_cardType, "خريطة المياه في مصر");
            _cardType.color = new Color(0.1f, 0.55f, 0.95f);
            _cardIcon.color = new Color(0.1f, 0.55f, 0.95f);
            _chipIconText.text = "🗺";
            ArabicText.Set(_cardBody, "دي خريطة مصر وعليها أهم مصادر المياه.\n\n✅ استخدم الفلتر على الشمال عشان تظهر أو تخفي كل نوع مياه.\n\n👆 اضغط على أي علامة على الخريطة عشان تقرأ عنها هنا!");
            UpdateCounter();
        }

        void UpdateCounter()
        {
            int total = _locations.Count;
            int n = _discovered.Count;
            ArabicText.Set(_counter, n >= total && total > 0
                ? "برافو! اكتشفت كل الأماكن 🎉"
                : "اكتشفت " + n + " من " + total);
        }

        string TypeIcon(string type)
        {
            for (int i = 0; i < Types.Length; i++) if (Types[i] == type) return TypeIcons[i];
            return "💧";
        }
    }
}