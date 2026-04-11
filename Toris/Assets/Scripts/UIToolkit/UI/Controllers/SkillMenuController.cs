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
            _inputActions.UI.Enable();
            _inputActions.UI.ToggleSkills.performed += OnToggleSkills;
        }

        private void OnDisable()
        {
            if (_inputActions != null)
            {
                _inputActions.UI.ToggleSkills.performed -= OnToggleSkills;
                _inputActions.UI.Disable();
            }
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
