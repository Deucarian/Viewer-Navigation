using System;
using System.Collections.Generic;
using Deucarian.CameraNavigation.InputSystemIntegration;
using Deucarian.PointerCapture;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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

            if (isTopDown && Mouse.current != null && Mouse.current.leftButton.isPressed)
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
            DeucarianPointerCaptureController capture = ResolvePointerCapture();
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            bool neutral = IsInputNeutral(mouse, keyboard);
            bool hasNewAction = HasNewNavigationAction(mouse, keyboard);
            capture.UpdateInputRearming(neutral, hasNewAction);

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                capture.NotifyEscapePressed();
                ResetCapture();
                return;
            }

            if (hasNewAction && !IsCurrentActionBlocked(mouse))
            {
                NavigationInputStarted?.Invoke();
            }

            if (ownsCapture && !IsButtonPressed(mouse, capturedButton))
            {
                ReleaseCapture();
            }

            if (!ownsCapture && TryGetCaptureButton(mouse, out DeucarianMouseButton button))
            {
                Vector2 position = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
                if (!IsPointerInputBlocked(position) && capture.RequestCapture(this))
                {
                    capturedButton = button;
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

        private bool IsCurrentActionBlocked(Mouse mouse)
        {
            Vector2 position = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            return IsPointerInputBlocked(position) || IsKeyboardInputBlocked();
        }

        private bool TryGetCaptureButton(Mouse mouse, out DeucarianMouseButton button)
        {
            if (mouse != null)
            {
                if (!isTopDown && mode == ViewerNavigationMode.Orbit &&
                    mouse.leftButton.wasPressedThisFrame)
                {
                    button = DeucarianMouseButton.Left;
                    return true;
                }

                if (mouse.rightButton.wasPressedThisFrame)
                {
                    button = DeucarianMouseButton.Right;
                    return true;
                }
            }

            button = default;
            return false;
        }

        private static bool HasNewNavigationAction(Mouse mouse, Keyboard keyboard)
        {
            return (mouse != null &&
                    (mouse.leftButton.wasPressedThisFrame ||
                     mouse.rightButton.wasPressedThisFrame ||
                     mouse.middleButton.wasPressedThisFrame ||
                     Mathf.Abs(mouse.scroll.ReadValue().y) > 0.0001f)) ||
                   (keyboard != null && keyboard.anyKey.wasPressedThisFrame);
        }

        private static bool IsInputNeutral(Mouse mouse, Keyboard keyboard)
        {
            bool mouseNeutral = mouse == null ||
                                (!mouse.leftButton.isPressed &&
                                 !mouse.rightButton.isPressed &&
                                 !mouse.middleButton.isPressed &&
                                 !mouse.forwardButton.isPressed &&
                                 !mouse.backButton.isPressed);
            return mouseNeutral && (keyboard == null || !keyboard.anyKey.isPressed);
        }

        private static bool IsButtonPressed(Mouse mouse, DeucarianMouseButton button)
        {
            ButtonControl control = GetButton(mouse, button);
            return control != null && control.isPressed;
        }

        private static ButtonControl GetButton(Mouse mouse, DeucarianMouseButton button)
        {
            if (mouse == null)
            {
                return null;
            }

            switch (button)
            {
                case DeucarianMouseButton.Right:
                    return mouse.rightButton;
                case DeucarianMouseButton.Middle:
                    return mouse.middleButton;
                case DeucarianMouseButton.Forward:
                    return mouse.forwardButton;
                case DeucarianMouseButton.Back:
                    return mouse.backButton;
                default:
                    return mouse.leftButton;
            }
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
