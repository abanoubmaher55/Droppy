using UnityEngine;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // Plays a click sound for every UI Button in the scene, including
    // transparent tab buttons and buttons that start inactive.
    // Put one on any object; it finds all buttons and re-scans when tabs change.
    public class UISoundManager : MonoBehaviour
    {
        public static UISoundManager I { get; private set; }

        [Tooltip("Short click sound. If empty, tries Resources/Audio/click.")]
        public AudioClip clickSound;
        [Range(0f, 1f)] public float volume = 0.8f;

        AudioSource _src;
        readonly System.Collections.Generic.HashSet<Button> _hooked = new System.Collections.Generic.HashSet<Button>();
        float _nextScan;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
            if (clickSound == null) clickSound = Resources.Load<AudioClip>("Audio/click");
            HookAll();
        }

        void Update()
        {
            // cheap re-scan so buttons created later (tabs, popups) also click
            if (Time.unscaledTime >= _nextScan) { _nextScan = Time.unscaledTime + 1.5f; HookAll(); }
        }

        public static void HookAllStatic() { if (I != null) I.HookAll(); }

        void HookAll()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in buttons)
            {
                if (b == null || _hooked.Contains(b)) continue;
                _hooked.Add(b);
                b.onClick.AddListener(PlayClick);
            }
        }

        public void PlayClick()
        {
            if (clickSound != null && _src != null) _src.PlayOneShot(clickSound, volume);
        }
    }
}
