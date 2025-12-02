using System.Collections.Generic;
using UnityEngine;

public static class TilePathfinder
{
    /// <summary>
    /// Finds a path from startWorld -> targetWorld.
    /// Returns waypoints in world space (tile centers).
    /// </summary>
    public static bool TryFindPath(
        Vector3 startWorld,
        Vector3 targetWorld,
        List<Vector3> outWorldPath,
        int maxRange = 30,
        bool allowDiagonal = true)
    {
        outWorldPath ??= new List<Vector3>();
        outWorldPath.Clear();

        var nav = TileNavWorld.Instance;
        if (nav == null) return false;

        Vector2Int start = nav.WorldToCell(startWorld);
        Vector2Int goal = nav.WorldToCell(targetWorld);

        bool startWalk = nav.IsWalkableCell(start);
        bool goalWalk = nav.IsWalkableCell(goal);
        //Debug.Log($"[TilePathfinder] start={start} walk={startWalk}, goal={goal} walk={goalWalk}");

        if (!startWalk || !goalWalk)
            return false;

        List<Vector2Int> tilePath = new List<Vector2Int>();
        if (!FindPathAStar(nav, start, goal, tilePath, maxRange, allowDiagonal))
            return false;

        for (int i = 0; i < tilePath.Count; i++)
        {
            outWorldPath.Add(nav.CellToWorldCenter(tilePath[i]));
        }

        return outWorldPath.Count > 0;
    }


    // ------------------- A* implementation -------------------

    private static bool FindPathAStar(
        TileNavWorld nav,
        Vector2Int start,
        Vector2Int goal,
        List<Vector2Int> outPath,
        int maxRange,
        bool allowDiagonal)
    {
        outPath.Clear();

        int minX = Mathf.Min(start.x, goal.x) - maxRange;
        int maxX = Mathf.Max(start.x, goal.x) + maxRange;
        int minY = Mathf.Min(start.y, goal.y) - maxRange;
        int maxY = Mathf.Max(start.y, goal.y) + maxRange;

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        if (width <= 0 || height <= 0)
            return false;

        int nodeCount = width * height;
        const float INF = float.MaxValue;

        float[] gCost = new float[nodeCount];
        float[] fCost = new float[nodeCount];
        int[] cameFrom = new int[nodeCount];
        bool[] closed = new bool[nodeCount];
        bool[] walkable = new bool[nodeCount];

        for (int i = 0; i < nodeCount; i++)
        {
            gCost[i] = INF;
            fCost[i] = INF;
            cameFrom[i] = -1;
            closed[i] = false;
            walkable[i] = false;
        }

        // Precompute walkability in the search window
        for (int y = 0; y < height; y++)
        {
            int wy = minY + y;
            for (int x = 0; x < width; x++)
            {
                int wx = minX + x;
                int idx = y * width + x;
                walkable[idx] = nav.IsWalkableCell(new Vector2Int(wx, wy));
            }
        }

        int startIdx = IndexOf(start.x, start.y, minX, minY, width, height);
        int goalIdx = IndexOf(goal.x, goal.y, minX, minY, width, height);
        if (startIdx < 0 || goalIdx < 0) return false;
        if (!walkable[startIdx] || !walkable[goalIdx]) return false;

        List<int> open = new List<int>();
        gCost[startIdx] = 0f;
        fCost[startIdx] = Heuristic(start, goal);
        open.Add(startIdx);

        List<Vector2Int> neighborOffsets = new List<Vector2Int>
        {
            new Vector2Int( 1,  0),
            new Vector2Int(-1,  0),
            new Vector2Int( 0,  1),
            new Vector2Int( 0, -1)
        };
        if (allowDiagonal)
        {
            neighborOffsets.Add(new Vector2Int(1, 1));
            neighborOffsets.Add(new Vector2Int(-1, 1));
            neighborOffsets.Add(new Vector2Int(1, -1));
            neighborOffsets.Add(new Vector2Int(-1, -1));
        }

        while (open.Count > 0)
        {
            int currentIdx = open[0];
            float bestF = fCost[currentIdx];

            for (int i = 1; i < open.Count; i++)
            {
                int idx = open[i];
                if (fCost[idx] < bestF)
                {
                    bestF = fCost[idx];
                    currentIdx = idx;
                }
            }

            if (currentIdx == goalIdx)
            {
                ReconstructPath(outPath, cameFrom, currentIdx, minX, minY, width);
                return outPath.Count > 0;
            }

            open.Remove(currentIdx);
            closed[currentIdx] = true;

            int cx, cy;
            CoordOf(currentIdx, minX, minY, width, out cx, out cy);

            foreach (var offset in neighborOffsets)
            {
                int nx = cx + offset.x;
                int ny = cy + offset.y;
                int nIdx = IndexOf(nx, ny, minX, minY, width, height);
                if (nIdx < 0) continue;
                if (closed[nIdx] || !walkable[nIdx]) continue;

                float stepCost = (offset.x == 0 || offset.y == 0) ? 1f : 1.4142f;
                float tentativeG = gCost[currentIdx] + stepCost;
                if (tentativeG >= gCost[nIdx]) continue;

                cameFrom[nIdx] = currentIdx;
                gCost[nIdx] = tentativeG;

                Vector2Int nWorld = new Vector2Int(nx, ny);
                fCost[nIdx] = tentativeG + Heuristic(nWorld, goal);

                if (!open.Contains(nIdx))
                    open.Add(nIdx);
            }
        }

        return false;
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx + dy + (Mathf.Sqrt(2f) - 2f) * Mathf.Min(dx, dy);
    }

    private static int IndexOf(int x, int y, int minX, int minY, int width, int height)
    {
        int lx = x - minX;
        int ly = y - minY;
        if (lx < 0 || ly < 0 || lx >= width || ly >= height)
            return -1;
        return ly * width + lx;
    }

    private static void CoordOf(int idx, int minX, int minY, int width, out int x, out int y)
    {
        y = idx / width;
        x = idx % width;
        x += minX;
        y += minY;
    }

    private static void ReconstructPath(
        List<Vector2Int> outPath,
        int[] cameFrom,
        int currentIdx,
        int minX,
        int minY,
        int width)
    {
        outPath.Clear();
        while (currentIdx >= 0)
        {
            int cx = currentIdx % width;
            int cy = currentIdx / width;
            outPath.Add(new Vector2Int(minX + cx, minY + cy));
            currentIdx = cameFrom[currentIdx];
        }
        outPath.Reverse();
    }
}
