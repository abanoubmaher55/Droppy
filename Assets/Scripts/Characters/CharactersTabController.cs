using UnityEngine;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // Characters tab (full screen). Everything is drawn from the pictures in
    // Assets/Resources/Art/characters_tab/ inside a 16:9 "stage" that always fits
    // the screen, so nothing is cropped and everything stays in place.
    //
    // Files used (in Resources/Art/characters_tab):
    //   characters_tab_bg, char_technical, char_chemical, char_collector, char_engineer,
    //   choose_play, play_technical, play-chemical, play_collecter, play_engineer,
    //   btn_info, btn_chat, btn_levels, btn_chars, btn_back
    public class CharactersTabController : MonoBehaviour
    {
        const string Folder = "Art/characters_tab/";

        // set by MainMenuController
        public System.Action<int> onTab;   // 0 info, 1 chat, 2 chars, 3 levels
        public System.Action onHome;

        // card order left to right: الفني، الكيميائي، محصل المياه، مهندس المياه (= GameManager indices 0..3)
        static readonly string[] CardFiles = { "char_technical", "char_chemical", "char_collector", "char_engineer" };
        static readonly string[] PlayFiles = { "play_technical", "play-chemical", "play_collecter", "play_engineer" };

        int _selected = -1;
        RawImage[] _cards;
        float[] _cardScale = { 1, 1, 1, 1 };
        float[] _cardTarget = { 1, 1, 1, 1 };
        RawImage _playImg;
        Texture2D _chooseTex;
        Texture2D[] _playTex;
        RectTransform _stage;

        // Loads Art/characters_tab/<name>. If two files share the name (e.g. play_technical.jpeg and
        // play_technical.png) the one with transparency wins, so either way works.
        static Texture2D Load(string name)
        {
            Texture2D best = null;
            foreach (var t in Resources.LoadAll<Texture2D>(Folder.TrimEnd('/')))
            {
                if (t == null || t.name != name) continue;
                if (best == null || UnityEngine.Experimental.Rendering.GraphicsFormatUtility.HasAlphaChannel(t.graphicsFormat))
                    best = t;
            }
            if (best == null) Debug.LogWarning("[SaveWaterEG] Missing " + Folder + name + " (put it in Assets/Resources/Art/characters_tab)");
            return best;
        }

        RawImage Pic(Transform parent, string name, Texture2D tex, float xMin, float yMin, float xMax, float yMax, bool raycast)
        {
            var rt = UIFactory.Place(parent, name, xMin, yMin, xMax, yMax);
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = tex != null ? Color.white : new Color(1, 1, 1, 0.25f);
            raw.raycastTarget = raycast;
            return raw;
        }

        void Start()
        {
            var root = (RectTransform)transform;

            // sky colour behind (only visible in letterbox bars)
            UIFactory.Panel(root, "Sky", new Color(0.35f, 0.72f, 0.95f)).GetComponent<Image>().raycastTarget = false;

            // 16:9 stage that fits inside the screen
            _stage = UIFactory.Root(root, "Stage");
            var fitter = _stage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;

            // background scene
            var bgTex = Load("characters_tab_bg");
            var bg = Pic(_stage, "BG", bgTex, 0, 0, 1, 1, false);
            if (bgTex == null) bg.color = new Color(0.42f, 0.76f, 0.96f);

            // ---- top tabs (right to left: levels, chars, chat, info) + back arrow ----
            TopTab("btn_info", "TabInfo", 0.370f, () => Go(0), false);
            TopTab("btn_chat", "TabChat", 0.525f, () => Go(1), false);
            TopTab("btn_chars", "TabChars", 0.680f, null, true);
            TopTab("btn_levels", "TabLevels", 0.835f, () => Go(3), false);

            var backTex = Load("btn_back");
            var back = Pic(_stage, "Back", backTex, 0.02f, 0.869f, 0.072f, 0.961f, true);
            back.gameObject.AddComponent<Button>().onClick.AddListener(() => { if (onHome != null) onHome(); });
            back.gameObject.AddComponent<MenuButtonFX>().phase = 0.5f;

            // title
            var titleRt = UIFactory.Place(_stage, "Title", 0.25f, 0.73f, 0.75f, 0.84f);
            var title = UIFactory.Label(titleRt, "t", "اختار بطلك!", 56, new Color(1f, 0.78f, 0.12f));
            title.fontStyle = FontStyle.Bold;
            var tol = title.gameObject.AddComponent<Outline>();
            tol.effectColor = new Color(0.55f, 0.28f, 0.02f, 0.9f);
            tol.effectDistance = new Vector2(2, -2);

            // ---- the four cards ----
            _cards = new RawImage[4];
            float[] cx = { 0.185f, 0.395f, 0.605f, 0.815f };
            for (int i = 0; i < 4; i++)
            {
                var tex = Load(CardFiles[i]);
                float w = 0.19f;
                var card = Pic(_stage, "Card" + i, tex, cx[i] - w / 2f, 0.17f, cx[i] + w / 2f, 0.69f, true);
                int idx = i;
                var b = card.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Select(idx));
                _cards[i] = card;
            }

            // ---- play button ----
            _chooseTex = Load("choose_play");
            _playTex = new Texture2D[4];
            for (int i = 0; i < 4; i++) _playTex[i] = Load(PlayFiles[i]);
            _playImg = Pic(_stage, "Play", _chooseTex, 0.345f, 0.020f, 0.655f, 0.170f, true);
            var pb = _playImg.gameObject.AddComponent<Button>();
            pb.transition = Selectable.Transition.None;
            pb.onClick.AddListener(Play);
            _playImg.gameObject.AddComponent<MenuButtonFX>().phase = 1f;
        }

        void TopTab(string file, string name, float xMin, UnityEngine.Events.UnityAction act, bool active)
        {
            var tex = Load(file);
            var raw = Pic(_stage, name, tex, xMin, 0.878f, xMin + 0.14f, 0.953f, act != null);
            if (act != null)
            {
                raw.gameObject.AddComponent<Button>().onClick.AddListener(act);
                raw.gameObject.AddComponent<MenuButtonFX>().phase = xMin * 5f;
            }
            else
            {
                raw.rectTransform.localScale = Vector3.one * 1.08f; // current tab
            }
        }

        void Go(int tab) { if (onTab != null) onTab(tab); }

        void Select(int i)
        {
            _selected = i;
            if (GameManager.I != null) GameManager.I.SelectCharacter(i);
            for (int k = 0; k < 4; k++)
            {
                bool on = k == i;
                _cardTarget[k] = on ? 1.08f : 0.94f;
                _cards[k].color = on ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
                if (on) _cards[k].transform.SetAsLastSibling();
            }
            _playImg.texture = _playTex[i] != null ? _playTex[i] : _chooseTex;
            _playImg.color = Color.white;
            // the play button sits above the cards in order, keep it on top
            _playImg.transform.SetAsLastSibling();
        }

        void Update()
        {
            if (_cards == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            for (int k = 0; k < 4; k++)
            {
                _cardScale[k] = Mathf.Lerp(_cardScale[k], _cardTarget[k], 1f - Mathf.Exp(-14f * dt));
                float bob = (k == _selected) ? 1f + 0.012f * Mathf.Sin(Time.unscaledTime * 4f) : 1f;
                _cards[k].rectTransform.localScale = Vector3.one * (_cardScale[k] * bob);
            }
        }

        void Play()
        {
            if (_selected < 0) return; // nothing picked yet
            if (GameManager.I != null) GameManager.I.StartGame();
        }
    }
}
