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
        public void ToolbarHasExplicitCenteredWidthForRuntimePanels()
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
                    Is.EqualTo(-toolbar.style.width.value.value * 0.5f));
                Assert.That(toolbar.Query<Button>().ToList(), Has.Count.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
