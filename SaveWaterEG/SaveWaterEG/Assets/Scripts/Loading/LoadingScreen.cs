using UnityEngine;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // Loading scene: the user's loading art fills the screen; a real progress
    // bar fills 1 -> 100% over the art's bar strip, then opens the main menu.
    public class LoadingScreen : MonoBehaviour
    {
        public float duration = 3.5f;

        RectTransform _fillRt;
        Text _percent;
        float _t;

        void Start()
        {
            UIFactory.EnsureCamera(new Color(0.35f, 0.72f, 0.95f));
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.EnsureCanvas("LoadingCanvas");

            // full-screen loading art (extracted from the user's design)
            var bgSprite = Resources.Load<Sprite>("Art/loading_bg");
            if (bgSprite != null)
            {
                var bg = UIFactory.Img(canvas.transform, "BG", bgSprite, Color.white, false);
                bg.type = Image.Type.Simple;
            }
            else
            {
                UIFactory.Panel(canvas.transform, "BG", new Color(0.35f, 0.72f, 0.95f));
            }

            // real progress bar over the art's bar strip (frame x .27-.73, y-up .25-.32)
            var frame = UIFactory.Place(canvas.transform, "BarFrame", 0.27f, 0.255f, 0.73f, 0.32f);
            UIFactory.Panel(frame, "frame", new Color(1, 1, 1, 0.55f));
            var fillArea = UIFactory.Place(frame, "FillArea", 0.015f, 0.18f, 0.985f, 0.82f);
            _fillRt = UIFactory.Place(fillArea, "Fill", 0, 0, 0.001f, 1f);
            UIFactory.Panel(_fillRt, "f", new Color(1f, 0.8f, 0.1f));
            _percent = UIFactory.Label(frame, "p", "1%", 30, new Color(0.05f, 0.3f, 0.6f));
            _percent.fontStyle = FontStyle.Bold;

            // little animated Droppy cheering in the corner while loading
            var droppyRt = UIFactory.Place(canvas.transform, "Droppy", 0.82f, 0.03f, 0.97f, 0.24f);
            var droppySprite = Resources.Load<Sprite>("Art/droppy");
            if (droppySprite != null)
            {
                UIFactory.Img(droppyRt, "img", droppySprite, Color.white);
                droppyRt.gameObject.AddComponent<DroppyAnimator>();
            }
        }

        void Update()
        {
            _t += Time.deltaTime;
            float p = Mathf.Clamp01(_t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 2f);
            int shown = Mathf.Max(1, Mathf.RoundToInt(eased * 100f));
            _percent.text = shown + "%";
            _fillRt.anchorMax = new Vector2(Mathf.Max(0.001f, eased), 1f);
            if (p >= 1f) GameManager.LoadSceneSmart(GameManager.SceneMenu, "Assets/Scenes/MainMenu.unity");
        }
    }
}
