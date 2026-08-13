using Deucarian.Diagnostics;
using Deucarian.PointerCapture;

namespace Deucarian.ViewerNavigation
{
    internal sealed class ViewerNavigationDiagnosticProvider : IDiagnosticProvider
    {
        private readonly ViewerNavigationController controller;

        public ViewerNavigationDiagnosticProvider(ViewerNavigationController controller)
        {
            this.controller = controller;
        }

        public string ProviderId => controller != null
            ? "viewer-navigation." + controller.GetInstanceID()
            : "viewer-navigation";

        public string DisplayName => "Viewer Navigation";

        public void Collect(DiagnosticReportBuilder builder)
        {
            DiagnosticSection section = builder.AddSection(
                ProviderId,
                DisplayName);
            if (controller == null)
            {
                section.AddItem(
                    "controller",
                    "Controller",
                    "Missing",
                    DiagnosticSeverity.Error);
                return;
            }

            ViewerNavigationSnapshot snapshot = controller.Snapshot;
            section
                .AddItem("mode", "Mode", snapshot.Mode.ToString())
                .AddItem("top_down", "Top Down", snapshot.IsTopDown.ToString())
                .AddItem(
                    "reference_bounds",
                    "Reference Bounds",
                    snapshot.HasReferenceBounds.ToString(),
                    snapshot.HasReferenceBounds
                        ? DiagnosticSeverity.Info
                        : DiagnosticSeverity.Warning)
                .AddItem(
                    "origin",
                    "Origin Captured",
                    snapshot.HasOrigin.ToString(),
                    snapshot.HasOrigin
                        ? DiagnosticSeverity.Info
                        : DiagnosticSeverity.Warning)
                .AddItem(
                    "transition",
                    "Transition",
                    snapshot.IsTransitioning
                        ? snapshot.TransitionKind.ToString()
                        : "Idle")
                .AddItem("revision", "State Revision", snapshot.Revision.ToString());

            DeucarianPointerCaptureController capture =
                controller.InteractionGate != null
                    ? controller.InteractionGate.PointerCapture
                    : null;
            if (capture != null)
            {
                DeucarianPointerCaptureDiagnosticsSnapshot pointer =
                    capture.GetDiagnosticsSnapshot();
                section.AddItem(
                    "pointer_capture",
                    "Pointer Capture",
                    pointer.State.ToString());
            }
        }
    }
}
