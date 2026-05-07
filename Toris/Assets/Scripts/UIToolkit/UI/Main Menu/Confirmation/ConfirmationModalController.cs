using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.UIToolkit
{
    public class ConfirmationModalController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset _modalTemplate;
        [SerializeField] private UIEventsSO _uiEvents;

        private ConfirmationModalView _view;
        private MainMenuUIManager _uiManager;
        private ConfirmationPayload _activePayload;

        private void Awake()
        {
            _uiManager = FindFirstObjectByType<MainMenuUIManager>();
        }

        private void Start()
        {
            if (_modalTemplate == null || _uiManager == null) return;

            TemplateContainer modalInstance = _modalTemplate.Instantiate();
            
            // Ensure the modal overlays everything without affecting layout
            modalInstance.style.position = Position.Absolute;
            modalInstance.style.top = 0;
            modalInstance.style.bottom = 0;
            modalInstance.style.left = 0;
            modalInstance.style.right = 0;

            _view = new ConfirmationModalView(modalInstance, _uiEvents);
            _view.Initialize();

            _view.OnConfirmClicked += HandleConfirm;
            _view.OnCancelClicked += HandleCancel;

            _uiManager.RegisterView(_view);

            // Listen for open requests specifically to capture the payload
            _uiEvents.OnRequestOpen += HandleOpenRequest;
        }

        private void HandleOpenRequest(ScreenType type, object payload)
        {
            if (type == ScreenType.ConfirmationModal && payload is ConfirmationPayload data)
            {
                _activePayload = data;
            }
        }

        private void HandleConfirm()
        {
            _activePayload?.OnConfirm?.Invoke();
            _uiEvents.OnRequestClose?.Invoke(ScreenType.ConfirmationModal);
            _activePayload = null;
        }

        private void HandleCancel()
        {
            _activePayload?.OnCancel?.Invoke();
            _uiEvents.OnRequestClose?.Invoke(ScreenType.ConfirmationModal);
            _activePayload = null;
        }

        private void OnDestroy()
        {
            if (_uiEvents != null) _uiEvents.OnRequestOpen -= HandleOpenRequest;
            
            if (_view != null)
            {
                _view.OnConfirmClicked -= HandleConfirm;
                _view.OnCancelClicked -= HandleCancel;
                _view.Dispose();
            }
        }
    }
}
