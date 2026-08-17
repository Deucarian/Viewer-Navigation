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
            IViewerNavigationInputBlocker inputBlocker = null,
            IDeucarianFramingBoundsStrategy<GameObject>
                referenceBoundsStrategy = null,
            IViewerNavigationAnimationPolicy animationPolicy = null)
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
                configuration,
                inputBlocker,
                referenceBoundsStrategy,
                animationPolicy);
            return installer;
        }

        public static ViewerNavigationInstaller CreateWithReferencePreset(
            Transform parent,
            Camera camera,
            IViewerNavigationAnimationPolicy animationPolicy = null)
        {
            ViewerNavigationReferenceCompositionProfile referencePreset =
                ViewerNavigationReferenceComposition.Resolve(animationPolicy);
            return Create(
                parent,
                camera,
                referencePreset.Preset,
                referencePreset.InputBlocker,
                referencePreset.BoundsStrategy,
                referencePreset.AnimationPolicy);
        }

        public static ViewerNavigationInstaller Create(
            Transform parent,
            Camera camera,
            DeucarianCameraNavigationControls controls,
            DeucarianInputSystemNavigationSettings inputSettings,
            IViewerNavigationMotionProfile motionProfile,
            IDeucarianCameraFramingSettings framingSettings = null,
            IViewerNavigationInputBlocker inputBlocker = null,
            IDeucarianFramingBoundsStrategy<GameObject>
                referenceBoundsStrategy = null)
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
                inputBlocker,
                referenceBoundsStrategy);
            return installer;
        }

        public void Initialize(
            Camera camera,
            ViewerNavigationSettings configuration,
            IViewerNavigationInputBlocker inputBlocker = null,
            IDeucarianFramingBoundsStrategy<GameObject>
                referenceBoundsStrategy = null,
            IViewerNavigationAnimationPolicy animationPolicy = null)
        {
            settings = configuration != null
                ? configuration
                : ViewerNavigationSettings.LoadReferencePreset();
            navigationCamera = camera;
            EnsureComponents();
            Controller.Initialize(
                camera,
                settings,
                inputBlocker,
                referenceBoundsStrategy,
                animationPolicy);
            Toolbar.Initialize(Controller, settings);
        }

        public void Initialize(
            Camera camera,
            DeucarianCameraNavigationControls controls,
            DeucarianInputSystemNavigationSettings inputSettings,
            IViewerNavigationMotionProfile motionProfile,
            IDeucarianCameraFramingSettings framingSettings = null,
            IViewerNavigationInputBlocker inputBlocker = null,
            IDeucarianFramingBoundsStrategy<GameObject>
                referenceBoundsStrategy = null)
        {
            settings = null;
            navigationCamera = camera;
            EnsureComponents();
            Controller.Initialize(
                camera,
                ResolveControls(controls),
                inputSettings,
                motionProfile,
                ResolveFramingSettings(framingSettings),
                inputBlocker,
                referenceBoundsStrategy);
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

        private static DeucarianCameraNavigationControls ResolveControls(
            DeucarianCameraNavigationControls configured)
        {
            return configured != null
                ? configured
                : Resources.Load<DeucarianCameraNavigationControls>(
                    DeucarianCameraNavigationControls.CanonicalResourcesPath);
        }

        private static IDeucarianCameraFramingSettings ResolveFramingSettings(
            IDeucarianCameraFramingSettings configured)
        {
            return configured ?? Resources.Load<DeucarianCameraFramingSettings>(
                DeucarianCameraFramingSettings.CanonicalResourcesPath);
        }
    }
}
