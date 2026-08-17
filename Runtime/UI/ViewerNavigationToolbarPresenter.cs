using System;
using Deucarian.Theming;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.UI
{
    /// <summary>
    /// Package-owned reference navigation toolbar. Consumers compose this through
    /// <see cref="ViewerNavigationInstaller"/> and never need to load UI assets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class ViewerNavigationToolbarPresenter : MonoBehaviour
    {
        public const string RootName = "DeucarianViewerNavigationRoot";
        public const string ToolbarName = "DeucarianViewerNavigationToolbar";
        public const string OrbitButtonName = "OrbitButton";
        public const string FlyButtonName = "FlyButton";
        public const string HomeButtonName = "HomeButton";
        public const string TopDownButtonName = "TopDownButton";
        public const string OrbitIconName = "OrbitIcon";
        public const string FlyIconName = "FlyIcon";
        public const string HomeIconName = "HomeIcon";
        public const string TopDownIconName = "TopDownIcon";
        public const string PerspectiveIconName = "PerspectiveIcon";

        public const string OrbitTooltip =
            "Orbit \u00b7 Left drag to rotate \u00b7 Right drag to pan \u00b7 Scroll to zoom";
        public const string FlyTooltip =
            "Fly \u00b7 WASD to move \u00b7 Right drag to look";
        public const string HomeTooltip =
            "Recenter \u00b7 Fit the model and reset the camera";
        public const string TopDownTooltip =
            "Top view \u00b7 Switch to an orthographic plan view";
        public const string PerspectiveTooltip =
            "Perspective view \u00b7 Return to an orbitable 3D view";

        [SerializeField] private UIDocument document;
        [SerializeField] private PanelSettings panelSettings;

        private readonly ViewerNavigationToolbarVisualState visualState =
            new ViewerNavigationToolbarVisualState();
        private ViewerNavigationController controller;
        private ViewerNavigationSettings settings;
        private DeucarianThemeProvider themeProvider;
        private IDisposable inputRegistration;
        private bool controllerSubscribed;

        public UIDocument Document => document;
        public VisualElement Root => root;
        public VisualElement ToolbarElement => toolbar;
        public ViewerViewCubeElement ViewCube => viewCube;

        public void Initialize(
            ViewerNavigationController navigationController,
            ViewerNavigationSettings configuration = null)
        {
            UnsubscribeController();
            inputRegistration?.Dispose();
            inputRegistration = null;
            controller = navigationController;
            settings = configuration;
            LoadPresentationAssets();
            EnsureDocument();
            BuildUi();
            BindThemeProvider();
            SubscribeController();
            inputRegistration = controller?.InteractionGate?.RegisterUiRoot(root);
            Refresh(controller != null ? controller.Snapshot : default);
        }

        /// <summary>
        /// Re-resolves package theme and state after a host changes shared UI state.
        /// </summary>
        public void RefreshVisualState()
        {
            visualState.InvalidatePresentation();
            ApplyTheme();
            ScheduleVisualStateRefresh();
        }

        private void LateUpdate()
        {
            if (viewCube == null || controller?.Camera == null)
            {
                return;
            }

            viewCube.UpdateOrientation(controller.Camera.transform.rotation);
            if (controller.IsTopDown)
            {
                viewCube.SetActiveFace(ViewerViewFace.Top);
            }
        }

        private void OnEnable()
        {
            SubscribeController();
            BindThemeProvider();
            ScheduleVisualStateRefresh();
        }

        private void OnDisable()
        {
            UnsubscribeController();
            UnbindThemeProvider();
        }

        private void OnDestroy()
        {
            UnsubscribeController();
            UnbindThemeProvider();
            DisposeUi();
            inputRegistration?.Dispose();
            inputRegistration = null;
            ReleaseRuntimePanelSettings();
        }

        private void Refresh(ViewerNavigationSnapshot snapshot)
        {
            visualState.Apply(
                snapshot,
                ShouldAnimateToolbarMotion());
            if (topButton != null)
            {
                topButton.tooltip = snapshot.IsTopDown
                    ? PerspectiveTooltip
                    : TopDownTooltip;
            }

            if (snapshot.IsTopDown)
            {
                viewCube?.SetActiveFace(ViewerViewFace.Top);
            }
        }

        private void BindThemeProvider()
        {
            DeucarianThemeProvider provider =
                DeucarianThemeRuntimeResolver.FindProvider(this);
            if (themeProvider == provider)
            {
                return;
            }

            UnbindThemeProvider();
            themeProvider = provider;
            if (themeProvider != null)
            {
                DeucarianThemeRuntimeResolver.EnsureProviderHasTheme(
                    themeProvider,
                    this);
                themeProvider.ThemeChanged += OnThemeChanged;
                themeProvider.StyleChanged += OnStyleChanged;
            }

            ApplyTheme();
        }

        private void UnbindThemeProvider()
        {
            if (themeProvider == null)
            {
                return;
            }

            themeProvider.ThemeChanged -= OnThemeChanged;
            themeProvider.StyleChanged -= OnStyleChanged;
            themeProvider = null;
        }

        private void OnThemeChanged(DeucarianTheme _) => RefreshVisualState();
        private void OnStyleChanged(DeucarianThemeStyle _) => RefreshVisualState();

        private bool ShouldAnimateToolbarMotion()
        {
            return controller?.MotionProfile != null
                ? controller.MotionProfile.AnimateTransitions
                : ViewerNavigationMotionPreferences.ShouldAnimate;
        }

        private void SubscribeController()
        {
            if (controllerSubscribed || controller == null || !isActiveAndEnabled)
            {
                return;
            }

            controller.StateChanged += Refresh;
            controllerSubscribed = true;
        }

        private void UnsubscribeController()
        {
            if (!controllerSubscribed || controller == null)
            {
                return;
            }

            controller.StateChanged -= Refresh;
            controllerSubscribed = false;
        }

        private void OnOrbitClicked() =>
            controller?.SetNavigationMode(ViewerNavigationMode.Orbit);
        private void OnFlyClicked() =>
            controller?.SetNavigationMode(ViewerNavigationMode.Fly);
        private void OnHomeClicked() => controller?.ReturnToOrigin();
        private void OnTopClicked() => controller?.ToggleTopDown();
        private void OnFaceSelected(ViewerViewFace face) =>
            controller?.NavigateToFace(face);
    }
}
