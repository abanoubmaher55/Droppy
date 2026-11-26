using System.Collections.Generic;
using UnityEngine;

namespace SaveWaterEG
{
    // One road intersection in the city grid.
    public class RoadNode
    {
        public int gx, gz;          // grid coords 0..n-1
        public Vector3 pos;         // world position (y = 0)
        public readonly List<RoadNode> next = new List<RoadNode>();
    }

    // Grid of road intersections with BFS helpers used for:
    // breadcrumbs arrows, wrong-way detection and NPC routes.
    public class RoadGraph
    {
        public readonly int size;
        public readonly float pitch;
        public readonly List<RoadNode> nodes = new List<RoadNode>();
        RoadNode[,] _grid;

        public RoadGraph(int size, float pitch)
        {
            this.size = size;
            this.pitch = pitch;
            _grid = new RoadNode[size, size];
            float half = (size - 1) * 0.5f * pitch;
            for (int x = 0; x < size; x++)
                for (int z = 0; z < size; z++)
                {
                    var n = new RoadNode { gx = x, gz = z, pos = new Vector3(x * pitch - half, 0, z * pitch - half) };
                    _grid[x, z] = n;
                    nodes.Add(n);
                }
            for (int x = 0; x < size; x++)
                for (int z = 0; z < size; z++)
                {
                    if (x + 1 < size) { _grid[x, z].next.Add(_grid[x + 1, z]); _grid[x + 1, z].next.Add(_grid[x, z]); }
                    if (z + 1 < size) { _grid[x, z].next.Add(_grid[x, z + 1]); _grid[x, z + 1].next.Add(_grid[x, z]); }
                }
        }

        public RoadNode Node(int x, int z) { return _grid[x, z]; }

        public RoadNode Nearest(Vector3 p)
        {
            RoadNode best = null; float bd = float.MaxValue;
            foreach (var n in nodes)
            {
                float d = (n.pos - p).sqrMagnitude;
                if (d < bd) { bd = d; best = n; }
            }
            return best;
        }

        // BFS hop-distance from one node to every node (grid graph, unweighted).
        public Dictionary<RoadNode, int> DistancesFrom(RoadNode from)
        {
            var dist = new Dictionary<RoadNode, int>();
            var q = new Queue<RoadNode>();
            dist[from] = 0; q.Enqueue(from);
            while (q.Count > 0)
            {
                var n = q.Dequeue();
                foreach (var m in n.next)
                    if (!dist.ContainsKey(m)) { dist[m] = dist[n] + 1; q.Enqueue(m); }
            }
            return dist;
        }

        // Shortest path from -> to (inclusive), null if unreachable.
        public List<RoadNode> Path(RoadNode from, RoadNode to)
        {
            if (from == to) return new List<RoadNode> { from };
            var parent = new Dictionary<RoadNode, RoadNode>();
            var q = new Queue<RoadNode>();
            parent[from] = null; q.Enqueue(from);
            while (q.Count > 0)
            {
                var n = q.Dequeue();
                if (n == to) break;
                foreach (var m in n.next)
                    if (!parent.ContainsKey(m)) { parent[m] = n; q.Enqueue(m); }
            }
            if (!parent.ContainsKey(to)) return null;
            var path = new List<RoadNode>();
            for (var n = to; n != null; n = parent[n]) path.Add(n);
            path.Reverse();
            return path;
        }
    }
}
