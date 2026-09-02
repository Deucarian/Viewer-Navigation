using Deucarian.CameraNavigation;
using Deucarian.CameraNavigation.InputSystemIntegration;
using Deucarian.PointerCapture;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationCommandTests
    {
        private GameObject root;
        private GameObject cameraObject;
        private Camera camera;
        private DeucarianCameraNavigationControls controls;
        private ViewerNavigationController controller;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Viewer Navigation Command Test");
            cameraObject = new GameObject("Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(
                new Vector3(3f, 4f, -12f),
                Quaternion.Euler(12f, -8f, 0f));
            controls = ScriptableObject.CreateInstance<
                DeucarianCameraNavigationControls>();
            controller = root.AddComponent<ViewerNavigationController>();
            controller.Initialize(
                camera,
                controls,
                navigationMotionProfile: new ImmediateMotionProfile());
            controller.SetReferenceBounds(
                new Bounds(Vector3.zero, Vector3.one * 8f),
                Vector3.zero);
            controller.CaptureOrigin();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(controls);
        }

        [Test]
        public void EmptyRequestsFailWithStableMessages()
        {
            Assert.That(
                controller.TryExecuteCommand(null, out string nullMessage),
                Is.False);
            Assert.That(nullMessage, Is.EqualTo("Navigation command was empty."));

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand(),
                    out string emptyMessage),
                Is.False);
            Assert.That(
                emptyMessage,
                Is.EqualTo(
                    "Navigation command did not include an action, mode, view, " +
                    "or sensitivity."));
        }

        [Test]
        public void ModeAndSensitivityMutateTheSingleSharedStateOwner()
        {
            ViewerNavigationInteractionGate gate = controller.InteractionGate;

            bool applied = controller.TryExecuteCommand(
                new ViewerNavigationCommand
                {
                    Mode = "fly",
                    Sensitivity = 0.5f,
                    GlobalSensitivity = 1.25f
                },
                out string message);

            Assert.That(applied, Is.True, message);
            Assert.That(message, Is.EqualTo("Navigation command applied."));
            Assert.That(controller.Mode, Is.EqualTo(ViewerNavigationMode.Fly));
            Assert.That(controls.GlobalSensitivity, Is.EqualTo(1.25f));
            Assert.That(controller.InteractionGate, Is.SameAs(gate));
        }

        [Test]
        public void HostCommandsDoNotInterruptAcceptedPointerCapture()
        {
            TestNavigationActionSource source =
                root.AddComponent<TestNavigationActionSource>();
            var capture = new TestPointerCaptureSession
            {
                RequestSucceeds = true
            };
            ViewerNavigationInteractionGate gate =
                controller.InteractionGate;
            gate.Configure(null, source);
            gate.SetPointerCaptureSessionForTesting(capture);
            source.SetCaptureAction(
                DeucarianMouseButton.Middle,
                isPressed: true,
                started: true);
            gate.ProcessInputState();
            capture.RaiseState(DeucarianPointerCaptureState.Active);

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand
                    {
                        Mode = "fly",
                        Sensitivity = 1.5f
                    },
                    out string message),
                Is.True,
                message);

            Assert.That(controller.InteractionGate, Is.SameAs(gate));
            Assert.That(
                capture.State,
                Is.EqualTo(DeucarianPointerCaptureState.Active));
            Assert.That(capture.ReleaseCount, Is.Zero);
            Assert.That(gate.IsPointerInputBlocked(Vector2.one), Is.False);
        }

        [TestCase("orbit", ViewerNavigationMode.Orbit)]
        [TestCase(" fly ", ViewerNavigationMode.Fly)]
        public void SupportedModesUseStableNormalization(
            string value,
            ViewerNavigationMode expected)
        {
            controller.SetNavigationMode(
                expected == ViewerNavigationMode.Orbit
                    ? ViewerNavigationMode.Fly
                    : ViewerNavigationMode.Orbit);

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand { Mode = value },
                    out string message),
                Is.True,
                message);
            Assert.That(controller.Mode, Is.EqualTo(expected));
        }

        [Test]
        public void UnsupportedModeDoesNotChangeState()
        {
            ViewerNavigationMode before = controller.Mode;

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand { Mode = "walk" },
                    out string message),
                Is.False);
            Assert.That(message, Is.EqualTo("Unsupported navigation mode: walk"));
            Assert.That(controller.Mode, Is.EqualTo(before));
        }

        [TestCase("return_to_origin")]
        [TestCase("return-origin")]
        [TestCase("origin")]
        [TestCase("reset")]
        [TestCase("reset_camera")]
        [TestCase("home")]
        public void OriginActionsPreserveTheCapturedControllerState(string action)
        {
            Vector3 origin = camera.transform.position;
            camera.transform.position = Vector3.one * 100f;

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand { Action = action },
                    out string message),
                Is.True,
                message);
            Assert.That(camera.transform.position, Is.EqualTo(origin));
        }

        [TestCase("topdown")]
        [TestCase("top")]
        [TestCase("top_view")]
        public void TopActionsUseTheAuthoritativeTopDownState(string action)
        {
            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand { Action = action },
                    out string message),
                Is.True,
                message);
            Assert.That(controller.IsTopDown, Is.True);
        }

        [TestCase("toggletopdown")]
        [TestCase("toggle_top")]
        [TestCase("toggle-view-top")]
        public void ToggleActionsUseTheAuthoritativeTopDownState(string action)
        {
            bool before = controller.IsTopDown;

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand { Action = action },
                    out string message),
                Is.True,
                message);
            Assert.That(controller.IsTopDown, Is.Not.EqualTo(before));
        }

        [TestCase("orbit", ViewerNavigationMode.Orbit)]
        [TestCase("fly", ViewerNavigationMode.Fly)]
        public void ModeActionsUseTheAuthoritativeMode(
            string action,
            ViewerNavigationMode expected)
        {
            controller.SetNavigationMode(
                expected == ViewerNavigationMode.Orbit
                    ? ViewerNavigationMode.Fly
                    : ViewerNavigationMode.Orbit);

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand { Action = action },
                    out string message),
                Is.True,
                message);
            Assert.That(controller.Mode, Is.EqualTo(expected));
        }

        [TestCase("refresh_default_pose")]
        [TestCase("capture-default-pose")]
        public void RefreshActionsReplaceTheCapturedOrigin(string action)
        {
            Vector3 expected = new Vector3(17f, 11f, -3f);
            camera.transform.position = expected;

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand { Action = action },
                    out string message),
                Is.True,
                message);
            camera.transform.position = Vector3.zero;
            controller.ReturnToOrigin(false);
            Assert.That(camera.transform.position, Is.EqualTo(expected));
        }

        [TestCase("top", 0f, 1f, 0f)]
        [TestCase("top_down", 0f, 1f, 0f)]
        [TestCase("bottom", 0f, -1f, 0f)]
        [TestCase("front", 0f, 0f, -1f)]
        [TestCase("back", 0f, 0f, 1f)]
        [TestCase("left", -1f, 0f, 0f)]
        [TestCase("right", 1f, 0f, 0f)]
        [TestCase("front_left_top", -1f, 1f, -1f)]
        [TestCase("front-right-top", 1f, 1f, -1f)]
        [TestCase("back left top", -1f, 1f, 1f)]
        [TestCase("backrighttop", 1f, 1f, 1f)]
        public void NamedViewsAreAcceptedWithoutReplacingReferenceState(
            string view,
            float expectedX,
            float expectedY,
            float expectedZ)
        {
            Bounds before = controller.ReferenceBounds;
            Vector3 pivot = controller.Pivot;

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand { View = view },
                    out string message),
                Is.True,
                message);
            Assert.That(controller.ReferenceBounds, Is.EqualTo(before));
            Assert.That(controller.Pivot, Is.EqualTo(pivot));
            Vector3 actualDirection =
                (camera.transform.position - pivot).normalized;
            var expectedDirection = new Vector3(
                expectedX,
                expectedY,
                expectedZ).normalized;
            Assert.That(
                Vector3.Angle(actualDirection, expectedDirection),
                Is.LessThan(0.1f),
                "Named view must preserve the established target-to-camera " +
                "direction mapping.");
        }

        [Test]
        public void UnsupportedActionAndViewFailWithStableMessages()
        {
            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand { Action = "spin" },
                    out string actionMessage),
                Is.False);
            Assert.That(
                actionMessage,
                Is.EqualTo("Unsupported navigation action: spin"));

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand { View = "north_east" },
                    out string viewMessage),
                Is.False);
            Assert.That(
                viewMessage,
                Is.EqualTo("Unsupported navigation view: north_east"));
        }

        [Test]
        public void LaterInvalidViewPreservesEarlierEstablishedMutations()
        {
            Vector3 origin = camera.transform.position;
            camera.transform.position = Vector3.one * 100f;

            bool applied = controller.TryExecuteCommand(
                new ViewerNavigationCommand
                {
                    GlobalSensitivity = 2.25f,
                    Mode = "fly",
                    Action = "home",
                    View = "north_east"
                },
                out string message);

            Assert.That(applied, Is.False);
            Assert.That(
                message,
                Is.EqualTo("Unsupported navigation view: north_east"));
            Assert.That(controls.GlobalSensitivity, Is.EqualTo(2.25f));
            Assert.That(controller.Mode, Is.EqualTo(ViewerNavigationMode.Fly));
            Assert.That(camera.transform.position, Is.EqualTo(origin));
        }

        [Test]
        public void InvalidModeStopsAfterTheEstablishedSensitivityStage()
        {
            ViewerNavigationMode modeBefore = controller.Mode;

            bool applied = controller.TryExecuteCommand(
                new ViewerNavigationCommand
                {
                    Sensitivity = 1.75f,
                    Mode = "walk",
                    Action = "fly"
                },
                out string message);

            Assert.That(applied, Is.False);
            Assert.That(message, Is.EqualTo("Unsupported navigation mode: walk"));
            Assert.That(controls.GlobalSensitivity, Is.EqualTo(1.75f));
            Assert.That(controller.Mode, Is.EqualTo(modeBefore));
        }

        [Test]
        public void NonFiniteSensitivityIsRejectedWithoutMutatingState()
        {
            controls.GlobalSensitivity = 0.75f;
            ViewerNavigationMode originalMode = controller.Mode;

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand
                    {
                        Sensitivity = float.NaN,
                        Mode = "fly"
                    },
                    out string nanMessage),
                Is.False);
            Assert.That(
                nanMessage,
                Is.EqualTo(
                    "Navigation sensitivity must be a finite number."));
            Assert.That(controls.GlobalSensitivity, Is.EqualTo(0.75f));
            Assert.That(controller.Mode, Is.EqualTo(originalMode));

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand
                    {
                        GlobalSensitivity = float.PositiveInfinity
                    },
                    out string infinityMessage),
                Is.False);
            Assert.That(
                infinityMessage,
                Is.EqualTo(
                    "Navigation sensitivity must be a finite number."));
            Assert.That(controls.GlobalSensitivity, Is.EqualTo(0.75f));
            Assert.That(controller.Mode, Is.EqualTo(originalMode));
        }

        [Test]
        public void SensitivityWithoutAvailableControlsRemainsHandled()
        {
            controller.Initialize(
                camera,
                navigationControls: null,
                navigationMotionProfile: new ImmediateMotionProfile());
            Assert.That(controller.Controls, Is.Null);

            Assert.That(
                controller.TryExecuteCommand(
                    new ViewerNavigationCommand { Sensitivity = 3f },
                    out string message),
                Is.True,
                message);
            Assert.That(message, Is.EqualTo("Navigation command applied."));
        }

        private sealed class ImmediateMotionProfile :
            IViewerNavigationMotionProfile
        {
            public bool AnimateTransitions => false;
            public float TransitionMatchFieldOfView => 0.1f;
            public float CalculateTransitionDuration(float distance) => 0f;
            public float EvaluateMovement(float normalizedTime) =>
                Mathf.Clamp01(normalizedTime);
            public float EvaluateRotation(float normalizedTime) =>
                Mathf.Clamp01(normalizedTime);
        }
    }
}
