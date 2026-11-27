using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaveWaterEG
{
  // Makes a picture button feel alive: gentle breathing, grows on hover,
  // dips on press (never smaller than the art underneath), springy bounce on click.
  public class MenuButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                              IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
  {
    public float phase;
    public float breatheAmount = 0.018f;
    public float hoverScale = 1.08f;
    public float pressScale = 1.02f;

    RectTransform _rt;
    RawImage _img;
    bool _hover, _down;
    float _scale = 1f, _vel;
    float _bounceT = -1f;

    void Awake() { _rt = (RectTransform)transform; _img = GetComponent<RawImage>(); }

    void OnEnable() { _scale = 1f; _vel = 0f; _bounceT = -1f; }

    public void OnPointerEnter(PointerEventData e) { _hover = true; }
    public void OnPointerExit(PointerEventData e) { _hover = false; _down = false; }
    public void OnPointerDown(PointerEventData e) { _down = true; }
    public void OnPointerUp(PointerEventData e) { _down = false; }
    public void OnPointerClick(PointerEventData e) { _bounceT = 0f; _vel = 4f; }

    void Update()
    {
      float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
      // idle: slow breathing, always >= 1 so the baked art underneath stays covered
      float idle = 1f + breatheAmount * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.1f + phase));
      float target = _down ? pressScale : (_hover ? hoverScale : idle);

      // spring toward the target (springy, slightly overshoots)
      float k = 180f, d = 14f;
      _vel += ((target - _scale) * k - _vel * d) * dt;
      _scale += _vel * dt;
      if (_scale < 1f) { _scale = 1f; if (_vel < 0f) _vel = 0f; }

      _rt.localScale = new Vector3(_scale, _scale, 1f);
      if (_img != null)
      {
        float c = _down ? 0.88f : (_hover ? 1.06f : 1f);
        _img.color = new Color(c, c, c, 1f);
      }
    }
  }
}