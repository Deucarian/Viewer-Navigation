using UnityEngine;
using Deucarian.PointerCapture;
namespace Deucarian.ViewerNavigation.Samples.DefinitionWorkflow
{
    [DefaultExecutionOrder(-2000)]
    public sealed class SampleViewerSetup : MonoBehaviour
    {
        [SerializeField] private ViewerNavigationController controller;
        [SerializeField] private Camera cameraToControl;
        private PointerCaptureScope capture;
        private void Awake()
        {
            capture = new PointerCaptureScope();
            controller.ConfigurePointerCapture(capture.OpenSession());
            controller.Initialize(cameraToControl, (ViewerNavigationSettings)null);
        }
        private void OnDestroy() => capture?.Dispose();
    }
}
