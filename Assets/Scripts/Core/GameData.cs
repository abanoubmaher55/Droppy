using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveWaterEG
{
    // ---------- Data models loaded from StreamingAssets JSON ----------

    [Serializable]
    public class MapLocation
    {
        public string id;
        public string name;        // Arabic display name
        public string type;        // fresh | salty | well | spring
        public float x;            // normalized 0..1 over the map panel (left->right)
        public float y;            // normalized 0..1 over the map panel (bottom->top)
        public string description; // short Arabic description for the info card
    }

    [Serializable]
    public class MapLocationList { public List<MapLocation> locations; }

    [Serializable]
    public class ChatQA
    {
        public string question;
        public List<string> keywords;
        public string answer;
    }

    [Serializable]
    public class ChatQAList { public List<ChatQA> items; }

    [Serializable]
    public class FixChoice
    {
        public string text;        // choice label (Arabic)
        public bool correct;
        public string explanation; // why wrong / why right (Arabic)
    }

    [Serializable]
    public class LevelData
    {
        public int level;
        public string title;        // e.g. "المستوى 1: الحنفية المكسورة"
        public string faultName;    // e.g. "حنفية مكسورة"
        public string faultIcon;    // emoji shown on the fault point
        public float wastePerSecond;// liters per second while unfixed
        public string intro;        // shown at level start
        public List<FixChoice> choices; // exactly 3, one correct
    }

    [Serializable]
    public class LevelList { public List<LevelData> levels; }

    [Serializable]
    public class RoleFaultSet { public int role; public List<LevelData> faults; }

    [Serializable]
    public class RoleFaultList { public List<RoleFaultSet> roles; }

    // ---------- JSON loading helpers ----------

    public static class GameData
    {
        public const int MaxLevels = 5;
        public const float WasteLimit = 25f; // liters -> game over

        static string LoadText(string fileName)
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var req = UnityEngine.Networking.UnityWebRequest.Get(path))
            {
                req.SendWebRequest();
                while (!req.isDone) { }
                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                { Debug.LogError("SaveWaterEG: cannot load " + path + " : " + req.error); return null; }
                return req.downloadHandler.text;
            }
#else
            if (!System.IO.File.Exists(path)) { Debug.LogError("SaveWaterEG: missing " + path); return null; }
            return System.IO.File.ReadAllText(path);
#endif
        }

        public static List<MapLocation> LoadLocations()
        {
            string json = LoadText("map_locations.json");
            return json == null ? new List<MapLocation>() : JsonUtility.FromJson<MapLocationList>(json).locations;
        }

        public static List<ChatQA> LoadChat()
        {
            string json = LoadText("chatbot_ar.json");
            return json == null ? new List<ChatQA>() : JsonUtility.FromJson<ChatQAList>(json).items;
        }

        public static List<LevelData> LoadLevels()
        {
            string json = LoadText("levels.json");
            var list = json == null ? new List<LevelData>() : JsonUtility.FromJson<LevelList>(json).levels;
            list.Sort((a, b) => a.level.CompareTo(b.level));
            return list;
        }

        // Per-role fault sets (role = selectedCharacter 0..3). Falls back to the
        // shared levels.json when the file or the role entry is missing.
        public static List<LevelData> LoadRoleFaults(int role)
        {
            string json = LoadText("role_faults.json");
            if (json != null)
            {
                var parsed = JsonUtility.FromJson<RoleFaultList>(json);
                if (parsed != null && parsed.roles != null)
                    foreach (var r in parsed.roles)
                        if (r.role == role && r.faults != null && r.faults.Count > 0)
                        {
                            r.faults.Sort((a, b) => a.level.CompareTo(b.level));
                            return r.faults;
                        }
            }
            return LoadLevels();
        }

        // Water type helpers (Arabic labels + colors)
        public static string TypeLabel(string type)
        {
            switch (type)
            {
                case "fresh": return "المياه العذبة";
                case "salty": return "المياه المالحة";
                case "well": return "الآبار";
                case "spring": return "العيون الطبيعية";
                default: return type;
            }
        }

        public static Color TypeColor(string type)
        {
            switch (type)
            {
                case "fresh": return new Color(0.15f, 0.55f, 1f);
                case "salty": return new Color(1f, 0.45f, 0.1f);
                case "well": return new Color(0.55f, 0.2f, 0.8f);
                case "spring": return new Color(0.95f, 0.8f, 0.1f);
                default: return Color.white;
            }
        }
    }
}
