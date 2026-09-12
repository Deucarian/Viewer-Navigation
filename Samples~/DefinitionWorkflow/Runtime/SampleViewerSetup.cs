using UnityEngine;
namespace Deucarian.ViewerNavigation.Samples.DefinitionWorkflow
{
    [DefaultExecutionOrder(-2000)]
    public sealed class SampleViewerSetup : MonoBehaviour
    {
        [SerializeField] private ViewerNavigationController controller;
        [SerializeField] private Camera cameraToControl;
        private void Awake() => controller.Initialize(cameraToControl, (ViewerNavigationSettings)null);
    }
}
