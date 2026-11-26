using UnityEngine;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // Transparent tab buttons over a background image sometimes stop catching
    // touches when their Image alpha is 0 with "Raycast Target" quirks, or when
    // an invisible graphic sits on top. Add this to the Canvas: it makes sure
    // every Button has a raycastable graphic and no invisible image blocks it.
    public class TransparentButtonFix : MonoBehaviour
    {
        void Start() { Fix(); }

        public void Fix()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in buttons)
            {
                var img = b.GetComponent<Image>();
                if (img == null)
                {
                    img = b.gameObject.AddComponent<Image>();
                    img.color = new Color(1, 1, 1, 0f); // invisible but raycastable
                }
                img.raycastTarget = true;
            }
        }
    }
}
