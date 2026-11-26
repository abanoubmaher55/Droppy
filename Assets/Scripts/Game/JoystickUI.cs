using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // Visible floating joystick (bottom-left). Works with touch and mouse.
    // PlayerController3D reads JoystickUI.Direction (world XZ, normalized).
    public class JoystickUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static Vector2 Direction { get; private set; }

        RectTransform _knob;
        RectTransform _rt;
        float _radius = 90f;

        public static JoystickUI Create(Transform canvasRoot)
        {
            var baseRt = UIFactory.Place(canvasRoot, "Joystick", 0.03f, 0.04f, 0.17f, 0.30f);
            var bg = UIFactory.Img(baseRt, "bg", Resources.Load<Sprite>("Art/circle"),
                new Color(1f, 1f, 1f, 0.28f), false);
            bg.raycastTarget = true;
            var knobRt = UIFactory.Place(baseRt, "Knob", 0.28f, 0.28f, 0.72f, 0.72f);
            var knob = UIFactory.Img(knobRt, "k", Resources.Load<Sprite>("Art/circle"),
                new Color(0.1f, 0.5f, 0.95f, 0.85f), false);
            knob.raycastTarget = false;
            var joy = baseRt.gameObject.AddComponent<JoystickUI>();
            joy._rt = baseRt;
            joy._knob = knobRt;
            Direction = Vector2.zero;
            return joy;
        }

        public void OnPointerDown(PointerEventData e) { OnDrag(e); }

        public void OnDrag(PointerEventData e)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rt, e.position, e.pressEventCamera, out local);
            float w = _rt.rect.width;
            _radius = Mathf.Max(40f, w * 0.5f);
            Vector2 d = new Vector2(local.x / _radius, local.y / _radius);
            if (d.magnitude > 1f) d.Normalize();
            Direction = d;
            _knob.anchorMin = new Vector2(0.5f + d.x * 0.28f, 0.5f + d.y * 0.28f) - Vector2.one * 0.22f;
            _knob.anchorMax = new Vector2(0.5f + d.x * 0.28f, 0.5f + d.y * 0.28f) + Vector2.one * 0.22f;
            _knob.offsetMin = Vector2.zero; _knob.offsetMax = Vector2.zero;
        }

        public void OnPointerUp(PointerEventData e)
        {
            Direction = Vector2.zero;
            _knob.anchorMin = Vector2.one * 0.28f;
            _knob.anchorMax = Vector2.one * 0.72f;
            _knob.offsetMin = Vector2.zero; _knob.offsetMax = Vector2.zero;
        }
    }
}
