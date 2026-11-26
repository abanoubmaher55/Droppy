using UnityEngine;
using UnityEngine.UI;

namespace SaveWaterEG
{
    // Top-down orthographic camera rendering the city into a RenderTexture
    // shown in the HUD. The player and fault are shown as round DOTS on a
    // markers-only layer the main camera culls out of the world.
    public class MinimapRig : MonoBehaviour
    {
        public const int MarkerLayer = 8; // unnamed user layer is fine

        public Transform target;          // player
        public Transform faultDot;

        Camera _cam;
        Transform _playerDot;

        public static MinimapRig Create(RawImage targetImage, Camera mainCam)
        {
            var go = new GameObject("MinimapRig");
            var rig = go.AddComponent<MinimapRig>();

            var camGo = new GameObject("MinimapCamera");
            camGo.transform.SetParent(go.transform, false);
            rig._cam = camGo.AddComponent<Camera>();
            rig._cam.orthographic = true;
            rig._cam.orthographicSize = 85f;
            rig._cam.clearFlags = CameraClearFlags.SolidColor;
            rig._cam.backgroundColor = new Color(0.35f, 0.55f, 0.4f);
            var rt = new RenderTexture(512, 512, 16);
            rig._cam.targetTexture = rt;
            rig._cam.cullingMask = ~(1 << 5); // everything except the UI layer
            if (targetImage != null) targetImage.texture = rt;

            // main camera must NOT render the marker dots
            if (mainCam != null) mainCam.cullingMask = mainCam.cullingMask & ~(1 << MarkerLayer);

            rig._playerDot = MakeDot(new Color(0.15f, 0.4f, 1f), 1.6f);
            rig.faultDot = MakeDot(new Color(1f, 0.15f, 0.1f), 2.2f);
            rig.faultDot.gameObject.SetActive(false);
            return rig;
        }

        static Transform MakeDot(Color c, float size)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            d.name = "MinimapDot";
            d.transform.localScale = Vector3.one * size;
            d.GetComponent<Renderer>().sharedMaterial = Gfx.UnlitMat(c);
            Gfx.StripCollider(d);
            SetLayerRecursively(d, MarkerLayer);
            return d.transform;
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform c in go.transform) SetLayerRecursively(c.gameObject, layer);
        }

        void LateUpdate()
        {
            if (target != null)
            {
                var p = target.position;
                _cam.transform.position = new Vector3(p.x, 80f, p.z);
                _cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                _playerDot.position = new Vector3(p.x, 30f, p.z);
            }
        }

        public void SetFault(Vector3 pos)
        {
            faultDot.position = new Vector3(pos.x, 30f, pos.z);
            faultDot.gameObject.SetActive(true);
        }

        public void ClearFault()
        {
            if (faultDot != null) faultDot.gameObject.SetActive(false);
        }
    }
}
