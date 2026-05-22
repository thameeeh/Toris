using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using PickingMode = UnityEngine.UIElements.PickingMode;
using UIDocument = UnityEngine.UIElements.UIDocument;
using VisualElement = UnityEngine.UIElements.VisualElement;

internal sealed class SceneUiInputSuspender
{
    private readonly List<EventSystem> suspendedEventSystems = new List<EventSystem>();
    private readonly Dictionary<VisualElement, PickingMode> suspendedPickingModes =
        new Dictionary<VisualElement, PickingMode>();
    private UIDocument excludedDocument;

    public void SetExcludedDocument(UIDocument document)
    {
        excludedDocument = document;
    }

    public void Suspend()
    {
        SuspendActiveEventSystems();
        SuspendUiToolkitPicking();
    }

    public void Resume()
    {
        ResumeUiToolkitPicking();
        ResumeEventSystems();
    }

    private void SuspendActiveEventSystems()
    {
        EventSystem[] eventSystems = UnityEngine.Object.FindObjectsOfType<EventSystem>();
        for (int i = 0; i < eventSystems.Length; i++)
        {
            EventSystem eventSystem = eventSystems[i];
            if (eventSystem == null || !eventSystem.enabled)
                continue;

            if (!suspendedEventSystems.Contains(eventSystem))
            {
                suspendedEventSystems.Add(eventSystem);
            }

            eventSystem.enabled = false;
        }
    }

    private void ResumeEventSystems()
    {
        for (int i = 0; i < suspendedEventSystems.Count; i++)
        {
            EventSystem eventSystem = suspendedEventSystems[i];
            if (eventSystem != null)
            {
                eventSystem.enabled = true;
            }
        }

        suspendedEventSystems.Clear();
    }

    private void SuspendUiToolkitPicking()
    {
        UIDocument[] documents = UnityEngine.Object.FindObjectsOfType<UIDocument>();
        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];
            if (document == null || document.rootVisualElement == null)
                continue;

            if (document == excludedDocument)
                continue;

            SuspendPickingRecursive(document.rootVisualElement);
        }
    }

    private void SuspendPickingRecursive(VisualElement element)
    {
        if (element == null)
            return;

        if (!suspendedPickingModes.ContainsKey(element))
        {
            suspendedPickingModes.Add(element, element.pickingMode);
        }

        element.pickingMode = PickingMode.Ignore;

        int childCount = element.childCount;
        for (int i = 0; i < childCount; i++)
        {
            SuspendPickingRecursive(element.ElementAt(i));
        }
    }

    private void ResumeUiToolkitPicking()
    {
        foreach (KeyValuePair<VisualElement, PickingMode> entry in suspendedPickingModes)
        {
            if (entry.Key != null)
            {
                entry.Key.pickingMode = entry.Value;
            }
        }

        suspendedPickingModes.Clear();
    }
}
