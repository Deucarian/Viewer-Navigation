using System;
using Deucarian.CameraNavigation.InputSystemIntegration;
using Deucarian.PointerCapture;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationInteractionGateTests
    {
        [Test]
        public void ConfiguredActionStartsNavigationWhenRelevantInputIsAllowed()
        {
            GameObject root = new GameObject("Interaction Gate Test");
            try
            {
                TestNavigationActionSource source =
                    root.AddComponent<TestNavigationActionSource>();
                ViewerNavigationInteractionGate gate =
                    root.AddComponent<ViewerNavigationInteractionGate>();
                gate.Configure(null, source);
                source.State = new DeucarianNavigationActionState(
                    new Vector2(20f, 40f),
                    DeucarianNavigationActionKinds.Pointer,
                    false,
                    false,
                    false,
                    default);
                int started = 0;
                gate.NavigationInputStarted += () => started++;

                gate.ProcessInputState();

                Assert.That(started, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BlockedPointerActionDoesNotStartNavigation()
        {
            GameObject root = new GameObject("Blocked Interaction Gate Test");
            try
            {
                TestNavigationActionSource source =
                    root.AddComponent<TestNavigationActionSource>();
                TestViewerNavigationInputBlocker blocker =
                    root.AddComponent<TestViewerNavigationInputBlocker>();
                blocker.BlockPointer = true;
                ViewerNavigationInteractionGate gate =
                    root.AddComponent<ViewerNavigationInteractionGate>();
                gate.Configure(blocker, source);
                source.State = new DeucarianNavigationActionState(
                    Vector2.one,
                    DeucarianNavigationActionKinds.Pointer,
                    false,
                    false,
                    true,
                    DeucarianMouseButton.Middle);
                int started = 0;
                gate.NavigationInputStarted += () => started++;

                gate.ProcessInputState();

                Assert.That(started, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BlockedKeyboardActionDoesNotStartNavigation()
        {
            GameObject root = new GameObject("Blocked Keyboard Gate Test");
            try
            {
                TestNavigationActionSource source =
                    root.AddComponent<TestNavigationActionSource>();
                TestViewerNavigationInputBlocker blocker =
                    root.AddComponent<TestViewerNavigationInputBlocker>();
                blocker.BlockKeyboard = true;
                ViewerNavigationInteractionGate gate =
                    root.AddComponent<ViewerNavigationInteractionGate>();
                gate.Configure(blocker, source);
                source.State = new DeucarianNavigationActionState(
                    Vector2.zero,
                    DeucarianNavigationActionKinds.Keyboard,
                    false,
                    false,
                    false,
                    default);
                int started = 0;
                gate.NavigationInputStarted += () => started++;

                gate.ProcessInputState();

                Assert.That(started, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void MixedActionStartsWhenEitherActiveChannelIsAllowed(
            bool blockPointer,
            bool blockKeyboard)
        {
            GameObject root = new GameObject("Mixed Interaction Gate Test");
            try
            {
                TestNavigationActionSource source =
                    root.AddComponent<TestNavigationActionSource>();
                TestViewerNavigationInputBlocker blocker =
                    root.AddComponent<TestViewerNavigationInputBlocker>();
                blocker.BlockPointer = blockPointer;
                blocker.BlockKeyboard = blockKeyboard;
                ViewerNavigationInteractionGate gate =
                    root.AddComponent<ViewerNavigationInteractionGate>();
                gate.Configure(blocker, source);
                source.State = new DeucarianNavigationActionState(
                    Vector2.one,
                    DeucarianNavigationActionKinds.Pointer |
                    DeucarianNavigationActionKinds.Keyboard,
                    false,
                    false,
                    false,
                    default);
                int started = 0;
                gate.NavigationInputStarted += () => started++;

                gate.ProcessInputState();

                Assert.That(started, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MixedActionDoesNotStartWhenBothActiveChannelsAreBlocked()
        {
            GameObject root = new GameObject("Fully Blocked Interaction Gate Test");
            try
            {
                TestNavigationActionSource source =
                    root.AddComponent<TestNavigationActionSource>();
                TestViewerNavigationInputBlocker blocker =
                    root.AddComponent<TestViewerNavigationInputBlocker>();
                blocker.BlockPointer = true;
                blocker.BlockKeyboard = true;
                ViewerNavigationInteractionGate gate =
                    root.AddComponent<ViewerNavigationInteractionGate>();
                gate.Configure(blocker, source);
                source.State = new DeucarianNavigationActionState(
                    Vector2.one,
                    DeucarianNavigationActionKinds.Pointer |
                    DeucarianNavigationActionKinds.Keyboard,
                    false,
                    false,
                    false,
                    default);
                int started = 0;
                gate.NavigationInputStarted += () => started++;

                gate.ProcessInputState();

                Assert.That(started, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TopDownBlocksConfiguredOrbitRotateAction()
        {
            GameObject root = new GameObject("Top Down Rotate Gate Test");
            try
            {
                TestNavigationActionSource source =
                    root.AddComponent<TestNavigationActionSource>();
                source.OrbitRotatePressed = true;
                ViewerNavigationInteractionGate gate =
                    root.AddComponent<ViewerNavigationInteractionGate>();
                gate.Configure(null, source);
                gate.SetNavigationState(ViewerNavigationMode.Orbit, true);

                Assert.That(gate.IsPointerInputBlocked(Vector2.one), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CaptureRequiredActionStartsOnlyAfterCaptureIsAccepted()
        {
            GameObject root = new GameObject("Capture Acceptance Gate Test");
            try
            {
                TestNavigationActionSource source =
                    root.AddComponent<TestNavigationActionSource>();
                ViewerNavigationInteractionGate gate =
                    root.AddComponent<ViewerNavigationInteractionGate>();
                var capture = new TestPointerCaptureSession
                {
                    RequestSucceeds = false
                };
                gate.Configure(null, source);
                gate.SetPointerCaptureSessionForTesting(capture);
                source.SetCaptureAction(
                    DeucarianMouseButton.Middle,
                    isPressed: true,
                    started: true);
                int started = 0;
                gate.NavigationInputStarted += () => started++;

                gate.ProcessInputState();

                Assert.That(capture.RequestCount, Is.EqualTo(1));
                Assert.That(started, Is.Zero);
                Assert.That(gate.IsPointerInputBlocked(Vector2.one), Is.True);

                source.SetCaptureAction(
                    DeucarianMouseButton.Middle,
                    isPressed: false,
                    started: false);
                gate.ProcessInputState();
                Assert.That(gate.IsPointerInputBlocked(Vector2.one), Is.False);

                capture.RequestSucceeds = true;
                source.SetCaptureAction(
                    DeucarianMouseButton.Middle,
                    isPressed: true,
                    started: true);
                gate.ProcessInputState();

                Assert.That(started, Is.Zero);
                Assert.That(capture.State, Is.EqualTo(
                    DeucarianPointerCaptureState.Requested));
                Assert.That(gate.IsPointerInputBlocked(Vector2.one), Is.True);

                capture.RaiseState(DeucarianPointerCaptureState.Active);

                Assert.That(started, Is.EqualTo(1));
                Assert.That(gate.IsPointerInputBlocked(Vector2.one), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(DeucarianPointerCaptureState.Rejected)]
        [TestCase(DeucarianPointerCaptureState.Lost)]
        public void RejectedOrLostCaptureBlocksHeldGestureUntilRelease(
            DeucarianPointerCaptureState terminalState)
        {
            GameObject root = new GameObject("Capture Loss Gate Test");
            try
            {
                TestNavigationActionSource source =
                    root.AddComponent<TestNavigationActionSource>();
                TestViewerNavigationInputBlocker blocker =
                    root.AddComponent<TestViewerNavigationInputBlocker>();
                ViewerNavigationInteractionGate gate =
                    root.AddComponent<ViewerNavigationInteractionGate>();
                var capture = new TestPointerCaptureSession
                {
                    RequestSucceeds = true
                };
                gate.Configure(blocker, source);
                gate.SetPointerCaptureSessionForTesting(capture);
                source.SetCaptureAction(
                    DeucarianMouseButton.Right,
                    isPressed: true,
                    started: true);
                gate.ProcessInputState();
                capture.RaiseState(DeucarianPointerCaptureState.Active);
                blocker.BlockPointer = true;

                Assert.That(
                    gate.IsPointerInputBlocked(Vector2.one),
                    Is.False,
                    "An accepted capture owns the drag even if the locked pointer overlaps UI.");

                capture.RaiseState(terminalState);

                Assert.That(gate.IsPointerInputBlocked(Vector2.one), Is.True);
                source.SetCaptureAction(
                    DeucarianMouseButton.Right,
                    isPressed: false,
                    started: false);
                blocker.BlockPointer = false;
                gate.ProcessInputState();
                Assert.That(gate.IsPointerInputBlocked(Vector2.one), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RemappedCaptureButtonIsBlockedBeforeCaptureRequest()
        {
            GameObject root = new GameObject("Remapped Capture Gate Test");
            try
            {
                TestNavigationActionSource source =
                    root.AddComponent<TestNavigationActionSource>();
                TestViewerNavigationInputBlocker blocker =
                    root.AddComponent<TestViewerNavigationInputBlocker>();
                blocker.BlockPointer = true;
                ViewerNavigationInteractionGate gate =
                    root.AddComponent<ViewerNavigationInteractionGate>();
                var capture = new TestPointerCaptureSession
                {
                    RequestSucceeds = true
                };
                gate.Configure(blocker, source);
                gate.SetPointerCaptureSessionForTesting(capture);
                source.SetCaptureAction(
                    DeucarianMouseButton.Middle,
                    isPressed: true,
                    started: true);

                gate.ProcessInputState();

                Assert.That(capture.RequestCount, Is.Zero);
                Assert.That(gate.IsPointerInputBlocked(Vector2.one), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    internal sealed class TestNavigationActionSource :
        MonoBehaviour,
        IDeucarianNavigationActionStateSource,
        IDeucarianCaptureRequiredActionStateSource
    {
        internal DeucarianNavigationActionState State { get; set; } =
            DeucarianNavigationActionState.None;
        internal bool OrbitRotatePressed { get; set; }
        internal bool CaptureRequiredPressed { get; set; }
        internal bool CaptureButtonPressed { get; set; }
        internal DeucarianMouseButton PressedCaptureButton { get; set; }

        internal void SetCaptureAction(
            DeucarianMouseButton button,
            bool isPressed,
            bool started)
        {
            CaptureRequiredPressed = isPressed;
            CaptureButtonPressed = isPressed;
            PressedCaptureButton = button;
            State = new DeucarianNavigationActionState(
                Vector2.one,
                started
                    ? DeucarianNavigationActionKinds.Pointer
                    : DeucarianNavigationActionKinds.None,
                !isPressed,
                false,
                started,
                button);
        }

        public DeucarianNavigationActionState ReadActionState(
            DeucarianInputSystemNavigationMode mode,
            bool isTopDown)
        {
            return State;
        }

        public bool IsButtonPressed(DeucarianMouseButton button)
        {
            return (CaptureButtonPressed && PressedCaptureButton == button) ||
                   (State.CaptureRequested && State.CaptureButton == button);
        }

        public bool IsOrbitRotatePressed()
        {
            return OrbitRotatePressed;
        }

        public bool IsCaptureRequiredPointerActionPressed(
            DeucarianInputSystemNavigationMode mode,
            bool isTopDown)
        {
            return CaptureRequiredPressed;
        }
    }

    internal sealed class TestViewerNavigationInputBlocker :
        MonoBehaviour,
        IViewerNavigationInputBlocker
    {
        internal bool BlockPointer { get; set; }
        internal bool BlockKeyboard { get; set; }

        public bool IsPointerInputBlocked(Vector2 screenPosition) => BlockPointer;
        public bool IsKeyboardInputBlocked() => BlockKeyboard;
    }

    internal sealed class TestPointerCaptureSession :
        IViewerPointerCaptureSession
    {
        public event EventHandler<DeucarianPointerCaptureStateChangedEventArgs>
            StateChanged;

        internal bool RequestSucceeds { get; set; }
        internal int RequestCount { get; private set; }
        internal int ReleaseCount { get; private set; }
        public DeucarianPointerCaptureState State { get; private set; } =
            DeucarianPointerCaptureState.Idle;

        public bool RequestCapture(object owner)
        {
            RequestCount++;
            RaiseState(
                RequestSucceeds
                    ? DeucarianPointerCaptureState.Requested
                    : DeucarianPointerCaptureState.Rejected);
            return RequestSucceeds;
        }

        public bool ReleaseCapture(object owner)
        {
            ReleaseCount++;
            RaiseState(DeucarianPointerCaptureState.Idle);
            return true;
        }

        public void UpdateInputRearming(
            bool isInputNeutral,
            bool hasNewCaptureAction)
        {
        }

        public void NotifyEscapePressed()
        {
            RaiseState(DeucarianPointerCaptureState.Idle);
        }

        internal void RaiseState(DeucarianPointerCaptureState state)
        {
            DeucarianPointerCaptureState previous = State;
            State = state;
            StateChanged?.Invoke(
                this,
                new DeucarianPointerCaptureStateChangedEventArgs(
                    previous,
                    state,
                    state == DeucarianPointerCaptureState.Rejected
                        ? DeucarianPointerCaptureReleaseReason.BrowserRejected
                        : state == DeucarianPointerCaptureState.Lost
                            ? DeucarianPointerCaptureReleaseReason.LockLost
                            : DeucarianPointerCaptureReleaseReason.None,
                    "Test capture state."));
        }
    }
}
