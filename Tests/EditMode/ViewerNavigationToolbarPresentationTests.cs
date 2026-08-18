using System.IO;
using System.Reflection;
using Deucarian.Diagnostics;
using Deucarian.Theming;
using Deucarian.UI;
using Deucarian.ViewerNavigation.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationToolbarPresentationTests
    {
        [Test]
        public void ReferenceCompositionOwnsTheCanonicalToolbarDocument()
        {
            GameObject root = new GameObject("Canonical Toolbar Test");
            GameObject cameraObject = new GameObject("Canonical Toolbar Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            try
            {
                ViewerNavigationInstaller installer =
                    ViewerNavigationInstaller.CreateWithReferencePreset(
                        root.transform,
                        camera);
                ViewerNavigationToolbarPresenter presenter =
                    installer.Toolbar;
                VisualTreeAsset toolbarAsset =
                    Resources.Load<VisualTreeAsset>(
                        ViewerNavigationToolbarPresenter
                            .ToolbarAssetResourcesPath);
                StyleSheet toolbarStyle = Resources.Load<StyleSheet>(
                    ViewerNavigationToolbarPresenter
                        .ToolbarAssetResourcesPath);
                PanelSettings toolbarPanel = DeucarianUIRuntimeAssets
                    .LoadRuntimePanelSettings();

                Assert.That(presenter, Is.Not.Null);
                Assert.That(toolbarAsset, Is.Not.Null);
                Assert.That(toolbarStyle, Is.Not.Null);
                Assert.That(toolbarPanel, Is.Not.Null);
                Assert.That(presenter.Document, Is.Not.Null);
                Assert.That(presenter.Document.enabled, Is.True);
                Assert.That(presenter.Document.panelSettings, Is.Not.Null);
                Assert.That(presenter.Document.visualTreeAsset, Is.Not.Null);
                Assert.That(
                    presenter.Document.visualTreeAsset,
                    Is.SameAs(toolbarAsset));
                Assert.That(
                    presenter.Document.panelSettings,
                    Is.SameAs(toolbarPanel));
                Assert.That(
                    presenter.Document.panelSettings.name,
                    Is.EqualTo("DeucarianRuntimePanelSettings"));
                Assert.That(
                    presenter.Document.sortingOrder,
                    Is.EqualTo(DeucarianUIDepth.PrimaryControls));
                Assert.That(
                    root.GetComponentsInChildren<UIDocument>(true).Length,
                    Is.EqualTo(1));
                Assert.That(presenter.Root, Is.Not.Null);
                Assert.That(
                    presenter.Document.rootVisualElement.styleSheets
                        .Contains(toolbarStyle),
                    Is.True);
                Assert.That(presenter.HasLoadedPresentationAssets, Is.True);
                Assert.That(presenter.HasAttachedPresentationAssets, Is.True);
                Assert.That(
                    presenter.Root.name,
                    Is.EqualTo(ViewerNavigationToolbarPresenter.RootName));
                Assert.That(
                    presenter.Root.pickingMode,
                    Is.EqualTo(PickingMode.Ignore));
                Assert.That(presenter.ToolbarElement, Is.Not.Null);
                Assert.That(
                    presenter.ToolbarElement.name,
                    Is.EqualTo(ViewerNavigationToolbarPresenter.ToolbarName));
                Assert.That(
                    presenter.ToolbarElement.pickingMode,
                    Is.EqualTo(PickingMode.Position));

                AssertToolbarControl(
                    presenter.ToolbarElement,
                    ViewerNavigationToolbarPresenter.OrbitButtonName,
                    ViewerNavigationToolbarPresenter.OrbitIconName);
                AssertToolbarControl(
                    presenter.ToolbarElement,
                    ViewerNavigationToolbarPresenter.FlyButtonName,
                    ViewerNavigationToolbarPresenter.FlyIconName);
                AssertToolbarControl(
                    presenter.ToolbarElement,
                    ViewerNavigationToolbarPresenter.HomeButtonName,
                    ViewerNavigationToolbarPresenter.HomeIconName);
                AssertToolbarControl(
                    presenter.ToolbarElement,
                    ViewerNavigationToolbarPresenter.TopDownButtonName,
                    ViewerNavigationToolbarPresenter.TopDownIconName);
                VisualElement perspective = presenter.ToolbarElement
                    .Q<VisualElement>(
                        ViewerNavigationToolbarPresenter
                            .PerspectiveIconName);
                Assert.That(perspective, Is.Not.Null);
                Assert.That(
                    perspective.pickingMode,
                    Is.EqualTo(PickingMode.Ignore));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ReferenceThemeAndProviderStyleDriveCanonicalChrome()
        {
            GameObject root = new GameObject("Toolbar Theme Test");
            GameObject cameraObject = new GameObject("Toolbar Theme Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            DeucarianThemeStyle compactStyle =
                DeucarianThemeStylePresets.CreateRuntimeStyle(
                    DeucarianThemeStyleIds.MaterialDark);
            try
            {
                ViewerNavigationInstaller installer =
                    ViewerNavigationInstaller.CreateWithReferencePreset(
                        root.transform,
                        camera);
                ViewerNavigationToolbarPresenter presenter =
                    installer.Toolbar;
                DeucarianTheme theme =
                    installer.ThemeProvider.CurrentTheme;
                DeucarianControlIslandProfile referenceProfile =
                    DeucarianControlIslandProfiles.Resolve(
                        installer.ThemeProvider.CurrentStyle);

                Assert.That(
                    presenter.ToolbarElement.style.width.value.value,
                    Is.EqualTo(referenceProfile.CalculatePanelWidth(4)));
                Assert.That(
                    presenter.Root.style.paddingBottom.value.value,
                    Is.EqualTo(
                        DeucarianControlIslandStyle.DefaultBottomOffset));
                Color raised = ResolveColor(
                    theme,
                    DeucarianBuiltinColorRoleIds.SurfaceRaised);
                Color expectedSurface = installer.ThemeProvider.CurrentStyle
                    .ResolveSurfaceColor(raised);
                AssertColor(
                    presenter.ToolbarElement.style.backgroundColor.value,
                    expectedSurface);

                installer.ThemeProvider.SetStyle(compactStyle);
                DeucarianControlIslandProfile compactProfile =
                    DeucarianControlIslandProfiles.Resolve(compactStyle);
                Assert.That(
                    presenter.ToolbarElement.style.width.value.value,
                    Is.EqualTo(compactProfile.CalculatePanelWidth(4)));

                FieldInfo visualStateField = typeof(
                        ViewerNavigationToolbarPresenter)
                    .GetField(
                        "visualState",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(visualStateField, Is.Not.Null);
                object visualState = visualStateField.GetValue(presenter);
                FieldInfo styleField = visualState.GetType().GetField(
                    "style",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(styleField, Is.Not.Null);
                Assert.That(
                    styleField.GetValue(visualState),
                    Is.SameAs(compactStyle));
                VisualElement tooltip =
                    presenter.RuntimeTooltip.Bubble;
                Assert.That(tooltip, Is.Not.Null);
                Assert.That(
                    tooltip.style.borderTopLeftRadius.value.value,
                    Is.EqualTo(compactStyle.CornerRadius));
            }
            finally
            {
                Object.DestroyImmediate(compactStyle);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void EmptyNearestProviderStillResolvesACompleteTheme()
        {
            GameObject root = new GameObject("Empty Theme Provider Test");
            root.AddComponent<DeucarianThemeProvider>();
            try
            {
                ViewerNavigationToolbarPresenter presenter =
                    root.AddComponent<ViewerNavigationToolbarPresenter>();

                presenter.Initialize(null);

                FieldInfo visualStateField = typeof(
                        ViewerNavigationToolbarPresenter)
                    .GetField(
                        "visualState",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(visualStateField, Is.Not.Null);
                object visualState = visualStateField.GetValue(presenter);
                Assert.That(visualState, Is.Not.Null);
                FieldInfo themeField = visualState.GetType().GetField(
                    "theme",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(themeField, Is.Not.Null);
                Assert.That(themeField.GetValue(visualState), Is.Not.Null);
                Assert.That(
                    presenter.ToolbarElement.style.backgroundColor.value,
                    Is.Not.EqualTo(DeucarianColorPalette.MissingColor));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NavigationPaletteUsesGenericAccentForActiveBackground()
        {
            DeucarianTheme theme =
                DeucarianViewerReferenceThemePreset.Resolve().DarkTheme;
            DeucarianIconButtonPalette palette =
                ViewerNavigationToolbarTheme.ResolvePalette(theme);

            AssertColor(
                palette.BackgroundSelected,
                ResolveColor(theme, DeucarianBuiltinColorRoleIds.Accent));
            AssertColor(
                palette.Icon,
                ResolveColor(theme, DeucarianBuiltinColorRoleIds.TextMuted));
            AssertColor(
                palette.IconActive,
                ResolveColor(
                    theme,
                    DeucarianBuiltinColorRoleIds.TextPrimary));
        }

        [Test]
        public void CanonicalAssetsAreCompleteAndConsumerNeutral()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(ViewerNavigationToolbarPresenter).Assembly);
            Assert.That(package, Is.Not.Null);
            string resourceRoot = Path.Combine(
                package.resolvedPath,
                "Runtime",
                "Resources",
                "Deucarian");
            string uxml = File.ReadAllText(
                Path.Combine(resourceRoot, "ViewerNavigationToolbar.uxml"));
            string uss = File.ReadAllText(
                Path.Combine(resourceRoot, "ViewerNavigationToolbar.uss"));
            string duplicatePanel = Path.Combine(
                resourceRoot,
                "ViewerNavigationToolbarPanelSettings.asset");

            StringAssert.Contains(
                ViewerNavigationToolbarPresenter.RootName,
                uxml);
            StringAssert.Contains(
                ViewerNavigationToolbarPresenter.ToolbarName,
                uxml);
            StringAssert.Contains("ViewerNavigationOrbit", uss);
            StringAssert.Contains("ViewerNavigationFly", uss);
            StringAssert.Contains("ViewerNavigationRecenter", uss);
            StringAssert.Contains("ViewerNavigationOrthographic", uss);
            StringAssert.Contains("ViewerNavigationPerspective", uss);
            Assert.False(File.Exists(duplicatePanel));
            Assert.That(
                DeucarianUIRuntimeAssets.LoadRuntimePanelSettings(),
                Is.Not.Null);
            StringAssert.DoesNotContain("Report", uxml + uss);
            StringAssert.DoesNotContain("Simultria", uxml + uss);
            StringAssert.DoesNotContain("Activity", uxml + uss);
        }

        [Test]
        public void DiagnosticsReportCanonicalToolbarHealth()
        {
            GameObject root = new GameObject("Toolbar Diagnostics Test");
            GameObject cameraObject =
                new GameObject("Toolbar Diagnostics Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            try
            {
                ViewerNavigationInstaller installer =
                    ViewerNavigationInstaller.CreateWithReferencePreset(
                        root.transform,
                        camera);
                var provider = new ViewerNavigationDiagnosticProvider(
                    installer.Controller);
                var builder = new DiagnosticReportBuilder();

                provider.Collect(builder);

                DiagnosticSection section = builder.Build().Sections[0];
                Assert.That(
                    FindItem(section, "toolbar_document").Value,
                    Is.EqualTo("Ready"));
                Assert.That(
                    FindItem(section, "toolbar_assets").Value,
                    Is.EqualTo("Loaded"));
                Assert.That(
                    FindItem(section, "toolbar_controls").Value,
                    Is.EqualTo("Complete"));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void DiagnosticsTreatAnUninstalledToolbarAsOptional()
        {
            GameObject root = new GameObject("Headless Navigation Diagnostics Test");
            try
            {
                ViewerNavigationController controller =
                    root.AddComponent<ViewerNavigationController>();
                var provider = new ViewerNavigationDiagnosticProvider(controller);
                var builder = new DiagnosticReportBuilder();

                provider.Collect(builder);

                DiagnosticItem item = FindItem(
                    builder.Build().Sections[0],
                    "toolbar_component");
                Assert.That(item.Value, Is.EqualTo("Not installed"));
                Assert.That(item.Severity, Is.EqualTo(DiagnosticSeverity.Info));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertToolbarControl(
            VisualElement toolbar,
            string buttonName,
            string iconName)
        {
            Button button = toolbar.Q<Button>(buttonName);
            Assert.That(button, Is.Not.Null);
            Assert.That(button.focusable, Is.True);
            Assert.That(button.pickingMode, Is.EqualTo(PickingMode.Position));
            VisualElement icon = button.Q<VisualElement>(iconName);
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }

        private static Color ResolveColor(
            DeucarianTheme theme,
            string roleId)
        {
            Assert.That(theme.TryGetColorById(roleId, out Color color), Is.True);
            return color;
        }

        private static DiagnosticItem FindItem(
            DiagnosticSection section,
            string key)
        {
            for (int i = 0; i < section.Items.Count; i++)
            {
                if (section.Items[i].Key == key)
                {
                    return section.Items[i];
                }
            }

            Assert.Fail("Missing diagnostic item: " + key);
            return null;
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f));
        }

    }
}
