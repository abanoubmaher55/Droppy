using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // Chat bot tab: kid asks a question, Droppy answers from the JSON data file
    // (questions/answers + keywords). Also answers "how does the game work?".
    public class ChatbotController : MonoBehaviour
    {
        RectTransform _scrollContent;
        ScrollRect _scroll;
        InputField _input;
        List<ChatQA> _qa;

        void Start()
        {
            _qa = GameData.LoadChat();
            var root = (RectTransform)transform;

            // header with Droppy
            var headRt = UIFactory.Place(root, "Head", 0.6f, 0.88f, 1f, 1f);
            var head = UIFactory.Label(headRt, "t", "اسأل دروبي عن المياه! 💬", 34, new Color(0.1f, 0.45f, 0.75f));
            head.fontStyle = FontStyle.Bold;

            // scroll area
            var scrollRt = UIFactory.Place(root, "Scroll", 0.02f, 0.16f, 0.98f, 0.87f);
            UIFactory.Panel(scrollRt, "bg", new Color(0.93f, 0.97f, 1f, 0.9f));
            _scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            var viewport = UIFactory.Root(scrollRt, "Viewport");
            var mask = viewport.gameObject.AddComponent<Image>();
            mask.sprite = UIFactory.WhiteSprite(); mask.color = new Color(1, 1, 1, 0.01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            _scrollContent = UIFactory.Sub(viewport, "Content", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, 0), new Vector2(-10, 0));
            var vlg = _scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = false; vlg.childControlHeight = false;
            vlg.spacing = 12; vlg.padding = new RectOffset(10, 10, 10, 10);
            var csf = _scrollContent.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll.content = _scrollContent; _scroll.viewport = viewport;
            _scroll.horizontal = false;

            // input row
            var inputRt = UIFactory.Place(root, "Input", 0.28f, 0.02f, 0.97f, 0.14f);
            UIFactory.Panel(inputRt, "bg", Color.white);
            _input = inputRt.gameObject.AddComponent<InputField>();
            var txt = UIFactory.Label(inputRt, "txt", "", 28, new Color(0.1f, 0.2f, 0.35f), TextAnchor.MiddleRight);
            ((RectTransform)txt.transform).offsetMin = new Vector2(15, 0);
            ((RectTransform)txt.transform).offsetMax = new Vector2(-15, 0);
            var ph = UIFactory.Label(inputRt, "ph", "اكتب سؤالك هنا...", 28, new Color(0.6f, 0.65f, 0.7f), TextAnchor.MiddleRight);
            ((RectTransform)ph.transform).offsetMax = new Vector2(-15, 0);
            _input.textComponent = txt; _input.placeholder = ph;
            _input.lineType = InputField.LineType.SingleLine;
            _input.onSubmit.AddListener(_ => Ask());

            var sendRt = UIFactory.Place(root, "Send", 0.03f, 0.02f, 0.26f, 0.14f);
            var sendB = UIFactory.MakeButton(sendRt, "b", "إرسال 📨", 30, new Color(0.2f, 0.75f, 0.35f), Color.white, Ask);
            sendB.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;

            BotSay("أهلاً! أنا دروبي 💧 اسألني أي سؤال عن المياه في مصر، أو عن طريقة اللعب!");
        }

        void Ask()
        {
            string q = _input.text.Trim();
            if (string.IsNullOrEmpty(q)) return;
            _input.text = "";
            UserSay(q);
            BotSay(Answer(q));
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
        }

        string Answer(string q)
        {
            // how to play (always available, even without data)
            if (q.Contains("اللعبة") || q.Contains("ألعب") || q.Contains("العب") || q.Contains("طريقة"))
                return "طريقة اللعب سهلة! اختار شخصيتك من تاب الشخصيات واضغط العب. هتنزل في مدينة فيها عطل بيهدر المياه. عداد الهدر بيزيد مع الوقت، ولو وصل 25 لتر بتخسر. اتبع الأسهم على الأرض للنقطة الحمراء على الخريطة المصغرة، ووصل للعطل بسرعة. هناك هيظهرلك 3 اختيارات، اختار الإصلاح الصح. كل ما تصلح أسرع تاخد نجوم أكتر! ⭐";

            // keyword matching against the JSON data
            ChatQA best = null; int bestScore = 0;
            foreach (var item in _qa)
            {
                int score = 0;
                if (!string.IsNullOrEmpty(item.question) && q.Contains(item.question)) score += 3;
                foreach (var k in item.keywords)
                    if (!string.IsNullOrEmpty(k) && q.Contains(k)) score += 2;
                if (score > bestScore) { bestScore = score; best = item; }
            }
            if (best != null && bestScore >= 2) return best.answer;

            return "سؤال حلو! بس مش لاقي إجابته عندي دلوقتي 😅 جرب تسألني عن: المياه العذبة، المياه المالحة، نهر النيل، ترشيد المياه، بحيرة ناصر، أو إزاي تلعب اللعبة.";
        }

        void UserSay(string msg) => AddBubble(msg, true);
        void BotSay(string msg) => AddBubble(msg, false);

        void AddBubble(string msg, bool user)
        {
            string wrapped = UIFactory.Wrap(msg, 42);
            int lines = wrapped.Split('\n').Length;
            float h = 46 + lines * 34;

            var row = new GameObject(user ? "UserRow" : "BotRow", typeof(RectTransform));
            var rowRt = (RectTransform)row.transform;
            rowRt.SetParent(_scrollContent, false);
            rowRt.sizeDelta = new Vector2(700, h);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = h; le.preferredWidth = 700;

            var bubble = UIFactory.Sub(rowRt, "b",
                user ? new Vector2(0.25f, 0) : new Vector2(0f, 0),
                user ? new Vector2(1f, 1) : new Vector2(0.75f, 1),
                Vector2.zero, Vector2.zero);
            UIFactory.Panel(bubble, "bg", user ? new Color(0.55f, 0.85f, 1f) : Color.white);
            var t = UIFactory.Label(bubble, "t", wrapped, 24, new Color(0.1f, 0.25f, 0.4f),
                TextAnchor.MiddleRight);
            ((RectTransform)t.transform).offsetMin = new Vector2(12, 2);
            ((RectTransform)t.transform).offsetMax = new Vector2(-12, -2);
        }
    }
}
