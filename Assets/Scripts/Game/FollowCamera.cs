using UnityEngine;

namespace SaveWaterEG
{
    // Fixed-angle third-person follow camera (north-up, no rotation):
    // simple, comfortable for kids, and consistent with the minimap.
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 12f, -9.5f);
        public float smooth = 6f;

        void LateUpdate()
        {
            if (target == null) return;
            var want = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, want, smooth * Time.deltaTime);
            transform.rotation = Quaternion.Euler(52f, 0f, 0f);
        }
    }
}
