using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator NewFaceActionSupersedesActiveTransition()
        {
            GameObject root = new GameObject("Navigation");
            GameObject cameraObject = new GameObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 2f, -10f);
            ViewerNavigationController controller =
                root.AddComponent<ViewerNavigationController>();
            controller.Initialize(
                camera,
                navigationMotionProfile: new TestMotionProfile(0.04f));
            controller.SetReferenceBounds(
                new Bounds(Vector3.zero, Vector3.one * 4f),
                Vector3.zero);

            controller.NavigateToFace(ViewerViewFace.Right);
            yield return null;
            controller.NavigateToFace(ViewerViewFace.Left);

            float deadline = Time.realtimeSinceStartup + 1f;
            while (controller.IsTransitioning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(controller.IsTransitioning, Is.False);
            Assert.That(camera.transform.position.x, Is.LessThan(0f));
            Assert.That(camera.orthographic, Is.False);
            Object.Destroy(root);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator DisablingControllerCancelsActiveTransition()
        {
            GameObject root = new GameObject("Navigation");
            GameObject cameraObject = new GameObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            ViewerNavigationController controller =
                root.AddComponent<ViewerNavigationController>();
            controller.Initialize(
                camera,
                navigationMotionProfile: new TestMotionProfile(1f));
            controller.SetReferenceBounds(
                new Bounds(Vector3.zero, Vector3.one * 4f),
                Vector3.zero);
            controller.NavigateToFace(ViewerViewFace.Right);
            Assert.That(controller.IsTransitioning, Is.True);

            controller.enabled = false;
            yield return null;

            Assert.That(controller.IsTransitioning, Is.False);
            Object.Destroy(root);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator TopDownProjectionCommitsOnlyAfterVisibleTransition()
        {
            GameObject root = new GameObject("Navigation");
            GameObject cameraObject = new GameObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 3f, -12f);
            ViewerNavigationController controller =
                root.AddComponent<ViewerNavigationController>();
            controller.Initialize(
                camera,
                navigationMotionProfile: new TestMotionProfile(0.08f));
            controller.SetReferenceBounds(
                new Bounds(Vector3.zero, Vector3.one * 4f),
                Vector3.zero);

            Assert.That(controller.SetTopDown(true), Is.True);
            Assert.That(controller.IsTransitioning, Is.True);
            Assert.That(controller.IsTopDown, Is.False);
            Assert.That(camera.orthographic, Is.False);

            float deadline = Time.realtimeSinceStartup + 1f;
            while (controller.IsTransitioning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(controller.IsTopDown, Is.True);
            Assert.That(camera.orthographic, Is.True);
            Object.Destroy(root);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator CancelingTopDownExitLeavesProjectionAndStateConsistent()
        {
            GameObject root = new GameObject("Navigation");
            GameObject cameraObject = new GameObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 3f, -12f);
            ViewerNavigationController controller =
                root.AddComponent<ViewerNavigationController>();
            controller.Initialize(
                camera,
                navigationMotionProfile: new TestMotionProfile(0.5f));
            controller.SetReferenceBounds(
                new Bounds(Vector3.zero, Vector3.one * 4f),
                Vector3.zero);
            controller.SetTopDown(true, false);

            Assert.That(controller.SetTopDown(false), Is.True);
            Assert.That(controller.IsTransitioning, Is.True);
            Assert.That(controller.IsTopDown, Is.False);
            Assert.That(camera.orthographic, Is.False);

            Assert.That(controller.CancelTransition(), Is.True);
            yield return null;

            Assert.That(controller.IsTransitioning, Is.False);
            Assert.That(controller.IsTopDown, Is.False);
            Assert.That(camera.orthographic, Is.False);
            Object.Destroy(root);
            Object.Destroy(cameraObject);
        }

        private sealed class TestMotionProfile : IViewerNavigationMotionProfile
        {
            private readonly float duration;

            public TestMotionProfile(float duration)
            {
                this.duration = duration;
            }

            public bool AnimateTransitions => true;
            public float TransitionMatchFieldOfView => 0.1f;
            public float CalculateTransitionDuration(float distance) => duration;
            public float EvaluateMovement(float normalizedTime) => normalizedTime;
            public float EvaluateRotation(float normalizedTime) => normalizedTime;
        }
    }
}
