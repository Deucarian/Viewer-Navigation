using Deucarian.Theming;
using Deucarian.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.UI
{
    internal static class ViewerNavigationToolbarChrome
    {
        private const int ButtonCount = 4;
        private const string OrbitIconResource =
            "Deucarian/ViewerNavigationOrbit";
        private const string FlyIconResource =
            "Deucarian/ViewerNavigationFly";
        private const string HomeIconResource =
            "Deucarian/ViewerNavigationRecenter";
        private const string TopDownIconResource =
            "Deucarian/ViewerNavigationOrthographic";
        private const string PerspectiveIconResource =
            "Deucarian/ViewerNavigationPerspective";

        public static void Apply(
            VisualElement root,
            VisualElement toolbar,
            Button[] buttons,
            VisualElement[] icons,
            DeucarianTheme theme,
            DeucarianThemeStyle style)
        {
            DeucarianControlIslandProfile profile =
                DeucarianControlIslandProfiles.Resolve(style);
            ApplyRoot(root, profile);
            ApplyToolbar(toolbar, theme, style, profile);
            ApplyButtons(buttons, style, profile);
            ApplyIcons(icons, profile);
        }

        public static void ApplyStateClasses(
            Button button,
            DeucarianIconButtonVisualState state)
        {
            if (button == null)
            {
                return;
            }

            button.EnableInClassList(
                ViewerNavigationToolbarClassNames.Active,
                state.Active);
            button.EnableInClassList(
                ViewerNavigationToolbarClassNames.Inactive,
                state.Inactive);
            button.EnableInClassList(
                ViewerNavigationToolbarClassNames.Disabled,
                state.Disabled);
            button.EnableInClassList(
                ViewerNavigationToolbarClassNames.Focused,
                state.Focused);
        }

        private static void ApplyRoot(
            VisualElement root,
            DeucarianControlIslandProfile profile)
        {
            if (root == null)
            {
                return;
            }

            root.style.backgroundColor = StyleKeyword.Null;
            root.style.justifyContent = Justify.FlexEnd;
            root.style.alignItems = Align.Center;
            root.style.paddingBottom =
                DeucarianControlIslandStyle.ResolveStackedBottomPadding(
                    0,
                    DeucarianControlIslandStyle.DefaultBottomOffset,
                    profile.RowHeight,
                    DeucarianControlIslandStyle.DefaultRowGap);
        }

        private static void ApplyToolbar(
            VisualElement toolbar,
            DeucarianTheme theme,
            DeucarianThemeStyle style,
            DeucarianControlIslandProfile profile)
        {
            if (toolbar == null)
            {
                return;
            }

            float width = profile.CalculatePanelWidth(ButtonCount);
            toolbar.style.position = Position.Relative;
            toolbar.style.left = StyleKeyword.Null;
            toolbar.style.right = StyleKeyword.Null;
            toolbar.style.top = StyleKeyword.Null;
            toolbar.style.bottom = StyleKeyword.Null;
            toolbar.style.marginLeft = 0f;
            toolbar.style.width = width;
            toolbar.style.minWidth = width;
            toolbar.style.maxWidth = width;
            DeucarianControlIslandStyle.ApplyPanel(
                toolbar,
                profile.CreatePanelChrome(width),
                style);

            Color surface = ViewerNavigationToolbarTheme.Resolve(
                theme,
                DeucarianBuiltinColorRoleIds.SurfaceRaised,
                new Color(0.11f, 0.14f, 0.18f, 0.94f));
            if (!DeucarianUIToolkitGlassPanel.Apply(
                    toolbar,
                    theme,
                    surface,
                    style))
            {
                toolbar.style.backgroundColor = surface;
            }

            toolbar.style.scale = new Scale(Vector3.one);
        }

        private static void ApplyButtons(
            Button[] buttons,
            DeucarianThemeStyle style,
            DeucarianControlIslandProfile profile)
        {
            if (buttons == null)
            {
                return;
            }

            DeucarianIconButtonChrome chrome =
                profile.CreateIconButtonChrome(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                DeucarianControlIslandStyle.ApplyIconButton(
                    button,
                    chrome,
                    style,
                    profile.VerticalPadding);
                button.style.visibility = Visibility.Visible;
                button.style.opacity = 1f;
                button.style.flexDirection = FlexDirection.Row;
                button.style.backgroundImage = StyleKeyword.Null;
                button.style.fontSize = 0f;
                button.style.whiteSpace = WhiteSpace.NoWrap;
                button.style.unityTextAlign = TextAnchor.MiddleCenter;
                button.style.scale = new Scale(Vector3.one);
            }
        }

        private static void ApplyIcons(
            VisualElement[] icons,
            DeucarianControlIslandProfile profile)
        {
            if (icons == null)
            {
                return;
            }

            string[] resources =
            {
                OrbitIconResource,
                FlyIconResource,
                HomeIconResource,
                TopDownIconResource,
                PerspectiveIconResource
            };
            DeucarianIconButtonChrome chrome =
                profile.CreateIconButtonChrome(true);
            int count = Mathf.Min(icons.Length, resources.Length);
            for (int i = 0; i < count; i++)
            {
                VisualElement icon = icons[i];
                if (icon == null)
                {
                    continue;
                }

                DeucarianControlIslandStyle.ApplyIcon(icon, chrome, true);
                icon.style.visibility = Visibility.Visible;
                icon.style.marginLeft = 0f;
                icon.style.marginRight = 0f;
                Texture2D texture = Resources.Load<Texture2D>(resources[i]);
                icon.style.backgroundImage = texture != null
                    ? new StyleBackground(texture)
                    : StyleKeyword.Null;
            }
        }
    }
}
