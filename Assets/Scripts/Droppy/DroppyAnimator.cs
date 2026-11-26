using UnityEngine;
using UnityEngine.EventSystems;

namespace SaveWaterEG
{
    // Animates Droppy on any UI Image: floating, sway, breathing squash,
    // a fake blink (fast squash), and a happy jump when tapped.
    // Optional: assign eyeOpen / eyeClosed sprites (same size PNGs) for a real blink.
    public class DroppyAnimator : MonoBehaviour, IPointerClickHandler
    {
        [Header("Optional eye sprites (real blink)")]
        public Sprite eyeOpen;
        public Sprite eyeClosed;

        [Header("Motion")]
        public float floatAmplitude = 12f;   // pixels up/down
        public float floatSpeed = 1.6f;
        public float swayAngle = 4f;         // degrees
        public float swaySpeed = 1.1f;
        public float breatheAmount = 0.05f;  // scale pulse

        [Header("Blink")]
        public float blinkEveryMin = 2.5f;
        public float blinkEveryMax = 5f;
        public float blinkTime = 0.12f;

        RectTransform _rt;
        UnityEngine.UI.Image _img;
        Vector2 _basePos;
        Vector3 _baseScale;
        float _nextBlink;
        float _blinkUntil;
        float _jumpVel;
        float _jumpOffset;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _img = GetComponentInChildren<UnityEngine.UI.Image>();
            _basePos = _rt.anchoredPosition;
            _baseScale = _rt.localScale;
            ScheduleBlink();
        }

        void OnEnable()
        {
            if (_rt != null) { _basePos = _rt.anchoredPosition; }
        }

        void ScheduleBlink() { _nextBlink = Time.time + Random.Range(blinkEveryMin, blinkEveryMax); }

        void Update()
        {
            float t = Time.time;

            // float + jump
            _jumpOffset += _jumpVel * Time.deltaTime;
            _jumpVel -= 900f * Time.deltaTime; // gravity
            if (_jumpOffset < 0f) { _jumpOffset = 0f; _jumpVel = 0f; }
            float y = Mathf.Sin(t * floatSpeed) * floatAmplitude;
            _rt.anchoredPosition = _basePos + new Vector2(0, y + _jumpOffset);

            // sway
            _rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * swaySpeed) * swayAngle);

            // breathing squash
            float b = 1f + Mathf.Sin(t * 2.2f) * breatheAmount;
            _rt.localScale = new Vector3(_baseScale.x * (2f - b) * 0.5f + _baseScale.x * 0.5f,
                                         _baseScale.y * b,
                                         _baseScale.z);

            // blink
            bool blinking = t < _blinkUntil;
            if (!blinking && t >= _nextBlink) { _blinkUntil = t + blinkTime; ScheduleBlink(); blinking = true; }

            if (eyeOpen != null && eyeClosed != null && _img != null)
            {
                // real blink with sprites
                _img.sprite = blinking ? eyeClosed : eyeOpen;
            }
            else if (blinking)
            {
                // fake blink: quick squash
                float k = 1f - Mathf.Sin(((_blinkUntil - t) / blinkTime) * Mathf.PI) * 0.35f;
                _rt.localScale = new Vector3(_rt.localScale.x, _baseScale.y * k, _baseScale.z);
            }
        }

        public void OnPointerClick(PointerEventData e)
        {
            _jumpVel = 420f; // happy hop when the child taps Droppy
        }
    }
}
