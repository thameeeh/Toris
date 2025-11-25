using NUnit.Framework;
using UnityEngine;

public class ProjectilePoolRegistryTests
{
    [Test]
    public void SpawnReuseAndReleaseFollowsProjectileContract()
    {
        var registryGO = new GameObject("ProjectilePoolRegistry");
        var registry = registryGO.AddComponent<ProjectilePoolRegistry>();

        var prefabGO = new GameObject("ArrowPrefab");
        prefabGO.AddComponent<Rigidbody2D>();
        prefabGO.AddComponent<BoxCollider2D>();
        var projectilePrefab = prefabGO.AddComponent<ArrowProjectile>();

        var spawned = registry.Spawn(projectilePrefab, Vector3.zero, Quaternion.identity);
        Assert.NotNull(spawned);
        Assert.IsTrue(spawned.gameObject.activeSelf);

        var body = spawned.GetComponent<Rigidbody2D>();
        var collider = spawned.GetComponent<Collider2D>();

        spawned.Initialize(Vector2.right, 5f, 1f, 1f);
        Assert.AreNotEqual(Vector2.zero, body.linearVelocity);

        registry.Release(spawned);
        Assert.IsFalse(spawned.gameObject.activeSelf);
        Assert.AreEqual(registry.transform, spawned.transform.parent);

        var reused = registry.Spawn(projectilePrefab, Vector3.up, Quaternion.identity);
        Assert.AreSame(spawned, reused);
        Assert.AreEqual(Vector2.zero, body.linearVelocity);
        Assert.IsTrue(collider.enabled);

        Object.DestroyImmediate(registryGO);
        Object.DestroyImmediate(prefabGO);
    }
}