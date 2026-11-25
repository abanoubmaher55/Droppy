using UnityEngine;

namespace SaveWaterEG
{
    // Top-down player movement: keyboard (WASD/arrows) on PC,
    // drag-anywhere virtual joystick on Android.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public float speed = 5.5f;
        public static Rect WorldBounds = new Rect(-9f, -5f, 18f, 10f);

        Rigidbody2D _rb;
        Vector2 _joyStart;
        Vector2 _joyDir;
        bool _joyActive;
        int _joyFinger = -1;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        void Update()
        {
            Vector2 dir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            // touch joystick (Android) / mouse drag (editor testing)
            if (Input.touchCount > 0)
            {
                foreach (var t in Input.touches)
                {
                    if (t.phase == TouchPhase.Began && t.position.x < Screen.width * 0.6f && _joyFinger == -1)
                    { _joyFinger = t.fingerId; _joyStart = t.position; _joyActive = true; }
                    else if (_joyActive && t.fingerId == _joyFinger)
                    {
                        if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                        {
                            Vector2 d = t.position - _joyStart;
                            if (d.magnitude > 20f) _joyDir = d.normalized; else _joyDir = Vector2.zero;
                        }
                        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                        { _joyFinger = -1; _joyActive = false; _joyDir = Vector2.zero; }
                    }
                }
            }
            else if (Input.GetMouseButton(0) && Input.mousePosition.x < Screen.width * 0.6f)
            {
                if (!_joyActive) { _joyActive = true; _joyStart = Input.mousePosition; }
                Vector2 d = (Vector2)Input.mousePosition - _joyStart;
                _joyDir = d.magnitude > 20f ? d.normalized : Vector2.zero;
            }
            else if (!Input.GetMouseButton(0) && _joyFinger == -1)
            { _joyActive = false; _joyDir = Vector2.zero; }

            if (dir.sqrMagnitude < 0.01f) dir = _joyDir;
            _rb.linearVelocity = dir.normalized * speed;

            // face movement
            if (dir.x != 0)
            {
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (dir.x > 0 ? 1 : -1);
                transform.localScale = s;
            }
        }

        void LateUpdate()
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, WorldBounds.xMin, WorldBounds.xMax);
            p.y = Mathf.Clamp(p.y, WorldBounds.yMin, WorldBounds.yMax);
            transform.position = p;
        }
    }
}
