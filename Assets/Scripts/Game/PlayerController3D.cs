using UnityEngine;

namespace SaveWaterEG
{
    // Third-person movement on a CharacterController: joystick (touch/mouse)
    // or WASD/arrows. Rotates the character toward movement, applies gravity,
    // clamps to the city bounds. Buildings block via their BoxColliders.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController3D : MonoBehaviour
    {
        public float speed = 7f;
        public float turnSpeed = 12f;
        public float bound = 68f;

        CharacterController _cc;
        CharacterRig _rig;
        float _vy;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _rig = GetComponentInChildren<CharacterRig>();
        }

        public void SetRig(CharacterRig rig) { _rig = rig; }

        void Update()
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude < 0.01f) input = JoystickUI.Direction;
            Vector3 dir = new Vector3(input.x, 0, input.y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            // camera-relative would need the camera; the follow cam looks north-fixed,
            // so screen up = world +Z. Keep it simple and predictable.
            if (dir.sqrMagnitude > 0.001f)
            {
                var look = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
            }

            _vy += Physics.gravity.y * Time.deltaTime;
            Vector3 vel = dir * speed + Vector3.up * _vy;
            var flags = _cc.Move(vel * Time.deltaTime);
            if ((flags & CollisionFlags.Below) != 0) _vy = -0.5f;

            var p = transform.position;
            p.x = Mathf.Clamp(p.x, -bound, bound);
            p.z = Mathf.Clamp(p.z, -bound, bound);
            transform.position = p;

            float planar = new Vector2(vel.x, vel.z).magnitude;
            if (_rig != null) _rig.SetSpeed(planar);
        }
    }
}
