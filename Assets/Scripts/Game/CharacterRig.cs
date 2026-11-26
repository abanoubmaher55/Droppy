using System;
using System.Reflection;
using UnityEngine;

namespace SaveWaterEG
{
    // Wraps a character visual: loads the user's own prefab from
    // Resources/Characters/<name> when present, otherwise builds a simple
    // capsule placeholder tinted with the role color. Normalizes scale so the
    // character is ~1.7m tall with feet at y=0, and drives animation:
    // an Animator float parameter "Speed" when present, else a leg/arm swing
    // on the placeholder. Safe with prefabs missing colliders or an Animator.
    // NOTE: Animator is accessed by REFLECTION so this file compiles even in
    // projects where the Animation module is not enabled in the manifest.
    public class CharacterRig : MonoBehaviour
    {
        public const float TargetHeight = 1.7f;

        Component _anim;                 // UnityEngine.Animator via reflection (may be null)
        bool _hasSpeed;
        MethodInfo _setFloat;
        Transform _legL, _legR, _armL, _armR;
        float _swing;
        float _speed;

        // Role prefab names under Resources/Characters (index = selectedCharacter).
        public static readonly string[] RolePrefabNames = { "technician", "chemist", "collector", "engineer" };

        public static readonly Color[] RoleColors =
        {
            new Color(0.2f, 0.55f, 0.95f),  // technician blue
            new Color(0.6f, 0.45f, 0.9f),   // chemist purple
            new Color(0.15f, 0.7f, 0.5f),   // collector green
            new Color(0.95f, 0.6f, 0.2f),   // engineer orange
        };

        // Builds a rig under `parent` for the given prefab name (may be missing).
        public static CharacterRig Create(Transform parent, string prefabName, Color tint, bool withLimbs)
        {
            var holder = new GameObject("Rig_" + prefabName);
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = Vector3.zero;
            var rig = holder.AddComponent<CharacterRig>();

            var prefab = Resources.Load<GameObject>("Characters/" + prefabName);
            GameObject visual = null;
            if (prefab != null)
            {
                visual = Instantiate(prefab, holder.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                StripPhysics(visual);
                rig.HookAnimator(visual);
                Normalize(visual);
            }
            else
            {
                visual = rig.BuildPlaceholder(holder.transform, tint, withLimbs);
            }
            return rig;
        }

        // Finds an Animator by name (no compile-time dependency on the module)
        // and checks for a float parameter called "Speed".
        void HookAnimator(GameObject visual)
        {
            var animType = Type.GetType("UnityEngine.Animator, UnityEngine.AnimationModule");
            if (animType == null) return;
            _anim = visual.GetComponentInChildren(animType);
            if (_anim == null) { _anim = null; return; }

            var rootMotionProp = animType.GetProperty("applyRootMotion");
            if (rootMotionProp != null) rootMotionProp.SetValue(_anim, false, null);

            try
            {
                var paramsProp = animType.GetProperty("parameters");
                var parms = paramsProp != null ? paramsProp.GetValue(_anim, null) as Array : null;
                if (parms != null)
                    foreach (var p in parms)
                    {
                        var pt = p.GetType();
                        var nameProp = pt.GetProperty("name");
                        var typeProp = pt.GetProperty("type");
                        if (nameProp == null || typeProp == null) continue;
                        string pname = nameProp.GetValue(p, null) as string;
                        string ptype = typeProp.GetValue(p, null).ToString();
                        if (pname == "Speed" && ptype == "Float") _hasSpeed = true;
                    }
            }
            catch (Exception) { _hasSpeed = false; }

            _setFloat = animType.GetMethod("SetFloat", new[] { typeof(string), typeof(float) });
            if (_setFloat == null) _hasSpeed = false;
        }

        // Removes Rigidbody/colliders from an imported prefab: CharacterController does the physics.
        static void StripPhysics(GameObject go)
        {
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>()) Destroy(rb);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
        }

        // Scales the visual to TargetHeight and puts its feet on y=0.
        static void Normalize(GameObject visual)
        {
            var rends = visual.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;
            Bounds b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            if (b.size.y > 0.01f)
            {
                float k = TargetHeight / b.size.y;
                visual.transform.localScale = visual.transform.localScale * k;
            }
            // recompute and lift so feet touch y=0 (holder origin = feet)
            rends = visual.GetComponentsInChildren<Renderer>();
            b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            var wp = visual.transform.position;
            wp.y -= b.min.y;
            visual.transform.position = wp;
        }

        GameObject BuildPlaceholder(Transform parent, Color tint, bool withLimbs)
        {
            var body = new GameObject("Body");
            body.transform.SetParent(parent, false);
            var skin = new Color(0.98f, 0.82f, 0.66f);

            // legs
            if (withLimbs)
            {
                _legL = Limb(parent, "LegL", new Vector3(-0.16f, 0.36f, 0), new Vector3(0.2f, 0.72f, 0.2f), tint * 0.8f);
                _legR = Limb(parent, "LegR", new Vector3(0.16f, 0.36f, 0), new Vector3(0.2f, 0.72f, 0.2f), tint * 0.8f);
            }
            // torso capsule
            var torso = Gfx.Prim(PrimitiveType.Capsule, "Torso", Vector3.zero, Vector3.one, tint, body.transform);
            torso.transform.localPosition = new Vector3(0, withLimbs ? 1.05f : 0.85f, 0);
            torso.transform.localScale = new Vector3(0.62f, 0.42f, 0.5f);
            Gfx.StripCollider(torso);
            // arms
            if (withLimbs)
            {
                _armL = Limb(parent, "ArmL", new Vector3(-0.42f, 1.05f, 0), new Vector3(0.15f, 0.6f, 0.15f), tint);
                _armR = Limb(parent, "ArmR", new Vector3(0.42f, 1.05f, 0), new Vector3(0.15f, 0.6f, 0.15f), tint);
            }
            // head
            var head = Gfx.Prim(PrimitiveType.Sphere, "Head", Vector3.zero, Vector3.one, skin, body.transform);
            head.transform.localPosition = new Vector3(0, withLimbs ? 1.52f : 1.32f, 0);
            head.transform.localScale = new Vector3(0.44f, 0.44f, 0.44f);
            Gfx.StripCollider(head);
            // eyes
            Eye(body.transform, new Vector3(-0.09f, withLimbs ? 1.56f : 1.36f, 0.18f));
            Eye(body.transform, new Vector3(0.09f, withLimbs ? 1.56f : 1.36f, 0.18f));
            return body;
        }

        static Transform Limb(Transform parent, string name, Vector3 pos, Vector3 scale, Color c)
        {
            var l = Gfx.Prim(PrimitiveType.Capsule, name, Vector3.zero, Vector3.one, c, parent);
            l.transform.localPosition = pos;
            l.transform.localScale = scale;
            Gfx.StripCollider(l);
            return l.transform;
        }

        static void Eye(Transform parent, Vector3 pos)
        {
            var e = Gfx.Prim(PrimitiveType.Sphere, "Eye", Vector3.zero, Vector3.one, Color.white, parent);
            e.transform.localPosition = pos;
            e.transform.localScale = Vector3.one * 0.1f;
            Gfx.StripCollider(e);
            var p = Gfx.Prim(PrimitiveType.Sphere, "Pupil", Vector3.zero, Vector3.one, Color.black, e.transform);
            p.transform.localPosition = new Vector3(0, 0, 0.55f);
            p.transform.localScale = Vector3.one * 0.5f;
            Gfx.StripCollider(p);
        }

        // Called every frame with the current planar speed (m/s).
        public void SetSpeed(float s) { _speed = s; }

        void Update()
        {
            if (_anim != null && _hasSpeed)
            {
                try { _setFloat.Invoke(_anim, new object[] { "Speed", _speed }); }
                catch (Exception) { /* keep playing without animation */ }
                return;
            }
            if (_legL == null) return;
            // placeholder walk swing
            float target = Mathf.Clamp01(_speed / 3f);
            _swing = Mathf.MoveTowards(_swing, target, Time.deltaTime * 6f);
            float a = Mathf.Sin(Time.time * 9f) * 32f * _swing;
            _legL.localRotation = Quaternion.Euler(a, 0, 0);
            _legR.localRotation = Quaternion.Euler(-a, 0, 0);
            if (_armL != null) _armL.localRotation = Quaternion.Euler(-a * 0.7f, 0, 0);
            if (_armR != null) _armR.localRotation = Quaternion.Euler(a * 0.7f, 0, 0);
        }
    }
}
