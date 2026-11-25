using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // One level: a city with one water fault. The waste counter rises until the
    // player reaches the red point and picks the correct fix. 25 L wasted = game over.
    public class GameplayController : MonoBehaviour
    {
        [Header("Runtime (auto)")]
        public float waste;
        public bool running;
        public bool over;

        LevelData _level;
        Transform _player;
        Vector3 _faultPos;
        float _reachedRadius = 1.4f;

        // HUD
        Text _wasteText;
        Text _titleText;
        RectTransform _miniPlayer, _miniFault;
        RectTransform _overlay;
        SpriteRenderer _faultMarker;
        readonly List<Transform> _arrows = new List<Transform>();
        Sprite _arrowSprite;
        Sprite _circleSprite;
        float _arrowTimer;

        static readonly Vector3[] FaultSpots =
        {
            new Vector3(6.5f, 3.2f, 0),
            new Vector3(-7f, 3.5f, 0),
            new Vector3(7f, -3.6f, 0),
            new Vector3(-6.5f, -3.4f, 0),
            new Vector3(0.5f, 4f, 0),
        };

        void Start()
        {
            var levels = GameData.LoadLevels();
            int idx = Mathf.Clamp(GameManager.I.currentLevel, 1, GameData.MaxLevels) - 1;
            _level = idx < levels.Count ? levels[idx] : FallbackLevel(idx);

            BuildCameraAndWorld();
            BuildPlayer();
            BuildFault(FaultSpots[idx % FaultSpots.Length]);
            BuildHud();
            ShowIntro();
        }

        LevelData FallbackLevel(int i)
        {
            return new LevelData
            {
                level = i + 1,
                title = "المستوى " + (i + 1),
                faultName = "حنفية مكسورة",
                wastePerSecond = 0.5f + i * 0.15f,
                intro = "في عطل بيهدر المياه! روح صلحه بسرعة.",
                choices = new List<FixChoice>
                {
                    new FixChoice { text = "نحط جردل تحتها", correct = false, explanation = "الجردل بيجمع المياه بس مش بيصلح العطل، والمياه لسه بتضيع!" },
                    new FixChoice { text = "نغير الجلدة", correct = true, explanation = "أحسنت! تغيير الجلدة بيوقف التسريب من جذوره. 👏" },
                    new FixChoice { text = "نحط عليها جورنال", correct = false, explanation = "الجورنال بيتبل وبيترمي، وده مش إصلاح أبداً!" },
                }
            };
        }

        // ---------------- world ----------------

        void BuildCameraAndWorld()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = new Color(0.5f, 0.8f, 0.95f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            _circleSprite = Resources.Load<Sprite>("Art/circle");
            _arrowSprite = Resources.Load<Sprite>("Art/arrow");
            if (_circleSprite == null) _circleSprite = UIFactory.WhiteSprite();
            if (_arrowSprite == null) _arrowSprite = UIFactory.WhiteSprite();

            // ground checker
            for (int x = -9; x < 9; x += 2)
                for (int y = -5; y < 5; y += 2)
                {
                    var tile = new GameObject("tile");
                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite = UIFactory.WhiteSprite();
                    sr.color = ((x + y) / 2) % 2 == 0 ? new Color(0.45f, 0.78f, 0.45f) : new Color(0.4f, 0.72f, 0.4f);
                    tile.transform.position = new Vector3(x + 1, y + 1, 1);
                    tile.transform.localScale = new Vector3(50, 50, 1);
                }

            // simple "city" blocks around the edges
            var rng = new System.Random(_level.level * 77);
            for (int i = 0; i < 6; i++)
            {
                var b = new GameObject("building");
                var sr = b.AddComponent<SpriteRenderer>();
                sr.sprite = UIFactory.WhiteSprite();
                sr.color = new Color(0.55f + (float)rng.NextDouble() * 0.2f, 0.5f, 0.6f);
                float bx = i % 2 == 0 ? -8f + i * 3.2f : 8f - i * 2.6f;
                float by = i < 3 ? 4.2f : -4.2f;
                b.transform.position = new Vector3(bx, by, 0);
                b.transform.localScale = new Vector3(28, 34, 1);
            }
        }

        void BuildPlayer()
        {
            var go = new GameObject("Player");
            go.transform.position = new Vector3(-7f, -4f, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _circleSprite;
            int c = Mathf.Max(0, GameManager.I.selectedCharacter);
            Color[] cols = { new Color(0.2f, 0.6f, 0.95f), new Color(0.55f, 0.35f, 0.85f), new Color(0.15f, 0.7f, 0.55f), new Color(0.95f, 0.55f, 0.2f) };
            sr.color = cols[c % cols.Length];
            go.transform.localScale = new Vector3(18, 18, 1);
            go.AddComponent<Rigidbody2D>();
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 2f;
            go.AddComponent<PlayerController>();
            _player = go.transform;
        }

        void BuildFault(Vector3 pos)
        {
            _faultPos = pos;
            var go = new GameObject("Fault");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _circleSprite;
            sr.color = new Color(1f, 0.15f, 0.1f);
            sr.sortingOrder = 2;
            go.transform.localScale = new Vector3(16, 16, 1);
            go.AddComponent<FaultPulse>();
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 3f; col.isTrigger = true;
            _faultMarker = sr;
        }

        // ---------------- HUD ----------------

        void BuildHud()
        {
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.EnsureCanvas("GameHud");
            canvas.gameObject.AddComponent<UISoundManager>();

            // waste counter (top left)
            var wasteRt = UIFactory.Place(canvas.transform, "Waste", 0.02f, 0.9f, 0.3f, 0.99f);
            UIFactory.Panel(wasteRt, "bg", new Color(0.1f, 0.25f, 0.45f, 0.85f));
            _wasteText = UIFactory.Label(wasteRt, "t", "", 30, Color.white);
            _wasteText.fontStyle = FontStyle.Bold;

            // level title (top center)
            var titleRt = UIFactory.Place(canvas.transform, "Title", 0.33f, 0.9f, 0.66f, 0.99f);
            _titleText = UIFactory.Label(titleRt, "t", "", 28, Color.white);
            _titleText.fontStyle = FontStyle.Bold;
            var titleBg = UIFactory.Place(canvas.transform, "TitleBg", 0.33f, 0.9f, 0.66f, 0.99f);
            var bg = UIFactory.Panel(titleBg, "bg", new Color(0.1f, 0.25f, 0.45f, 0.7f));
            titleBg.SetSiblingIndex(titleRt.GetSiblingIndex());

            // menu button (next to counter)
            var menuRt = UIFactory.Place(canvas.transform, "MenuBtn", 0.02f, 0.8f, 0.1f, 0.89f);
            UIFactory.MakeButton(menuRt, "b", "🏠", 26, new Color(0.1f, 0.55f, 0.95f), Color.white, () => GameManager.I.GoToMenu());

            // minimap (top right)
            var mini = UIFactory.Place(canvas.transform, "Minimap", 0.78f, 0.76f, 0.985f, 0.99f);
            var miniBg = UIFactory.Panel(mini, "bg", new Color(0.85f, 0.93f, 1f, 0.9f));
            var frame = miniBg.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(0.1f, 0.35f, 0.6f);
            var mmLabel = UIFactory.Place(mini, "l", 0, 0.85f, 1, 1f);
            UIFactory.Label(mmLabel, "t", "الخريطة", 18, new Color(0.1f, 0.35f, 0.6f));

            _miniFault = UIFactory.Place(mini, "fdot", 0, 0, 0.09f, 0.14f);
            UIFactory.Panel(_miniFault, "d", new Color(1f, 0.1f, 0.1f));
            _miniPlayer = UIFactory.Place(mini, "pdot", 0, 0, 0.07f, 0.11f);
            UIFactory.Panel(_miniPlayer, "d", new Color(0.1f, 0.4f, 1f));

            var ov = UIFactory.Root(canvas.transform, "Overlay");
            _overlay = ov;
            ov.gameObject.SetActive(false);
        }

        Vector2 WorldToMini(Vector3 w)
        {
            float x = Mathf.InverseLerp(PlayerController.WorldBounds.xMin, PlayerController.WorldBounds.xMax, w.x);
            float y = Mathf.InverseLerp(PlayerController.WorldBounds.yMin, PlayerController.WorldBounds.yMax, w.y);
            return new Vector2(x, y);
        }

        // ---------------- overlays ----------------

        void ShowIntro()
        {
            running = false;
            _overlay.gameObject.SetActive(true);
            foreach (Transform c in _overlay) Destroy(c.gameObject);

            var card = UIFactory.Place(_overlay, "card", 0.2f, 0.2f, 0.8f, 0.82f);
            UIFactory.Panel(card, "bg", new Color(1, 1, 1, 0.98f));
            var tRt = UIFactory.Place(card, "t", 0.05f, 0.72f, 0.95f, 0.97f);
            var t = UIFactory.Label(tRt, "x", _level.title, 40, new Color(0.1f, 0.4f, 0.7f));
            t.fontStyle = FontStyle.Bold;
            var bRt = UIFactory.Place(card, "b", 0.08f, 0.3f, 0.92f, 0.7f);
            UIFactory.Label(bRt, "x", _level.intro + "\n\nشخصيتك: " + GameManager.CharacterNames[Mathf.Max(0, GameManager.I.selectedCharacter)], 26, new Color(0.2f, 0.3f, 0.45f));
            var sRt = UIFactory.Place(card, "s", 0.3f, 0.05f, 0.7f, 0.24f);
            var sb = UIFactory.MakeButton(sRt, "x", "ابدأ! 🚀", 36, new Color(0.15f, 0.8f, 0.3f), Color.white, () =>
            {
                _overlay.gameObject.SetActive(false);
                running = true;
            });
            sb.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
        }

        void ShowChoices()
        {
            running = false; // freeze waste while deciding (kid-friendly)
            _overlay.gameObject.SetActive(true);
            foreach (Transform c in _overlay) Destroy(c.gameObject);

            var card = UIFactory.Place(_overlay, "card", 0.18f, 0.15f, 0.82f, 0.88f);
            UIFactory.Panel(card, "bg", new Color(1, 1, 1, 0.98f));
            var tRt = UIFactory.Place(card, "t", 0.05f, 0.84f, 0.95f, 0.99f);
            var t = UIFactory.Label(tRt, "x", "وصلت! العطل: " + _level.faultName, 34, new Color(0.85f, 0.3f, 0.1f));
            t.fontStyle = FontStyle.Bold;
            var qRt = UIFactory.Place(card, "q", 0.05f, 0.72f, 0.95f, 0.85f);
            UIFactory.Label(qRt, "x", "إزاي تصلحه؟ اختار الإجابة الصح:", 26, new Color(0.2f, 0.3f, 0.45f));

            var explRt = UIFactory.Place(card, "expl", 0.06f, 0.02f, 0.94f, 0.3f);
            var expl = UIFactory.Label(explRt, "x", "", 24, new Color(0.8f, 0.2f, 0.2f), TextAnchor.MiddleCenter);

            var shuffled = new List<FixChoice>(_level.choices);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int j = Random.Range(i, shuffled.Count);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            for (int i = 0; i < shuffled.Count; i++)
            {
                var choice = shuffled[i];
                float y1 = 0.34f + (2 - i) * 0.13f;
                var bRt = UIFactory.Place(card, "c" + i, 0.08f, y1, 0.92f, y1 + 0.115f);
                var btn = UIFactory.MakeButton(bRt, "x", choice.text, 28, new Color(0.15f, 0.55f, 0.95f), Color.white, null);
                btn.onClick.AddListener(() =>
                {
                    if (choice.correct) { expl.text = ""; Win(choice); }
                    else
                    {
                        btn.interactable = false;
                        btn.GetComponent<Image>().color = new Color(0.6f, 0.6f, 0.6f);
                        ArabicText.Set(expl, "❌ " + choice.explanation + "\nجرب إجابة تانية!");
                    }
                });
            }
        }

        void Win(FixChoice correct)
        {
            over = true;
            int earned = waste < 8f ? 3 : waste < 16f ? 2 : 1;
            GameManager.I.CompleteLevel(_level.level, earned);
            if (_faultMarker != null) _faultMarker.color = new Color(0.2f, 0.85f, 0.3f);
            ClearArrows();

            foreach (Transform c in _overlay) Destroy(c.gameObject);
            _overlay.gameObject.SetActive(true);
            var card = UIFactory.Place(_overlay, "card", 0.22f, 0.16f, 0.78f, 0.88f);
            UIFactory.Panel(card, "bg", new Color(1, 1, 1, 0.98f));

            var tRt = UIFactory.Place(card, "t", 0.05f, 0.84f, 0.95f, 0.98f);
            var t = UIFactory.Label(tRt, "x", "برافو! صلحت العطل! 🎉", 38, new Color(0.1f, 0.6f, 0.25f));
            t.fontStyle = FontStyle.Bold;

            var starRt = UIFactory.Place(card, "stars", 0.1f, 0.6f, 0.9f, 0.84f);
            UIFactory.Label(starRt, "x", new string('⭐', earned) + new string('☆', 3 - earned), 70, new Color(0.95f, 0.75f, 0.1f));

            var wRt = UIFactory.Place(card, "w", 0.08f, 0.42f, 0.92f, 0.6f);
            UIFactory.Label(wRt, "x", "المياه المهدورة: " + waste.ToString("0.0") + " لتر\n✅ " + correct.explanation, 26, new Color(0.2f, 0.3f, 0.45f));

            bool last = _level.level >= GameData.MaxLevels;
            var nRt = UIFactory.Place(card, "n", 0.1f, 0.06f, 0.55f, 0.34f * 0.75f);
            var nb = UIFactory.MakeButton(nRt, "x", last ? "القائمة الرئيسية 🏠" : "المستوى التالي ▶", 32, new Color(0.15f, 0.8f, 0.3f), Color.white, () =>
            {
                if (last) GameManager.I.GoToMenu(); else GameManager.I.NextLevelOrMenu();
            });
            nb.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;

            var rRt = UIFactory.Place(card, "r", 0.6f, 0.06f, 0.9f, 0.32f * 0.75f);
            UIFactory.MakeButton(rRt, "x", "إعادة 🔁", 30, new Color(0.95f, 0.6f, 0.15f), Color.white,
                () => GameManager.LoadSceneSmart(GameManager.SceneGame, "Assets/Scenes/Game.unity"));

            if (last)
            {
                var totRt = UIFactory.Place(card, "tot", 0.1f, 0.32f, 0.9f, 0.44f);
                UIFactory.Label(totRt, "x", "خلصت كل المستويات! مجموع نجومك: " + GameManager.I.TotalStars() + " / 15 🏆", 26, new Color(0.55f, 0.35f, 0.05f));
            }
        }

        void GameOver()
        {
            over = true;
            running = false;
            ClearArrows();
            foreach (Transform c in _overlay) Destroy(c.gameObject);
            _overlay.gameObject.SetActive(true);
            var card = UIFactory.Place(_overlay, "card", 0.22f, 0.24f, 0.78f, 0.8f);
            UIFactory.Panel(card, "bg", new Color(1f, 0.97f, 0.97f, 0.99f));
            var tRt = UIFactory.Place(card, "t", 0.05f, 0.76f, 0.95f, 0.97f);
            var t = UIFactory.Label(tRt, "x", "انتهت اللعبة! 💧", 44, new Color(0.85f, 0.2f, 0.15f));
            t.fontStyle = FontStyle.Bold;
            var bRt = UIFactory.Place(card, "b", 0.08f, 0.42f, 0.92f, 0.74f);
            UIFactory.Label(bRt, "x", "وصل هدر المياه 25 لتر قبل ما تصلح العطل.\nالمرة الجاية اتبع الأسهم أسرع! 💪", 26, new Color(0.3f, 0.3f, 0.4f));
            var rRt = UIFactory.Place(card, "r", 0.08f, 0.08f, 0.5f, 0.36f);
            var rb = UIFactory.MakeButton(rRt, "x", "حاول تاني 🔁", 32, new Color(0.15f, 0.8f, 0.3f), Color.white,
                () => GameManager.LoadSceneSmart(GameManager.SceneGame, "Assets/Scenes/Game.unity"));
            rb.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            var mRt = UIFactory.Place(card, "m", 0.55f, 0.08f, 0.92f, 0.36f);
            UIFactory.MakeButton(mRt, "x", "القائمة 🏠", 32, new Color(0.15f, 0.55f, 0.95f), Color.white, () => GameManager.I.GoToMenu());
        }

        // ---------------- arrows ----------------

        void ClearArrows()
        {
            foreach (var a in _arrows) if (a != null) Destroy(a.gameObject);
            _arrows.Clear();
        }

        void RefreshArrows()
        {
            ClearArrows();
            if (!running || over) return;
            Vector3 from = _player.position;
            Vector3 to = _faultPos;
            Vector3 dir = (to - from).normalized;
            float dist = Vector3.Distance(from, to);
            int count = Mathf.Min(10, Mathf.FloorToInt(dist / 1.4f));
            for (int i = 1; i <= count; i++)
            {
                var go = new GameObject("arrow");
                go.transform.position = from + dir * (i * 1.4f);
                go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _arrowSprite;
                sr.color = new Color(1f, 0.9f, 0.1f, 0.9f);
                sr.sortingOrder = 1;
                go.transform.localScale = new Vector3(10, 14, 1);
                _arrows.Add(go.transform);
            }
        }

        // ---------------- main loop ----------------

        void Update()
        {
            if (_level == null) return;

            if (running && !over)
            {
                waste += _level.wastePerSecond * Time.deltaTime;
                if (waste >= GameData.WasteLimit) { waste = GameData.WasteLimit; GameOver(); }

                _arrowTimer -= Time.deltaTime;
                if (_arrowTimer <= 0f) { _arrowTimer = 0.5f; RefreshArrows(); }

                if (Vector3.Distance(_player.position, _faultPos) <= _reachedRadius)
                    ShowChoices();
            }

            if (_wasteText != null)
            {
                ArabicText.Set(_wasteText, "🚰 الهدر: " + waste.ToString("0.0") + " / 25 لتر");
                _wasteText.color = waste > 18f ? new Color(1f, 0.4f, 0.3f) : Color.white;
            }
            if (_titleText != null)
                ArabicText.Set(_titleText, _level.title + "  |  " + GameManager.CharacterNames[Mathf.Max(0, GameManager.I.selectedCharacter)]);

            // minimap dots
            if (_miniPlayer != null && _player != null)
            {
                Vector2 m = WorldToMini(_player.position);
                _miniPlayer.anchorMin = new Vector2(m.x - 0.035f, m.y - 0.055f);
                _miniPlayer.anchorMax = new Vector2(m.x + 0.035f, m.y + 0.055f);
            }
            if (_miniFault != null)
            {
                Vector2 m = WorldToMini(_faultPos);
                _miniFault.anchorMin = new Vector2(m.x - 0.045f, m.y - 0.07f);
                _miniFault.anchorMax = new Vector2(m.x + 0.045f, m.y + 0.07f);
            }
        }
    }
}
