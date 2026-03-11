using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.TestTools;

public class MusicLibraryTests
{
    private MusicLibrary _musicLibrary;

    [SetUp]
    public void SetUp()
    {
        _musicLibrary = ScriptableObject.CreateInstance<MusicLibrary>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_musicLibrary);
    }

    /// <summary>
    /// Helper method to create a MusicDefinition instance with a specific ID.
    /// This uses reflection to set the private serialized 'id' field, which is necessary
    /// because we are testing without full Unity Editor serialization mocking.
    /// </summary>
    private MusicDefinition CreateMusicDefinition(string id)
    {
        var definition = ScriptableObject.CreateInstance<MusicDefinition>();
        var idField = typeof(MusicDefinition).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
        if (idField != null)
        {
            idField.SetValue(definition, id);
        }
        return definition;
    }

    /// <summary>
    /// Helper method to inject the definitions array into the MusicLibrary instance
    /// using reflection.
    /// </summary>
    private void SetMusicLibraryDefinitions(MusicDefinition[] definitions)
    {
        var definitionsField = typeof(MusicLibrary).GetField("definitions", BindingFlags.NonPublic | BindingFlags.Instance);
        if (definitionsField != null)
        {
            definitionsField.SetValue(_musicLibrary, definitions);
        }
    }

    /// <summary>
    /// Helper method to access the internal cache dictionary to verify test results.
    /// </summary>
    private Dictionary<string, MusicDefinition> GetCache()
    {
        var cacheField = typeof(MusicLibrary).GetField("idToDefinition", BindingFlags.NonPublic | BindingFlags.Instance);
        return cacheField?.GetValue(_musicLibrary) as Dictionary<string, MusicDefinition>;
    }

    [Test]
    public void RebuildCache_WithNullDefinitionsArray_DoesNotThrowAndCacheIsEmpty()
    {
        SetMusicLibraryDefinitions(null);

        Assert.DoesNotThrow(() => _musicLibrary.RebuildCache());
        var cache = GetCache();
        Assert.AreEqual(0, cache.Count);
    }

    [Test]
    public void RebuildCache_WithValidDefinitions_AddsToCache()
    {
        var def1 = CreateMusicDefinition("track1");
        var def2 = CreateMusicDefinition("track2");
        SetMusicLibraryDefinitions(new[] { def1, def2 });

        _musicLibrary.RebuildCache();

        var cache = GetCache();
        Assert.AreEqual(2, cache.Count);
        Assert.IsTrue(cache.ContainsKey("track1"));
        Assert.IsTrue(cache.ContainsKey("track2"));
        Assert.AreSame(def1, cache["track1"]);
        Assert.AreSame(def2, cache["track2"]);

        Object.DestroyImmediate(def1);
        Object.DestroyImmediate(def2);
    }

    [Test]
    public void RebuildCache_WithNullDefinitionInArray_IgnoresNull()
    {
        var def1 = CreateMusicDefinition("track1");
        SetMusicLibraryDefinitions(new[] { def1, null });

        _musicLibrary.RebuildCache();

        var cache = GetCache();
        Assert.AreEqual(1, cache.Count);
        Assert.IsTrue(cache.ContainsKey("track1"));

        Object.DestroyImmediate(def1);
    }

    [Test]
    public void RebuildCache_WithEmptyOrWhitespaceId_IgnoresDefinition()
    {
        var defNull = CreateMusicDefinition(null);
        var defEmpty = CreateMusicDefinition("");
        var defWhitespace = CreateMusicDefinition("   ");
        var defValid = CreateMusicDefinition("valid");

        SetMusicLibraryDefinitions(new[] { defNull, defEmpty, defWhitespace, defValid });

        _musicLibrary.RebuildCache();

        var cache = GetCache();
        Assert.AreEqual(1, cache.Count);
        Assert.IsTrue(cache.ContainsKey("valid"));

        Object.DestroyImmediate(defNull);
        Object.DestroyImmediate(defEmpty);
        Object.DestroyImmediate(defWhitespace);
        Object.DestroyImmediate(defValid);
    }

    [Test]
    public void RebuildCache_WithDuplicateIds_KeepsFirstAndLogsError()
    {
        var def1 = CreateMusicDefinition("duplicate");
        var def2 = CreateMusicDefinition("duplicate");

        SetMusicLibraryDefinitions(new[] { def1, def2 });

        // Using LogAssert to expect the error message from the duplicate ID check
        LogAssert.Expect(LogType.Error, "MusicLibrary has duplicate id: 'duplicate'.");

        _musicLibrary.RebuildCache();

        var cache = GetCache();
        Assert.AreEqual(1, cache.Count);
        // It should keep the first one
        Assert.AreSame(def1, cache["duplicate"]);

        Object.DestroyImmediate(def1);
        Object.DestroyImmediate(def2);
    }

    [Test]
    public void RebuildCache_ClearsExistingCacheBeforeRebuilding()
    {
        var def1 = CreateMusicDefinition("track1");
        SetMusicLibraryDefinitions(new[] { def1 });
        _musicLibrary.RebuildCache();

        var cache = GetCache();
        Assert.AreEqual(1, cache.Count);

        // Change definitions and rebuild
        var def2 = CreateMusicDefinition("track2");
        SetMusicLibraryDefinitions(new[] { def2 });
        _musicLibrary.RebuildCache();

        cache = GetCache();
        Assert.AreEqual(1, cache.Count);
        Assert.IsTrue(cache.ContainsKey("track2"));
        Assert.IsFalse(cache.ContainsKey("track1"));

        Object.DestroyImmediate(def1);
        Object.DestroyImmediate(def2);
    }
}
