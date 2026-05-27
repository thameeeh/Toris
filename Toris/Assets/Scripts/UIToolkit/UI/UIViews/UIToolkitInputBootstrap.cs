using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace OutlandHaven.UIToolkit
{
    public static class UIToolkitInputBootstrap
    {
        private static InputSystem_Actions uiActions;

        public static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            }

            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            if (!eventSystem.TryGetComponent(out InputSystemUIInputModule inputModule))
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            AssignActionsIfMissing(inputModule);
            // Use the project-configured module so integrations do not create an unfiltered controller-enabled one.
            ControllerFeatureGate.ApplyAvailability(inputModule.actionsAsset);
        }

        private static void AssignActionsIfMissing(InputSystemUIInputModule inputModule)
        {
            if (inputModule == null || inputModule.actionsAsset != null)
            {
                return;
            }

            uiActions ??= new InputSystem_Actions();
            InputBindingSettings.ApplyTo(uiActions);
            uiActions.UI.Enable();

            inputModule.actionsAsset = uiActions.asset;
            inputModule.point = InputActionReference.Create(uiActions.UI.Point);
            inputModule.leftClick = InputActionReference.Create(uiActions.UI.Click);
            inputModule.rightClick = InputActionReference.Create(uiActions.UI.RightClick);
            inputModule.middleClick = InputActionReference.Create(uiActions.UI.MiddleClick);
            inputModule.scrollWheel = InputActionReference.Create(uiActions.UI.ScrollWheel);
            inputModule.move = InputActionReference.Create(uiActions.UI.Navigate);
            inputModule.submit = InputActionReference.Create(uiActions.UI.Submit);
            inputModule.cancel = InputActionReference.Create(uiActions.UI.Cancel);
            inputModule.trackedDevicePosition = InputActionReference.Create(uiActions.UI.TrackedDevicePosition);
            inputModule.trackedDeviceOrientation = InputActionReference.Create(uiActions.UI.TrackedDeviceOrientation);
        }
    }
}
