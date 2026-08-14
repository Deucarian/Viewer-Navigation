using Deucarian.CameraNavigation.InputSystemIntegration;
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
    }

    internal sealed class TestNavigationActionSource :
        MonoBehaviour,
        IDeucarianNavigationActionStateSource
    {
        internal DeucarianNavigationActionState State { get; set; } =
            DeucarianNavigationActionState.None;
        internal bool OrbitRotatePressed { get; set; }

        public DeucarianNavigationActionState ReadActionState(
            DeucarianInputSystemNavigationMode mode,
            bool isTopDown)
        {
            return State;
        }

        public bool IsButtonPressed(DeucarianMouseButton button)
        {
            return State.CaptureRequested && State.CaptureButton == button;
        }

        public bool IsOrbitRotatePressed()
        {
            return OrbitRotatePressed;
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
}
