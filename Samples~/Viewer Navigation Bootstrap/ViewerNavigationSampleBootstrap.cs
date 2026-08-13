using UnityEngine;

namespace Deucarian.ViewerNavigation.Samples
{
    public sealed class ViewerNavigationSampleBootstrap : MonoBehaviour
    {
        [SerializeField] private Camera viewerCamera;
        [SerializeField] private GameObject referenceModel;
        [SerializeField] private ViewerNavigationSettings settings;

        private ViewerNavigationInstaller installer;

        private void Start()
        {
            if (viewerCamera == null)
            {
                viewerCamera = GetComponentInChildren<Camera>(true);
            }

            if (viewerCamera == null)
            {
                return;
            }

            installer = ViewerNavigationInstaller.Create(
                transform,
                viewerCamera,
                settings);
            installer.BeginReferenceLoad();
            if (referenceModel != null)
            {
                installer.RegisterReference(
                    referenceModel,
                    frame: true,
                    captureOrigin: true);
            }
        }
    }
}
