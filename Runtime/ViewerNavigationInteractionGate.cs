using System;
using System.Collections.Generic;
using Deucarian.CameraNavigation.InputSystemIntegration;
using Deucarian.PointerCapture;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DeucarianPointerCaptureController))]
    public sealed class ViewerNavigationInteractionGate :
        MonoBehaviour,
        IDeucarianNavigationInputBlocker
    {
        private readonly List<VisualElement> uiRoots = new List<VisualElement>();
        private DeucarianPointerCaptureController pointerCapture;
        private IViewerNavigationInputBlocker externalBlocker;
        private IDeucarianNavigationActionStateSource actionStateSource;
        private ViewerNavigationMode mode;
        private bool isTopDown;
        private DeucarianMouseButton capturedButton;
        private bool ownsCapture;

        public event Action NavigationInputStarted;

        public DeucarianPointerCaptureController PointerCapture =>
            ResolvePointerCapture();

        public void Configure(IViewerNavigationInputBlocker blocker)
        {
            externalBlocker = blocker;
        }

        public void Configure(
            IViewerNavigationInputBlocker blocker,
            IDeucarianNavigationActionStateSource navigationActionStateSource)
        {
            externalBlocker = blocker;
            actionStateSource = navigationActionStateSource;
        }

        public void SetNavigationState(ViewerNavigationMode navigationMode, bool topDown)
        {
            mode = navigationMode;
            isTopDown = topDown;
        }

        public IDisposable RegisterUiRoot(VisualElement root)
        {
            if (root == null)
            {
                return EmptyRegistration.Instance;
            }

            if (!uiRoots.Contains(root))
            {
                uiRoots.Add(root);
            }

            return new UiRootRegistration(this, root);
        }

        public bool IsPointerInputBlocked(Vector2 screenPosition)
        {
            if (externalBlocker != null &&
                externalBlocker.IsPointerInputBlocked(screenPosition))
            {
                return true;
            }

            if (isTopDown && actionStateSource != null &&
                actionStateSource.IsOrbitRotatePressed())
            {
                return true;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.IsPointerOverGameObject())
            {
                return true;
            }

            Vector2 topLeft = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            for (int i = uiRoots.Count - 1; i >= 0; i--)
            {
                VisualElement root = uiRoots[i];
                if (root?.panel == null)
                {
                    continue;
                }

                Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel, topLeft);
                VisualElement picked = root.panel.Pick(panelPosition);
                if (picked != null &&
                    picked != root.panel.visualTree &&
                    picked.pickingMode != PickingMode.Ignore)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsKeyboardInputBlocked()
        {
            if (externalBlocker != null && externalBlocker.IsKeyboardInputBlocked())
            {
                return true;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
            {
                return true;
            }

            for (int i = uiRoots.Count - 1; i >= 0; i--)
            {
                VisualElement focused =
                    uiRoots[i]?.panel?.focusController?.focusedElement as VisualElement;
                if (focused != null && focused.focusable && focused.enabledInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            ResolvePointerCapture();
        }

        private void Update()
        {
            ProcessInputState();
        }

        public void ProcessInputState()
        {
            DeucarianPointerCaptureController capture = ResolvePointerCapture();
            if (actionStateSource == null)
            {
                capture.UpdateInputRearming(true, false);
                return;
            }

            DeucarianNavigationActionState actionState =
                actionStateSource.ReadActionState(
                    isTopDown || mode == ViewerNavigationMode.Orbit
                        ? DeucarianInputSystemNavigationMode.Orbit
                        : DeucarianInputSystemNavigationMode.Fly,
                    isTopDown);
            capture.UpdateInputRearming(
                actionState.IsNeutral,
                actionState.HasNewNavigationAction);

            if (actionState.EscapePressed)
            {
                capture.NotifyEscapePressed();
                ResetCapture();
                return;
            }

            if (HasAllowedNavigationAction(actionState))
            {
                NavigationInputStarted?.Invoke();
            }

            if (ownsCapture &&
                !actionStateSource.IsButtonPressed(capturedButton))
            {
                ReleaseCapture();
            }

            if (!ownsCapture && actionState.CaptureRequested)
            {
                if (!IsPointerInputBlocked(actionState.PointerPosition) &&
                    capture.RequestCapture(this))
                {
                    capturedButton = actionState.CaptureButton;
                    ownsCapture = true;
                }
            }
        }

        private void OnDisable()
        {
            ReleaseCapture();
        }

        private DeucarianPointerCaptureController ResolvePointerCapture()
        {
            if (pointerCapture == null)
            {
                pointerCapture = GetComponent<DeucarianPointerCaptureController>();
                if (pointerCapture == null)
                {
                    pointerCapture = gameObject.AddComponent<DeucarianPointerCaptureController>();
                }
            }

            return pointerCapture;
        }

        private bool HasAllowedNavigationAction(
            DeucarianNavigationActionState actionState)
        {
            bool pointerAllowed =
                actionState.HasPointerAction &&
                !IsPointerInputBlocked(actionState.PointerPosition);
            bool keyboardAllowed =
                actionState.HasKeyboardAction &&
                !IsKeyboardInputBlocked();
            return pointerAllowed || keyboardAllowed;
        }

        private void ReleaseCapture()
        {
            if (pointerCapture != null && ownsCapture)
            {
                pointerCapture.ReleaseCapture(this);
            }

            ResetCapture();
        }

        private void ResetCapture()
        {
            ownsCapture = false;
            capturedButton = default;
        }

        private void UnregisterUiRoot(VisualElement root)
        {
            uiRoots.Remove(root);
        }

        private sealed class UiRootRegistration : IDisposable
        {
            private ViewerNavigationInteractionGate owner;
            private VisualElement root;

            public UiRootRegistration(
                ViewerNavigationInteractionGate owner,
                VisualElement root)
            {
                this.owner = owner;
                this.root = root;
            }

            public void Dispose()
            {
                owner?.UnregisterUiRoot(root);
                owner = null;
                root = null;
            }
        }

        private sealed class EmptyRegistration : IDisposable
        {
            public static readonly EmptyRegistration Instance = new EmptyRegistration();
            public void Dispose() { }
        }
    }
}
