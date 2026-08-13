using Deucarian.CameraNavigation;
using Deucarian.CameraNavigation.InputSystemIntegration;
using Deucarian.ViewerNavigation.UI;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    [DisallowMultipleComponent]
    public sealed class ViewerNavigationInstaller : MonoBehaviour
    {
        private const string GameObjectName = "DeucarianViewerNavigation";

        [SerializeField] private ViewerNavigationSettings settings;
        [SerializeField] private Camera navigationCamera;

        public ViewerNavigationController Controller { get; private set; }
        public ViewerNavigationToolbarPresenter Toolbar { get; private set; }

        public static ViewerNavigationInstaller Create(
            Transform parent,
            Camera camera,
            ViewerNavigationSettings configuration = null,
            IViewerNavigationInputBlocker inputBlocker = null)
        {
            ViewerNavigationInstaller installer = FindUnder(parent);
            if (installer == null)
            {
                GameObject gameObject = new GameObject(GameObjectName);
                if (parent != null)
                {
                    gameObject.transform.SetParent(parent, false);
                }

                installer = gameObject.AddComponent<ViewerNavigationInstaller>();
            }

            installer.Initialize(camera, configuration, inputBlocker);
            return installer;
        }

        public static ViewerNavigationInstaller Create(
            Transform parent,
            Camera camera,
            DeucarianCameraNavigationControls controls,
            DeucarianInputSystemNavigationSettings inputSettings,
            IViewerNavigationMotionProfile motionProfile,
            IDeucarianCameraFramingSettings framingSettings = null,
            IViewerNavigationInputBlocker inputBlocker = null)
        {
            ViewerNavigationInstaller installer = FindUnder(parent);
            if (installer == null)
            {
                GameObject gameObject = new GameObject(GameObjectName);
                if (parent != null)
                {
                    gameObject.transform.SetParent(parent, false);
                }

                installer = gameObject.AddComponent<ViewerNavigationInstaller>();
            }

            installer.Initialize(
                camera,
                controls,
                inputSettings,
                motionProfile,
                framingSettings,
                inputBlocker);
            return installer;
        }

        public void Initialize(
            Camera camera,
            ViewerNavigationSettings configuration,
            IViewerNavigationInputBlocker inputBlocker = null)
        {
            settings = configuration;
            navigationCamera = camera;
            EnsureComponents();
            Controller.Initialize(camera, configuration, inputBlocker);
            Toolbar.Initialize(Controller, configuration);
        }

        public void Initialize(
            Camera camera,
            DeucarianCameraNavigationControls controls,
            DeucarianInputSystemNavigationSettings inputSettings,
            IViewerNavigationMotionProfile motionProfile,
            IDeucarianCameraFramingSettings framingSettings = null,
            IViewerNavigationInputBlocker inputBlocker = null)
        {
            settings = null;
            navigationCamera = camera;
            EnsureComponents();
            Controller.Initialize(
                camera,
                controls,
                inputSettings,
                motionProfile,
                framingSettings,
                inputBlocker);
            Toolbar.Initialize(Controller, settings);
        }

        public void BeginReferenceLoad()
        {
            Controller?.BeginReferenceLoad();
        }

        public bool RegisterReference(
            GameObject referenceRoot,
            bool frame = true,
            bool captureOrigin = true)
        {
            return Controller != null &&
                   Controller.RegisterReference(referenceRoot, frame, captureOrigin);
        }

        private void EnsureComponents()
        {
            Controller = GetComponent<ViewerNavigationController>();
            if (Controller == null)
            {
                Controller = gameObject.AddComponent<ViewerNavigationController>();
            }

            Toolbar = GetComponent<ViewerNavigationToolbarPresenter>();
            if (Toolbar == null)
            {
                Toolbar = gameObject.AddComponent<ViewerNavigationToolbarPresenter>();
            }
        }

        private static ViewerNavigationInstaller FindUnder(Transform parent)
        {
            return parent != null
                ? parent.GetComponentInChildren<ViewerNavigationInstaller>(true)
                : null;
        }
    }
}
