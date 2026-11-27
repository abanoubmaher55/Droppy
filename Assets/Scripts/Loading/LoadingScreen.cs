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

            // Switch off any other canvas already in the scene (old loading text / old bar / old background
            // left over from earlier setups). Only our own canvas should draw on this screen.
            foreach (var other in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other == canvas || other.transform.IsChildOf(canvas.transform)) continue;
                Debug.Log("[SaveWaterEG] Loading: disabled stray canvas '" + other.name + "' (" + other.GetComponentsInChildren<Graphic>(true).Length + " UI elements)");
                other.gameObject.SetActive(false);
            }

            // Solid sky colour behind everything (shows only in letterbox bars on odd screen shapes).
            UIFactory.Panel(canvas.transform, "Sky", new Color(0.35f, 0.72f, 0.95f)).GetComponent<Image>().raycastTarget = false;

            // "Stage": a rect that always has the art's aspect ratio and fits fully inside the screen
            // (no cropping, any window shape / Game view size). Art and bar are children of it, so the
            // bar always stays in the same spot on the art.
            var bgTex = Resources.Load<Texture2D>("Art/loading_bg");
            float aspect = bgTex != null ? (float)bgTex.width / bgTex.height : 16f / 9f;
            var stage = UIFactory.Root(canvas.transform, "Stage");
            var fitter = stage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = aspect;

            if (bgTex != null)
            {
                var bgRt = UIFactory.Root(stage, "BG");
                var raw = bgRt.gameObject.AddComponent<RawImage>();
                raw.texture = bgTex;
                raw.color = Color.white;
                raw.raycastTarget = false;
            }
            else
            {
                Debug.LogWarning("[SaveWaterEG] Art/loading_bg NOT found in Resources/Art");
            }

            // progress bar inside the stage (x .265-.735, y-up .108-.160 = the white box the user drew)
            var frame = UIFactory.Place(stage, "BarFrame", 0.265f, 0.108f, 0.735f, 0.160f);
            UIFactory.Panel(frame, "frame", new Color(1, 1, 1, 0.55f));
            var fillArea = UIFactory.Place(frame, "FillArea", 0.015f, 0.18f, 0.985f, 0.82f);
            _fillRt = UIFactory.Place(fillArea, "Fill", 0, 0, 0.001f, 1f);
            UIFactory.Panel(_fillRt, "f", new Color(1f, 0.8f, 0.1f));
            _percent = UIFactory.Label(frame, "p", "1%", 30, new Color(0.05f, 0.3f, 0.6f));
            _percent.fontStyle = FontStyle.Bold;

            // our own loading text, just under the bar
            var lblRt = UIFactory.Place(stage, "LoadingText", 0.265f, 0.045f, 0.735f, 0.105f);
            var lbl = UIFactory.Label(lblRt, "t", "جاري التحميل...", 34, Color.white);
            lbl.fontStyle = FontStyle.Bold;
            var ol = lbl.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.05f, 0.25f, 0.5f, 0.9f);
            ol.effectDistance = new Vector2(2, -2);
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
