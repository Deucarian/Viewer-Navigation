using System;
using Deucarian.Theming;
using Deucarian.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.UI
{
    internal sealed class ViewerNavigationToolbarVisualState
    {
        private readonly ButtonVisual orbit = new ButtonVisual();
        private readonly ButtonVisual fly = new ButtonVisual();
        private readonly ButtonVisual home = new ButtonVisual();
        private readonly ButtonVisual topDown = new ButtonVisual();

        private MonoBehaviour host;
        private DeucarianTheme theme;
        private DeucarianThemeStyle style;
        private DeucarianIconButtonPalette palette;
        private DeucarianIconSwap topDownIconSwap;
        private bool isTopDown;
        private Func<bool> interactionAnimationPolicy;

        public void Initialize(
            MonoBehaviour coroutineHost,
            Button orbitButton,
            Button flyButton,
            Button homeButton,
            Button topDownButton,
            VisualElement orbitIcon,
            VisualElement flyIcon,
            VisualElement homeIcon,
            VisualElement topDownIcon,
            VisualElement perspectiveIcon,
            Func<bool> shouldAnimateInteractions = null)
        {
            Dispose();
            host = coroutineHost;
            interactionAnimationPolicy = shouldAnimateInteractions;
            orbit.Configure(
                host,
                orbitButton,
                orbitIcon,
                null,
                ResolveInteractionAnimation);
            fly.Configure(
                host,
                flyButton,
                flyIcon,
                null,
                ResolveInteractionAnimation);
            home.Configure(
                host,
                homeButton,
                homeIcon,
                null,
                ResolveInteractionAnimation);
            topDown.Configure(
                host,
                topDownButton,
                topDownIcon,
                perspectiveIcon,
                ResolveInteractionAnimation);
            topDownIconSwap = new DeucarianIconSwap(
                host,
                topDownIcon,
                perspectiveIcon,
                DeucarianMotionProfile.IconSwap);
            topDownIconSwap.SetFirstVisible(isTopDown, false);
        }

        public void Dispose()
        {
            topDownIconSwap?.Stop();
            topDownIconSwap = null;
            orbit.Dispose();
            fly.Dispose();
            home.Dispose();
            topDown.Dispose();
            host = null;
            theme = null;
            style = null;
            interactionAnimationPolicy = null;
        }

        internal bool ShouldAnimateInteractions =>
            interactionAnimationPolicy != null
                ? interactionAnimationPolicy()
                : ViewerNavigationMotionPreferences.ShouldAnimate;

        public void ApplyTheme(
            DeucarianTheme currentTheme,
            DeucarianThemeStyle currentStyle)
        {
            theme = currentTheme;
            style = currentStyle ??
                    (theme != null ? theme.VisualStyle : null);
            palette = ViewerNavigationToolbarTheme.ResolvePalette(theme);
            InvalidatePresentation();
            ApplyAll(false);
        }

        public void InvalidatePresentation()
        {
            orbit.InvalidatePresentation();
            fly.InvalidatePresentation();
            home.InvalidatePresentation();
            topDown.InvalidatePresentation();
        }

        public void SetEnabled(bool enabled)
        {
            orbit.Enabled = enabled;
            fly.Enabled = enabled;
            home.Enabled = enabled;
            topDown.Enabled = enabled;
            ApplyAll(false);
        }

        public void Apply(
            ViewerNavigationSnapshot snapshot,
            bool animate = false)
        {
            orbit.Selected = snapshot.Mode == ViewerNavigationMode.Orbit;
            fly.Selected = snapshot.Mode == ViewerNavigationMode.Fly;
            home.Selected = false;
            topDown.Selected = false;

            bool targetTopDown = ResolvePresentedTopDown(snapshot);
            bool changed = isTopDown != targetTopDown;
            isTopDown = targetTopDown;
            topDownIconSwap?.SetFirstVisible(
                isTopDown,
                changed && animate);
            ApplyAll(animate);
        }

        internal static bool ResolvePresentedTopDown(
            ViewerNavigationSnapshot snapshot)
        {
            if (snapshot.IsTransitioning)
            {
                if (snapshot.TransitionKind ==
                    ViewerNavigationTransitionKind.EnterTopDown)
                {
                    return true;
                }

                if (snapshot.TransitionKind ==
                    ViewerNavigationTransitionKind.ExitTopDown)
                {
                    return false;
                }
            }

            return snapshot.IsTopDown;
        }

        private void ApplyAll(bool animate)
        {
            orbit.Apply(palette, style, animate);
            fly.Apply(palette, style, animate);
            home.Apply(palette, style, animate);
            topDown.Apply(palette, style, animate);
        }

        private bool ResolveInteractionAnimation() =>
            ShouldAnimateInteractions;

        private sealed class ButtonVisual
        {
            private readonly DeucarianIconButtonInteraction interaction =
                new DeucarianIconButtonInteraction();
            private Button button;
            private VisualElement primaryIcon;
            private VisualElement secondaryIcon;
            private DeucarianAnimatedIconButton buttonMotion;
            private DeucarianAnimatedIconButton primaryIconMotion;
            private DeucarianAnimatedIconButton secondaryIconMotion;
            private DeucarianIconButtonPalette palette;
            private DeucarianThemeStyle style;
            private Func<bool> shouldAnimate;

            public bool Enabled { get; set; } = true;
            public bool Selected { get; set; }

            public void Configure(
                MonoBehaviour host,
                Button target,
                VisualElement primaryIcon,
                VisualElement secondaryIcon,
                Func<bool> animationPolicy)
            {
                Dispose();
                button = target;
                this.primaryIcon = primaryIcon;
                this.secondaryIcon = secondaryIcon;
                shouldAnimate = animationPolicy;
                bool hasAlternateIcon = secondaryIcon != null;
                buttonMotion = new DeucarianAnimatedIconButton(
                    host,
                    button,
                    hasAlternateIcon ? null : primaryIcon,
                    DeucarianMotionProfile.ControlState,
                    true,
                    false,
                    false);
                if (hasAlternateIcon)
                {
                    primaryIconMotion = CreateIconMotion(host, primaryIcon);
                    secondaryIconMotion = CreateIconMotion(host, secondaryIcon);
                }

                interaction.Bind(button, ApplyCurrent);
            }

            public void Dispose()
            {
                interaction.Unbind();
                buttonMotion?.Stop();
                primaryIconMotion?.Stop();
                secondaryIconMotion?.Stop();
                button = null;
                primaryIcon = null;
                secondaryIcon = null;
                buttonMotion = null;
                primaryIconMotion = null;
                secondaryIconMotion = null;
                shouldAnimate = null;
                Enabled = true;
                Selected = false;
            }

            public void InvalidatePresentation()
            {
                buttonMotion?.InvalidatePresentation();
                primaryIconMotion?.InvalidatePresentation();
                secondaryIconMotion?.InvalidatePresentation();
            }

            public void Apply(
                DeucarianIconButtonPalette currentPalette,
                DeucarianThemeStyle currentStyle,
                bool animate)
            {
                palette = currentPalette;
                style = currentStyle;
                DeucarianIconButtonVisualState state = CreateState();
                ViewerNavigationToolbarChrome.ApplyStateClasses(
                    button,
                    state);
                buttonMotion?.SetState(palette, state, style, animate);
                primaryIconMotion?.SetState(palette, state, style, animate);
                secondaryIconMotion?.SetState(palette, state, style, animate);
                if (button != null)
                {
                    button.style.scale = new Scale(Vector3.one);
                }

                float iconScale = interaction.Pressed ? 0.96f : 1f;
                ApplyIconScale(primaryIcon, iconScale);
                ApplyIconScale(secondaryIcon, iconScale);
            }

            private void ApplyCurrent()
            {
                Apply(
                    palette,
                    style,
                    shouldAnimate != null && shouldAnimate());
            }

            private DeucarianIconButtonVisualState CreateState()
            {
                if (!Enabled)
                {
                    interaction.Reset();
                }

                return new DeucarianIconButtonVisualState(
                    true,
                    Enabled,
                    Selected,
                    interaction.Hovered,
                    interaction.Pressed,
                    interaction.Focused);
            }

            private static DeucarianAnimatedIconButton CreateIconMotion(
                MonoBehaviour host,
                VisualElement icon)
            {
                return new DeucarianAnimatedIconButton(
                    host,
                    null,
                    icon,
                    DeucarianMotionProfile.ControlState,
                    false,
                    true,
                    false);
            }

            private static void ApplyIconScale(
                VisualElement icon,
                float scale)
            {
                if (icon != null)
                {
                    icon.style.scale = new Scale(
                        new Vector3(scale, scale, 1f));
                }
            }
        }
    }

    internal static class ViewerNavigationToolbarTheme
    {
        public static DeucarianIconButtonPalette ResolvePalette(
            DeucarianTheme theme)
        {
            Color normal = Resolve(
                theme,
                DeucarianBuiltinColorRoleIds.UiNormal,
                Color.clear);
            Color selected = Resolve(
                theme,
                DeucarianBuiltinColorRoleIds.Accent,
                new Color(0.769f, 0.631f, 0.976f, 1f));
            return new DeucarianIconButtonPalette(
                normal,
                Resolve(
                    theme,
                    DeucarianBuiltinColorRoleIds.UiHighlighted,
                    new Color(1f, 1f, 1f, 0.12f)),
                Resolve(
                    theme,
                    DeucarianBuiltinColorRoleIds.UiPressed,
                    new Color(1f, 1f, 1f, 0.2f)),
                selected,
                normal,
                Resolve(
                    theme,
                    DeucarianBuiltinColorRoleIds.TextPrimary,
                    Color.white),
                Resolve(
                    theme,
                    DeucarianBuiltinColorRoleIds.TextMuted,
                    new Color(0.4f, 0.4f, 0.4f, 1f)),
                Resolve(
                    theme,
                    DeucarianBuiltinColorRoleIds.Primary,
                    Color.white),
                Resolve(
                    theme,
                    DeucarianBuiltinColorRoleIds.TextPrimary,
                    Color.white),
                Resolve(
                    theme,
                    DeucarianBuiltinColorRoleIds.TextDisabled,
                    new Color(0.6f, 0.6f, 0.6f, 1f)),
                Resolve(
                    theme,
                    DeucarianBuiltinColorRoleIds.TextMuted,
                    new Color(0.4f, 0.4f, 0.4f, 1f)),
                Resolve(
                    theme,
                    DeucarianBuiltinColorRoleIds.UiFocused,
                    selected));
        }

        public static Color Resolve(
            DeucarianTheme theme,
            string roleId,
            Color fallback)
        {
            return theme != null && theme.TryGetColorById(roleId, out Color value)
                ? value
                : fallback;
        }
    }
}
