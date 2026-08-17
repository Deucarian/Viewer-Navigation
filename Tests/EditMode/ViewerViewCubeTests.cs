using Deucarian.ViewerNavigation.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerViewCubeTests
    {
        [TestCase(ViewerViewFace.Top, 0f, 1f, 0f)]
        [TestCase(ViewerViewFace.Bottom, 0f, -1f, 0f)]
        [TestCase(ViewerViewFace.Front, 0f, 0f, -1f)]
        [TestCase(ViewerViewFace.Back, 0f, 0f, 1f)]
        [TestCase(ViewerViewFace.Left, -1f, 0f, 0f)]
        [TestCase(ViewerViewFace.Right, 1f, 0f, 0f)]
        public void SixPrimaryFacesHaveCanonicalDirections(
            ViewerViewFace face,
            float x,
            float y,
            float z)
        {
            Assert.That(
                ViewerViewFacePolicy.GetDirectionFromTargetToCamera(face),
                Is.EqualTo(new Vector3(x, y, z)));
        }

        [Test]
        public void ElementCreatesInteractiveButtonForEveryFace()
        {
            ViewerViewCubeElement cube = new ViewerViewCubeElement();

            foreach (ViewerViewFace face in
                     System.Enum.GetValues(typeof(ViewerViewFace)))
            {
                Button button = cube.GetFaceButton(face);
                Assert.That(button, Is.Not.Null, face.ToString());
                Assert.That(button.pickingMode, Is.EqualTo(PickingMode.Position));
            }
        }

        [Test]
        public void SelectionRaisesExactlyOneFaceEvent()
        {
            ViewerViewCubeElement cube = new ViewerViewCubeElement();
            int calls = 0;
            ViewerViewFace selected = default;
            cube.FaceSelected += face =>
            {
                calls++;
                selected = face;
            };

            cube.SelectFace(ViewerViewFace.Right);

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(selected, Is.EqualTo(ViewerViewFace.Right));
            Assert.That(cube.ActiveFace, Is.EqualTo(ViewerViewFace.Right));
        }

        [Test]
        public void OrientationFollowsCameraWithoutChangingNavigation()
        {
            ViewerViewCubeElement cube = new ViewerViewCubeElement();

            cube.UpdateOrientation(Quaternion.identity);
            Assert.That(cube.ActiveFace, Is.EqualTo(ViewerViewFace.Front));

            cube.UpdateOrientation(Quaternion.Euler(0f, 180f, 0f));
            Assert.That(cube.ActiveFace, Is.EqualTo(ViewerViewFace.Back));
        }

        [Test]
        public void OrientationUpdatesPreserveAppliedThemePalette()
        {
            ViewerViewCubeElement cube = new ViewerViewCubeElement();
            Color surface = new Color(0.21f, 0.31f, 0.41f, 1f);
            Color text = new Color(0.91f, 0.81f, 0.71f, 1f);
            Color accent = new Color(0.61f, 0.11f, 0.31f, 1f);
            cube.ApplyPalette(surface, text, accent);

            cube.UpdateOrientation(Quaternion.Euler(0f, 180f, 0f));

            Button active = cube.GetFaceButton(ViewerViewFace.Back);
            Button inactive = cube.GetFaceButton(ViewerViewFace.Front);
            Assert.That(active.style.backgroundColor.value, Is.EqualTo(accent));
            Assert.That(active.style.color.value, Is.EqualTo(text));
            Assert.That(
                inactive.style.backgroundColor.value,
                Is.EqualTo(new Color(surface.r, surface.g, surface.b, 0.96f)));
        }
    }

    public sealed class ViewerNavigationToolbarPresenterTests
    {
        [Test]
        public void ToolbarUsesCenteredReferenceControlIslandLayout()
        {
            var gameObject = new GameObject("Viewer Navigation Toolbar Test");
            try
            {
                var presenter =
                    gameObject.AddComponent<ViewerNavigationToolbarPresenter>();
                presenter.Initialize(null);

                VisualElement toolbar = presenter.Root.Q<VisualElement>(
                    ViewerNavigationToolbarPresenter.ToolbarName);
                Assert.That(toolbar, Is.Not.Null);
                Assert.That(toolbar.style.width.value.value, Is.GreaterThan(0f));
                Assert.That(
                    toolbar.style.marginLeft.value.value,
                    Is.Zero);
                Assert.That(
                    presenter.Root.style.alignItems.value,
                    Is.EqualTo(Align.Center));
                Assert.That(
                    presenter.Root.style.justifyContent.value,
                    Is.EqualTo(Justify.FlexEnd));
                Assert.That(
                    presenter.Root.style.paddingBottom.value.value,
                    Is.GreaterThan(0f));
                Assert.That(toolbar.Query<Button>().ToList(), Has.Count.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ToolbarUsesCanonicalIconsAndTooltipsInsteadOfLetters()
        {
            var gameObject = new GameObject("Viewer Navigation Toolbar Test");
            try
            {
                var presenter =
                    gameObject.AddComponent<ViewerNavigationToolbarPresenter>();
                presenter.Initialize(null);

                AssertIconButton(
                    presenter.Root,
                    ViewerNavigationToolbarPresenter.OrbitButtonName,
                    ViewerNavigationToolbarPresenter.OrbitIconName,
                    ViewerNavigationToolbarPresenter.OrbitTooltip);
                AssertIconButton(
                    presenter.Root,
                    ViewerNavigationToolbarPresenter.FlyButtonName,
                    ViewerNavigationToolbarPresenter.FlyIconName,
                    ViewerNavigationToolbarPresenter.FlyTooltip);
                AssertIconButton(
                    presenter.Root,
                    ViewerNavigationToolbarPresenter.HomeButtonName,
                    ViewerNavigationToolbarPresenter.HomeIconName,
                    ViewerNavigationToolbarPresenter.HomeTooltip);
                AssertIconButton(
                    presenter.Root,
                    ViewerNavigationToolbarPresenter.TopDownButtonName,
                    ViewerNavigationToolbarPresenter.PerspectiveIconName,
                    ViewerNavigationToolbarPresenter.TopDownTooltip);

                Assert.That(
                    presenter.Root.Q<VisualElement>(
                        ViewerNavigationRuntimeTooltipPresenter.BubbleName),
                    Is.Not.Null);
                Assert.That(presenter.ViewCube, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TopDownStateSwapsOrthographicAndPerspectiveIcons()
        {
            var gameObject = new GameObject("Viewer Navigation Visual State Test");
            var state = new ViewerNavigationToolbarVisualState();
            try
            {
                var orbit = new Button();
                var fly = new Button();
                var home = new Button();
                var top = new Button();
                var orbitIcon = new VisualElement();
                var flyIcon = new VisualElement();
                var homeIcon = new VisualElement();
                var orthographicIcon = new VisualElement();
                var perspectiveIcon = new VisualElement();
                state.Initialize(
                    gameObject.AddComponent<ViewerNavigationToolbarPresenter>(),
                    orbit,
                    fly,
                    home,
                    top,
                    orbitIcon,
                    flyIcon,
                    homeIcon,
                    orthographicIcon,
                    perspectiveIcon);

                state.Apply(CreateSnapshot(false));
                Assert.That(
                    orthographicIcon.style.display.value,
                    Is.EqualTo(DisplayStyle.None));
                Assert.That(
                    perspectiveIcon.style.display.value,
                    Is.EqualTo(DisplayStyle.Flex));

                state.Apply(CreateSnapshot(true));
                Assert.That(
                    orthographicIcon.style.display.value,
                    Is.EqualTo(DisplayStyle.Flex));
                Assert.That(
                    perspectiveIcon.style.display.value,
                    Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                state.Dispose();
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EnterTopDownTransitionTargetsIconBeforeStateCommit()
        {
            ViewerNavigationSnapshot entering =
                new ViewerNavigationSnapshot(
                    ViewerNavigationMode.Orbit,
                    false,
                    false,
                    false,
                    true,
                    ViewerNavigationTransitionKind.EnterTopDown,
                    1);
            ViewerNavigationSnapshot exiting =
                new ViewerNavigationSnapshot(
                    ViewerNavigationMode.Orbit,
                    true,
                    false,
                    false,
                    true,
                    ViewerNavigationTransitionKind.ExitTopDown,
                    2);

            Assert.That(
                ViewerNavigationToolbarVisualState
                    .ResolvePresentedTopDown(entering),
                Is.True);
            Assert.That(
                ViewerNavigationToolbarVisualState
                    .ResolvePresentedTopDown(exiting),
                Is.False);
        }

        [Test]
        public void InteractionAnimationUsesInjectedMotionPolicyDynamically()
        {
            var gameObject = new GameObject("Toolbar Motion Policy Test");
            var state = new ViewerNavigationToolbarVisualState();
            bool shouldAnimate = false;
            try
            {
                state.Initialize(
                    gameObject.AddComponent<ViewerNavigationToolbarPresenter>(),
                    new Button(),
                    new Button(),
                    new Button(),
                    new Button(),
                    new VisualElement(),
                    new VisualElement(),
                    new VisualElement(),
                    new VisualElement(),
                    new VisualElement(),
                    () => shouldAnimate);

                Assert.That(state.ShouldAnimateInteractions, Is.False);
                shouldAnimate = true;
                Assert.That(state.ShouldAnimateInteractions, Is.True);
            }
            finally
            {
                state.Dispose();
                Object.DestroyImmediate(gameObject);
            }
        }

        private static ViewerNavigationSnapshot CreateSnapshot(bool isTopDown) =>
            new ViewerNavigationSnapshot(
                ViewerNavigationMode.Orbit,
                isTopDown,
                false,
                false,
                false,
                ViewerNavigationTransitionKind.None,
                1);

        private static void AssertIconButton(
            VisualElement root,
            string buttonName,
            string visibleIconName,
            string tooltip)
        {
            Button button = root.Q<Button>(buttonName);
            Assert.That(button, Is.Not.Null, buttonName);
            Assert.That(button.text, Is.Empty, buttonName);
            Assert.That(button.tooltip, Is.EqualTo(tooltip), buttonName);
            VisualElement icon = button.Q<VisualElement>(visibleIconName);
            Assert.That(icon, Is.Not.Null, visibleIconName);
            Assert.That(
                icon.style.backgroundImage.value.texture,
                Is.Not.Null,
                visibleIconName);
            Assert.That(
                icon.style.display.value,
                Is.EqualTo(DisplayStyle.Flex),
                visibleIconName);
        }

    }
}
