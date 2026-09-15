using Deucarian.CameraNavigation;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationRefocusTimingTests
    {
        [TestCase(false, 0.1f, 5f)]
        [TestCase(false, 100f, 10f)]
        [TestCase(true, 0.1f, 20f)]
        [TestCase(true, 100f, 10f)]
        public void PerActionDurationIsIndependentOfDistanceAndRetainsSensitivity(
            bool frameBounds, float distance, float sensitivity)
        {
            var root = new GameObject("Action duration");
            var controls = ScriptableObject.CreateInstance<DeucarianCameraNavigationControls>();
            try
            {
                var camera = root.AddComponent<Camera>();
                var navigation = root.AddComponent<ViewerNavigationController>();
                navigation.Initialize(camera, controls);
                navigation.SetManualUpdates(true);
                navigation.SetGlobalSensitivity(sensitivity);
                Vector3 position = Vector3.forward * distance;
                bool accepted = frameBounds
                    ? navigation.TryFrame(new DeucarianCameraFramingTarget(
                        new Bounds(position, Vector3.one), position), out _, durationSeconds: 1f)
                    : navigation.TryRestorePose(new DeucarianCameraPose(position,
                        Quaternion.identity, false, 1f, 60f), position, out _, durationSeconds: 1f);
                Assert.True(accepted);
                float duration = 10f / sensitivity;
                Advance(navigation, duration * 0.85f);
                Assert.True(navigation.IsTransitioning, "Short moves must retain the full focus duration.");
                Advance(navigation, duration * 0.2f);
                Assert.False(navigation.IsTransitioning, "Long moves must use the same duration.");

                var start = DeucarianCameraPose.Capture(camera);
                var next = new DeucarianCameraPose(start.Position + Vector3.forward,
                    start.Rotation, false, 1f, start.FieldOfView);
                Assert.True(navigation.TryRestorePose(next, next.Position, out _));
                Advance(navigation, 0.25f * 10f / sensitivity);
                Assert.False(navigation.IsTransitioning, "An action override must not leak into later navigation.");
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(controls); }
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidActionDurationDoesNotCancelAnExistingMove(float duration)
        {
            var root = new GameObject("Invalid action duration");
            try
            {
                var camera = root.AddComponent<Camera>();
                var navigation = root.AddComponent<ViewerNavigationController>();
                navigation.Initialize(camera);
                navigation.SetManualUpdates(true);
                var target = new DeucarianCameraPose(Vector3.forward * 10f,
                    Quaternion.identity, false, 1f, 60f);
                Assert.True(navigation.TryRestorePose(target, Vector3.zero, out _, durationSeconds: 1f));
                Advance(navigation, 0.2f);
                Assert.False(navigation.TryRestorePose(target, Vector3.zero, out _, durationSeconds: duration));
                Assert.True(navigation.IsTransitioning);
                Advance(navigation, 1f);
                Assert.That(camera.transform.position, Is.EqualTo(target.Position));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(true, 0f)]
        [TestCase(false, 1f)]
        public void ZeroDurationOrDisabledAnimationCommitsImmediately(bool animate, float duration)
        {
            var root = new GameObject("Immediate action duration");
            try
            {
                var camera = root.AddComponent<Camera>();
                var navigation = root.AddComponent<ViewerNavigationController>();
                navigation.Initialize(camera);
                navigation.SetManualUpdates(true);
                var target = new DeucarianCameraPose(Vector3.forward * 10f,
                    Quaternion.identity, false, 1f, 60f);
                Assert.True(navigation.TryRestorePose(target, Vector3.zero, out _, animate, duration));
                Assert.False(navigation.IsTransitioning);
                Assert.That(camera.transform.position, Is.EqualTo(target.Position));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(5f)]
        [TestCase(10f)]
        [TestCase(20f)]
        public void StationaryCaptureTurnAndReturnUseTheSameSensitivity(float sensitivity)
        {
            var root = new GameObject("Refocus timing");
            var controls = ScriptableObject.CreateInstance<DeucarianCameraNavigationControls>();
            try
            {
                var camera = root.AddComponent<Camera>();
                var navigation = root.AddComponent<ViewerNavigationController>();
                navigation.Initialize(camera, controls);
                navigation.SetManualUpdates(true);
                navigation.CaptureOrigin();
                Assert.True(navigation.SetGlobalSensitivity(sensitivity));
                var target = new DeucarianCameraPose(Vector3.zero, Quaternion.Euler(0, 90, 0), false, 1, 60);
                Assert.That(navigation.TryRestorePose(target, Vector3.forward, out _), Is.True);
                float duration = 10f / sensitivity;
                Advance(navigation, duration * 0.5f);
                Assert.That(navigation.IsTransitioning, Is.True, "A stationary turn must not snap in the minimum duration.");
                Assert.That(Quaternion.Angle(camera.transform.rotation, target.Rotation), Is.GreaterThan(1f));
                Advance(navigation, duration * 0.6f);
                Assert.That(navigation.IsTransitioning, Is.False);
                Assert.That(Quaternion.Angle(camera.transform.rotation, target.Rotation), Is.LessThan(0.001f));
                Assert.That(navigation.ReturnToOrigin(), Is.True);
                Advance(navigation, duration * 0.5f);
                Assert.That(navigation.IsTransitioning, Is.True);
                Advance(navigation, duration * 0.6f);
                Assert.That(navigation.IsTransitioning, Is.False);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(controls); }
        }

        [Test]
        public void LensOnlyRestoreUsesTransitionInsteadOfSnapping()
        {
            var root = new GameObject("Lens timing");
            var controls = ScriptableObject.CreateInstance<DeucarianCameraNavigationControls>();
            try
            {
                var camera = root.AddComponent<Camera>();
                var navigation = root.AddComponent<ViewerNavigationController>();
                navigation.Initialize(camera, controls);
                navigation.SetManualUpdates(true);
                var target = new DeucarianCameraPose(Vector3.zero, Quaternion.identity, false, 1, 105);
                navigation.TryRestorePose(target, Vector3.forward, out _);
                Advance(navigation, 0.2f);
                Assert.That(navigation.IsTransitioning, Is.True);
                Advance(navigation, 0.4f);
                Assert.That(navigation.IsTransitioning, Is.False);
                Assert.That(camera.fieldOfView, Is.EqualTo(105f));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(controls); }
        }
        private static void Advance(ViewerNavigationController navigation, float seconds)
        {
            const float step = 1f / 120f;
            for (int i = 0; i < Mathf.CeilToInt(seconds / step); i++) navigation.Tick(step);
        }
    }
}
