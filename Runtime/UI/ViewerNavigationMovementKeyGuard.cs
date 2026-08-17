using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.UI
{
    /// <summary>
    /// Keeps viewer movement keys out of UI Toolkit while preserving keyboard and
    /// gamepad focus navigation. Documents sharing a panel share one callback set.
    /// </summary>
    public static class ViewerNavigationMovementKeyGuard
    {
        private static readonly Dictionary<IPanel, PanelRegistration>
            PanelRegistrations =
                new Dictionary<IPanel, PanelRegistration>();
        private static readonly HashSet<KeyCode> HeldMovementKeys =
            new HashSet<KeyCode>();

        public static IDisposable Bind(
            VisualElement documentRoot,
            Func<bool> movementKeyState = null)
        {
            return documentRoot != null
                ? new RootBinding(documentRoot, movementKeyState)
                : EmptyRegistration.Instance;
        }

        internal static bool IsMovementKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.W:
                case KeyCode.A:
                case KeyCode.S:
                case KeyCode.D:
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                case KeyCode.LeftArrow:
                case KeyCode.RightArrow:
                case KeyCode.Q:
                case KeyCode.E:
                case KeyCode.PageUp:
                case KeyCode.PageDown:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool ShouldConsumeDirectionalNavigation(
            NavigationMoveEvent.Direction direction,
            bool movementKeyHeld)
        {
            if (!movementKeyHeld)
            {
                return false;
            }

            switch (direction)
            {
                case NavigationMoveEvent.Direction.Left:
                case NavigationMoveEvent.Direction.Up:
                case NavigationMoveEvent.Direction.Right:
                case NavigationMoveEvent.Direction.Down:
                    return true;
                default:
                    return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            foreach (PanelRegistration registration in
                     PanelRegistrations.Values)
            {
                registration.Dispose();
            }

            PanelRegistrations.Clear();
            HeldMovementKeys.Clear();
            Application.focusChanged -= OnApplicationFocusChanged;
        }

        private static void AcquirePanel(
            IPanel panel,
            Func<bool> movementKeyState)
        {
            if (panel == null)
            {
                return;
            }

            if (PanelRegistrations.TryGetValue(
                    panel,
                    out PanelRegistration registration))
            {
                registration.AddBinding(movementKeyState);
                return;
            }

            bool firstPanel = PanelRegistrations.Count == 0;
            PanelRegistrations.Add(
                panel,
                new PanelRegistration(panel, movementKeyState));
            if (firstPanel)
            {
                Application.focusChanged += OnApplicationFocusChanged;
            }
        }

        private static void ReleasePanel(
            IPanel panel,
            Func<bool> movementKeyState)
        {
            if (panel == null ||
                !PanelRegistrations.TryGetValue(
                    panel,
                    out PanelRegistration registration))
            {
                return;
            }

            registration.RemoveBinding(movementKeyState);
            if (registration.ReferenceCount > 0)
            {
                return;
            }

            registration.Dispose();
            PanelRegistrations.Remove(panel);
            if (PanelRegistrations.Count == 0)
            {
                HeldMovementKeys.Clear();
                Application.focusChanged -= OnApplicationFocusChanged;
            }
        }

        private static void OnKeyDown(KeyDownEvent evt)
        {
            if (!IsMovementKey(evt.keyCode))
            {
                return;
            }

            HeldMovementKeys.Add(evt.keyCode);
            Consume(evt);
        }

        private static void OnKeyUp(KeyUpEvent evt)
        {
            if (!IsMovementKey(evt.keyCode))
            {
                return;
            }

            HeldMovementKeys.Remove(evt.keyCode);
            Consume(evt);
        }

        private static void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (ShouldConsumeDirectionalNavigation(
                    evt.direction,
                    IsMovementKeyActive(evt)))
            {
                Consume(evt);
            }
        }

        private static bool IsMovementKeyActive(EventBase evt)
        {
            if (HeldMovementKeys.Count > 0)
            {
                return true;
            }

            VisualElement currentTarget =
                evt.currentTarget as VisualElement;
            IPanel panel = currentTarget?.panel;
            return panel != null &&
                   PanelRegistrations.TryGetValue(
                       panel,
                       out PanelRegistration registration) &&
                   registration.IsMovementKeyActive();
        }

        private static void Consume(EventBase evt)
        {
            VisualElement currentTarget =
                evt.currentTarget as VisualElement;
            currentTarget?.panel?.focusController?.IgnoreEvent(evt);
            evt.StopImmediatePropagation();
        }

        private static void OnApplicationFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
            {
                HeldMovementKeys.Clear();
            }
        }

        private sealed class PanelRegistration : IDisposable
        {
            private readonly VisualElement panelRoot;
            private readonly List<Func<bool>> movementKeyStates =
                new List<Func<bool>>();

            public PanelRegistration(
                IPanel panel,
                Func<bool> movementKeyState)
            {
                panelRoot = panel.visualTree;
                AddBinding(movementKeyState);
                panelRoot.RegisterCallback<KeyDownEvent>(
                    OnKeyDown,
                    TrickleDown.TrickleDown);
                panelRoot.RegisterCallback<KeyUpEvent>(
                    OnKeyUp,
                    TrickleDown.TrickleDown);
                panelRoot.RegisterCallback<NavigationMoveEvent>(
                    OnNavigationMove,
                    TrickleDown.TrickleDown);
            }

            public int ReferenceCount => movementKeyStates.Count;

            public void AddBinding(Func<bool> movementKeyState)
            {
                movementKeyStates.Add(movementKeyState);
            }

            public void RemoveBinding(Func<bool> movementKeyState)
            {
                movementKeyStates.Remove(movementKeyState);
            }

            public bool IsMovementKeyActive()
            {
                for (int i = 0; i < movementKeyStates.Count; i++)
                {
                    Func<bool> state = movementKeyStates[i];
                    if (state != null && state())
                    {
                        return true;
                    }
                }

                return false;
            }

            public void Dispose()
            {
                panelRoot.UnregisterCallback<KeyDownEvent>(
                    OnKeyDown,
                    TrickleDown.TrickleDown);
                panelRoot.UnregisterCallback<KeyUpEvent>(
                    OnKeyUp,
                    TrickleDown.TrickleDown);
                panelRoot.UnregisterCallback<NavigationMoveEvent>(
                    OnNavigationMove,
                    TrickleDown.TrickleDown);
            }
        }

        private sealed class RootBinding : IDisposable
        {
            private VisualElement documentRoot;
            private IPanel panel;
            private readonly Func<bool> movementKeyState;

            public RootBinding(
                VisualElement root,
                Func<bool> movementKeyState)
            {
                documentRoot = root;
                this.movementKeyState = movementKeyState;
                documentRoot.RegisterCallback<AttachToPanelEvent>(
                    OnAttachToPanel);
                documentRoot.RegisterCallback<DetachFromPanelEvent>(
                    OnDetachFromPanel);
                AttachToPanel(documentRoot.panel);
            }

            public void Dispose()
            {
                if (documentRoot == null)
                {
                    return;
                }

                documentRoot.UnregisterCallback<AttachToPanelEvent>(
                    OnAttachToPanel);
                documentRoot.UnregisterCallback<DetachFromPanelEvent>(
                    OnDetachFromPanel);
                DetachFromPanel();
                documentRoot = null;
            }

            private void OnAttachToPanel(AttachToPanelEvent evt) =>
                AttachToPanel(evt.destinationPanel);

            private void OnDetachFromPanel(DetachFromPanelEvent _) =>
                DetachFromPanel();

            private void AttachToPanel(IPanel targetPanel)
            {
                if (panel == targetPanel)
                {
                    return;
                }

                DetachFromPanel();
                panel = targetPanel;
                AcquirePanel(panel, movementKeyState);
            }

            private void DetachFromPanel()
            {
                if (panel == null)
                {
                    return;
                }

                ReleasePanel(panel, movementKeyState);
                panel = null;
            }
        }

        private sealed class EmptyRegistration : IDisposable
        {
            public static readonly EmptyRegistration Instance =
                new EmptyRegistration();

            public void Dispose()
            {
            }
        }
    }
}
