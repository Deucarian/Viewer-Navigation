using Deucarian.CameraNavigation;
using Deucarian.CameraNavigation.InputSystemIntegration;
using Deucarian.Theming;
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
        public DeucarianThemeProvider ThemeProvider { get; private set; }

        public static ViewerNavigationInstaller Create(
            Transform parent,
            Camera camera,
            ViewerNavigationSettings configuration = null,
            IViewerNavigationInputBlocker inputBlocker = null,
            IDeucarianFramingBoundsStrategy<GameObject>
                referenceBoundsStrategy = null,
            IViewerNavigationAnimationPolicy animationPolicy = null)
        {
            ViewerNavigationInstaller installer = FindOrCreate(parent);

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
            IViewerNavigationAnimationPolicy animationPolicy = null,
            DeucarianThemeProvider themeProvider = null)
        {
            ViewerNavigationReferenceCompositionProfile referencePreset =
                ViewerNavigationReferenceComposition.Resolve(animationPolicy);
            return referencePreset.Compose(parent, camera, themeProvider);
        }

        internal static ViewerNavigationInstaller CreateWithReferenceComposition(
            Transform parent,
            Camera camera,
            ViewerNavigationReferenceCompositionProfile composition,
            DeucarianThemeProvider themeProvider = null)
        {
            ViewerNavigationInstaller installer = FindOrCreate(parent);
            installer.InitializeReference(
                camera,
                composition,
                themeProvider);
            return installer;
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
            ViewerNavigationInstaller installer = FindOrCreate(parent);

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

        private void InitializeReference(
            Camera camera,
            ViewerNavigationReferenceCompositionProfile composition,
            DeucarianThemeProvider themeProvider)
        {
            settings = composition.Preset;
            navigationCamera = camera;
            EnsureReferenceThemeProvider(
                composition.ThemeProfile,
                composition.ThemeMode,
                themeProvider);
            EnsureComponents();
            Controller.Initialize(
                camera,
                settings,
                composition.InputBlocker,
                composition.BoundsStrategy,
                composition.AnimationPolicy);
            Toolbar.Initialize(Controller, settings);
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

        private void EnsureReferenceThemeProvider(
            DeucarianViewerReferenceThemeProfile themeProfile,
            DeucarianThemeMode themeMode,
            DeucarianThemeProvider authoritativeProvider)
        {
            ThemeProvider = authoritativeProvider != null
                ? authoritativeProvider
                : GetComponent<DeucarianThemeProvider>();
            if (ThemeProvider == null)
            {
                ThemeProvider = gameObject.AddComponent<DeucarianThemeProvider>();
            }

            // An injected provider is authoritative for the whole viewer. Keep
            // a deliberate family/mode/style intact; initialize only an empty
            // provider (including the navigation-owned fallback).
            if (ThemeProvider.CurrentTheme == null)
            {
                ThemeProvider.SetThemeFamily(
                    themeProfile.ThemeFamily,
                    themeMode);
            }
        }

        private static ViewerNavigationInstaller FindOrCreate(Transform parent)
        {
            ViewerNavigationInstaller installer = FindUnder(parent);
            if (installer != null)
            {
                return installer;
            }

            GameObject gameObject = new GameObject(GameObjectName);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject.AddComponent<ViewerNavigationInstaller>();
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
