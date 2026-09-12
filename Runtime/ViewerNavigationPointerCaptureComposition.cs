using System;
using Deucarian.PointerCapture;

namespace Deucarian.ViewerNavigation
{
    public static class ViewerNavigationPointerCaptureComposition
    {
        /// <summary>Lends a capture session before initialization; navigation never owns the shared service.</summary>
        public static void ConfigurePointerCapture(this ViewerNavigationController viewer, IPointerCaptureSession session)
        {
            if (viewer == null) throw new ArgumentNullException(nameof(viewer));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var gate = viewer.GetComponent<ViewerNavigationInteractionGate>();
            if (gate == null) gate = viewer.gameObject.AddComponent<ViewerNavigationInteractionGate>();
            gate.ConfigureCapture(session);
        }
    }
}
