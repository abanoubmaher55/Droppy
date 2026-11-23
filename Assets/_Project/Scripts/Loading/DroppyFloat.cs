using UnityEngine;

// حركة عايمة لـ Droppy: طلوع ونزول بسيط + نبضة صغيرة في الحجم
public class DroppyFloat : MonoBehaviour
{
    public float floatHeight = 25f;   // مسافة الطلوع والنزول (بكسل)
    public float floatSpeed = 2f;     // سرعة الحركة
    public float pulseAmount = 0.03f; // مقدار النبضة في الحجم

    private RectTransform rt;
    private Vector2 startPos;
    private Vector3 startScale;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
        startScale = rt.localScale;
    }

    private void Update()
    {
        float t = Time.unscaledTime * floatSpeed;
        rt.anchoredPosition = startPos + new Vector2(0f, Mathf.Sin(t) * floatHeight);
        rt.localScale = startScale * (1f + Mathf.Sin(t * 1.3f) * pulseAmount);
    }
}
