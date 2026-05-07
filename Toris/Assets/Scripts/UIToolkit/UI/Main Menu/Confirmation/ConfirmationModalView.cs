using System;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.UIToolkit
{
    public class ConfirmationModalView : GameView
    {
        public override ScreenType ID => ScreenType.ConfirmationModal;

        private Label _titleLabel;
        private Label _messageLabel;
        private Button _confirmButton;
        private Button _cancelButton;

        public event Action OnConfirmClicked;
        public event Action OnCancelClicked;

        public ConfirmationModalView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents) { }

        public override void Initialize()
        {
            base.Initialize();

            _titleLabel = Root.Q<Label>("Label_Title");
            _messageLabel = Root.Q<Label>("Label_Message");
            _confirmButton = Root.Q<Button>("Btn_Confirm");
            _cancelButton = Root.Q<Button>("Btn_Cancel");

            if (_confirmButton != null) _confirmButton.clicked += HandleConfirmClicked;
            if (_cancelButton != null) _cancelButton.clicked += HandleCancelClicked;
        }

        public override void Setup(object payload)
        {
            base.Setup(payload);

            if (payload is ConfirmationPayload data)
            {
                if (_titleLabel != null) _titleLabel.text = data.Title;
                if (_messageLabel != null) _messageLabel.text = data.Message;
            }
        }

        private void HandleConfirmClicked() => OnConfirmClicked?.Invoke();
        private void HandleCancelClicked() => OnCancelClicked?.Invoke();

        public override void Dispose()
        {
            if (_confirmButton != null) _confirmButton.clicked -= HandleConfirmClicked;
            if (_cancelButton != null) _cancelButton.clicked -= HandleCancelClicked;
            base.Dispose();
        }
    }
}
