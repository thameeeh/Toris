using UnityEngine;

public static class ChunkStreamingBoundsCalculator
{
    public static bool TryCalculate(
        Grid grid,
        Camera camera,
        WorldProfile worldProfile,
        int preloadChunks,
        int unloadHysteresisChunks,
        out ChunkStreamingBounds bounds)
    {
        bounds = default;

        if (grid == null || camera == null || worldProfile == null)
            return false;

        GetCameraChunkRect(grid, camera, worldProfile, out Vector2Int loadMinChunk, out Vector2Int loadMaxChunk);

        int paddingChunks = Mathf.Max(0, worldProfile.viewDistanceChunks) + Mathf.Max(0, preloadChunks);
        loadMinChunk -= new Vector2Int(paddingChunks, paddingChunks);
        loadMaxChunk += new Vector2Int(paddingChunks, paddingChunks);

        int unloadPadding = Mathf.Max(0, unloadHysteresisChunks);
        Vector2Int unloadMinChunk = loadMinChunk - new Vector2Int(unloadPadding, unloadPadding);
        Vector2Int unloadMaxChunk = loadMaxChunk + new Vector2Int(unloadPadding, unloadPadding);

        bounds = new ChunkStreamingBounds(
            loadMinChunk,
            loadMaxChunk,
            unloadMinChunk,
            unloadMaxChunk);

        return true;
    }

    private static void GetCameraChunkRect(
        Grid grid,
        Camera camera,
        WorldProfile worldProfile,
        out Vector2Int minChunk,
        out Vector2Int maxChunk)
    {
        float zPlane = 0f;
        float distanceToPlane = DistanceAlongCameraForwardToZPlane(camera, zPlane);

        Vector3 worldBottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, distanceToPlane));
        Vector3 worldBottomRight = camera.ViewportToWorldPoint(new Vector3(1f, 0f, distanceToPlane));
        Vector3 worldTopLeft = camera.ViewportToWorldPoint(new Vector3(0f, 1f, distanceToPlane));
        Vector3 worldTopRight = camera.ViewportToWorldPoint(new Vector3(1f, 1f, distanceToPlane));

        Vector3Int cellBottomLeft = grid.WorldToCell(worldBottomLeft);
        Vector3Int cellBottomRight = grid.WorldToCell(worldBottomRight);
        Vector3Int cellTopLeft = grid.WorldToCell(worldTopLeft);
        Vector3Int cellTopRight = grid.WorldToCell(worldTopRight);

        int minX = Mathf.Min(cellBottomLeft.x, cellBottomRight.x, cellTopLeft.x, cellTopRight.x) - 1;
        int maxX = Mathf.Max(cellBottomLeft.x, cellBottomRight.x, cellTopLeft.x, cellTopRight.x) + 1;
        int minY = Mathf.Min(cellBottomLeft.y, cellBottomRight.y, cellTopLeft.y, cellTopRight.y) - 1;
        int maxY = Mathf.Max(cellBottomLeft.y, cellBottomRight.y, cellTopLeft.y, cellTopRight.y) + 1;

        int chunkSize = Mathf.Max(1, worldProfile.chunkSize);

        minChunk = TileToChunk(new Vector2Int(minX, minY), chunkSize);
        maxChunk = TileToChunk(new Vector2Int(maxX, maxY), chunkSize);
    }

    private static float DistanceAlongCameraForwardToZPlane(Camera camera, float zPlane)
    {
        Vector3 cameraPosition = camera.transform.position;
        Vector3 forward = camera.transform.forward;

        float denominator = forward.z;
        if (Mathf.Abs(denominator) < 0.00001f)
            return camera.nearClipPlane;

        float t = (zPlane - cameraPosition.z) / denominator;
        if (t < 0f)
            t = -t;

        return Mathf.Max(camera.nearClipPlane, t);
    }

    private static Vector2Int TileToChunk(Vector2Int tile, int chunkSize)
    {
        int chunkX = FloorDiv(tile.x, chunkSize);
        int chunkY = FloorDiv(tile.y, chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    private static int FloorDiv(int value, int divisor)
    {
        if (divisor == 0)
            return 0;

        int quotient = value / divisor;
        int remainder = value % divisor;

        if (remainder != 0 && ((remainder > 0) != (divisor > 0)))
            quotient--;

        return quotient;
    }
}
