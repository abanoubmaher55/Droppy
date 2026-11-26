using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // One level in the 3D city: a role-specific water fault somewhere on the
    // road grid. The waste counter rises until the player follows the arrows,
    // reaches the fault and picks the correct fix. 25 L wasted = game over.
    public class GameplayController : MonoBehaviour
    {
        [Header("Runtime (auto)")]
        public float waste;
        public bool running;
        public bool over;

        LevelData _level;
        CityResult _city;
        Transform _player;
        CharacterRig _playerRig;
        Vector3 _faultPos;
        float _reachedRadius = 3f;
        FaultVisual _fault;
        ArrowTrail _trail;
        MinimapRig _minimap;
        Droppy3D _droppy;

        // HUD
        Text _wasteText;
        Text _titleText;
        Text _wrongWayText;
        RectTransform _overlay;

        // wrong-way detection
        Dictionary<RoadNode, int> _distField;
        float _arrowTimer;
        float _wrongWayTimer;
        float _badDirectionTime;
        int _lastDist = -1;

        void Start()
        {
            int role = Mathf.Max(0, GameManager.I.selectedCharacter);
            var faults = GameData.LoadRoleFaults(role);
            int idx = Mathf.Clamp(GameManager.I.currentLevel, 1, GameData.MaxLevels) - 1;
            _level = idx < faults.Count ? faults[idx] : FallbackLevel(idx);

            _city = CityBuilder.Build(20261125);
            BuildCamera();
            BuildPlayer(role);
            BuildFault(idx);
            BuildNpcs(9, 555 + idx);
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

        void BuildCamera()
        {
            var cam = UIFactory.EnsureCamera(new Color(0.55f, 0.78f, 0.95f));
            cam.orthographic = false;
            cam.fieldOfView = 55f;
            cam.transform.position = _city.spawn + new Vector3(0, 12f, -9.5f);
            var fc = cam.gameObject.AddComponent<FollowCamera>();
            // target wired after the player exists (BuildPlayer)
            _followCam = fc;
            _mainCam = cam;
        }

        FollowCamera _followCam;
        Camera _mainCam;
        PlayerController3D _pc;

        void BuildPlayer(int role)
        {
            var go = new GameObject("Player");
            // starts in the sky and drops in with Droppy at the level start
            go.transform.position = _city.spawn + Vector3.up * 9f;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.7f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0, 0.85f, 0);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.4f;

            string prefabName = CharacterRig.RolePrefabNames[role % CharacterRig.RolePrefabNames.Length];
            _playerRig = CharacterRig.Create(go.transform, prefabName,
                CharacterRig.RoleColors[role % CharacterRig.RoleColors.Length], true);

            var pc = go.AddComponent<PlayerController3D>();
            pc.bound = _city.bound;
            pc.SetRig(_playerRig);
            _pc = pc;
            _pc.enabled = false; // walks once the intro's start button is pressed
            _player = go.transform;
            if (_followCam != null) _followCam.target = _player;

            _droppy = Droppy3D.Create(_player);
            _droppy.transform.position = _player.position + new Vector3(1.4f, 4f, 0.4f);
        }

        void BuildFault(int idx)
        {
            var spots = _city.faultSpots;
            _faultPos = spots[idx % spots.Count];
            _fault = FaultVisual.Create(_faultPos, idx + GameManager.I.selectedCharacter, _city.root);
            _distField = _city.graph.DistancesFrom(_city.graph.Nearest(_faultPos));
            _trail = ArrowTrail.Create(_city.root);
        }

        void BuildNpcs(int count, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var start = _city.graph.nodes[rng.Next(_city.graph.nodes.Count)];
                // don't start an NPC on top of the fault or the player
                if ((start.pos - _faultPos).sqrMagnitude < 25f) continue;
                if ((start.pos - _city.spawn).sqrMagnitude < 25f) continue;
                NpcWalker.Spawn(_city.graph, start, rng);
            }
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
            _wasteText = UIFactory.Label(wasteRt, "t", "x", 30, Color.white);
            _wasteText.fontStyle = FontStyle.Bold;

            // level title (top center)
            var titleRt = UIFactory.Place(canvas.transform, "Title", 0.33f, 0.9f, 0.66f, 0.99f);
            UIFactory.Panel(titleRt, "bg", new Color(0.1f, 0.25f, 0.45f, 0.7f));
            _titleText = UIFactory.Label(titleRt, "t", "x", 28, Color.white);
            _titleText.fontStyle = FontStyle.Bold;

            // menu button
            var menuRt = UIFactory.Place(canvas.transform, "MenuBtn", 0.02f, 0.8f, 0.1f, 0.89f);
            UIFactory.MakeButton(menuRt, "b", "🏠", 26, new Color(0.1f, 0.55f, 0.95f), Color.white, () => GameManager.I.GoToMenu());

            // minimap (top right): live render of the city, player + fault as dots
            var mini = UIFactory.Place(canvas.transform, "Minimap", 0.78f, 0.74f, 0.985f, 0.99f);
            var miniBg = UIFactory.Panel(mini, "bg", new Color(0.85f, 0.93f, 1f, 0.9f));
            var frame = miniBg.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(0.1f, 0.35f, 0.6f);
            var mmLabel = UIFactory.Place(mini, "l", 0, 0.85f, 1, 1f);
            UIFactory.Label(mmLabel, "t", "الخريطة", 18, new Color(0.1f, 0.35f, 0.6f));
            var rawRt = UIFactory.Place(mini, "view", 0.02f, 0.02f, 0.98f, 0.84f);
            var raw = rawRt.gameObject.AddComponent<RawImage>();
            raw.color = Color.white;
            _minimap = MinimapRig.Create(raw, _mainCam);
            _minimap.target = _player;
            _minimap.SetFault(_faultPos);

            // wrong-way warning (center, hidden)
            var warnRt = UIFactory.Place(canvas.transform, "Warn", 0.2f, 0.62f, 0.8f, 0.72f);
            UIFactory.Panel(warnRt, "bg", new Color(0.9f, 0.25f, 0.15f, 0.92f));
            _wrongWayText = UIFactory.Label(warnRt, "t", "⚠️ أنت ماشي غلط، لازم تمشي في المكان الصح", 30, Color.white);
            _wrongWayText.fontStyle = FontStyle.Bold;
            warnRt.gameObject.SetActive(false);
            _warnPanel = warnRt;

            // joystick (bottom-left)
            JoystickUI.Create(canvas.transform);

            var ov = UIFactory.Root(canvas.transform, "Overlay");
            _overlay = ov;
            ov.gameObject.SetActive(false);
        }

        RectTransform _warnPanel;

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
            UIFactory.Label(bRt, "x", _level.intro + "\n\nشخصيتك: " + GameManager.CharacterNames[Mathf.Max(0, GameManager.I.selectedCharacter)] + "\nاتبع الأسهم الصفرا على الطريق! 💛", 26, new Color(0.2f, 0.3f, 0.45f));
            var sRt = UIFactory.Place(card, "s", 0.3f, 0.05f, 0.7f, 0.24f);
            var sb = UIFactory.MakeButton(sRt, "x", "ابدأ! 🚀", 36, new Color(0.15f, 0.8f, 0.3f), Color.white, () =>
            {
                _overlay.gameObject.SetActive(false);
                if (_pc != null) _pc.enabled = true;
                running = true;
            });
            sb.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
        }

        void ShowChoices()
        {
            running = false; // freeze waste while deciding (kid-friendly)
            if (_pc != null) _pc.enabled = false;
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
            var expl = UIFactory.Label(explRt, "x", "x", 24, new Color(0.8f, 0.2f, 0.2f), TextAnchor.MiddleCenter);
            expl.text = "";

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
                    if (choice.correct) { Win(choice); }
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
            if (_pc != null) _pc.enabled = false;
            int earned = waste < 8f ? 3 : waste < 16f ? 2 : 1;
            GameManager.I.CompleteLevel(_level.level, earned);
            if (_fault != null) _fault.SetFixed();
            if (_trail != null) _trail.ClearTrail();
            if (_minimap != null) _minimap.ClearFault();

            foreach (Transform c in _overlay) Destroy(c.gameObject);
            _overlay.gameObject.SetActive(true);
            var card = UIFactory.Place(_overlay, "card", 0.2f, 0.12f, 0.8f, 0.9f);
            UIFactory.Panel(card, "bg", new Color(1, 1, 1, 0.98f));

            var tRt = UIFactory.Place(card, "t", 0.05f, 0.85f, 0.95f, 0.99f);
            var t = UIFactory.Label(tRt, "x", "برافو! صلحت العطل! 🎉", 38, new Color(0.1f, 0.6f, 0.25f));
            t.fontStyle = FontStyle.Bold;

            var starRt = UIFactory.Place(card, "stars", 0.1f, 0.63f, 0.9f, 0.85f);
            UIFactory.Label(starRt, "x", new string('⭐', earned) + new string('☆', 3 - earned), 70, new Color(0.95f, 0.75f, 0.1f));

            var wRt = UIFactory.Place(card, "w", 0.08f, 0.45f, 0.92f, 0.63f);
            UIFactory.Label(wRt, "x", "المياه المهدورة: " + waste.ToString("0.0") + " لتر\n✅ " + correct.explanation, 24, new Color(0.2f, 0.3f, 0.45f));

            bool last = _level.level >= GameData.MaxLevels;
            if (last)
            {
                var totRt = UIFactory.Place(card, "tot", 0.1f, 0.34f, 0.9f, 0.46f);
                UIFactory.Label(totRt, "x", "خلصت كل مستويات " + GameManager.CharacterNames[Mathf.Max(0, GameManager.I.selectedCharacter)] + "! مجموع نجومك: " + GameManager.I.TotalStars() + " 🏆", 24, new Color(0.55f, 0.35f, 0.05f));
            }

            // three buttons: next level / main menu / retry
            var nRt = UIFactory.Place(card, "n", 0.06f, 0.05f, 0.37f, 0.28f);
            var nb = UIFactory.MakeButton(nRt, "x", last ? "العب تاني من الأول ▶" : "المستوى التالي ▶", 26, new Color(0.15f, 0.8f, 0.3f), Color.white, () =>
            {
                if (last) { GameManager.I.currentLevel = 1; GameManager.LoadSceneSmart(GameManager.SceneGame, "Assets/Scenes/Game.unity"); }
                else GameManager.I.NextLevelOrMenu();
            });
            nb.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;

            var mRt = UIFactory.Place(card, "m", 0.385f, 0.05f, 0.625f, 0.28f);
            UIFactory.MakeButton(mRt, "x", "القائمة 🏠", 26, new Color(0.15f, 0.55f, 0.95f), Color.white, () => GameManager.I.GoToMenu());

            var rRt = UIFactory.Place(card, "r", 0.64f, 0.05f, 0.94f, 0.28f);
            UIFactory.MakeButton(rRt, "x", "إعادة 🔁", 26, new Color(0.95f, 0.6f, 0.15f), Color.white,
                () => GameManager.LoadSceneSmart(GameManager.SceneGame, "Assets/Scenes/Game.unity"));
        }

        void GameOver()
        {
            over = true;
            running = false;
            if (_pc != null) _pc.enabled = false;
            if (_trail != null) _trail.ClearTrail();
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

        // ---------------- main loop ----------------

        void Update()
        {
            if (_level == null) return;

            if (running && !over && _player != null)
            {
                waste += _level.wastePerSecond * Time.deltaTime;
                if (waste >= GameData.WasteLimit) { waste = GameData.WasteLimit; GameOver(); }

                _arrowTimer -= Time.deltaTime;
                if (_arrowTimer <= 0f) { _arrowTimer = 0.5f; RefreshArrows(); CheckWrongWay(); }

                Vector3 flat = _player.position - _faultPos; flat.y = 0;
                if (flat.magnitude <= _reachedRadius)
                    ShowChoices();
            }

            if (_wasteText != null)
            {
                ArabicText.Set(_wasteText, "🚰 الهدر: " + waste.ToString("0.0") + " / 25 لتر");
                _wasteText.color = waste > 18f ? new Color(1f, 0.4f, 0.3f) : Color.white;
            }
            if (_titleText != null)
                ArabicText.Set(_titleText, _level.title + "  |  " + GameManager.CharacterNames[Mathf.Max(0, GameManager.I.selectedCharacter)]);

            // wrong-way message visibility
            if (_wrongWayTimer > 0f)
            {
                _wrongWayTimer -= Time.deltaTime;
                if (_wrongWayTimer <= 0f && _warnPanel != null) _warnPanel.gameObject.SetActive(false);
            }
        }

        void RefreshArrows()
        {
            if (_trail == null || _player == null) return;
            var from = _city.graph.Nearest(_player.position);
            var to = _city.graph.Nearest(_faultPos);
            var path = _city.graph.Path(from, to);
            if (path == null) { _trail.ClearTrail(); return; }
            var pts = new List<Vector3>();
            foreach (var n in path) pts.Add(n.pos);
            _trail.SetPath(pts);
        }

        void CheckWrongWay()
        {
            if (_player == null || _distField == null) return;
            bool moving = JoystickUI.Direction.sqrMagnitude > 0.04f
                || Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.2f
                || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.2f;
            if (!moving) { _badDirectionTime = 0f; return; }

            var node = _city.graph.Nearest(_player.position);
            int dist;
            if (!_distField.TryGetValue(node, out dist)) return;
            if (_lastDist >= 0 && dist > _lastDist) _badDirectionTime += 0.5f;
            else _badDirectionTime = Mathf.Max(0f, _badDirectionTime - 0.5f);
            _lastDist = dist;

            if (_badDirectionTime >= 2.5f && _wrongWayTimer <= 0f)
            {
                _badDirectionTime = 0f;
                _wrongWayTimer = 3f;
                if (_warnPanel != null) _warnPanel.gameObject.SetActive(true);
            }
        }
    }
}
