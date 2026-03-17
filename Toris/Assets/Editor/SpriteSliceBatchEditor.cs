using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class SpriteSliceBatchEditor : EditorWindow
{
    private Texture2D _texture;
    private Vector2 _scroll;

    private readonly HashSet<string> _selectedSpriteNames = new();

    private bool _selectAll = true;
    private string _nameContains = "";

    private bool _editPivot = true;
    private SpriteAlignment _alignment = SpriteAlignment.Custom;
    private Vector2 _customPivot = new Vector2(0.5f, 0.0f);

    private bool _editPositionOffset = false;
    private Vector2 _positionOffset = Vector2.zero;

    private bool _editSizeOffset = false;
    private Vector2 _sizeOffset = Vector2.zero;

    private bool _renameWithPrefix = false;
    private string _prefix = "";

    [MenuItem("Tools/Sprites/Batch Edit Sprite Slices")]
    public static void Open()
    {
        GetWindow<SpriteSliceBatchEditor>("Sprite Slice Batch Editor");
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is Texture2D tex)
        {
            _texture = tex;
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Batch edit sprite slices on a Multiple sprite sheet.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            _texture = (Texture2D)EditorGUILayout.ObjectField("Texture", _texture, typeof(Texture2D), false);

            if (GUILayout.Button("Use Selected", GUILayout.Width(100)))
            {
                if (Selection.activeObject is Texture2D tex)
                    _texture = tex;
            }
        }

        if (_texture == null)
        {
            EditorGUILayout.HelpBox("Select a texture asset with Sprite Mode = Multiple.", MessageType.Info);
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(_texture);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
        {
            EditorGUILayout.HelpBox("Selected asset is not a texture importer.", MessageType.Error);
            return;
        }

        if (importer.textureType != TextureImporterType.Sprite)
        {
            EditorGUILayout.HelpBox("Texture Type must be Sprite (2D and UI).", MessageType.Warning);
        }

        if (importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            EditorGUILayout.HelpBox("Sprite Mode must be Multiple.", MessageType.Warning);
        }

        SpriteRect[] spriteRects = GetSpriteRects(importer);
        if (spriteRects == null || spriteRects.Length == 0)
        {
            EditorGUILayout.HelpBox("No sprite slices found.", MessageType.Warning);
            return;
        }

        DrawSelectionUI(spriteRects);
        EditorGUILayout.Space(8);
        DrawBatchEditUI(spriteRects, importer, assetPath);
    }

    private void DrawSelectionUI(SpriteRect[] spriteRects)
    {
        EditorGUILayout.LabelField("Select slices", EditorStyles.boldLabel);

        _selectAll = EditorGUILayout.ToggleLeft("Select all", _selectAll);
        _nameContains = EditorGUILayout.TextField("Name contains", _nameContains);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh selection"))
            {
                RebuildSelection(spriteRects);
            }

            if (GUILayout.Button("Clear"))
            {
                _selectedSpriteNames.Clear();
                _selectAll = false;
            }
        }

        if (_selectedSpriteNames.Count == 0 && _selectAll)
            RebuildSelection(spriteRects);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Selected: {CountSelected(spriteRects)} / {spriteRects.Length}");

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(220));
        foreach (var rect in spriteRects.OrderBy(r => r.name))
        {
            bool selectedNow = IsSelected(rect);
            bool selectedNext = EditorGUILayout.ToggleLeft(
                $"{rect.name}   Rect: ({rect.rect.x}, {rect.rect.y}, {rect.rect.width}, {rect.rect.height})",
                selectedNow
            );

            if (selectedNext && !selectedNow)
                _selectedSpriteNames.Add(rect.name);
            else if (!selectedNext && selectedNow)
            {
                _selectedSpriteNames.Remove(rect.name);
                _selectAll = false;
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawBatchEditUI(SpriteRect[] spriteRects, TextureImporter importer, string assetPath)
    {
        EditorGUILayout.LabelField("Batch edits", EditorStyles.boldLabel);

        EditorGUILayout.Space(4);
        _editPivot = EditorGUILayout.ToggleLeft("Edit pivot/alignment", _editPivot);
        using (new EditorGUI.DisabledScope(!_editPivot))
        {
            _alignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Alignment", _alignment);

            if (_alignment == SpriteAlignment.Custom)
            {
                _customPivot = EditorGUILayout.Vector2Field("Custom Pivot", _customPivot);
                _customPivot.x = Mathf.Clamp01(_customPivot.x);
                _customPivot.y = Mathf.Clamp01(_customPivot.y);
            }
        }

        EditorGUILayout.Space(6);
        _editPositionOffset = EditorGUILayout.ToggleLeft("Offset rect position", _editPositionOffset);
        using (new EditorGUI.DisabledScope(!_editPositionOffset))
        {
            _positionOffset = EditorGUILayout.Vector2Field("Position Offset", _positionOffset);
        }

        EditorGUILayout.Space(6);
        _editSizeOffset = EditorGUILayout.ToggleLeft("Offset rect size", _editSizeOffset);
        using (new EditorGUI.DisabledScope(!_editSizeOffset))
        {
            _sizeOffset = EditorGUILayout.Vector2Field("Size Offset", _sizeOffset);
        }

        EditorGUILayout.Space(6);
        _renameWithPrefix = EditorGUILayout.ToggleLeft("Add prefix to selected names", _renameWithPrefix);
        using (new EditorGUI.DisabledScope(!_renameWithPrefix))
        {
            _prefix = EditorGUILayout.TextField("Prefix", _prefix);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Apply to selected slices", GUILayout.Height(30)))
        {
            ApplyChanges(spriteRects, importer, assetPath);
        }
    }

    private void ApplyChanges(SpriteRect[] spriteRects, TextureImporter importer, string assetPath)
    {
        int changed = 0;

        foreach (var rect in spriteRects)
        {
            if (!IsSelected(rect))
                continue;

            Undo.RegisterCompleteObjectUndo(importer, "Batch Edit Sprite Slices");

            if (_editPivot)
            {
                rect.alignment = _alignment;
                if (_alignment == SpriteAlignment.Custom)
                    rect.pivot = _customPivot;
            }

            if (_editPositionOffset)
            {
                var r = rect.rect;
                r.position += _positionOffset;
                rect.rect = r;
            }

            if (_editSizeOffset)
            {
                var r = rect.rect;
                r.size += _sizeOffset;
                r.width = Mathf.Max(1, r.width);
                r.height = Mathf.Max(1, r.height);
                rect.rect = r;
            }

            if (_renameWithPrefix && !string.IsNullOrWhiteSpace(_prefix))
            {
                if (!rect.name.StartsWith(_prefix, StringComparison.Ordinal))
                    rect.name = _prefix + rect.name;
            }

            changed++;
        }

        SetSpriteRects(importer, spriteRects);

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        Debug.Log($"[SpriteSliceBatchEditor] Updated {changed} slice(s) on '{assetPath}'.");
    }

    private void RebuildSelection(SpriteRect[] spriteRects)
    {
        _selectedSpriteNames.Clear();

        foreach (var rect in spriteRects)
        {
            bool include = _selectAll;

            if (!string.IsNullOrWhiteSpace(_nameContains))
            {
                include &= rect.name.IndexOf(_nameContains, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (include)
                _selectedSpriteNames.Add(rect.name);
        }
    }

    private bool IsSelected(SpriteRect rect)
    {
        if (_selectAll && string.IsNullOrWhiteSpace(_nameContains))
            return true;

        if (_selectedSpriteNames.Contains(rect.name))
            return true;

        if (_selectAll && !string.IsNullOrWhiteSpace(_nameContains))
            return rect.name.IndexOf(_nameContains, StringComparison.OrdinalIgnoreCase) >= 0;

        return false;
    }

    private int CountSelected(SpriteRect[] spriteRects)
    {
        int count = 0;
        foreach (var rect in spriteRects)
        {
            if (IsSelected(rect))
                count++;
        }
        return count;
    }

    private static SpriteRect[] GetSpriteRects(TextureImporter importer)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();

        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            Debug.LogError("Could not get ISpriteEditorDataProvider. Make sure the 2D Sprite package is installed.");
            return null;
        }

        dataProvider.InitSpriteEditorDataProvider();
        return dataProvider.GetSpriteRects();
    }

    private static void SetSpriteRects(TextureImporter importer, SpriteRect[] spriteRects)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();

        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            Debug.LogError("Could not get ISpriteEditorDataProvider. Make sure the 2D Sprite package is installed.");
            return;
        }

        dataProvider.InitSpriteEditorDataProvider();
        dataProvider.SetSpriteRects(spriteRects);
        dataProvider.Apply();
    }
}