using System;
using Deucarian.PointerCapture;

namespace Deucarian.ViewerNavigation
{
    internal interface IViewerPointerCaptureSession
    {
        event EventHandler<DeucarianPointerCaptureStateChangedEventArgs>
            StateChanged;

        DeucarianPointerCaptureState State { get; }

        bool RequestCapture(object owner);
        bool ReleaseCapture(object owner);
        void UpdateInputRearming(bool isInputNeutral, bool hasNewCaptureAction);
        void NotifyEscapePressed();
    }

    internal sealed class ViewerPointerCaptureSession :
        IViewerPointerCaptureSession
    {
        private readonly DeucarianPointerCaptureController controller;

        internal ViewerPointerCaptureSession(
            DeucarianPointerCaptureController captureController)
        {
            controller = captureController ??
                         throw new ArgumentNullException(
                             nameof(captureController));
        }

        public event EventHandler<DeucarianPointerCaptureStateChangedEventArgs>
            StateChanged
        {
            add => controller.StateChanged += value;
            remove => controller.StateChanged -= value;
        }

        public DeucarianPointerCaptureState State => controller.State;

        public bool RequestCapture(object owner) =>
            controller.RequestCapture(owner);

        public bool ReleaseCapture(object owner) =>
            controller.ReleaseCapture(owner);

        public void UpdateInputRearming(
            bool isInputNeutral,
            bool hasNewCaptureAction)
        {
            controller.UpdateInputRearming(
                isInputNeutral,
                hasNewCaptureAction);
        }

        public void NotifyEscapePressed()
        {
            controller.NotifyEscapePressed();
        }
    }
}
