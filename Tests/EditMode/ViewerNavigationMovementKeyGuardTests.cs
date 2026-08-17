using System;
using System.Collections;
using System.Reflection;
using Deucarian.ViewerNavigation.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationMovementKeyGuardTests
    {
        private static readonly KeyCode[] MovementKeys =
        {
            KeyCode.W,
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow,
            KeyCode.Q,
            KeyCode.E,
            KeyCode.PageUp,
            KeyCode.PageDown
        };

        private static readonly KeyCode[] AccessibilityKeys =
        {
            KeyCode.Tab,
            KeyCode.Space,
            KeyCode.Return,
            KeyCode.KeypadEnter,
            KeyCode.Escape
        };

        [TestCaseSource(nameof(MovementKeys))]
        public void ClassificationIncludesEveryConfiguredMovementKey(
            KeyCode keyCode)
        {
            Assert.That(
                ViewerNavigationMovementKeyGuard.IsMovementKey(keyCode),
                Is.True);
        }

        [TestCaseSource(nameof(AccessibilityKeys))]
        public void ClassificationExcludesAccessibilityKeys(KeyCode keyCode)
        {
            Assert.That(
                ViewerNavigationMovementKeyGuard.IsMovementKey(keyCode),
                Is.False);
        }

        [TestCaseSource(nameof(MovementKeys))]
        public void MovementKeyDownAndUpNeverReachUiControls(KeyCode keyCode)
        {
            using (PanelFixture fixture = PanelFixture.Create())
            using (IDisposable binding =
                   ViewerNavigationMovementKeyGuard.Bind(fixture.Root))
            {
                int keyDownDeliveries = 0;
                int keyUpDeliveries = 0;
                fixture.Target.RegisterCallback<KeyDownEvent>(
                    _ => keyDownDeliveries++);
                fixture.Target.RegisterCallback<KeyUpEvent>(
                    _ => keyUpDeliveries++);

                using (KeyDownEvent keyDown = KeyDownEvent.GetPooled(
                           '\0',
                           keyCode,
                           EventModifiers.None))
                {
                    keyDown.target = fixture.Target;
                    fixture.Target.SendEvent(keyDown);
                    Assert.That(keyDownDeliveries, Is.Zero);
                    Assert.That(keyDown.isPropagationStopped, Is.True);
                    Assert.That(
                        keyDown.isImmediatePropagationStopped,
                        Is.True);
                }

                using (KeyUpEvent keyUp = KeyUpEvent.GetPooled(
                           '\0',
                           keyCode,
                           EventModifiers.None))
                {
                    keyUp.target = fixture.Target;
                    fixture.Target.SendEvent(keyUp);
                    Assert.That(keyUpDeliveries, Is.Zero);
                    Assert.That(keyUp.isPropagationStopped, Is.True);
                    Assert.That(
                        keyUp.isImmediatePropagationStopped,
                        Is.True);
                }
            }
        }

        [TestCaseSource(nameof(AccessibilityKeys))]
        public void AccessibilityKeysStillReachUiControls(KeyCode keyCode)
        {
            using (PanelFixture fixture = PanelFixture.Create())
            using (IDisposable binding =
                   ViewerNavigationMovementKeyGuard.Bind(fixture.Root))
            {
                int keyDownDeliveries = 0;
                int keyUpDeliveries = 0;
                fixture.Target.RegisterCallback<KeyDownEvent>(
                    _ => keyDownDeliveries++);
                fixture.Target.RegisterCallback<KeyUpEvent>(
                    _ => keyUpDeliveries++);

                using (KeyDownEvent keyDown = KeyDownEvent.GetPooled(
                           '\0',
                           keyCode,
                           EventModifiers.None))
                {
                    keyDown.target = fixture.Target;
                    fixture.Target.SendEvent(keyDown);
                    Assert.That(keyDownDeliveries, Is.EqualTo(1));
                }

                using (KeyUpEvent keyUp = KeyUpEvent.GetPooled(
                           '\0',
                           keyCode,
                           EventModifiers.None))
                {
                    keyUp.target = fixture.Target;
                    fixture.Target.SendEvent(keyUp);
                    Assert.That(keyUpDeliveries, Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void DirectionalNavigationIsConsumedOnlyWhileMovementKeyIsHeld()
        {
            using (PanelFixture fixture = PanelFixture.Create())
            using (IDisposable binding =
                   ViewerNavigationMovementKeyGuard.Bind(fixture.Root))
            {
                int deliveries = 0;
                fixture.Target.RegisterCallback<NavigationMoveEvent>(
                    _ => deliveries++);

                SendKeyDown(fixture.Target, KeyCode.W);
                using (NavigationMoveEvent keyboardMove =
                       NavigationMoveEvent.GetPooled(
                           Vector2.up,
                           EventModifiers.None))
                {
                    keyboardMove.target = fixture.Target;
                    fixture.Target.SendEvent(keyboardMove);
                    Assert.That(deliveries, Is.Zero);
                    Assert.That(keyboardMove.isPropagationStopped, Is.True);
                    Assert.That(
                        keyboardMove.isImmediatePropagationStopped,
                        Is.True);
                }

                SendKeyUp(fixture.Target, KeyCode.W);
                using (NavigationMoveEvent unreservedMove =
                       NavigationMoveEvent.GetPooled(
                           Vector2.up,
                           EventModifiers.None))
                {
                    unreservedMove.target = fixture.Target;
                    fixture.Target.SendEvent(unreservedMove);
                    Assert.That(deliveries, Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void InjectedInputStateClosesNavigationMoveBeforeKeyDownGap()
        {
            bool movementKeyActive = true;
            using (PanelFixture fixture = PanelFixture.Create())
            using (IDisposable binding =
                   ViewerNavigationMovementKeyGuard.Bind(
                       fixture.Root,
                       () => movementKeyActive))
            {
                int deliveries = 0;
                fixture.Target.RegisterCallback<NavigationMoveEvent>(
                    _ => deliveries++);

                using (NavigationMoveEvent keyboardMove =
                       NavigationMoveEvent.GetPooled(
                           Vector2.right,
                           EventModifiers.None))
                {
                    keyboardMove.target = fixture.Target;
                    fixture.Target.SendEvent(keyboardMove);
                    Assert.That(deliveries, Is.Zero);
                    Assert.That(
                        keyboardMove.isImmediatePropagationStopped,
                        Is.True);
                }

                movementKeyActive = false;
                using (NavigationMoveEvent gamepadMove =
                       NavigationMoveEvent.GetPooled(
                           Vector2.right,
                           EventModifiers.None))
                {
                    gamepadMove.target = fixture.Target;
                    fixture.Target.SendEvent(gamepadMove);
                    Assert.That(deliveries, Is.EqualTo(1));
                }
            }
        }

        [TestCase(NavigationMoveEvent.Direction.Left, true, true)]
        [TestCase(NavigationMoveEvent.Direction.Up, true, true)]
        [TestCase(NavigationMoveEvent.Direction.Right, true, true)]
        [TestCase(NavigationMoveEvent.Direction.Down, true, true)]
        [TestCase(NavigationMoveEvent.Direction.Next, true, false)]
        [TestCase(NavigationMoveEvent.Direction.Previous, true, false)]
        [TestCase(NavigationMoveEvent.Direction.Left, false, false)]
        [TestCase(NavigationMoveEvent.Direction.Up, false, false)]
        [TestCase(NavigationMoveEvent.Direction.Right, false, false)]
        [TestCase(NavigationMoveEvent.Direction.Down, false, false)]
        public void DirectionalPolicyOnlyConsumesMovementWithReservedKey(
            NavigationMoveEvent.Direction direction,
            bool movementKeyHeld,
            bool expected)
        {
            Assert.That(
                ViewerNavigationMovementKeyGuard
                    .ShouldConsumeDirectionalNavigation(
                        direction,
                        movementKeyHeld),
                Is.EqualTo(expected));
        }

        [Test]
        public void SharedPanelBindingsAreReferenceCounted()
        {
            using (PanelFixture fixture = PanelFixture.Create())
            {
                IPanel panel = fixture.Root.panel;
                IDisposable first = null;
                IDisposable second = null;
                try
                {
                    Assert.That(HasPanelRegistration(panel), Is.False);
                    first = ViewerNavigationMovementKeyGuard.Bind(fixture.Root);
                    Assert.That(GetPanelReferenceCount(panel), Is.EqualTo(1));
                    second = ViewerNavigationMovementKeyGuard.Bind(fixture.Root);
                    Assert.That(GetPanelReferenceCount(panel), Is.EqualTo(2));
                    first.Dispose();
                    first = null;
                    Assert.That(GetPanelReferenceCount(panel), Is.EqualTo(1));
                    second.Dispose();
                    second = null;
                    Assert.That(HasPanelRegistration(panel), Is.False);
                }
                finally
                {
                    first?.Dispose();
                    second?.Dispose();
                }
            }
        }

        [Test]
        public void NullAndRepeatedBindingDisposalAreSafe()
        {
            IDisposable empty = ViewerNavigationMovementKeyGuard.Bind(null);
            Assert.DoesNotThrow(empty.Dispose);
            Assert.DoesNotThrow(empty.Dispose);

            using (PanelFixture fixture = PanelFixture.Create())
            {
                IPanel panel = fixture.Root.panel;
                IDisposable binding =
                    ViewerNavigationMovementKeyGuard.Bind(fixture.Root);
                Assert.DoesNotThrow(binding.Dispose);
                Assert.DoesNotThrow(binding.Dispose);
                Assert.That(HasPanelRegistration(panel), Is.False);
            }
        }

        private static void SendKeyDown(VisualElement target, KeyCode keyCode)
        {
            using (KeyDownEvent evt = KeyDownEvent.GetPooled(
                       '\0',
                       keyCode,
                       EventModifiers.None))
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        private static void SendKeyUp(VisualElement target, KeyCode keyCode)
        {
            using (KeyUpEvent evt = KeyUpEvent.GetPooled(
                       '\0',
                       keyCode,
                       EventModifiers.None))
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        private static bool HasPanelRegistration(IPanel panel)
        {
            return GetPanelRegistrations().Contains(panel);
        }

        private static int GetPanelReferenceCount(IPanel panel)
        {
            object registration = GetPanelRegistrations()[panel];
            Assert.That(registration, Is.Not.Null);
            PropertyInfo property = registration.GetType().GetProperty(
                "ReferenceCount",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null);
            return (int)property.GetValue(registration);
        }

        private static IDictionary GetPanelRegistrations()
        {
            FieldInfo field = typeof(ViewerNavigationMovementKeyGuard)
                .GetField(
                    "PanelRegistrations",
                    BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            IDictionary registrations = field.GetValue(null) as IDictionary;
            Assert.That(registrations, Is.Not.Null);
            return registrations;
        }

        private sealed class PanelFixture : IDisposable
        {
            private GameObject documentObject;
            private PanelSettings panelSettings;

            public VisualElement Root { get; private set; }
            public Button Target { get; private set; }

            public static PanelFixture Create()
            {
                var fixture = new PanelFixture();
                fixture.CreateInternal();
                return fixture;
            }

            public void Dispose()
            {
                if (documentObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(documentObject);
                    documentObject = null;
                }

                if (panelSettings != null)
                {
                    UnityEngine.Object.DestroyImmediate(panelSettings);
                    panelSettings = null;
                }

                Root = null;
                Target = null;
            }

            private void CreateInternal()
            {
                panelSettings =
                    ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "Movement Key Guard Test Panel";
                documentObject =
                    new GameObject("Movement Key Guard Test Document");
                UIDocument document =
                    documentObject.AddComponent<UIDocument>();
                document.panelSettings = panelSettings;
                Root = document.rootVisualElement;
                Assert.That(Root, Is.Not.Null);
                Assert.That(Root.panel, Is.Not.Null);
                Target = new Button { name = "MovementKeyTarget" };
                Root.Add(Target);
            }
        }
    }
}
