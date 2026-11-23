using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// شاشة التحميل: الشريط يمتلي في 10 ثواني وبعدها تفتح الشاشة الرئيسية
public class LoadingScreen : MonoBehaviour
{
    public Slider progressBar;                 // شريط التحميل
    public string nextSceneName = "MainMenu";  // اسم الشاشة اللي بعد اللودينج
    public float loadDuration = 10f;           // مدة التحميل بالثواني

    private void Start()
    {
        if (progressBar != null) progressBar.value = 0f;
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        // نبدأ تحميل الشاشة التالية في الخلفية من غير ما تفتح
        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        if (op == null)
        {
            Debug.LogError("مش لاقي الـ Scene: " + nextSceneName + " (ضيفها في Build Settings)");
            yield break;
        }
        op.allowSceneActivation = false;

        // الشريط يمتلي على مدار المدة المحددة
        float t = 0f;
        while (t < loadDuration)
        {
            t += Time.unscaledDeltaTime;
            if (progressBar != null) progressBar.value = Mathf.Clamp01(t / loadDuration);
            yield return null;
        }
        if (progressBar != null) progressBar.value = 1f;

        // ننتظر لحد ما الشاشة التالية تجهز (0.9 = جاهزة)
        while (op.progress < 0.9f) yield return null;

        op.allowSceneActivation = true;
    }
}
