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
    }
}
