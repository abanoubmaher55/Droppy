using System.Collections.Generic;
using UnityEngine;

namespace SaveWaterEG
{
    // Pooled 3D yellow arrows laid on the road as breadcrumbs toward the fault.
    // Each arrow is two boxes (shaft + V head), unlit, gently bobbing.
    public class ArrowTrail : MonoBehaviour
    {
        const int Pool = 8;
        readonly List<Transform> _arrows = new List<Transform>();
        Material _mat;
        float _t;

        public static ArrowTrail Create(Transform parent)
        {
            var go = new GameObject("ArrowTrail");
            if (parent != null) go.transform.SetParent(parent, false);
            var a = go.AddComponent<ArrowTrail>();
            a._mat = Gfx.UnlitMat(new Color(1f, 0.85f, 0.1f));
            for (int i = 0; i < Pool; i++)
            {
                var ar = a.BuildArrow();
                ar.gameObject.SetActive(false);
                a._arrows.Add(ar);
            }
            return a;
        }

        Transform BuildArrow()
        {
            var root = new GameObject("Arrow").transform;
            root.SetParent(transform, false);
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "shaft";
            shaft.transform.SetParent(root, false);
            shaft.transform.localPosition = new Vector3(0, 0, -0.35f);
            shaft.transform.localScale = new Vector3(0.5f, 0.08f, 1.1f);
            shaft.GetComponent<Renderer>().sharedMaterial = _mat;
            Gfx.StripCollider(shaft);
            for (int s = -1; s <= 1; s += 2)
            {
                var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
                head.name = "head";
                head.transform.SetParent(root, false);
                head.transform.localPosition = new Vector3(s * 0.3f, 0, 0.35f);
                head.transform.localRotation = Quaternion.Euler(0, s * 45f, 0);
                head.transform.localScale = new Vector3(0.5f, 0.08f, 0.9f);
                head.GetComponent<Renderer>().sharedMaterial = _mat;
                Gfx.StripCollider(head);
            }
            return root;
        }

        // Lays arrows along the path points (world positions, player first).
        public void SetPath(List<Vector3> points)
        {
            int ai = 0;
            if (points != null && points.Count > 1)
            {
                for (int i = 1; i < points.Count && ai < Pool; i++, ai++)
                {
                    var a = _arrows[ai];
                    a.gameObject.SetActive(true);
                    Vector3 p = points[i]; p.y = 0.18f;
                    a.position = p;
                    Vector3 look = points[i] - points[i - 1]; look.y = 0;
                    if (look.sqrMagnitude > 0.01f) a.rotation = Quaternion.LookRotation(look);
                }
            }
            for (int i = ai; i < Pool; i++) _arrows[i].gameObject.SetActive(false);
        }

        public void ClearTrail()
        {
            foreach (var a in _arrows) a.gameObject.SetActive(false);
        }

        void Update()
        {
            _t = Time.time;
            float k = 1f + Mathf.Sin(_t * 5f) * 0.08f;
            foreach (var a in _arrows)
                if (a.gameObject.activeSelf) a.localScale = Vector3.one * k;
        }
    }
}
