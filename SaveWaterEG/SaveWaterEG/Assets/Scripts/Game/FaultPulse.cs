using UnityEngine;

namespace SaveWaterEG
{
    // Simple pulsing scale for the fault marker.
    public class FaultPulse : MonoBehaviour
    {
        Vector3 _base;
        void Start() { _base = transform.localScale; }
        void Update()
        {
            float k = 1f + Mathf.Sin(Time.time * 4f) * 0.15f;
            transform.localScale = _base * k;
        }
    }
}
