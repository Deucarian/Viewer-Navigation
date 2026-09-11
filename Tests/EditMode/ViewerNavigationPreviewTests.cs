using Deucarian.CameraNavigation;
using Deucarian.CameraNavigation.InputSystemIntegration;
using Deucarian.ViewerNavigation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationPreviewTests
    {
        [Test]
        public void PreviewUsesTheActualControllerWithoutEnablingGlobalInputSources()
        {
            using (var preview = new ViewerNavigationPreview(() => null))
            {
                Assert.IsTrue(EditorSceneManager.IsPreviewScene(preview.Camera.gameObject.scene));
                Assert.IsTrue(preview.Controller.UsesManualUpdates);
                Assert.IsFalse(preview.Camera.GetComponent<DeucarianInputSystemCameraNavigationRig>().enabled);
                Assert.IsFalse(preview.Camera.GetComponent<DeucarianOrbitInputSystemSource>().enabled);
                Assert.IsFalse(preview.Camera.GetComponent<DeucarianFlyInputSystemSource>().enabled);
                Assert.IsFalse(preview.Camera.GetComponent<DeucarianInputSystemNavigationActionSource>().enabled);
            }
        }

        [Test]
        public void FrameRetargetAndResetStartAtTheCurrentPoseAndFinishThroughExplicitTicks()
        {
            using (var preview = new ViewerNavigationPreview(() => null))
            {
                var original = preview.Camera.transform.position;
                preview.Run(() => preview.Controller.FrameReference());
                Assert.IsTrue(preview.Controller.IsTransitioning);
                Assert.AreEqual(original, preview.Camera.transform.position);
                for (int i = 0; i < 4; i++) preview.Update(.02f);
                var intermediate = preview.Camera.transform.position;
                Assert.Greater(Vector3.Distance(original, intermediate), .001f);
                preview.Run(() => preview.Controller.ReturnToOrigin());
                Assert.AreEqual(intermediate, preview.Camera.transform.position);
                for (int i = 0; i < 100; i++) preview.Update(.02f);
                Assert.IsFalse(preview.Controller.IsTransitioning);
                Assert.That(Vector3.Distance(original, preview.Camera.transform.position), Is.LessThan(.0001f));
            }
        }

        [Test]
        public void ChangedMotionValuesApplyWithoutRecreatingThePreviewOrRestartingTheMove()
        {
            var settings = ScriptableObject.CreateInstance<ViewerNavigationSettings>();
            try
            {
                using (var preview = new ViewerNavigationPreview(() => settings))
                {
                    var camera = preview.Camera;
                    preview.Run(() => preview.Controller.NavigateToFace(ViewerViewFace.Right));
                    preview.Update(.02f);
                    using (var serialized = new SerializedObject(settings))
                    {
                        serialized.FindProperty("movementCurve").animationCurveValue = AnimationCurve.EaseInOut(0, 0, 1, 1);
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    preview.Update(.02f);
                    Assert.AreSame(camera, preview.Camera);
                    Assert.IsTrue(preview.Controller.IsTransitioning);
                    for (int i = 0; i < 100; i++) preview.Update(.02f);
                    Assert.IsFalse(preview.Controller.IsTransitioning);
                }
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void PausingCancelsTransitionsAndLeavesTheCurrentPoseStable()
        {
            using (var preview = new ViewerNavigationPreview(() => null))
            {
                preview.Run(() => preview.Controller.NavigateToFace(ViewerViewFace.Right));
                preview.Update(.02f); preview.Update(.02f);
                preview.Pause();
                var captured = DeucarianCameraPose.Capture(preview.Camera);
                for (int i = 0; i < 20; i++) preview.Update(.02f);
                Assert.IsFalse(preview.Controller.IsTransitioning);
                Assert.AreEqual(captured.Position, preview.Camera.transform.position);
                Assert.AreEqual(captured.Rotation, preview.Camera.transform.rotation);
            }
        }

        [Test]
        public void MidTransitionRotationCancelsWithoutUnsolicitedRadialMovement()
        {
            using (var preview = new ViewerNavigationPreview(() => null))
            {
                preview.Run(() => preview.Controller.FrameReference());
                for (int i = 0; i < 4; i++) preview.Update(.02f);
                Assert.IsTrue(preview.Controller.IsTransitioning);
                float distance = Vector3.Distance(preview.Camera.transform.position, preview.Navigation.Pivot);
                preview.Navigation.ApplyInput(
                    new DeucarianOrbitCameraInput(new Vector2(.5f, 0), Vector2.zero, 0, false),
                    DeucarianFlyCameraInput.None, .02f);
                Assert.IsFalse(preview.Controller.IsTransitioning);
                Assert.That(Vector3.Distance(preview.Camera.transform.position, preview.Navigation.Pivot), Is.EqualTo(distance).Within(.0001f));
                var afterRotation = preview.Camera.transform.position;
                for (int i = 0; i < 20; i++) preview.Update(.02f);
                Assert.That(Vector3.Distance(preview.Camera.transform.position, afterRotation), Is.LessThan(.0001f));
            }
        }

        [Test]
        public void ReplacingDependenciesOnTheSameProfileRefreshesControllerWithoutReplacingCamera()
        {
            var settings = ScriptableObject.CreateInstance<ViewerNavigationSettings>();
            var controls = ScriptableObject.CreateInstance<DeucarianCameraNavigationControls>();
            var framing = ScriptableObject.CreateInstance<DeucarianCameraFramingSettings>();
            var input = ScriptableObject.CreateInstance<DeucarianInputSystemNavigationSettings>();
            try
            {
                using (var preview = new ViewerNavigationPreview(() => settings))
                {
                    var camera = preview.Camera;
                    preview.Run(() => preview.Controller.FrameReference());
                    preview.Update(.02f);
                    using (var serialized = new SerializedObject(settings))
                    {
                        serialized.FindProperty("showToolbar").boolValue = false;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    preview.Update(.02f);
                    Assert.IsTrue(preview.Controller.IsTransitioning, "A presentation edit must not restart navigation.");
                    var pose = DeucarianCameraPose.Capture(camera);
                    using (var serialized = new SerializedObject(settings))
                    {
                        serialized.FindProperty("controls").objectReferenceValue = controls;
                        serialized.FindProperty("framingSettings").objectReferenceValue = framing;
                        serialized.FindProperty("inputSettings").objectReferenceValue = input;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    preview.Update(.02f);
                    Assert.AreSame(camera, preview.Camera);
                    Assert.AreEqual(pose.Position, camera.transform.position);
                    Assert.AreSame(controls, preview.Controller.Controls);
                    Assert.AreSame(framing, preview.Controller.FramingSettings);
                    Assert.AreSame(input, camera.GetComponent<DeucarianInputSystemCameraNavigationRig>().InputSettings);
                }
            }
            finally
            {
                Object.DestroyImmediate(settings); Object.DestroyImmediate(controls);
                Object.DestroyImmediate(framing); Object.DestroyImmediate(input);
            }
        }

        [Test]
        public void TopDownCommandDoesNotSwitchProjectionBeforeItsFirstTick()
        {
            using (var preview = new ViewerNavigationPreview(() => null))
            {
                var captured = DeucarianCameraPose.Capture(preview.Camera);
                preview.Run(() => preview.Controller.SetTopDown(true));
                Assert.AreEqual(captured.Position, preview.Camera.transform.position);
                Assert.IsFalse(preview.Camera.orthographic);
                for (int i = 0; i < 100; i++) preview.Update(.02f);
                Assert.IsTrue(preview.Camera.orthographic);
                Assert.IsTrue(preview.Controller.IsTopDown);
                Assert.IsFalse(preview.Controller.IsTransitioning);
            }
        }
    }
}
