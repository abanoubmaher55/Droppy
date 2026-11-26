using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveWaterEG
{
    // Game-wide state: selected character, current level, stars per level.
    // Auto-created; survives scene loads.
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        [Header("State")]
        public int selectedCharacter = -1;   // 0 فني, 1 كيميائي, 2 محصل مياه, 3 مهندس مياه
        public int currentLevel = 1;         // 1..5
        public int[] stars = new int[GameData.MaxLevels];

        public static readonly string[] CharacterNames = { "الفني", "الكيميائي", "محصل المياه", "مهندس المياه" };
        public static readonly string[] CharacterIcons = { "🔧", "🧪", "💧", "📐" };
        public static readonly string[] CharacterBlurbs =
        {
            "بيصلح أي عطل بسرعة ويعرف كل عدد الشغل.",
            "بيفهم تركيبة المياه وبيحلل أي تلوث.",
            "بيجمع عدادات المياه وبيحسب الهدر بدقة.",
            "بيصمم شبكات المياه ويحل المشاكل الكبيرة."
        };

        public const int SceneLoading = 0;
        public const int SceneMenu = 1;
        public const int SceneGame = 2;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (I == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SelectCharacter(int index) { selectedCharacter = index; }

        // Loads by build-settings index; inside the editor, if the scenes were
        // never added to Build Settings (e.g. Assets copied into a new project),
        // falls back to loading the scene file directly so Play always works.
        public static void LoadSceneSmart(int index, string scenePath)
        {
#if UNITY_EDITOR
            if (SceneManager.sceneCountInBuildSettings <= index)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    scenePath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(index);
        }

        public void StartGame()
        {
            if (selectedCharacter < 0) selectedCharacter = 0;
            currentLevel = 1;
            LoadSceneSmart(SceneGame, "Assets/Scenes/Game.unity");
        }

        public void CompleteLevel(int levelIndex, int earnedStars)
        {
            if (levelIndex >= 1 && levelIndex <= GameData.MaxLevels)
                stars[levelIndex - 1] = Mathf.Max(stars[levelIndex - 1], earnedStars);
        }

        public int TotalStars()
        {
            int s = 0; foreach (var v in stars) s += v; return s;
        }

        public void NextLevelOrMenu()
        {
            if (currentLevel >= GameData.MaxLevels) GoToMenu();
            else { currentLevel++; LoadSceneSmart(SceneGame, "Assets/Scenes/Game.unity"); }
        }

        public void GoToMenu() { LoadSceneSmart(SceneMenu, "Assets/Scenes/MainMenu.unity"); }
    }
}
