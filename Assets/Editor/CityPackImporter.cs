#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SaveWaterEG.EditorTools
{
    // Optional hook for the free "CITY package" (Asset Store #107224).
    // If the user imported it, this copies a set of its prefabs into
    // Assets/Resources/CityPack and re-maps their materials to the Standard
    // shader so nothing renders pink in the built-in render pipeline.
    // CityBuilder uses those prefabs automatically; without the package the
    // generated low-poly city is used. Runs on load and via the menu item.
    [InitializeOnLoad]
    public static class CityPackImporter
    {
        const string DestFolder = "Assets/Resources/CityPack";

        static CityPackImporter()
        {
            EditorApplication.delayCall += () => TryImport(false);
        }

        [MenuItem("SaveWaterEG/Re-import City Package prefabs")]
        public static void ForceImport() { TryImport(true); }

        static void TryImport(bool force)
        {
            // find the package's prefab folder (common install names)
            string[] guesses = { "Assets/CITY package", "Assets/CityPackage", "Assets/City package", "Assets/CITY", "Assets/PolygonCity" };
            string packFolder = null;
            foreach (var g in guesses)
                if (AssetDatabase.IsValidFolder(g)) { packFolder = g; break;
            }
            if (packFolder == null)
            {
                // broader search: any prefab whose path mentions "city package"
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    if (p.ToLowerInvariant().Contains("city") && p.ToLowerInvariant().Contains("package"))
                    {
                        packFolder = Path.GetDirectoryName(p).Replace('\\', '/');
                        break;
                    }
                }
            }
            if (packFolder == null) return; // package not installed: generated city is used

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(DestFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "CityPack");
            else if (!force && AssetDatabase.FindAssets("t:Prefab", new[] { DestFolder }).Length >= 6)
                return; // already imported

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { packFolder });
            var copied = 0;
            foreach (var guid in guids)
            {
                if (copied >= 12) break;
                var srcPath = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(srcPath);
                // prefer building-like prefabs
                var lower = name.ToLowerInvariant();
                if (!(lower.Contains("build") || lower.Contains("house") || lower.Contains("shop") || lower.Contains("bank") || lower.Contains("hospital") || lower.Contains("police") || lower.Contains("station")))
                    continue;
                var destPath = DestFolder + "/" + name + ".prefab";
                if (!AssetDatabase.CopyAsset(srcPath, destPath)) continue;
                FixMaterials(destPath);
                copied++;
            }
            if (copied > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("SaveWaterEG: imported " + copied + " CITY package prefabs into Resources/CityPack.");
            }
        }

        // Re-maps URP/pink materials on a copied prefab to built-in Standard.
        static void FixMaterials(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;
            var cache = new Dictionary<string, Material>();
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    if (m.shader != null && (m.shader.name == "Standard" || m.shader.name == "Mobile/Diffuse")) continue;
                    string key = AssetDatabase.GetAssetPath(m) + "|" + m.name;
                    if (!cache.TryGetValue(key, out var fixed_))
                    {
                        fixed_ = new Material(Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse"));
                        if (m.HasProperty("_BaseColor")) fixed_.color = m.GetColor("_BaseColor");
                        else if (m.HasProperty("_Color")) fixed_.color = m.GetColor("_Color");
                        Texture tex = null;
                        if (m.HasProperty("_BaseMap")) tex = m.GetTexture("_BaseMap");
                        if (tex == null && m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");
                        if (tex != null) fixed_.mainTexture = tex;
                        var matPath = DestFolder + "/Mat_" + Sanitize(m.name) + ".mat";
                        AssetDatabase.CreateAsset(fixed_, matPath);
                        cache[key] = fixed_;
                    }
                    mats[i] = cache[key];
                }
                r.sharedMaterials = mats;
            }
            EditorUtility.SetDirty(prefab);
        }

        static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }
    }
}
#endif
