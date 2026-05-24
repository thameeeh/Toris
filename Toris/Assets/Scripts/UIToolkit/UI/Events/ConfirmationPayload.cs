using System;

namespace OutlandHaven.UIToolkit
{
    /// <summary>
    /// Data Transfer Object for configuring the Confirmation Modal.
    /// </summary>
    public class ConfirmationPayload
    {
        public string Title;
        public string Message;
        public string ConfirmText;
        public string CancelText;
        public Action OnConfirm;
        public Action OnCancel;

        public ConfirmationPayload(
            string title,
            string message,
            Action onConfirm,
            Action onCancel = null,
            string confirmText = "Confirm",
            string cancelText = "Cancel")
        {
            Title = title;
            Message = message;
            ConfirmText = confirmText;
            CancelText = cancelText;
            OnConfirm = onConfirm;
            OnCancel = onCancel;
        }
    }
}
