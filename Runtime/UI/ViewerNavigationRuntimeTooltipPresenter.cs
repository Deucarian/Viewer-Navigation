using System.Collections.Generic;
using Deucarian.Theming;
using Deucarian.Theming.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.UI
{
    /// <summary>
    /// Runtime tooltip surface for player builds and keyboard navigation.
    /// </summary>
    internal sealed class ViewerNavigationRuntimeTooltipPresenter
    {
        public const string BubbleName = "ViewerNavigationRuntimeTooltip";
        public const string LabelName = "ViewerNavigationRuntimeTooltipLabel";

        private const long PointerDelayMilliseconds = 420L;
        private const long FocusDelayMilliseconds = 180L;
        private const float EdgeInset = 10f;
        private const float PointerOffsetX = 14f;
        private const float PointerOffsetY = 18f;

        private readonly VisualElement root;
        private readonly VisualElement bubble;
        private readonly Label label;
        private readonly Component themeContext;
        private readonly List<VisualElement> targets = new List<VisualElement>();
        private IVisualElementScheduledItem pendingShow;
        private IVisualElementScheduledItem pendingPointerActivationClear;
        private VisualElement pendingTarget;
        private VisualElement pointerActivatedTarget;
        private Vector2 anchor;
        private bool anchorFromFocus;
        private bool visible;

        public ViewerNavigationRuntimeTooltipPresenter(
            Component context,
            VisualElement tooltipRoot)
        {
            themeContext = context;
            root = tooltipRoot;
            if (root == null)
            {
                return;
            }

            bubble = new VisualElement
            {
                name = BubbleName,
                pickingMode = PickingMode.Position
            };
            bubble.style.display = DisplayStyle.None;
            bubble.style.position = Position.Absolute;
            bubble.style.maxWidth = 320f;
            bubble.style.paddingLeft = 11f;
            bubble.style.paddingRight = 11f;
            bubble.style.paddingTop = 8f;
            bubble.style.paddingBottom = 8f;
            bubble.style.opacity = 0f;

            label = new Label(string.Empty)
            {
                name = LabelName,
                pickingMode = PickingMode.Ignore
            };
            label.style.fontSize = 12f;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            bubble.Add(label);
            root.Add(bubble);
            root.RegisterCallback<TooltipEvent>(
                OnTooltipRequested,
                TrickleDown.TrickleDown);
        }

        public void Bind(VisualElement target)
        {
            if (target == null || targets.Contains(target))
            {
                return;
            }

            targets.Add(target);
            target.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            target.RegisterCallback<FocusInEvent>(OnFocusIn);
            target.RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        public void ApplyTheme(
            DeucarianTheme theme,
            DeucarianThemeStyle style)
        {
            if (bubble == null)
            {
                return;
            }

            DeucarianUIToolkitThemeTypography.Apply(
                bubble,
                theme,
                themeContext);
            if (!DeucarianUIToolkitThemeStyleUtility.ApplyPanel(
                    bubble,
                    theme,
                    style))
            {
                bubble.style.backgroundColor = new Color(0.055f, 0.075f, 0.1f, 0.96f);
                bubble.style.borderTopLeftRadius = 8f;
                bubble.style.borderTopRightRadius = 8f;
                bubble.style.borderBottomLeftRadius = 8f;
                bubble.style.borderBottomRightRadius = 8f;
            }

            if (label != null)
            {
                label.style.color = ViewerNavigationToolbarTheme.Resolve(
                    theme,
                    DeucarianBuiltinColorRoleIds.TextPrimary,
                    Color.white);
            }
        }

        public void Dispose()
        {
            CancelPendingShow();
            CancelPendingPointerActivationClear();
            Hide();
            for (int i = 0; i < targets.Count; i++)
            {
                VisualElement target = targets[i];
                if (target == null)
                {
                    continue;
                }

                target.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
                target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
                target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
                target.UnregisterCallback<FocusInEvent>(OnFocusIn);
                target.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            }

            targets.Clear();
            root?.UnregisterCallback<TooltipEvent>(
                OnTooltipRequested,
                TrickleDown.TrickleDown);
            bubble?.RemoveFromHierarchy();
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            VisualElement target = evt.currentTarget as VisualElement;
            if (!HasTooltip(target))
            {
                return;
            }

            anchor = RootPosition(evt.position) +
                     new Vector2(PointerOffsetX, PointerOffsetY);
            QueueShow(target, PointerDelayMilliseconds, false);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.currentTarget != pendingTarget || anchorFromFocus)
            {
                return;
            }

            anchor = RootPosition(evt.position) +
                     new Vector2(PointerOffsetX, PointerOffsetY);
            if (visible)
            {
                PositionBubble();
            }
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (evt.currentTarget == pendingTarget)
            {
                Hide();
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            pointerActivatedTarget = evt.currentTarget as VisualElement;
            Hide();
            CancelPendingPointerActivationClear();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.currentTarget == pointerActivatedTarget && root != null)
            {
                pendingPointerActivationClear = root.schedule
                    .Execute(ClearPointerActivation)
                    .StartingIn(0L);
            }
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.currentTarget == pointerActivatedTarget)
            {
                ClearPointerActivation();
            }
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            VisualElement target = evt.currentTarget as VisualElement;
            if (target != null && target == pointerActivatedTarget)
            {
                ClearPointerActivation();
                Hide();
                return;
            }

            if (!HasTooltip(target))
            {
                return;
            }

            Rect bounds = target.worldBound;
            anchor = RootPosition(new Vector2(bounds.xMin, bounds.yMax)) +
                     new Vector2(0f, 9f);
            QueueShow(target, FocusDelayMilliseconds, true);
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            if (evt.currentTarget == pendingTarget)
            {
                Hide();
            }
        }

        private void OnTooltipRequested(TooltipEvent evt)
        {
            VisualElement target = evt.target as VisualElement;
            if (target != null && target == pointerActivatedTarget)
            {
                evt.StopPropagation();
                return;
            }

            if (!HasTooltip(target))
            {
                return;
            }

            Rect bounds = target.worldBound;
            anchor = RootPosition(new Vector2(bounds.xMin, bounds.yMax)) +
                     new Vector2(0f, 9f);
            QueueShow(target, 0L, false);
            evt.StopPropagation();
        }

        private void QueueShow(
            VisualElement target,
            long delayMilliseconds,
            bool fromFocus)
        {
            CancelPendingShow();
            pendingTarget = target;
            anchorFromFocus = fromFocus;
            if (root != null)
            {
                pendingShow = root.schedule
                    .Execute(ShowPending)
                    .StartingIn(delayMilliseconds);
            }
        }

        private void ShowPending()
        {
            pendingShow = null;
            if (!HasTooltip(pendingTarget) || pendingTarget.panel == null)
            {
                Hide();
                return;
            }

            label.text = pendingTarget.tooltip;
            bubble.style.display = DisplayStyle.Flex;
            bubble.style.opacity = 1f;
            bubble.BringToFront();
            visible = true;
            root.schedule.Execute(PositionBubble).ExecuteLater(0L);
        }

        private void PositionBubble()
        {
            if (!visible || bubble == null || root == null)
            {
                return;
            }

            float rootWidth = Mathf.Max(0f, root.resolvedStyle.width);
            float rootHeight = Mathf.Max(0f, root.resolvedStyle.height);
            float bubbleWidth = Mathf.Max(180f, bubble.resolvedStyle.width);
            float bubbleHeight = Mathf.Max(34f, bubble.resolvedStyle.height);
            float left = anchor.x;
            float top = anchor.y;

            if (rootWidth > 0f)
            {
                left = Mathf.Clamp(
                    left,
                    EdgeInset,
                    Mathf.Max(EdgeInset, rootWidth - bubbleWidth - EdgeInset));
            }

            if (rootHeight > 0f && top + bubbleHeight + EdgeInset > rootHeight)
            {
                Rect targetBounds = pendingTarget != null
                    ? pendingTarget.worldBound
                    : default;
                top = RootPosition(new Vector2(targetBounds.xMin, targetBounds.yMin)).y
                      - bubbleHeight
                      - 9f;
            }

            if (rootHeight > 0f)
            {
                top = Mathf.Clamp(
                    top,
                    EdgeInset,
                    Mathf.Max(EdgeInset, rootHeight - bubbleHeight - EdgeInset));
            }

            bubble.style.left = left;
            bubble.style.top = top;
        }

        private void Hide()
        {
            CancelPendingShow();
            pendingTarget = null;
            visible = false;
            if (bubble != null)
            {
                bubble.style.display = DisplayStyle.None;
                bubble.style.opacity = 0f;
            }
        }

        private void CancelPendingShow()
        {
            pendingShow?.Pause();
            pendingShow = null;
        }

        private void ClearPointerActivation()
        {
            pendingPointerActivationClear = null;
            pointerActivatedTarget = null;
        }

        private void CancelPendingPointerActivationClear()
        {
            pendingPointerActivationClear?.Pause();
            pendingPointerActivationClear = null;
        }

        private Vector2 RootPosition(Vector2 panelPosition) =>
            root != null ? root.WorldToLocal(panelPosition) : panelPosition;

        private static bool HasTooltip(VisualElement target) =>
            target != null && !string.IsNullOrWhiteSpace(target.tooltip);
    }
}
