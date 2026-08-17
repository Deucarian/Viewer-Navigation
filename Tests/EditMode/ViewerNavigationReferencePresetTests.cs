using System.IO;
using System.Reflection;
using Deucarian.CameraNavigation;
using Deucarian.Theming;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationReferencePresetTests
    {
        [Test]
        public void PackagedReferencePresetMatchesProvenViewerTuning()
        {
            ViewerNavigationSettings preset =
                ViewerNavigationSettings.LoadReferencePreset();

            Assert.That(preset, Is.Not.Null);
            Assert.That(preset.Controls, Is.Not.Null);
            Assert.That(preset.FramingSettings, Is.Not.Null);
            Assert.That(preset.AnimateTransitions, Is.True);
            Assert.That(preset.CalculateTransitionDuration(2f), Is.EqualTo(0.1f));
            Assert.That(preset.CalculateTransitionDuration(10f), Is.EqualTo(0.5f));
            Assert.That(preset.CalculateTransitionDuration(100f), Is.EqualTo(1.25f));
            Assert.That(preset.TransitionMatchFieldOfView, Is.EqualTo(0.1f));
            Assert.That(
                preset.EvaluateMovement(0.25f),
                Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(
                preset.EvaluateRotation(0.75f),
                Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(preset.ReferencePadding, Is.EqualTo(1.25f));
            Assert.That(preset.ShowToolbar, Is.True);
            Assert.That(preset.ShowViewCube, Is.False);
            Assert.That(preset.Controls.GlobalSensitivity, Is.EqualTo(10f));
            Assert.That(preset.Controls.OrbitKeyboardPanSpeed, Is.EqualTo(0.9f));
            Assert.That(preset.Controls.OrbitRotationSpeed, Is.EqualTo(0.35f));
            Assert.That(preset.Controls.InvertOrbitRotation, Is.True);
            Assert.That(preset.Controls.FlyMoveSpeed, Is.EqualTo(2f));
            Assert.That(preset.Controls.FlyRotationSpeed, Is.EqualTo(0.24f));
            Assert.That(preset.Controls.WheelZoomStep, Is.EqualTo(0.12f));
            Assert.That(preset.Controls.BoostScale, Is.EqualTo(4f));
            Assert.That(
                preset.FramingSettings.RotationPolicy,
                Is.EqualTo(
                    DeucarianCameraFramingRotationPolicy
                        .PreserveCurrentCameraRotation));
            Assert.That(preset.FramingSettings.PaddingMultiplier, Is.EqualTo(1f));
            Assert.That(
                preset.FramingSettings.RelaxedDistanceMultiplier,
                Is.EqualTo(6f));
            Assert.That(
                preset.FramingSettings.NearClipClearanceMultiplier,
                Is.EqualTo(1.05f));
        }

        [Test]
        public void SettingsInstallerDefaultsToPackagedReferencePreset()
        {
            GameObject root = new GameObject("Reference Preset Test");
            GameObject cameraObject = new GameObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            try
            {
                ViewerNavigationSettings preset =
                    ViewerNavigationSettings.LoadReferencePreset();
                ViewerNavigationInstaller installer =
                    ViewerNavigationInstaller.Create(
                        root.transform,
                        camera,
                        configuration: null);

                Assert.That(installer.Controller.MotionProfile, Is.SameAs(preset));
                Assert.That(installer.Controller.Controls, Is.SameAs(preset.Controls));
                Assert.That(
                    installer.Controller.FramingSettings,
                    Is.SameAs(preset.FramingSettings));
                Assert.That(installer.Toolbar, Is.Not.Null);
                Assert.That(installer.Toolbar.ViewCube, Is.Null);
                Assert.That(installer.ThemeProvider, Is.Null);
                Assert.That(
                    installer.GetComponent<DeucarianThemeProvider>(),
                    Is.Null,
                    "The non-reference Create overload must remain unthemed.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ReferencePresetCompositionInjectsSharedNavigationHelpers()
        {
            ViewerNavigationReferenceCompositionProfile composition =
                ViewerNavigationReferenceComposition.Resolve();

            Assert.That(composition.Preset, Is.Not.Null);
            Assert.That(
                composition.Preset,
                Is.SameAs(ViewerNavigationSettings.LoadReferencePreset()));
            Assert.That(
                composition.InputBlocker,
                Is.TypeOf<ViewerNavigationUiInputBlocker>());
            Assert.That(
                composition.BoundsStrategy,
                Is.TypeOf<ViewerNavigationMeshBoundsStrategy>());
            Assert.That(
                composition.AnimationPolicy,
                Is.TypeOf<ViewerNavigationAnimationPolicy>());
            Assert.That(composition.AnimationPolicy.ShouldAnimate, Is.False);
            DeucarianViewerReferenceThemeProfile themeProfile =
                DeucarianViewerReferenceThemePreset.Resolve();
            Assert.That(composition.ThemeProfile, Is.SameAs(themeProfile));
            Assert.That(
                composition.ThemeMode,
                Is.EqualTo(DeucarianThemeMode.Dark));
            Assert.That(
                composition.ThemeProfile.DarkTheme,
                Is.SameAs(themeProfile.ThemeFamily.DarkTheme));
            Assert.That(
                composition.ThemeProfile.VisualStyle.StyleId,
                Is.EqualTo(DeucarianThemeStyleIds.FrostedGlass));
        }

        [Test]
        public void ReferenceCompositionInjectsTheResolvedPolicies()
        {
            bool shouldAnimate = false;
            var policy = new ViewerNavigationAnimationPolicy(
                () => shouldAnimate);
            ViewerNavigationReferenceCompositionProfile composition =
                ViewerNavigationReferenceComposition.Resolve(policy);
            Assert.That(composition.AnimationPolicy, Is.SameAs(policy));
            GameObject root = new GameObject("Reference Composition Injection Test");
            GameObject cameraObject = new GameObject("Reference Composition Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            try
            {
                ViewerNavigationInstaller installer =
                    composition.Compose(root.transform, camera);

                Assert.That(
                    installer.Controller.Controls,
                    Is.SameAs(composition.Preset.Controls));
                Assert.That(
                    installer.Controller.FramingSettings,
                    Is.SameAs(composition.Preset.FramingSettings));
                Assert.That(
                    installer.Controller.InputBlocker,
                    Is.SameAs(composition.InputBlocker));
                Assert.That(
                    installer.Controller.ReferenceBoundsStrategy,
                    Is.SameAs(composition.BoundsStrategy));
                Assert.That(installer.ThemeProvider, Is.Not.Null);
                Assert.That(
                    installer.ThemeProvider,
                    Is.SameAs(
                        installer.GetComponent<DeucarianThemeProvider>()));
                Assert.That(
                    installer.ThemeProvider.CurrentThemeFamily,
                    Is.SameAs(composition.ThemeProfile.ThemeFamily));
                Assert.That(
                    installer.ThemeProvider.ThemeMode,
                    Is.EqualTo(DeucarianThemeMode.Dark));
                Assert.That(
                    installer.ThemeProvider.CurrentTheme,
                    Is.SameAs(composition.ThemeProfile.DarkTheme));
                Assert.That(
                    installer.ThemeProvider.CurrentStyle,
                    Is.SameAs(composition.ThemeProfile.VisualStyle));
                Assert.That(
                    installer.ThemeProvider.CurrentStyle.StyleId,
                    Is.EqualTo(DeucarianThemeStyleIds.FrostedGlass));
                FieldInfo boundProviderField = installer.Toolbar
                    .GetType()
                    .GetField(
                        "themeProvider",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(boundProviderField, Is.Not.Null);
                Assert.That(
                    boundProviderField.GetValue(installer.Toolbar),
                    Is.SameAs(installer.ThemeProvider));
                Assert.That(
                    installer.Controller.MotionProfile.AnimateTransitions,
                    Is.False);

                shouldAnimate = true;
                Assert.That(
                    installer.Controller.MotionProfile.AnimateTransitions,
                    Is.True);
                Assert.That(
                    installer.Controller.MotionProfile
                        .CalculateTransitionDuration(10f),
                    Is.EqualTo(
                        composition.Preset.CalculateTransitionDuration(10f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void WithPresetPreservesAllPolicyAndThemeIdentities()
        {
            ViewerNavigationReferenceCompositionProfile composition =
                ViewerNavigationReferenceComposition.Resolve();
            ViewerNavigationSettings replacement =
                ScriptableObject.CreateInstance<ViewerNavigationSettings>();
            try
            {
                ViewerNavigationReferenceCompositionProfile customized =
                    composition.WithPreset(replacement);

                Assert.That(customized.Preset, Is.SameAs(replacement));
                Assert.That(
                    customized.InputBlocker,
                    Is.SameAs(composition.InputBlocker));
                Assert.That(
                    customized.BoundsStrategy,
                    Is.SameAs(composition.BoundsStrategy));
                Assert.That(
                    customized.AnimationPolicy,
                    Is.SameAs(composition.AnimationPolicy));
                Assert.That(
                    customized.ThemeProfile,
                    Is.SameAs(composition.ThemeProfile));
                Assert.That(
                    customized.ThemeProfile.ThemeFamily,
                    Is.SameAs(composition.ThemeProfile.ThemeFamily));
                Assert.That(
                    customized.ThemeProfile.DarkTheme,
                    Is.SameAs(composition.ThemeProfile.DarkTheme));
                Assert.That(
                    customized.ThemeProfile.VisualStyle,
                    Is.SameAs(composition.ThemeProfile.VisualStyle));
                Assert.That(
                    customized.ThemeMode,
                    Is.EqualTo(composition.ThemeMode));
            }
            finally
            {
                Object.DestroyImmediate(replacement);
            }
        }

        [Test]
        public void AnimationPolicyDefaultsToSharedMotionPreferenceAndReevaluatesItsDelegate()
        {
            var defaultPolicy = new ViewerNavigationAnimationPolicy();
            Assert.That(
                defaultPolicy.ShouldAnimate,
                Is.EqualTo(ViewerNavigationMotionPreferences.ShouldAnimate));
            Assert.That(defaultPolicy.UsesSharedMotionPreference, Is.True);

            bool shouldAnimate = false;
            var delegatedPolicy = new ViewerNavigationAnimationPolicy(
                () => shouldAnimate);
            Assert.That(delegatedPolicy.UsesSharedMotionPreference, Is.False);
            Assert.That(delegatedPolicy.ShouldAnimate, Is.False);

            shouldAnimate = true;
            Assert.That(delegatedPolicy.ShouldAnimate, Is.True);
        }

        [TestCase(false, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        public void SharedMotionPreferenceRequiresRuntimeWithoutReducedMotion(
            bool isPlaying,
            bool prefersReducedMotion,
            bool expected)
        {
            Assert.That(
                ViewerNavigationMotionPreferences.ResolveShouldAnimate(
                    isPlaying,
                    prefersReducedMotion),
                Is.EqualTo(expected));
        }

        [Test]
        public void WebGlMotionPreferenceInteropIsDefensiveAndConsumerNeutral()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(ViewerNavigationAnimationPolicy).Assembly);
            Assert.That(package, Is.Not.Null);
            string plugin = File.ReadAllText(
                Path.Combine(
                    package.resolvedPath,
                    "Runtime",
                    "Plugins",
                    "WebGL",
                    "DeucarianViewerNavigation.jslib"));

            StringAssert.Contains(
                "DeucarianViewerNavigationPrefersReducedMotion",
                plugin);
            StringAssert.Contains(
                "(prefers-reduced-motion: reduce)",
                plugin);
            StringAssert.Contains(
                "typeof window.matchMedia !== \"function\"",
                plugin);
            StringAssert.Contains("catch (error)", plugin);
            StringAssert.DoesNotContain("Report", plugin);
            StringAssert.DoesNotContain("Activity", plugin);
        }

        [Test]
        public void MeshBoundsPolicyIncludesInactiveMeshesAndIgnoresOtherRenderers()
        {
            var strategy = new ViewerNavigationMeshBoundsStrategy();
            GameObject emptySource = new GameObject("Empty Bounds Source");
            GameObject source = new GameObject("Reference Bounds Source");
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(source.transform, false);
            cube.transform.localPosition = new Vector3(4f, 2f, -3f);
            cube.transform.localScale = new Vector3(2f, 4f, 6f);
            cube.SetActive(false);
            GameObject spriteObject = new GameObject("Distant Sprite");
            spriteObject.transform.SetParent(source.transform, false);
            spriteObject.transform.localPosition = new Vector3(1000f, 0f, 0f);
            Texture2D texture = new Texture2D(1, 1);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            spriteObject.AddComponent<SpriteRenderer>().sprite = sprite;
            try
            {
                Assert.That(
                    strategy.TryGetBounds(null, out _),
                    Is.False);
                Assert.That(
                    strategy.TryGetBounds(emptySource, out _),
                    Is.False);
                Assert.That(
                    strategy.TryGetBounds(source, out Bounds bounds),
                    Is.True);

                Bounds expected = cube.GetComponent<MeshRenderer>().bounds;
                Assert.That(bounds.center, Is.EqualTo(expected.center));
                Assert.That(bounds.size, Is.EqualTo(expected.size));
            }
            finally
            {
                Object.DestroyImmediate(emptySource);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void UiInputPolicyPreservesCoordinatesPickingAndFocusedAncestors()
        {
            Assert.That(
                ViewerNavigationUiInputBlocker.ToTopLeftScreenPosition(
                    new Vector2(25f, 30f),
                    100f),
                Is.EqualTo(new Vector2(25f, 70f)));

            var panelRoot = new VisualElement();
            var picked = new VisualElement();
            var ignored = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            Assert.That(
                ViewerNavigationUiInputBlocker.IsPanelPickBlockingInput(
                    picked,
                    panelRoot),
                Is.True);
            Assert.That(
                ViewerNavigationUiInputBlocker.IsPanelPickBlockingInput(
                    panelRoot,
                    panelRoot),
                Is.False);
            Assert.That(
                ViewerNavigationUiInputBlocker.IsPanelPickBlockingInput(
                    ignored,
                    panelRoot),
                Is.False);

            var button = new Button();
            var buttonChild = new VisualElement();
            button.Add(buttonChild);
            Assert.That(
                ViewerNavigationUiInputBlocker.IsKeyboardInteractiveElement(
                    buttonChild),
                Is.True);
            Assert.That(
                ViewerNavigationUiInputBlocker.IsKeyboardInteractiveElement(
                    new Toggle()),
                Is.True);
            Assert.That(
                ViewerNavigationUiInputBlocker.IsKeyboardInteractiveElement(
                    new VisualElement()),
                Is.False);
        }

        [Test]
        public void CreateWithReferencePresetHonorsSharedAnimationPolicy()
        {
            bool shouldAnimate = false;
            ViewerNavigationSettings preset = ViewerNavigationSettings.LoadReferencePreset();
            var policy = new ViewerNavigationAnimationPolicy(() => shouldAnimate);
            GameObject root = new GameObject("Reference Preset Motion Policy Test");
            GameObject cameraObject = new GameObject("Reference Preset Motion Policy Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            try
            {
                ViewerNavigationInstaller installer =
                    ViewerNavigationInstaller.CreateWithReferencePreset(
                        root.transform,
                        camera,
                        policy);

                Assert.That(installer.Controller.MotionProfile.AnimateTransitions, Is.False);
                Assert.That(installer.Controller.MotionProfile.CalculateTransitionDuration(1f), Is.Zero);

                shouldAnimate = true;
                Assert.That(installer.Controller.MotionProfile.AnimateTransitions, Is.True);
                Assert.That(
                    installer.Controller.MotionProfile.CalculateTransitionDuration(1f),
                    Is.EqualTo(preset.CalculateTransitionDuration(1f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ViewCubeIsDisabledByDefaultAndCanBeExplicitlyEnabled()
        {
            GameObject root = new GameObject("View Cube Opt In Test");
            GameObject cameraObject = new GameObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ViewerNavigationSettings settings =
                ScriptableObject.CreateInstance<ViewerNavigationSettings>();
            try
            {
                Assert.That(settings.ShowViewCube, Is.False);

                FieldInfo field = typeof(ViewerNavigationSettings).GetField(
                    "showViewCube",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                field.SetValue(settings, true);

                ViewerNavigationInstaller installer =
                    ViewerNavigationInstaller.Create(
                        root.transform,
                        camera,
                        settings);

                Assert.That(installer.Toolbar, Is.Not.Null);
                Assert.That(installer.Toolbar.ViewCube, Is.Not.Null);

                field.SetValue(settings, false);
                installer.Toolbar.Initialize(installer.Controller, settings);

                Assert.That(installer.Toolbar.ViewCube, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void AnimationPolicyGatesPresetWithoutForkingItsTuning()
        {
            GameObject root = new GameObject("Animation Policy Test");
            GameObject cameraObject = new GameObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            var policy = new TestAnimationPolicy { ShouldAnimate = false };
            try
            {
                ViewerNavigationSettings preset =
                    ViewerNavigationSettings.LoadReferencePreset();
                ViewerNavigationInstaller installer =
                    ViewerNavigationInstaller.Create(
                        root.transform,
                        camera,
                        preset,
                        animationPolicy: policy);

                Assert.That(
                    installer.Controller.MotionProfile.AnimateTransitions,
                    Is.False);
                Assert.That(
                    installer.Controller.MotionProfile
                        .CalculateTransitionDuration(10f),
                    Is.Zero);
                Assert.That(
                    installer.Controller.MotionProfile
                        .TransitionMatchFieldOfView,
                    Is.EqualTo(preset.TransitionMatchFieldOfView));
                Assert.That(
                    installer.Controller.MotionProfile.EvaluateMovement(0.4f),
                    Is.EqualTo(preset.EvaluateMovement(0.4f)));

                policy.ShouldAnimate = true;

                Assert.That(
                    installer.Controller.MotionProfile.AnimateTransitions,
                    Is.True);
                Assert.That(
                    installer.Controller.MotionProfile
                        .CalculateTransitionDuration(10f),
                    Is.EqualTo(preset.CalculateTransitionDuration(10f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private sealed class TestAnimationPolicy :
            IViewerNavigationAnimationPolicy
        {
            public bool ShouldAnimate { get; set; }
        }
    }
}
