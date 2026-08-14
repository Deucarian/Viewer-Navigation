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

        [SerializeField] private UIDocument document;
        [SerializeField] private PanelSettings panelSettings;

        private ViewerNavigationController controller;
        private ViewerNavigationSettings settings;
        private PanelSettings runtimePanelSettings;
        private VisualElement root;
        private VisualElement toolbar;
        private Button orbitButton;
        private Button flyButton;
        private Button homeButton;
        private Button topButton;
        private ViewerViewCubeElement viewCube;
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
        }

        private void OnDisable()
        {
            UnsubscribeController();
        }

        private void OnDestroy()
        {
            UnsubscribeController();
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
                runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                runtimePanelSettings.name = "Runtime Viewer Navigation Panel Settings";
                document.panelSettings = runtimePanelSettings;
            }

            document.sortingOrder = 1110;
        }

        private void BuildUi()
        {
            VisualElement documentRoot = document.rootVisualElement;
            documentRoot.Clear();
            documentRoot.pickingMode = PickingMode.Ignore;

            root = new VisualElement { name = RootName, pickingMode = PickingMode.Ignore };
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

            if (settings == null || settings.ShowViewCube)
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
            float toolbarWidth =
                DeucarianControlIslandStyle.CalculatePanelWidth(
                    DeucarianControlIslandStyle.CompactPanel,
                    DeucarianControlIslandStyle.RoundedSquareButton,
                    4);
            toolbar = new VisualElement
            {
                name = ToolbarName,
                pickingMode = PickingMode.Position
            };
            toolbar.style.position = Position.Absolute;
            toolbar.style.bottom = 56f;
            toolbar.style.left = Length.Percent(50f);
            toolbar.style.width = toolbarWidth;
            toolbar.style.marginLeft = -toolbarWidth * 0.5f;
            root.Add(toolbar);

            orbitButton = CreateToolbarButton("Orbit", "O", "Orbit navigation");
            flyButton = CreateToolbarButton("Fly", "F", "Fly navigation");
            homeButton = CreateToolbarButton("Home", "H", "Return to origin");
            topButton = CreateToolbarButton("Top", "T", "Toggle top-down view");
            orbitButton.clicked += OnOrbitClicked;
            flyButton.clicked += OnFlyClicked;
            homeButton.clicked += OnHomeClicked;
            topButton.clicked += OnTopClicked;
            toolbar.Add(orbitButton);
            toolbar.Add(flyButton);
            toolbar.Add(homeButton);
            toolbar.Add(topButton);
        }

        private static Button CreateToolbarButton(
            string name,
            string label,
            string tooltip)
        {
            Button button = new Button
            {
                name = name + "Button",
                text = label,
                tooltip = tooltip,
                pickingMode = PickingMode.Position
            };
            DeucarianControlIslandStyle.ApplyIconButton(
                button,
                DeucarianControlIslandStyle.RoundedSquareButton);
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.RegisterCallback<PointerDownEvent>(evt => evt.StopImmediatePropagation());
            button.RegisterCallback<PointerUpEvent>(evt => evt.StopImmediatePropagation());
            return button;
        }

        private void ApplyTheme()
        {
            DeucarianTheme theme = DeucarianThemeRuntimeResolver.ResolveTheme(this);
            DeucarianThemeStyle visualStyle = theme != null ? theme.VisualStyle : null;
            DeucarianControlIslandProfile profile =
                DeucarianControlIslandProfiles.Resolve(visualStyle);
            if (toolbar != null)
            {
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
            }

            Color surface = ResolveColor(
                theme,
                DeucarianBuiltinColorRoleIds.SurfaceRaised,
                new Color(0.08f, 0.11f, 0.15f, 1f));
            Color text = ResolveColor(
                theme,
                DeucarianBuiltinColorRoleIds.TextPrimary,
                new Color(0.88f, 0.92f, 0.95f, 1f));
            Color accent = ResolveColor(
                theme,
                DeucarianBuiltinColorRoleIds.Accent,
                new Color(0.10f, 0.72f, 0.74f, 1f));
            viewCube?.ApplyPalette(surface, text, accent);
            ApplyButtonState(controller != null ? controller.Snapshot : default, surface, text, accent);
        }

        private void Refresh(ViewerNavigationSnapshot snapshot)
        {
            DeucarianTheme theme = DeucarianThemeRuntimeResolver.ResolveTheme(this);
            Color surface = ResolveColor(
                theme,
                DeucarianBuiltinColorRoleIds.SurfaceRaised,
                new Color(0.08f, 0.11f, 0.15f, 1f));
            Color text = ResolveColor(
                theme,
                DeucarianBuiltinColorRoleIds.TextPrimary,
                new Color(0.88f, 0.92f, 0.95f, 1f));
            Color accent = ResolveColor(
                theme,
                DeucarianBuiltinColorRoleIds.Accent,
                new Color(0.10f, 0.72f, 0.74f, 1f));
            ApplyButtonState(snapshot, surface, text, accent);
            if (snapshot.IsTopDown)
            {
                viewCube?.SetActiveFace(ViewerViewFace.Top);
            }
        }

        private void ApplyButtonState(
            ViewerNavigationSnapshot snapshot,
            Color surface,
            Color text,
            Color accent)
        {
            SetButtonSelected(orbitButton, snapshot.Mode == ViewerNavigationMode.Orbit, surface, text, accent);
            SetButtonSelected(flyButton, snapshot.Mode == ViewerNavigationMode.Fly, surface, text, accent);
            SetButtonSelected(topButton, snapshot.IsTopDown, surface, text, accent);
            SetButtonSelected(homeButton, false, surface, text, accent);
        }

        private static void SetButtonSelected(
            Button button,
            bool selected,
            Color surface,
            Color text,
            Color accent)
        {
            if (button == null)
            {
                return;
            }

            button.style.backgroundColor = selected ? accent : surface;
            button.style.color = selected ? Color.white : text;
        }

        private static Color ResolveColor(
            DeucarianTheme theme,
            string role,
            Color fallback)
        {
            return theme != null && theme.TryGetColorById(role, out Color value)
                ? value
                : fallback;
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
