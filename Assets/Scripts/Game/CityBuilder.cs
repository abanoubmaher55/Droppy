using System.Collections.Generic;
using UnityEngine;

namespace SaveWaterEG
{
    public class CityResult
    {
        public RoadGraph graph;
        public Vector3 spawn;                    // player spawn (road corner)
        public List<Vector3> faultSpots;         // roadside spots, far from spawn first
        public Transform root;
        public float roadHalfWidth = 4f;
        public float bound = 68f;                // playable clamp
    }

    // Builds the whole 3D city in code (built-in pipeline safe materials).
    // If the user imported the free "CITY package" from the Asset Store and the
    // editor importer copied prefabs into Resources/CityPack, those prefabs are
    // used for buildings; otherwise low-poly primitive buildings are generated.
    public static class CityBuilder
    {
        public const int GridSize = 5;           // 5x5 roads
        public const float Pitch = 30f;          // distance between parallel roads
        public const float RoadWidth = 8f;

        static readonly Color[] BuildingColors =
        {
            new Color(0.95f, 0.75f, 0.55f), new Color(0.75f, 0.85f, 0.95f),
            new Color(0.95f, 0.65f, 0.65f), new Color(0.80f, 0.92f, 0.75f),
            new Color(0.88f, 0.80f, 0.95f), new Color(0.98f, 0.88f, 0.60f),
            new Color(0.70f, 0.85f, 0.85f),
        };

        public static CityResult Build(int seed)
        {
            var rng = new System.Random(seed);
            var root = new GameObject("City").transform;
            var res = new CityResult();
            res.graph = new RoadGraph(GridSize, Pitch);
            res.root = root;

            float half = (GridSize - 1) * 0.5f * Pitch;   // 60
            float citySpan = half * 2f + Pitch;            // roads span + margin

            BuildLight();
            BuildGround(root, citySpan);
            BuildRoads(root, half, citySpan);
            BuildBlocks(root, rng, half);
            BuildNile(root, half, citySpan);
            ScatterTrees(root, rng, half);

            res.spawn = new Vector3(-half, 0, -half);
            // fault spots: roadside points at intersections, farthest-first-ish variety
            var spots = new List<Vector3>();
            int[,] coords = { { 4, 4 }, { 0, 4 }, { 4, 0 }, { 2, 4 }, { 4, 2 } };
            for (int i = 0; i < coords.GetLength(0); i++)
            {
                var n = res.graph.Node(coords[i, 0], coords[i, 1]);
                // offset a little off the intersection centre toward the sidewalk
                spots.Add(n.pos + new Vector3(0, 0, RoadWidth * 0.25f));
            }
            res.faultSpots = spots;
            return res;
        }

        static void BuildLight()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool hasDir = false;
            foreach (var l in lights) if (l.type == LightType.Directional) hasDir = true;
            if (!hasDir)
            {
                var go = new GameObject("Sun");
                var l = go.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.15f;
                l.color = new Color(1f, 0.97f, 0.9f);
                go.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.72f);
        }

        static void BuildGround(Transform root, float span)
        {
            var g = Gfx.Prim(PrimitiveType.Cube, "Ground", new Vector3(0, -0.55f, 0),
                new Vector3(span * 2.4f, 1f, span * 2.4f), new Color(0.52f, 0.74f, 0.45f), root);
            // keep ground collider (CharacterController walks on it)
        }

        static void BuildRoads(Transform root, float half, float span)
        {
            var asphalt = new Color(0.28f, 0.29f, 0.32f);
            var dashMat = Gfx.UnlitMat(new Color(0.95f, 0.95f, 0.85f));
            float len = half * 2f + RoadWidth + 4f;
            for (int i = 0; i < GridSize; i++)
            {
                float c = i * Pitch - half;
                // road along X at z=c
                Gfx.Prim(PrimitiveType.Cube, "RoadX", new Vector3(0, 0.03f, c),
                    new Vector3(len, 0.06f, RoadWidth), asphalt, root);
                // road along Z at x=c
                Gfx.Prim(PrimitiveType.Cube, "RoadZ", new Vector3(c, 0.04f, 0),
                    new Vector3(RoadWidth, 0.06f, len), asphalt, root);
            }
            // centre dashes
            for (int i = 0; i < GridSize; i++)
            {
                float c = i * Pitch - half;
                for (float p = -half - 6f; p <= half + 6f; p += 4f)
                {
                    // skip inside intersections (near another road centre)
                    bool nearCross = false;
                    for (int j = 0; j < GridSize; j++)
                    {
                        float cc = j * Pitch - half;
                        if (Mathf.Abs(p - cc) < RoadWidth * 0.75f) { nearCross = true; break; }
                    }
                    if (nearCross) continue;
                    var d1 = Gfx.Prim(PrimitiveType.Cube, "dash", new Vector3(p, 0.08f, c),
                        new Vector3(1.6f, 0.02f, 0.35f), Color.white, root);
                    d1.GetComponent<Renderer>().sharedMaterial = dashMat;
                    Gfx.StripCollider(d1);
                    var d2 = Gfx.Prim(PrimitiveType.Cube, "dash", new Vector3(c, 0.09f, p),
                        new Vector3(0.35f, 0.02f, 1.6f), Color.white, root);
                    d2.GetComponent<Renderer>().sharedMaterial = dashMat;
                    Gfx.StripCollider(d2);
                }
            }
        }

        static void BuildBlocks(Transform root, System.Random rng, float half)
        {
            // blocks between roads: centres at -45,-15,15,45
            var packPrefabs = Resources.LoadAll<GameObject>("CityPack");
            int packUsed = 0;
            for (int bx = 0; bx < GridSize - 1; bx++)
                for (int bz = 0; bz < GridSize - 1; bz++)
                {
                    float cx = (bx + 0.5f) * Pitch - half;
                    float cz = (bz + 0.5f) * Pitch - half;
                    bool park = rng.NextDouble() < 0.2;
                    if (park) { BuildPark(root, rng, cx, cz); continue; }

                    if (packPrefabs != null && packPrefabs.Length > 0)
                    {
                        var pf = packPrefabs[rng.Next(packPrefabs.Length)];
                        if (pf != null)
                        {
                            var inst = Object.Instantiate(pf, new Vector3(cx, 0, cz),
                                Quaternion.Euler(0, 90 * rng.Next(4), 0), root);
                            inst.name = "PackBuilding";
                            NormalizePackBuilding(inst);
                            packUsed++;
                            continue;
                        }
                    }
                    BuildBuilding(root, rng, cx, cz);
                }
            if (packUsed > 0) Debug.Log("SaveWaterEG: using " + packUsed + " CityPack prefabs for buildings.");
        }

        // Scales an imported prefab to a sane footprint and guarantees a collider.
        static void NormalizePackBuilding(GameObject inst)
        {
            var rends = inst.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) { EnsureBoxCollider(inst, new Vector3(10, 10, 10)); return; }
            Bounds b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            float maxFoot = Mathf.Max(b.size.x, b.size.z);
            if (maxFoot > 0.01f)
            {
                float k = Mathf.Min(18f / maxFoot, 1.5f);
                inst.transform.localScale = inst.transform.localScale * k;
            }
            // recompute and sit on ground
            rends = inst.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                b = rends[0].bounds;
                foreach (var r in rends) b.Encapsulate(r.bounds);
                var p = inst.transform.position;
                p.y -= b.min.y;
                inst.transform.position = p;
            }
            if (inst.GetComponentInChildren<Collider>() == null)
                EnsureBoxCollider(inst, b.size);
        }

        static void EnsureBoxCollider(GameObject go, Vector3 size)
        {
            var bc = go.AddComponent<BoxCollider>();
            // approximate in local space
            bc.center = Vector3.zero;
            var s = go.transform.localScale;
            bc.size = new Vector3(
                size.x / Mathf.Max(0.001f, s.x),
                size.y / Mathf.Max(0.001f, s.y),
                size.z / Mathf.Max(0.001f, s.z));
        }

        static void BuildBuilding(Transform root, System.Random rng, float cx, float cz)
        {
            float w = 12f + (float)rng.NextDouble() * 6f;
            float d = 12f + (float)rng.NextDouble() * 6f;
            float h = 7f + (float)rng.NextDouble() * 13f;
            var col = BuildingColors[rng.Next(BuildingColors.Length)];
            var b = Gfx.Prim(PrimitiveType.Cube, "Building", new Vector3(cx, h * 0.5f, cz),
                new Vector3(w, h, d), col, root); // keeps its BoxCollider: blocks the player

            // darker base strip
            var base_ = Gfx.Prim(PrimitiveType.Cube, "BBase", new Vector3(cx, 0.5f, cz),
                new Vector3(w + 0.3f, 1f, d + 0.3f), col * 0.7f, root);
            Gfx.StripCollider(base_);
            // roof slab
            var roof = Gfx.Prim(PrimitiveType.Cube, "Roof", new Vector3(cx, h + 0.25f, cz),
                new Vector3(w + 0.6f, 0.5f, d + 0.6f), col * 0.8f, root);
            Gfx.StripCollider(roof);
            // window strips: two lit bands on the front face
            var winMat = Gfx.UnlitMat(new Color(1f, 0.95f, 0.6f));
            int floors = Mathf.Max(1, Mathf.FloorToInt(h / 4f));
            for (int f = 0; f < floors; f++)
            {
                float wy = 2.6f + f * 4f;
                if (wy > h - 1.2f) break;
                var win = Gfx.Prim(PrimitiveType.Cube, "Win", new Vector3(cx, wy, cz + d * 0.5f + 0.06f),
                    new Vector3(w * 0.7f, 1.1f, 0.05f), Color.white, root);
                win.GetComponent<Renderer>().sharedMaterial = winMat;
                Gfx.StripCollider(win);
                var win2 = Gfx.Prim(PrimitiveType.Cube, "Win", new Vector3(cx, wy, cz - d * 0.5f - 0.06f),
                    new Vector3(w * 0.7f, 1.1f, 0.05f), Color.white, root);
                win2.GetComponent<Renderer>().sharedMaterial = winMat;
                Gfx.StripCollider(win2);
            }
        }

        static void BuildPark(Transform root, System.Random rng, float cx, float cz)
        {
            var lawn = Gfx.Prim(PrimitiveType.Cube, "Park", new Vector3(cx, 0.1f, cz),
                new Vector3(20f, 0.2f, 20f), new Color(0.35f, 0.68f, 0.35f), root);
            Gfx.StripCollider(lawn);
            for (int i = 0; i < 4; i++)
            {
                float tx = cx + (float)(rng.NextDouble() - 0.5) * 14f;
                float tz = cz + (float)(rng.NextDouble() - 0.5) * 14f;
                BuildTree(root, tx, tz, 0.9f + (float)rng.NextDouble() * 0.4f);
            }
        }

        static void ScatterTrees(Transform root, System.Random rng, float half)
        {
            for (int i = 0; i < GridSize; i++)
            {
                float c = i * Pitch - half;
                for (float p = -half + 8f; p <= half - 8f; p += 18f)
                {
                    if (rng.NextDouble() < 0.5) BuildTree(root, p, c + RoadWidth * 0.5f + 1.6f, 0.8f + (float)rng.NextDouble() * 0.3f);
                    else BuildTree(root, c - RoadWidth * 0.5f - 1.6f, p, 0.8f + (float)rng.NextDouble() * 0.3f);
                }
            }
        }

        static void BuildTree(Transform root, float x, float z, float k)
        {
            var trunk = Gfx.Prim(PrimitiveType.Cylinder, "Trunk", new Vector3(x, 1.1f * k, z),
                new Vector3(0.5f * k, 1.1f * k, 0.5f * k), new Color(0.45f, 0.3f, 0.18f), root);
            Gfx.StripCollider(trunk);
            var leaves = Gfx.Prim(PrimitiveType.Sphere, "Leaves", new Vector3(x, 2.6f * k, z),
                new Vector3(2.2f * k, 2.4f * k, 2.2f * k), new Color(0.25f, 0.6f, 0.3f), root);
            Gfx.StripCollider(leaves);
        }

        static void BuildNile(Transform root, float half, float span)
        {
            // decorative Nile strip on the east edge
            var water = Gfx.Prim(PrimitiveType.Cube, "Nile", new Vector3(half + 26f, 0.02f, 0),
                new Vector3(30f, 0.05f, span * 2.2f), new Color(0.15f, 0.5f, 0.85f), root);
            Gfx.StripCollider(water);
            var sand = Gfx.Prim(PrimitiveType.Cube, "Bank", new Vector3(half + 9.5f, 0.04f, 0),
                new Vector3(4f, 0.06f, span * 2.2f), new Color(0.9f, 0.82f, 0.6f), root);
            Gfx.StripCollider(sand);
        }
    }
}
