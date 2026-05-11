using UnityEngine;

[System.Serializable]
public sealed class DecoyTotemRuntime : PlayerAbilityRuntime
{
    private DecoyTotem _activeTotem;

    public bool HasActiveTotem => _activeTotem != null && !_activeTotem.IsExpired;

    public bool PlaceTotem(
        DecoyTotem totemPrefab,
        Vector2 position,
        DecoyTotemSettings settings,
        bool replaceExistingTotem)
    {
        if (_activeTotem != null)
        {
            if (!replaceExistingTotem && !_activeTotem.IsExpired)
                return false;

            _activeTotem.Dismiss("replaced");
            _activeTotem = null;
        }

        DecoyTotem totem = SpawnTotem(totemPrefab, position, settings);
        if (totem == null)
            return false;

        _activeTotem = totem;
        _activeTotem.Initialize(settings);
        return true;
    }

    public void Tick()
    {
        if (_activeTotem == null || _activeTotem.IsExpired)
            _activeTotem = null;
    }

    private static DecoyTotem SpawnTotem(DecoyTotem totemPrefab, Vector2 position, DecoyTotemSettings settings)
    {
        if (totemPrefab != null)
            return Object.Instantiate(totemPrefab, position, Quaternion.identity);

        GameObject totemObject = new GameObject("DecoyTotem");
        totemObject.transform.position = position;

        Rigidbody2D rigidbody2D = totemObject.AddComponent<Rigidbody2D>();
        rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        rigidbody2D.gravityScale = 0f;
        rigidbody2D.constraints = RigidbodyConstraints2D.FreezeAll;

        CircleCollider2D collider = totemObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = Mathf.Max(0.05f, settings.targetColliderRadius);

        return totemObject.AddComponent<DecoyTotem>();
    }
}
