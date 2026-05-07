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
        public Action OnConfirm;
        public Action OnCancel;

        public ConfirmationPayload(string title, string message, Action onConfirm, Action onCancel = null)
        {
            Title = title;
            Message = message;
            OnConfirm = onConfirm;
            OnCancel = onCancel;
        }
    }
}
