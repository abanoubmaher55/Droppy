using System.Collections.Generic;
using UnityEngine;

namespace SaveWaterEG
{
    // A real-looking water fault in the city: a prop (pipe / hydrant / tank /
    // valve / crack) built from primitives, a code-animated water spray
    // (no particle shaders) and a pulsing puddle. Turns green when fixed.
    public class FaultVisual : MonoBehaviour
    {
        public bool fixed_;

        readonly List<Transform> _drops = new List<Transform>();
        readonly List<Vector3> _dropVel = new List<Vector3>();
        Transform _puddle;
        Renderer _propMain;
        float _t;
        Vector3 _sprayPos;

        public static FaultVisual Create(Vector3 pos, int kind, Transform parent)
        {
            var go = new GameObject("Fault");
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent, true);
            var f = go.AddComponent<FaultVisual>();
            f.BuildProp(kind);
            f.BuildSpray();
            f.BuildPuddle();
            var col = go.AddComponent<SphereCollider>();
            col.radius = 2.5f; col.isTrigger = true;
            return f;
        }

        void BuildProp(int kind)
        {
            var metal = new Color(0.6f, 0.62f, 0.65f);
            var rusty = new Color(0.65f, 0.4f, 0.25f);
            GameObject main = null;
            switch (kind % 5)
            {
                case 0: // broken pipe sticking out of the ground
                    main = Gfx.Prim(PrimitiveType.Cylinder, "Pipe", new Vector3(0, 0.5f, 0),
                        new Vector3(0.5f, 0.5f, 0.5f), metal, transform);
                    _sprayPos = new Vector3(0, 1.0f, 0);
                    break;
                case 1: // fire hydrant
                    main = Gfx.Prim(PrimitiveType.Capsule, "Hydrant", new Vector3(0, 0.55f, 0),
                        new Vector3(0.6f, 0.4f, 0.6f), new Color(0.9f, 0.15f, 0.1f), transform);
                    var cap = Gfx.Prim(PrimitiveType.Sphere, "Cap", new Vector3(0, 0.95f, 0),
                        new Vector3(0.5f, 0.35f, 0.5f), new Color(0.9f, 0.15f, 0.1f), transform);
                    Gfx.StripCollider(cap);
                    _sprayPos = new Vector3(0, 0.9f, 0);
                    break;
                case 2: // water tank on legs
                    main = Gfx.Prim(PrimitiveType.Cylinder, "Tank", new Vector3(0, 1.4f, 0),
                        new Vector3(1.3f, 0.7f, 1.3f), new Color(0.3f, 0.55f, 0.8f), transform);
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = i * Mathf.PI * 2f / 3f;
                        var leg = Gfx.Prim(PrimitiveType.Cube, "Leg",
                            new Vector3(Mathf.Cos(ang) * 0.8f, 0.35f, Mathf.Sin(ang) * 0.8f),
                            new Vector3(0.15f, 0.7f, 0.15f), metal, transform);
                        Gfx.StripCollider(leg);
                    }
                    _sprayPos = new Vector3(0, 1.1f, 0);
                    break;
                case 3: // valve wheel on a pipe
                    main = Gfx.Prim(PrimitiveType.Cylinder, "Pipe", new Vector3(0, 0.35f, 0),
                        new Vector3(0.35f, 0.35f, 0.35f), rusty, transform);
                    var wheel = Gfx.Prim(PrimitiveType.Cylinder, "Wheel", new Vector3(0, 0.85f, 0),
                        new Vector3(0.7f, 0.08f, 0.7f), new Color(0.85f, 0.2f, 0.15f), transform);
                    Gfx.StripCollider(wheel);
                    _sprayPos = new Vector3(0, 0.7f, 0);
                    break;
                default: // ground crack (dark slab) leaking
                    main = Gfx.Prim(PrimitiveType.Cube, "Crack", new Vector3(0, 0.06f, 0),
                        new Vector3(1.6f, 0.12f, 0.4f), new Color(0.15f, 0.15f, 0.18f), transform);
                    main.transform.rotation = Quaternion.Euler(0, 25f, 0);
                    _sprayPos = new Vector3(0, 0.25f, 0);
                    break;
            }
            Gfx.StripCollider(main);
            _propMain = main.GetComponent<Renderer>();
        }

        void BuildSpray()
        {
            var dropMat = Gfx.UnlitMat(new Color(0.55f, 0.8f, 1f));
            for (int i = 0; i < 14; i++)
            {
                var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                d.name = "drop";
                d.transform.SetParent(transform, false);
                d.transform.localScale = Vector3.one * 0.14f;
                d.GetComponent<Renderer>().sharedMaterial = dropMat;
                Gfx.StripCollider(d);
                _drops.Add(d.transform);
                _dropVel.Add(Vector3.zero);
                RespawnDrop(i, Random.value);
            }
        }

        void RespawnDrop(int i, float headStart = 0f)
        {
            Vector3 dir = new Vector3(Random.Range(-0.6f, 0.6f), 1f, Random.Range(-0.6f, 0.6f)).normalized;
            _dropVel[i] = dir * Random.Range(2.5f, 4.5f);
            _drops[i].localPosition = _sprayPos + _dropVel[i] * headStart * 0.5f;
        }

        void BuildPuddle()
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p.name = "Puddle";
            p.transform.SetParent(transform, false);
            p.transform.localPosition = new Vector3(0, 0.04f, 0);
            p.transform.localScale = new Vector3(2.2f, 0.03f, 2.2f);
            var r = p.GetComponent<Renderer>();
            r.sharedMaterial = Gfx.UnlitMat(new Color(0.3f, 0.6f, 1f));
            Gfx.StripCollider(p);
            _puddle = p.transform;
        }

        // Called when the player picks the correct fix.
        public void SetFixed()
        {
            fixed_ = true;
            foreach (var d in _drops) d.gameObject.SetActive(false);
            if (_propMain != null) _propMain.sharedMaterial = Gfx.Mat(new Color(0.3f, 0.8f, 0.4f));
        }

        void Update()
        {
            _t = Time.time;
            if (!fixed_)
            {
                // droplets arc up and fall back down
                for (int i = 0; i < _drops.Count; i++)
                {
                    var v = _dropVel[i] + Physics.gravity * 0.5f * Time.deltaTime;
                    _dropVel[i] = v;
                    var d = _drops[i];
                    d.localPosition += v * Time.deltaTime;
                    if (d.localPosition.y < 0.05f) RespawnDrop(i);
                }
            }
            // puddle breathes (shrinks once fixed)
            if (_puddle != null)
            {
                float s = fixed_
                    ? Mathf.Max(0.6f, _puddle.localScale.x - Time.deltaTime * 0.6f)
                    : 2.2f + Mathf.Sin(_t * 2f) * 0.25f;
                _puddle.localScale = new Vector3(s, 0.03f, s);
            }
        }
    }
}
