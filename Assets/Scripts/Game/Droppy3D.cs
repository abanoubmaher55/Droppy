using UnityEngine;

namespace SaveWaterEG
{
    // 3D Droppy: loads Resources/Characters/droppy prefab if the user added one,
    // otherwise builds the classic droplet from primitives (blue body, eyes,
    // pupils). Hovers beside the player with a springy follow, bobs and blinks.
    public class Droppy3D : MonoBehaviour
    {
        public Transform target;
        public Vector3 sideOffset = new Vector3(1.4f, 1.6f, 0.4f);

        Vector3 _vel;
        Transform _body;
        Transform _eyeL, _eyeR;
        float _nextBlink;
        float _blinkUntil;
        float _t;
        bool _isFallback;

        public static Droppy3D Create(Transform target)
        {
            var go = new GameObject("Droppy3D");
            var d = go.AddComponent<Droppy3D>();
            d.target = target;
            d.transform.position = target.position + d.sideOffset;

            var prefab = Resources.Load<GameObject>("Characters/droppy");
            if (prefab != null)
            {
                var v = Object.Instantiate(prefab, go.transform);
                v.transform.localPosition = Vector3.zero;
                foreach (var rb in v.GetComponentsInChildren<Rigidbody>()) Object.Destroy(rb);
                foreach (var c in v.GetComponentsInChildren<Collider>()) Object.Destroy(c);
                // normalize to ~1.1m
                var rends = v.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    foreach (var r in rends) b.Encapsulate(r.bounds);
                    if (b.size.y > 0.01f) v.transform.localScale = v.transform.localScale * (1.1f / b.size.y);
                }
                d._body = v.transform;
            }
            else
            {
                d._isFallback = true;
                d.BuildFallback(go.transform);
            }
            d.ScheduleBlink();
            return d;
        }

        void BuildFallback(Transform parent)
        {
            var body = new GameObject("Body");
            body.transform.SetParent(parent, false);
            _body = body.transform;
            var blue = new Color(0.25f, 0.6f, 1f);
            var b = Gfx.Prim(PrimitiveType.Sphere, "Drop", Vector3.zero, Vector3.one, blue, body.transform);
            b.transform.localScale = new Vector3(0.8f, 1.0f, 0.8f);
            Gfx.StripCollider(b);
            // little tip on top (droplet point)
            var tip = Gfx.Prim(PrimitiveType.Sphere, "Tip", Vector3.zero, Vector3.one, blue, body.transform);
            tip.transform.localPosition = new Vector3(0, 0.55f, 0);
            tip.transform.localScale = new Vector3(0.34f, 0.42f, 0.34f);
            Gfx.StripCollider(tip);
            _eyeL = MakeEye(body.transform, -0.17f);
            _eyeR = MakeEye(body.transform, 0.17f);
            // smile: small dark flattened sphere
            var smile = Gfx.Prim(PrimitiveType.Sphere, "Smile", Vector3.zero, Vector3.one,
                new Color(0.05f, 0.2f, 0.4f), body.transform);
            smile.transform.localPosition = new Vector3(0, -0.12f, 0.34f);
            smile.transform.localScale = new Vector3(0.18f, 0.08f, 0.06f);
            Gfx.StripCollider(smile);
        }

        Transform MakeEye(Transform parent, float x)
        {
            var e = Gfx.Prim(PrimitiveType.Sphere, "Eye", Vector3.zero, Vector3.one, Color.white, parent);
            e.transform.localPosition = new Vector3(x, 0.12f, 0.3f);
            e.transform.localScale = Vector3.one * 0.2f;
            Gfx.StripCollider(e);
            var p = Gfx.Prim(PrimitiveType.Sphere, "Pupil", Vector3.zero, Vector3.one, Color.black, e.transform);
            p.transform.localPosition = new Vector3(0, 0, 0.6f);
            p.transform.localScale = Vector3.one * 0.5f;
            Gfx.StripCollider(p);
            return e.transform;
        }

        void ScheduleBlink() { _nextBlink = Time.time + Random.Range(2.5f, 5f); }

        void Update()
        {
            _t = Time.time;
            if (target != null)
            {
                var want = target.position + target.rotation * sideOffset
                    + Vector3.up * (Mathf.Sin(_t * 2.2f) * 0.18f);
                transform.position = Vector3.SmoothDamp(transform.position, want, ref _vel, 0.35f);
                // face travel direction (or the player when idle)
                var face = target.rotation * Vector3.forward;
                if (face.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(face), 6f * Time.deltaTime);
            }
            // blink (fallback eyes only)
            if (_isFallback && _eyeL != null)
            {
                bool blinking = _t < _blinkUntil;
                if (!blinking && _t >= _nextBlink) { _blinkUntil = _t + 0.12f; ScheduleBlink(); blinking = true; }
                float sy = blinking ? 0.15f : 1f;
                _eyeL.localScale = new Vector3(0.2f, 0.2f * sy, 0.2f);
                _eyeR.localScale = new Vector3(0.2f, 0.2f * sy, 0.2f);
            }
        }
    }
}
