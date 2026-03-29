using System;
using System.Collections.Generic;
using UnityEngine;

public static class SitePlacementSampling
{
    public static List<Vector2Int> PickSpacedCentersInBiomeDisk(
        int biomeSeed,
        Vector2Int originTile,
        float radiusTiles,
        int targetCount,
        int spacingTiles,
        int avoidOriginRadiusTiles,
        int attemptsPerTarget,
        int baseAttempts,
        int relaxSteps,
        int relaxSpacingStep,
        int relaxedSpacingFloor,
        int relaxStartIndexBase,
        uint sampleSalt,
        Func<Vector2Int, bool> isCandidateAllowed)
    {
        List<Vector2Int> chosenCenters = new List<Vector2Int>(Mathf.Max(0, targetCount));

        if (targetCount <= 0 || isCandidateAllowed == null)
            return chosenCenters;

        int attempts = Mathf.Max(baseAttempts, targetCount * attemptsPerTarget);

        for (int i = 0; i < attempts && chosenCenters.Count < targetCount; i++)
        {
            Vector2Int candidateTile = PickPointInDisk(
                biomeSeed,
                i,
                originTile,
                radiusTiles,
                sampleSalt);

            if ((candidateTile - originTile).sqrMagnitude < avoidOriginRadiusTiles * avoidOriginRadiusTiles)
                continue;

            if (!isCandidateAllowed(candidateTile))
                continue;

            if (!IsFarEnough(candidateTile, chosenCenters, spacingTiles))
                continue;

            chosenCenters.Add(candidateTile);
        }

        for (int relaxStep = 0; relaxStep < relaxSteps && chosenCenters.Count < targetCount; relaxStep++)
        {
            int relaxedSpacing = Mathf.Max(
                relaxedSpacingFloor,
                spacingTiles - (relaxStep + 1) * relaxSpacingStep);

            int startIndex = relaxStartIndexBase + relaxStep * relaxStartIndexBase;

            for (int i = 0; i < attempts && chosenCenters.Count < targetCount; i++)
            {
                Vector2Int candidateTile = PickPointInDisk(
                    biomeSeed,
                    startIndex + i,
                    originTile,
                    radiusTiles,
                    sampleSalt);

                if (!isCandidateAllowed(candidateTile))
                    continue;

                if (!IsFarEnough(candidateTile, chosenCenters, relaxedSpacing))
                    continue;

                chosenCenters.Add(candidateTile);
            }
        }

        return chosenCenters;
    }

    private static bool IsFarEnough(
        Vector2Int candidateTile,
        List<Vector2Int> chosenCenters,
        int spacingTiles)
    {
        int spacingSquared = spacingTiles * spacingTiles;

        for (int i = 0; i < chosenCenters.Count; i++)
        {
            if ((chosenCenters[i] - candidateTile).sqrMagnitude < spacingSquared)
                return false;
        }

        return true;
    }

    private static Vector2Int PickPointInDisk(
        int biomeSeed,
        int index,
        Vector2Int originTile,
        float radiusTiles,
        uint sampleSalt)
    {
        uint angleHash = DeterministicHash.Hash((uint)biomeSeed, index, 0, sampleSalt);
        uint radiusHash = DeterministicHash.Hash((uint)biomeSeed, index, 1, sampleSalt);

        float angle01 = DeterministicHash.Hash01(angleHash);
        float radius01 = DeterministicHash.Hash01(radiusHash);

        float angleRadians = angle01 * Mathf.PI * 2f;
        float distance = Mathf.Sqrt(radius01) * radiusTiles;

        int offsetX = Mathf.RoundToInt(Mathf.Cos(angleRadians) * distance);
        int offsetY = Mathf.RoundToInt(Mathf.Sin(angleRadians) * distance);

        return originTile + new Vector2Int(offsetX, offsetY);
    }
}