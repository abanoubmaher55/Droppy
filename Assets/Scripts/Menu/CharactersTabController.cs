using UnityEngine;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // Characters tab: pick one of 4 heroes (فني، كيميائي، محصل مياه، مهندس مياه),
    // then press العب (Play) to start the game.
    public class CharactersTabController : MonoBehaviour
    {
        int _selected = -1;
        Image[] _cards;
        Text _playLabel;

        static readonly Color[] CardColors =
        {
            new Color(0.2f, 0.6f, 0.95f),
            new Color(0.55f, 0.35f, 0.85f),
            new Color(0.15f, 0.7f, 0.55f),
            new Color(0.95f, 0.55f, 0.2f),
        };

        void Start()
        {
            var root = (RectTransform)transform;

            var headRt = UIFactory.Place(root, "Head", 0.25f, 0.9f, 0.75f, 1f);
            var head = UIFactory.Label(headRt, "t", "اختار بطلك! 🦸", 40, new Color(0.35f, 0.2f, 0.6f));
            head.fontStyle = FontStyle.Bold;

            _cards = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                float x0 = 0.06f + i * 0.24f;
                var cardRt = UIFactory.Place(root, "Card" + i, x0, 0.32f, x0 + 0.21f, 0.86f);
                int idx = i;
                var b = UIFactory.MakeButton(cardRt, "b", "", 30, Color.white, Color.white, () => Select(idx));
                _cards[i] = b.GetComponent<Image>();
                _cards[i].color = CardColors[i] * 0.55f + new Color(0.45f, 0.45f, 0.45f);

                var iconRt = UIFactory.Place(cardRt, "icon", 0.1f, 0.55f, 0.9f, 0.97f);
                var icon = UIFactory.Label(iconRt, "t", GameManager.CharacterIcons[i], 80, Color.white);
                var nameRt = UIFactory.Place(cardRt, "name", 0.03f, 0.4f, 0.97f, 0.56f);
                var nameT = UIFactory.Label(nameRt, "t", GameManager.CharacterNames[i], 32, Color.white);
                nameT.fontStyle = FontStyle.Bold;
                var blurbRt = UIFactory.Place(cardRt, "blurb", 0.05f, 0.04f, 0.95f, 0.4f);
                UIFactory.Label(blurbRt, "t", GameManager.CharacterBlurbs[i], 20, new Color(1, 1, 1, 0.95f), TextAnchor.UpperCenter);
            }

            // Play button
            var playRt = UIFactory.Place(root, "Play", 0.32f, 0.04f, 0.68f, 0.26f);
            var playB = UIFactory.MakeButton(playRt, "b", "اختار شخصية الأول 👆", 46, new Color(0.15f, 0.8f, 0.3f), Color.white, Play);
            _playLabel = playB.GetComponentInChildren<Text>();
            _playLabel.fontStyle = FontStyle.Bold;
            RefreshPlay();
        }

        void Select(int i)
        {
            _selected = i;
            GameManager.I.SelectCharacter(i);
            for (int k = 0; k < 4; k++)
            {
                bool on = k == i;
                _cards[k].color = on ? CardColors[k] : CardColors[k] * 0.55f + new Color(0.45f, 0.45f, 0.45f);
                _cards[k].transform.localScale = on ? Vector3.one * 1.05f : Vector3.one;
            }
            RefreshPlay();
        }

        void RefreshPlay()
        {
            ArabicText.Set(_playLabel, _selected < 0 ? "اختار شخصية الأول 👆" : "العب بـ" + GameManager.CharacterNames[_selected] + " ▶");
        }

        void Play()
        {
            if (_selected < 0) { RefreshPlay(); return; }
            GameManager.I.StartGame();
        }
    }
}
