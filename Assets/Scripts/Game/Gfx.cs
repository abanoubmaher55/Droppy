using UnityEngine;

namespace SaveWaterEG
{
    // Material helpers that are safe in the built-in render pipeline.
    // Every lookup goes through Shader.Find with fallbacks so nothing renders pink.
    public static class Gfx
    {
        static Shader _lit;
        static Shader _unlit;

        public static Shader Lit()
        {
            if (_lit == null)
            {
                _lit = Shader.Find("Standard");
                if (_lit == null) _lit = Shader.Find("Mobile/Diffuse");
                if (_lit == null) _lit = Shader.Find("Legacy Shaders/Diffuse");
                if (_lit == null) _lit = Shader.Find("Unlit/Color");
            }
            return _lit;
        }

        public static Shader Unlit()
        {
            if (_unlit == null)
            {
                _unlit = Shader.Find("Unlit/Color");
                if (_unlit == null) _unlit = Shader.Find("Sprites/Default");
            }
            return _unlit;
        }

        public static Material Mat(Color c)
        {
            var m = new Material(Lit());
            m.color = c;
            return m;
        }

        public static Material UnlitMat(Color c)
        {
            var m = new Material(Unlit());
            m.color = c;
            return m;
        }

        // Makes a colored primitive with the lit material.
        public static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            if (parent != null) go.transform.SetParent(parent, true);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = Mat(color);
            return go;
        }

        // Removes the auto collider primitives come with (we add our own where needed).
        public static void StripCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
        }
    }
}
