using NUnit.Framework;
using UnityEngine;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationControllerTests
    {
        private GameObject root;
        private GameObject cameraObject;
        private Camera camera;
        private ViewerNavigationController controller;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Viewer Navigation Test");
            cameraObject = new GameObject("Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(
                new Vector3(3f, 4f, -12f),
                Quaternion.Euler(12f, -8f, 0f));
            controller = root.AddComponent<ViewerNavigationController>();
            controller.Initialize(camera);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void SettingReferenceBoundsDoesNotMoveCamera()
        {
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            bool orthographic = camera.orthographic;

            Assert.That(
                controller.SetReferenceBounds(
                    new Bounds(new Vector3(4f, 2f, 8f), new Vector3(10f, 5f, 6f)),
                    new Vector3(4f, 2f, 8f)),
                Is.True);

            Assert.That(camera.transform.position, Is.EqualTo(position));
            Assert.That(camera.transform.rotation, Is.EqualTo(rotation));
            Assert.That(camera.orthographic, Is.EqualTo(orthographic));
        }

        [Test]
        public void CapturedOriginRestoresPoseAndProjection()
        {
            controller.SetReferenceBounds(
                new Bounds(Vector3.zero, Vector3.one * 4f),
                Vector3.zero);
            Vector3 origin = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            controller.CaptureOrigin();
            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(Vector3.one * 100f, Quaternion.identity);

            Assert.That(controller.ReturnToOrigin(false), Is.True);

            Assert.That(camera.transform.position, Is.EqualTo(origin));
            Assert.That(camera.transform.rotation, Is.EqualTo(rotation));
            Assert.That(camera.orthographic, Is.False);
        }

        [Test]
        public void TopFaceUsesTopDownOrthographicPolicy()
        {
            controller.SetReferenceBounds(
                new Bounds(Vector3.zero, new Vector3(10f, 4f, 8f)),
                Vector3.zero);

            Assert.That(controller.NavigateToFace(ViewerViewFace.Top, false), Is.True);

            Assert.That(camera.orthographic, Is.True);
            Assert.That(controller.IsTopDown, Is.True);
            Assert.That(camera.transform.forward.y, Is.LessThan(-0.99f));
        }

        [Test]
        public void RepeatingCurrentModeIsAnIdempotentNoOp()
        {
            int events = 0;
            controller.ModeChanged += _ => events++;

            Assert.That(controller.SetNavigationMode(ViewerNavigationMode.Orbit), Is.False);
            Assert.That(controller.SetNavigationMode(ViewerNavigationMode.Fly), Is.True);
            Assert.That(controller.SetNavigationMode(ViewerNavigationMode.Fly), Is.False);
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void BeginReferenceLoadClearsBoundsAndOriginWithoutChangingMode()
        {
            controller.SetNavigationMode(ViewerNavigationMode.Fly);
            controller.SetReferenceBounds(
                new Bounds(Vector3.zero, Vector3.one * 4f),
                Vector3.zero);
            controller.CaptureOrigin();

            controller.BeginReferenceLoad();

            Assert.That(controller.Mode, Is.EqualTo(ViewerNavigationMode.Fly));
            Assert.That(controller.HasReferenceBounds, Is.False);
            Assert.That(controller.HasOrigin, Is.False);
        }

        [Test]
        public void InvalidReplacementDoesNotDestroyLastReference()
        {
            Bounds valid = new Bounds(Vector3.one, Vector3.one * 3f);
            controller.SetReferenceBounds(valid, valid.center);

            bool applied = controller.SetReferenceBounds(
                new Bounds(Vector3.zero, Vector3.zero),
                Vector3.zero);

            Assert.That(applied, Is.False);
            Assert.That(controller.HasReferenceBounds, Is.True);
            Assert.That(controller.ReferenceBounds, Is.EqualTo(valid));
        }
    }
}
