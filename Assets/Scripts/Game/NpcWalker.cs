using UnityEngine;

namespace SaveWaterEG
{
    // A pedestrian that strolls between road intersections on the sidewalk.
    // Uses the same CharacterRig (Resources/Characters/npc prefab if present,
    // else a pastel placeholder). Never blocks the player (no collider).
    public class NpcWalker : MonoBehaviour
    {
        RoadGraph _graph;
        RoadNode _target;
        float _speed;
        float _pause;
        CharacterRig _rig;
        float _side;   // sidewalk offset (left/right of the walking direction)

        public static NpcWalker Spawn(RoadGraph graph, RoadNode start, System.Random rng)
        {
            var go = new GameObject("NPC");
            go.transform.position = start.pos;
            var n = go.AddComponent<NpcWalker>();
            n._graph = graph;
            n._target = start;
            n._speed = 1.6f + (float)rng.NextDouble() * 1.2f;
            n._side = (rng.NextDouble() < 0.5 ? -1f : 1f) * 4.8f;
            var tint = new Color(0.5f + (float)rng.NextDouble() * 0.4f,
                                 0.45f + (float)rng.NextDouble() * 0.4f,
                                 0.5f + (float)rng.NextDouble() * 0.4f);
            n._rig = CharacterRig.Create(go.transform, "npc", tint, true);
            n.PickNext();
            return n;
        }

        void PickNext()
        {
            if (_target == null || _target.next.Count == 0) return;
            var from = _target;
            _target = from.next[Random.Range(0, from.next.Count)];
            _pause = Random.Range(0f, 1.5f);
        }

        void Update()
        {
            if (_target == null) return;
            if (_pause > 0f)
            {
                _pause -= Time.deltaTime;
                if (_rig != null) _rig.SetSpeed(0f);
                return;
            }
            Vector3 toGoal = _target.pos - transform.position; toGoal.y = 0;
            if (toGoal.magnitude < 1.2f) { PickNext(); return; }
            Vector3 dir = toGoal.normalized;
            // aim for the sidewalk lane beside the target, not the intersection centre
            Vector3 laneGoal = _target.pos + Vector3.Cross(Vector3.up, dir) * _side;
            Vector3 toLane = laneGoal - transform.position; toLane.y = 0;
            Vector3 moveDir = toLane.sqrMagnitude > 0.01f ? toLane.normalized : dir;
            Vector3 p = transform.position + moveDir * _speed * Time.deltaTime;
            p.y = 0;
            transform.position = p;
            if (moveDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(moveDir), 8f * Time.deltaTime);
            if (_rig != null) _rig.SetSpeed(_speed);
        }
    }
}
