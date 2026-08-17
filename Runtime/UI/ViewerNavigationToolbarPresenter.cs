using System;
using Deucarian.Common;
using Deucarian.Theming;
using Deucarian.Theming.UIToolkit;
using Deucarian.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.UI
{
    [DisallowMultipleComponent]
    public sealed class ViewerNavigationToolbarPresenter : MonoBehaviour
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
            "Orbit · Left drag to rotate · Right drag to pan · Scroll to zoom";
        public const string FlyTooltip =
            "Fly · WASD to move · Right drag to look";
        public const string HomeTooltip =
            "Recenter · Fit the model and reset the camera";
        public const string TopDownTooltip =
            "Top view · Switch to an orthographic plan view";
        public const string PerspectiveTooltip =
            "Perspective view · Return to an orbitable 3D view";

        private const string OrbitIconResource =
            "Deucarian/ViewerNavigationOrbit";
        private const string FlyIconResource =
            "Deucarian/ViewerNavigationFly";
        private const string HomeIconResource =
            "Deucarian/ViewerNavigationRecenter";
        private const string TopDownIconResource =
            "Deucarian/ViewerNavigationOrthographic";
        private const string PerspectiveIconResource =
            "Deucarian/ViewerNavigationPerspective";

        [SerializeField] private UIDocument document;
        [SerializeField] private PanelSettings panelSettings;

        private readonly ViewerNavigationToolbarVisualState visualState =
            new ViewerNavigationToolbarVisualState();
        private ViewerNavigationController controller;
        private ViewerNavigationSettings settings;
        private PanelSettings runtimePanelSettings;
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
        private ViewerNavigationRuntimeTooltipPresenter runtimeTooltip;
        private DeucarianThemeProvider themeProvider;
        private IDisposable inputRegistration;
        private bool controllerSubscribed;

        public UIDocument Document => document;
        public VisualElement Root => root;
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
            EnsureDocument();
            BuildUi();
            BindThemeProvider();
            SubscribeController();
            inputRegistration = controller?.InteractionGate?.RegisterUiRoot(root);
            Refresh(controller != null ? controller.Snapshot : default);
        }

        private void LateUpdate()
        {
            if (viewCube != null && controller?.Camera != null)
            {
                viewCube.UpdateOrientation(controller.Camera.transform.rotation);
                if (controller.IsTopDown)
                {
                    viewCube.SetActiveFace(ViewerViewFace.Top);
                }
            }
        }

        private void OnEnable()
        {
            SubscribeController();
            BindThemeProvider();
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
            if (runtimePanelSettings != null)
            {
                UnityObjectUtility.DestroySafely(runtimePanelSettings);
                runtimePanelSettings = null;
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
                    "Runtime Viewer Navigation Panel Settings";
                document.panelSettings = runtimePanelSettings;
            }

            document.sortingOrder = 1110;
        }

        private void BuildUi()
        {
            VisualElement documentRoot = document.rootVisualElement;
            DisposeUi();
            documentRoot.Clear();
            documentRoot.pickingMode = PickingMode.Ignore;

            root = new VisualElement
            {
                name = RootName,
                pickingMode = PickingMode.Ignore
            };
            root.style.position = Position.Absolute;
            root.style.left = 0f;
            root.style.right = 0f;
            root.style.top = 0f;
            root.style.bottom = 0f;
            documentRoot.Add(root);

            if (settings == null || settings.ShowToolbar)
            {
                BuildToolbar();
            }

            if (settings != null && settings.ShowViewCube)
            {
                viewCube = new ViewerViewCubeElement();
                viewCube.style.right = 20f;
                viewCube.style.top = 20f;
                viewCube.FaceSelected += OnFaceSelected;
                root.Add(viewCube);
            }

            ApplyTheme();
        }

        private void BuildToolbar()
        {
            toolbar = new VisualElement
            {
                name = ToolbarName,
                pickingMode = PickingMode.Position
            };
            toolbar.style.position = Position.Absolute;
            toolbar.style.bottom = DeucarianControlIslandStyle.DefaultBottomOffset;
            toolbar.style.left = Length.Percent(50f);
            root.Add(toolbar);

            orbitButton = CreateToolbarButton(OrbitButtonName, OrbitTooltip);
            flyButton = CreateToolbarButton(FlyButtonName, FlyTooltip);
            homeButton = CreateToolbarButton(HomeButtonName, HomeTooltip);
            topButton = CreateToolbarButton(TopDownButtonName, TopDownTooltip);
            orbitIcon = CreateIcon(OrbitIconName, OrbitIconResource);
            flyIcon = CreateIcon(FlyIconName, FlyIconResource);
            homeIcon = CreateIcon(HomeIconName, HomeIconResource);
            topDownIcon = CreateIcon(TopDownIconName, TopDownIconResource);
            perspectiveIcon = CreateIcon(
                PerspectiveIconName,
                PerspectiveIconResource);
            orbitButton.Add(orbitIcon);
            flyButton.Add(flyIcon);
            homeButton.Add(homeIcon);
            topButton.Add(topDownIcon);
            topButton.Add(perspectiveIcon);

            orbitButton.clicked += OnOrbitClicked;
            flyButton.clicked += OnFlyClicked;
            homeButton.clicked += OnHomeClicked;
            topButton.clicked += OnTopClicked;
            toolbar.Add(orbitButton);
            toolbar.Add(flyButton);
            toolbar.Add(homeButton);
            toolbar.Add(topButton);

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
                perspectiveIcon);
            runtimeTooltip = new ViewerNavigationRuntimeTooltipPresenter(root);
            runtimeTooltip.Bind(orbitButton);
            runtimeTooltip.Bind(flyButton);
            runtimeTooltip.Bind(homeButton);
            runtimeTooltip.Bind(topButton);
        }

        private static Button CreateToolbarButton(string name, string tooltip)
        {
            Button button = new Button
            {
                name = name,
                text = string.Empty,
                tooltip = tooltip,
                pickingMode = PickingMode.Position
            };
            button.RegisterCallback<PointerDownEvent>(
                evt => evt.StopImmediatePropagation());
            button.RegisterCallback<PointerUpEvent>(
                evt => evt.StopImmediatePropagation());
            return button;
        }

        private static VisualElement CreateIcon(
            string name,
            string resourceName)
        {
            VisualElement icon = new VisualElement
            {
                name = name,
                pickingMode = PickingMode.Ignore
            };
            Texture2D texture = Resources.Load<Texture2D>(resourceName);
            if (texture != null)
            {
                icon.style.backgroundImage = new StyleBackground(texture);
            }

            return icon;
        }

        private void ApplyTheme()
        {
            DeucarianTheme theme = themeProvider != null
                ? themeProvider.CurrentTheme
                : DeucarianThemeRuntimeResolver.ResolveTheme(this);
            DeucarianThemeStyle visualStyle = themeProvider != null
                ? themeProvider.CurrentStyle
                : theme != null ? theme.VisualStyle : null;
            DeucarianControlIslandProfile profile =
                DeucarianControlIslandProfiles.Resolve(visualStyle);
            if (toolbar != null)
            {
                float toolbarWidth = profile.CalculatePanelWidth(4);
                toolbar.style.width = toolbarWidth;
                toolbar.style.marginLeft = -toolbarWidth * 0.5f;
                DeucarianControlIslandStyle.ApplyPanel(
                    toolbar,
                    profile.CreatePanelChrome(),
                    visualStyle);
                if (!DeucarianUIToolkitThemeStyleUtility.ApplyPanel(
                        toolbar,
                        theme,
                        visualStyle))
                {
                    toolbar.style.backgroundColor =
                        new Color(0.055f, 0.075f, 0.1f, 0.9f);
                }

                DeucarianIconButtonChrome buttonChrome =
                    profile.CreateIconButtonChrome(true);
                ApplyButtonLayout(orbitButton, orbitIcon, null, buttonChrome, visualStyle);
                ApplyButtonLayout(flyButton, flyIcon, null, buttonChrome, visualStyle);
                ApplyButtonLayout(homeButton, homeIcon, null, buttonChrome, visualStyle);
                ApplyButtonLayout(
                    topButton,
                    topDownIcon,
                    perspectiveIcon,
                    buttonChrome,
                    visualStyle);
            }

            visualState.ApplyTheme(theme);
            visualState.SetEnabled(controller != null);
            visualState.Apply(controller != null ? controller.Snapshot : default);
            runtimeTooltip?.ApplyTheme(theme);

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
                new Color(0.10f, 0.72f, 0.74f, 1f));
            viewCube?.ApplyPalette(surface, text, accent);
        }

        private static void ApplyButtonLayout(
            Button button,
            VisualElement primaryIcon,
            VisualElement secondaryIcon,
            DeucarianIconButtonChrome chrome,
            DeucarianThemeStyle style)
        {
            DeucarianControlIslandStyle.ApplyIconButton(
                button,
                chrome,
                style);
            DeucarianControlIslandStyle.ApplyIcon(
                primaryIcon,
                chrome,
                true);
            DeucarianControlIslandStyle.ApplyIcon(
                secondaryIcon,
                chrome,
                true);
        }

        private void Refresh(ViewerNavigationSnapshot snapshot)
        {
            visualState.Apply(snapshot);
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
                themeProvider.ThemeChanged += OnThemeChanged;
                themeProvider.StyleChanged += OnStyleChanged;
            }

            ApplyTheme();
        }

        private void UnbindThemeProvider()
        {
            if (themeProvider != null)
            {
                themeProvider.ThemeChanged -= OnThemeChanged;
                themeProvider.StyleChanged -= OnStyleChanged;
                themeProvider = null;
            }
        }

        private void OnThemeChanged(DeucarianTheme _) => ApplyTheme();
        private void OnStyleChanged(DeucarianThemeStyle _) => ApplyTheme();

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

        private void DisposeUi()
        {
            visualState.Dispose();
            runtimeTooltip?.Dispose();
            runtimeTooltip = null;
            if (viewCube != null)
            {
                viewCube.FaceSelected -= OnFaceSelected;
            }

            if (orbitButton != null)
            {
                orbitButton.clicked -= OnOrbitClicked;
            }

            if (flyButton != null)
            {
                flyButton.clicked -= OnFlyClicked;
            }

            if (homeButton != null)
            {
                homeButton.clicked -= OnHomeClicked;
            }

            if (topButton != null)
            {
                topButton.clicked -= OnTopClicked;
            }

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
