using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.Tutorial
{
    public static class TutorialAnchorRegistry
    {
        private static readonly Dictionary<string, VisualElement> Anchors = new Dictionary<string, VisualElement>();

        public static void Register(string anchorId, VisualElement element)
        {
            if (string.IsNullOrWhiteSpace(anchorId) || element == null)
                return;

            Anchors[anchorId] = element;
        }

        public static void Unregister(string anchorId, VisualElement element)
        {
            if (string.IsNullOrWhiteSpace(anchorId))
                return;

            if (Anchors.TryGetValue(anchorId, out VisualElement registeredElement) && registeredElement == element)
                Anchors.Remove(anchorId);
        }

        public static bool TryGetVisibleBounds(string anchorId, out Rect bounds)
        {
            bounds = default(Rect);

            if (!TryGetElement(anchorId, out VisualElement element))
                return false;

            if (element == null || element.panel == null)
                return false;

            bounds = element.worldBound;
            return bounds.width > 0f && bounds.height > 0f;
        }

        public static bool TryGetElement(string anchorId, out VisualElement element)
        {
            element = null;

            return !string.IsNullOrWhiteSpace(anchorId)
                && Anchors.TryGetValue(anchorId, out element)
                && element != null;
        }
    }
}
