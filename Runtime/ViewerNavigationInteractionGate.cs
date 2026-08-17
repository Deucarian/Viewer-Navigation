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
        private IViewerPointerCaptureSession pointerCaptureSession;
        private IViewerNavigationInputBlocker externalBlocker;
        private IDeucarianNavigationActionStateSource actionStateSource;
        private ViewerNavigationMode mode;
        private bool isTopDown;
        private DeucarianMouseButton capturedButton;
        private DeucarianMouseButton deniedCaptureButton;
        private bool ownsCapture;
        private bool captureDeniedUntilRelease;
        private bool captureSessionSubscribed;

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

        internal bool IsNavigationMovementKeyActive()
        {
            if (actionStateSource == null)
            {
                return false;
            }

            return actionStateSource.ReadActionState(
                    isTopDown || mode == ViewerNavigationMode.Orbit
                        ? DeucarianInputSystemNavigationMode.Orbit
                        : DeucarianInputSystemNavigationMode.Fly,
                    isTopDown)
                .HasKeyboardAction;
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
            if (IsTopDownRotationBlocked())
            {
                return true;
            }

            if (HasAcceptedCapture())
            {
                return false;
            }

            if (captureDeniedUntilRelease ||
                IsCaptureRequiredPointerActionPressed())
            {
                return true;
            }

            return IsPointerBlockedByApplication(screenPosition);
        }

        private bool IsPointerBlockedByApplication(Vector2 screenPosition)
        {
            if (IsTopDownRotationBlocked() ||
                (externalBlocker != null &&
                 externalBlocker.IsPointerInputBlocked(screenPosition)))
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

        private void OnEnable()
        {
            ResolvePointerCapture();
            SubscribeCaptureSession();
        }

        private void Update()
        {
            ProcessInputState();
        }

        public void ProcessInputState()
        {
            ResolvePointerCapture();
            if (actionStateSource == null)
            {
                pointerCaptureSession.UpdateInputRearming(true, false);
                ReleaseCapture();
                return;
            }

            DeucarianNavigationActionState actionState =
                actionStateSource.ReadActionState(
                    isTopDown || mode == ViewerNavigationMode.Orbit
                        ? DeucarianInputSystemNavigationMode.Orbit
                        : DeucarianInputSystemNavigationMode.Fly,
                    isTopDown);
            pointerCaptureSession.UpdateInputRearming(
                actionState.IsNeutral,
                actionState.CaptureRequested);
            ClearCaptureDenialAfterRelease();

            if (actionState.EscapePressed)
            {
                pointerCaptureSession.NotifyEscapePressed();
                ResetCapture();
                return;
            }

            if (ownsCapture &&
                !actionStateSource.IsButtonPressed(capturedButton))
            {
                ReleaseCapture();
            }

            if (!ownsCapture && actionState.CaptureRequested)
            {
                if (!IsPointerBlockedByApplication(actionState.PointerPosition) &&
                    pointerCaptureSession.RequestCapture(this) &&
                    IsOwnedCaptureState(pointerCaptureSession.State))
                {
                    capturedButton = actionState.CaptureButton;
                    ownsCapture = true;
                    captureDeniedUntilRelease = false;
                    deniedCaptureButton = default;
                }
                else
                {
                    DenyCaptureUntilRelease(actionState.CaptureButton);
                }
            }

            if (HasAllowedNavigationAction(actionState))
            {
                NavigationInputStarted?.Invoke();
            }
        }

        private void OnDisable()
        {
            ReleaseCapture();
            UnsubscribeCaptureSession();
        }

        private void OnDestroy()
        {
            UnsubscribeCaptureSession();
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

            if (pointerCaptureSession == null)
            {
                pointerCaptureSession =
                    new ViewerPointerCaptureSession(pointerCapture);
                SubscribeCaptureSession();
            }

            return pointerCapture;
        }

        private bool HasAllowedNavigationAction(
            DeucarianNavigationActionState actionState)
        {
            bool pointerAllowed =
                actionState.HasPointerAction &&
                (actionState.CaptureRequested
                    ? HasAcceptedCapture()
                    : !IsPointerInputBlocked(actionState.PointerPosition));
            bool keyboardAllowed =
                actionState.HasKeyboardAction &&
                !IsKeyboardInputBlocked();
            return pointerAllowed || keyboardAllowed;
        }

        private void ReleaseCapture()
        {
            if (pointerCaptureSession != null && ownsCapture)
            {
                pointerCaptureSession.ReleaseCapture(this);
            }

            ResetCapture();
        }

        private void ResetCapture()
        {
            ownsCapture = false;
            capturedButton = default;
        }

        internal void SetPointerCaptureSessionForTesting(
            IViewerPointerCaptureSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            ReleaseCapture();
            UnsubscribeCaptureSession();
            pointerCaptureSession = session;
            captureDeniedUntilRelease = false;
            deniedCaptureButton = default;
            SubscribeCaptureSession();
        }

        private bool HasAcceptedCapture()
        {
            return ownsCapture &&
                   pointerCaptureSession != null &&
                   pointerCaptureSession.State ==
                       DeucarianPointerCaptureState.Active;
        }

        private bool IsCaptureRequiredPointerActionPressed()
        {
            return actionStateSource is
                       IDeucarianCaptureRequiredActionStateSource captureSource &&
                   captureSource.IsCaptureRequiredPointerActionPressed(
                       isTopDown || mode == ViewerNavigationMode.Orbit
                           ? DeucarianInputSystemNavigationMode.Orbit
                           : DeucarianInputSystemNavigationMode.Fly,
                       isTopDown);
        }

        private bool IsTopDownRotationBlocked()
        {
            return isTopDown &&
                   actionStateSource != null &&
                   actionStateSource.IsOrbitRotatePressed();
        }

        private void DenyCaptureUntilRelease(DeucarianMouseButton button)
        {
            deniedCaptureButton = button;
            captureDeniedUntilRelease = true;
            ResetCapture();
        }

        private void ClearCaptureDenialAfterRelease()
        {
            if (!captureDeniedUntilRelease ||
                actionStateSource.IsButtonPressed(deniedCaptureButton))
            {
                return;
            }

            captureDeniedUntilRelease = false;
            deniedCaptureButton = default;
        }

        private void SubscribeCaptureSession()
        {
            if (captureSessionSubscribed ||
                pointerCaptureSession == null ||
                !isActiveAndEnabled)
            {
                return;
            }

            pointerCaptureSession.StateChanged += OnPointerCaptureStateChanged;
            captureSessionSubscribed = true;
        }

        private void UnsubscribeCaptureSession()
        {
            if (!captureSessionSubscribed || pointerCaptureSession == null)
            {
                return;
            }

            pointerCaptureSession.StateChanged -= OnPointerCaptureStateChanged;
            captureSessionSubscribed = false;
        }

        private void OnPointerCaptureStateChanged(
            object sender,
            DeucarianPointerCaptureStateChangedEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                return;
            }

            if (eventArgs.CurrentState ==
                DeucarianPointerCaptureState.Requested)
            {
                return;
            }

            if (eventArgs.CurrentState ==
                DeucarianPointerCaptureState.Active)
            {
                if (ownsCapture)
                {
                    NavigationInputStarted?.Invoke();
                }

                return;
            }

            if (ownsCapture &&
                actionStateSource != null &&
                actionStateSource.IsButtonPressed(capturedButton))
            {
                deniedCaptureButton = capturedButton;
                captureDeniedUntilRelease = true;
            }

            ResetCapture();
        }

        private static bool IsOwnedCaptureState(
            DeucarianPointerCaptureState state)
        {
            return state == DeucarianPointerCaptureState.Requested ||
                   state == DeucarianPointerCaptureState.Active;
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
