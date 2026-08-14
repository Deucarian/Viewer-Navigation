using Deucarian.CameraNavigation;
using NUnit.Framework;
using UnityEngine;

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
            Assert.That(preset.ShowViewCube, Is.True);
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
                Assert.That(installer.Toolbar.ViewCube, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
