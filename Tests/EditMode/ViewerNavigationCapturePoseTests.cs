using Deucarian.CameraNavigation;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationCapturePoseTests
    {
        [Test]
        public void RestoresExactPoseWithoutMovingToFitReferenceBounds()
        {
            var root = new GameObject("Capture pose test");
            try
            {
                var camera = root.AddComponent<Camera>();
                var navigation = root.AddComponent<ViewerNavigationController>();
                navigation.Initialize(camera);
                navigation.SetReferenceBounds(new Bounds(Vector3.zero, Vector3.one * 1000f), Vector3.zero);
                navigation.CaptureOrigin();
                var target = new DeucarianCameraPose(new Vector3(7f, 2f, 9f),
                    Quaternion.Euler(-12f, 35f, 0f), false, 1f, 47f);
                Assert.That(navigation.TryRestorePose(target, target.Position + target.Rotation * Vector3.forward,
                    out _, false), Is.True);
                Assert.That(camera.transform.position, Is.EqualTo(target.Position));
                Assert.That(Quaternion.Angle(camera.transform.rotation, target.Rotation), Is.LessThan(0.001f));
                Assert.That(camera.fieldOfView, Is.EqualTo(47f));
                Assert.That(camera.orthographic, Is.False);
                Assert.That(navigation.ReturnToOrigin(false), Is.True);
                Assert.That(camera.transform.position, Is.EqualTo(Vector3.zero));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void NewCaptureSupersedesThePreviousTransitionAndManualCancellationStopsIt()
        {
            var root = new GameObject("Capture transition test");
            try
            {
                var camera = root.AddComponent<Camera>();
                var navigation = root.AddComponent<ViewerNavigationController>();
                navigation.Initialize(camera, navigationMotionProfile: new LinearMotion());
                navigation.SetManualUpdates(true);
                var first = new DeucarianCameraPose(Vector3.one * 10f, Quaternion.Euler(0, 30, 0), false, 1, 60);
                var second = new DeucarianCameraPose(Vector3.one * -5f, Quaternion.Euler(0, -45, 0), false, 1, 47);
                Assert.That(navigation.TryRestorePose(first, Vector3.zero, out _), Is.True);
                navigation.Tick(0.2f);
                Assert.That(navigation.TryRestorePose(second, Vector3.zero, out _), Is.True);
                for (int i = 0; i < 30; i++) navigation.Tick(0.1f);
                Assert.That(camera.transform.position, Is.EqualTo(second.Position));
                Assert.That(Quaternion.Angle(camera.transform.rotation, second.Rotation), Is.LessThan(0.001f));
                Assert.That(navigation.TryRestorePose(first, Vector3.zero, out _), Is.True);
                navigation.Tick(0.2f);
                Assert.That(navigation.CancelTransition(), Is.True);
                Vector3 stopped = camera.transform.position;
                for (int i = 0; i < 30; i++) navigation.Tick(0.1f);
                Assert.That(camera.transform.position, Is.EqualTo(stopped));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private sealed class LinearMotion : IViewerNavigationMotionProfile
        {
            public bool AnimateTransitions => true;
            public float TransitionMatchFieldOfView => 0.1f;
            public float EvaluateMovement(float time) => time;
            public float EvaluateRotation(float time) => time;
        }

        [Test]
        public void RejectsNonFinitePoseWithoutChangingCamera()
        {
            var root = new GameObject("Invalid capture pose test");
            try
            {
                var camera = root.AddComponent<Camera>();
                var navigation = root.AddComponent<ViewerNavigationController>();
                navigation.Initialize(camera);
                var target = new DeucarianCameraPose(new Vector3(float.NaN, 0f, 0f),
                    Quaternion.identity, false, 1f, 60f);
                Assert.That(navigation.TryRestorePose(target, Vector3.zero, out _), Is.False);
                Assert.That(camera.transform.position, Is.EqualTo(Vector3.zero));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
