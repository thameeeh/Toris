using UnityEngine.UIElements;
using System;

namespace OutlandHaven.UIToolkit
{
    /// <summary>
    /// This is a base class for a functional unit of the UI. This can make up a full-screen interface or just
    /// part of one.
    /// </summary>

    public class UIView : IDisposable
    {
        protected bool m_HideOnAwake = true;

        // UI reveals other underlaying UIs, partially see-through
        protected bool m_IsOverlay;

        protected VisualElement m_TopElement;

        // Properties
        public VisualElement Root => m_TopElement;
        public bool IsTransparent => m_IsOverlay;
        public bool IsHidden => m_TopElement.style.display == DisplayStyle.None;

        // Constructor
        /// <summary>
        /// Initializes a new instance of the UIView class.
        /// </summary>
        /// <param name="topElement">The topmost VisualElement in the UXML hierarchy.</param>
        public UIView(VisualElement topElement)
        {
            m_TopElement = topElement ?? throw new ArgumentNullException(nameof(topElement));
            Initialize();
        }

        public virtual void Initialize()
        {
            if (m_HideOnAwake)
            {
                Hide();
            }
            SetVisualElements();
            RegisterButtonCallbacks();
        }

        // Sets up the VisualElements for the UI. Override to customize.
        protected virtual void SetVisualElements()
        {

        }

        // Registers callbacks for buttons in the UI. Override to customize.
        protected virtual void RegisterButtonCallbacks()
        {

        }

        // Displays the UI.
        public virtual void Show()
        {
            m_TopElement.style.display = DisplayStyle.Flex;
        }

        // Hides the UI.
        public virtual void Hide()
        {
            m_TopElement.style.display = DisplayStyle.None;
        }

        // Unregisters any callbacks or event handlers. Override to customize.
        public virtual void Dispose()
        {

        }
    }
}