using Deucarian.CameraNavigation;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationRefocusTimingTests
    {
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
