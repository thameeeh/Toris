using UnityEngine;
using UnityEngine.InputSystem;

namespace OutlandHaven.UIToolkit
{
    public class SkillMenuController : MonoBehaviour
    {
        [SerializeField] private UIEventsSO _uiEvents;

        private InputSystem_Actions _inputActions;

        private void OnEnable()
        {
            _inputActions = new InputSystem_Actions();
            // Settings rebinding hook: this standalone toggle input must honor saved overrides.
            InputBindingSettings.ApplyTo(_inputActions);
            ControllerFeatureGate.ApplyAvailability(_inputActions);
            _inputActions.UI.Enable();
            _inputActions.UI.ToggleSkills.performed += OnToggleSkills;
            InputBindingSettings.OnBindingsChanged += HandleInputBindingsChanged;
        }

        private void OnDisable()
        {
            InputBindingSettings.OnBindingsChanged -= HandleInputBindingsChanged;

            if (_inputActions != null)
            {
                _inputActions.UI.ToggleSkills.performed -= OnToggleSkills;
                _inputActions.UI.Disable();
            }
        }

        private void HandleInputBindingsChanged()
        {
            InputBindingSettings.ApplyTo(_inputActions);
            ControllerFeatureGate.ApplyAvailability(_inputActions);
        }

        private void OnToggleSkills(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                _uiEvents.OnRequestOpen?.Invoke(ScreenType.Skills, null);
            }
        }

        private void OnValidate()
        {
            if (_uiEvents == null)
            {
                Debug.LogError($"<color=red>[UIEventsSO]</color> is missing on GameObject: <b>{name}</b>", this);
            }
        }
    }
}
