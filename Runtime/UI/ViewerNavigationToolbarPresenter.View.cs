using System;
using Deucarian.Common;
using Deucarian.Theming;
using Deucarian.Theming.UIToolkit;
using Deucarian.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.UI
{
    public sealed partial class ViewerNavigationToolbarPresenter
    {
        internal const string ToolbarAssetResourcesPath =
            "Deucarian/ViewerNavigationToolbar";

        private PanelSettings runtimePanelSettings;
        private VisualTreeAsset toolbarAsset;
        private StyleSheet toolbarStyleSheet;
        private VisualElement root;
        private VisualElement toolbar;
        private Button orbitButton;
        private Button flyButton;
        private Button homeButton;
        private Button topButton;
        private VisualElement orbitIcon;
        private VisualElement flyIcon;
        private VisualElement homeIcon;
        private VisualElement topDownIcon;
        private VisualElement perspectiveIcon;
        private ViewerViewCubeElement viewCube;
        private DeucarianRuntimeTooltipPresenter runtimeTooltip;
        private IDisposable movementKeyGuard;
        private VisualElement geometryRefreshRoot;
        private bool visualStateRefreshScheduled;

        internal bool IsToolbarExpected =>
            settings == null || settings.ShowToolbar;

        internal bool HasLoadedPresentationAssets =>
            toolbarAsset != null &&
            toolbarStyleSheet != null &&
            panelSettings != null;

        internal bool HasAttachedPresentationAssets =>
            HasLoadedPresentationAssets &&
            document != null &&
            document.visualTreeAsset == toolbarAsset &&
            document.panelSettings == panelSettings &&
            root != null &&
            root.panel != null &&
            document.rootVisualElement.styleSheets.Contains(
                toolbarStyleSheet);

        internal bool HasRequiredToolbarControls =>
            toolbar != null &&
            orbitButton != null &&
            flyButton != null &&
            homeButton != null &&
            topButton != null &&
            orbitIcon != null &&
            flyIcon != null &&
            homeIcon != null &&
            topDownIcon != null &&
            perspectiveIcon != null;

        private void LoadPresentationAssets()
        {
            toolbarAsset = Resources.Load<VisualTreeAsset>(
                ToolbarAssetResourcesPath);
            toolbarStyleSheet = Resources.Load<StyleSheet>(
                ToolbarAssetResourcesPath);
            if (panelSettings == null)
            {
                panelSettings = DeucarianUIRuntimeAssets
                    .LoadRuntimePanelSettings();
            }

            if (toolbarAsset == null || toolbarStyleSheet == null)
            {
                ViewerNavigationLog.UI.Warning(
                    "Canonical navigation toolbar assets were not loaded; " +
                    "the package fallback view will be used.");
            }
        }

        private void EnsureDocument()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }

            if (document == null)
            {
                document = gameObject.AddComponent<UIDocument>();
            }

            if (panelSettings != null)
            {
                document.panelSettings = panelSettings;
            }
            else if (document.panelSettings == null)
            {
                runtimePanelSettings =
                    ScriptableObject.CreateInstance<PanelSettings>();
                runtimePanelSettings.name =
                    "Runtime Deucarian Viewer Navigation Panel Settings";
                document.panelSettings = runtimePanelSettings;
            }

            if (toolbarAsset != null)
            {
                document.visualTreeAsset = toolbarAsset;
            }

            document.sortingOrder =
                DeucarianUIDepth.PrimaryControls;
        }

        private void BuildUi()
        {
            DisposeUi();
            VisualElement documentRoot = document.rootVisualElement;
            documentRoot.Clear();
            documentRoot.pickingMode = PickingMode.Ignore;
            if (toolbarAsset != null)
            {
                toolbarAsset.CloneTree(documentRoot);
            }
            else
            {
                ViewerNavigationToolbarViewFactory.BuildFallback(documentRoot);
            }

            if (toolbarStyleSheet != null &&
                !documentRoot.styleSheets.Contains(toolbarStyleSheet))
            {
                documentRoot.styleSheets.Add(toolbarStyleSheet);
            }

            ResolveElements(documentRoot);
            if (root == null)
            {
                documentRoot.Clear();
                ViewerNavigationToolbarViewFactory.BuildFallback(documentRoot);
                ResolveElements(documentRoot);
            }

            ViewerNavigationToolbarViewFactory.ConfigureView(
                documentRoot,
                root,
                toolbar,
                ToolbarButtons(),
                ToolbarIcons());
            Func<bool> movementKeyState = controller?.InteractionGate != null
                ? controller.InteractionGate.IsNavigationMovementKeyActive
                : null;
            movementKeyGuard = ViewerNavigationMovementKeyGuard.Bind(
                documentRoot,
                movementKeyState);

            if (settings != null && !settings.ShowToolbar)
            {
                RemoveToolbar();
            }
            else
            {
                BindToolbar();
            }

            if (settings != null && settings.ShowViewCube && root != null)
            {
                viewCube = new ViewerViewCubeElement();
                viewCube.style.right = 20f;
                viewCube.style.top = 20f;
                viewCube.FaceSelected += OnFaceSelected;
                root.Add(viewCube);
                EnsureRuntimeTooltip();
                runtimeTooltip.BindTree(viewCube);
            }

            RegisterGeometryRefresh(documentRoot);
            ApplyTheme();
        }

        private void ResolveElements(VisualElement documentRoot)
        {
            root = documentRoot.Q<VisualElement>(RootName);
            toolbar = documentRoot.Q<VisualElement>(ToolbarName);
            orbitButton = documentRoot.Q<Button>(OrbitButtonName);
            flyButton = documentRoot.Q<Button>(FlyButtonName);
            homeButton = documentRoot.Q<Button>(HomeButtonName);
            topButton = documentRoot.Q<Button>(TopDownButtonName);
            orbitIcon = documentRoot.Q<VisualElement>(OrbitIconName);
            flyIcon = documentRoot.Q<VisualElement>(FlyIconName);
            homeIcon = documentRoot.Q<VisualElement>(HomeIconName);
            topDownIcon = documentRoot.Q<VisualElement>(TopDownIconName);
            perspectiveIcon =
                documentRoot.Q<VisualElement>(PerspectiveIconName);
        }

        private Button[] ToolbarButtons() => new[]
        {
            orbitButton,
            flyButton,
            homeButton,
            topButton
        };

        private VisualElement[] ToolbarIcons() => new[]
        {
            orbitIcon,
            flyIcon,
            homeIcon,
            topDownIcon,
            perspectiveIcon
        };

        private void BindToolbar()
        {
            if (orbitButton == null || flyButton == null ||
                homeButton == null || topButton == null)
            {
                ViewerNavigationLog.UI.Warning(
                    "Canonical navigation toolbar is missing required controls.");
                return;
            }

            orbitButton.tooltip = OrbitTooltip;
            flyButton.tooltip = FlyTooltip;
            homeButton.tooltip = HomeTooltip;
            topButton.tooltip = TopDownTooltip;
            orbitButton.clicked += OnOrbitClicked;
            flyButton.clicked += OnFlyClicked;
            homeButton.clicked += OnHomeClicked;
            topButton.clicked += OnTopClicked;
            visualState.Initialize(
                this,
                orbitButton,
                flyButton,
                homeButton,
                topButton,
                orbitIcon,
                flyIcon,
                homeIcon,
                topDownIcon,
                perspectiveIcon,
                ShouldAnimateToolbarMotion);
            EnsureRuntimeTooltip();
            runtimeTooltip.Bind(orbitButton);
            runtimeTooltip.Bind(flyButton);
            runtimeTooltip.Bind(homeButton);
            runtimeTooltip.Bind(topButton);
        }

        private void EnsureRuntimeTooltip()
        {
            if (runtimeTooltip != null)
            {
                return;
            }

            runtimeTooltip =
                DeucarianRuntimeTooltipPresenter.CreateForDocument(
                    this,
                    document);
        }

        private void RemoveToolbar()
        {
            toolbar?.RemoveFromHierarchy();
            toolbar = null;
            orbitButton = null;
            flyButton = null;
            homeButton = null;
            topButton = null;
            orbitIcon = null;
            flyIcon = null;
            homeIcon = null;
            topDownIcon = null;
            perspectiveIcon = null;
        }

        private void ApplyTheme()
        {
            DeucarianTheme theme = themeProvider != null
                ? themeProvider.CurrentTheme
                : null;
            if (theme == null)
            {
                theme = DeucarianThemeRuntimeResolver.ResolveTheme(this);
            }

            if (theme == null)
            {
                theme = DeucarianViewerReferenceThemePreset
                    .Resolve()
                    .DefaultTheme;
            }
            DeucarianThemeStyle style = themeProvider != null
                ? themeProvider.CurrentStyle
                : null;
            if (style == null && theme != null)
            {
                style = theme.VisualStyle;
            }
            DeucarianUIToolkitThemeTypography.Apply(
                root,
                theme,
                this);
            ViewerNavigationToolbarChrome.Apply(
                root,
                toolbar,
                ToolbarButtons(),
                ToolbarIcons(),
                theme,
                style);
            visualState.ApplyTheme(theme, style);
            visualState.SetEnabled(controller != null);
            visualState.Apply(
                controller != null ? controller.Snapshot : default,
                false);
            runtimeTooltip?.ApplyTheme(theme, style);

            Color surface = ViewerNavigationToolbarTheme.Resolve(
                theme,
                DeucarianBuiltinColorRoleIds.SurfaceRaised,
                new Color(0.08f, 0.11f, 0.15f, 1f));
            Color text = ViewerNavigationToolbarTheme.Resolve(
                theme,
                DeucarianBuiltinColorRoleIds.TextPrimary,
                new Color(0.88f, 0.92f, 0.95f, 1f));
            Color accent = ViewerNavigationToolbarTheme.Resolve(
                theme,
                DeucarianBuiltinColorRoleIds.Accent,
                new Color(0.77f, 0.63f, 0.98f, 1f));
            viewCube?.ApplyPalette(surface, text, accent);
        }

        private void RegisterGeometryRefresh(VisualElement target)
        {
            UnregisterGeometryRefresh();
            geometryRefreshRoot = target;
            geometryRefreshRoot?.RegisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);
        }

        private void UnregisterGeometryRefresh()
        {
            geometryRefreshRoot?.UnregisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);
            geometryRefreshRoot = null;
            visualStateRefreshScheduled = false;
        }

        private void OnGeometryChanged(GeometryChangedEvent _) =>
            ScheduleVisualStateRefresh();

        private void ScheduleVisualStateRefresh()
        {
            VisualElement schedulingRoot = geometryRefreshRoot ?? root;
            if (visualStateRefreshScheduled || schedulingRoot == null)
            {
                return;
            }

            visualStateRefreshScheduled = true;
            schedulingRoot.schedule.Execute(() =>
            {
                visualStateRefreshScheduled = false;
                if (this != null && isActiveAndEnabled)
                {
                    visualState.InvalidatePresentation();
                    visualState.SetEnabled(controller != null);
                    visualState.Apply(
                        controller != null
                            ? controller.Snapshot
                            : default,
                        false);
                }
            }).ExecuteLater(0L);
        }

        private void DisposeUi()
        {
            UnregisterGeometryRefresh();
            movementKeyGuard?.Dispose();
            movementKeyGuard = null;
            visualState.Dispose();
            runtimeTooltip?.Dispose();
            runtimeTooltip = null;
            if (viewCube != null)
            {
                viewCube.FaceSelected -= OnFaceSelected;
                viewCube.RemoveFromHierarchy();
            }

            if (orbitButton != null) orbitButton.clicked -= OnOrbitClicked;
            if (flyButton != null) flyButton.clicked -= OnFlyClicked;
            if (homeButton != null) homeButton.clicked -= OnHomeClicked;
            if (topButton != null) topButton.clicked -= OnTopClicked;
            root = null;
            toolbar = null;
            orbitButton = null;
            flyButton = null;
            homeButton = null;
            topButton = null;
            orbitIcon = null;
            flyIcon = null;
            homeIcon = null;
            topDownIcon = null;
            perspectiveIcon = null;
            viewCube = null;
        }

        private void ReleaseRuntimePanelSettings()
        {
            if (runtimePanelSettings == null)
            {
                return;
            }

            UnityObjectUtility.DestroySafely(runtimePanelSettings);
            runtimePanelSettings = null;
        }
    }
}
