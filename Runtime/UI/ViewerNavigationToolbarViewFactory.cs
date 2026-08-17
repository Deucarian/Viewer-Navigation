using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.UI
{
    internal static class ViewerNavigationToolbarClassNames
    {
        public const string Root = "deucarian-viewer-navigation-root";
        public const string Toolbar = "deucarian-viewer-navigation-toolbar";
        public const string GlassPanel = "deucarian-viewer-navigation-glass";
        public const string Button = "deucarian-viewer-navigation-button";
        public const string Icon = "deucarian-viewer-navigation-icon";
        public const string Active =
            "deucarian-viewer-navigation-button--active";
        public const string Inactive =
            "deucarian-viewer-navigation-button--inactive";
        public const string Disabled =
            "deucarian-viewer-navigation-button--disabled";
        public const string Focused =
            "deucarian-viewer-navigation-button--focused";
    }

    internal static class ViewerNavigationToolbarViewFactory
    {
        public static void BuildFallback(VisualElement documentRoot)
        {
            if (documentRoot == null)
            {
                return;
            }

            VisualElement root = new VisualElement
            {
                name = ViewerNavigationToolbarPresenter.RootName
            };
            VisualElement toolbar = new VisualElement
            {
                name = ViewerNavigationToolbarPresenter.ToolbarName
            };
            toolbar.Add(CreateButton(
                ViewerNavigationToolbarPresenter.OrbitButtonName,
                ViewerNavigationToolbarPresenter.OrbitIconName));
            toolbar.Add(CreateButton(
                ViewerNavigationToolbarPresenter.FlyButtonName,
                ViewerNavigationToolbarPresenter.FlyIconName));
            toolbar.Add(CreateButton(
                ViewerNavigationToolbarPresenter.HomeButtonName,
                ViewerNavigationToolbarPresenter.HomeIconName));

            Button topDown = CreateButton(
                ViewerNavigationToolbarPresenter.TopDownButtonName,
                ViewerNavigationToolbarPresenter.TopDownIconName);
            topDown.Add(CreateIcon(
                ViewerNavigationToolbarPresenter.PerspectiveIconName));
            toolbar.Add(topDown);
            root.Add(toolbar);
            documentRoot.Add(root);
        }

        public static void ConfigureView(
            VisualElement documentRoot,
            VisualElement root,
            VisualElement toolbar,
            Button[] buttons,
            VisualElement[] icons)
        {
            ConfigureFullScreen(documentRoot);
            ConfigureFullScreen(root);
            if (documentRoot != null)
            {
                documentRoot.pickingMode = PickingMode.Ignore;
                documentRoot.style.backgroundColor = Color.clear;
                documentRoot.style.backgroundImage = StyleKeyword.Null;
            }

            if (root != null)
            {
                root.pickingMode = PickingMode.Ignore;
                root.AddToClassList(ViewerNavigationToolbarClassNames.Root);
            }

            if (toolbar != null)
            {
                toolbar.pickingMode = PickingMode.Position;
                toolbar.AddToClassList(
                    ViewerNavigationToolbarClassNames.Toolbar);
                toolbar.AddToClassList(
                    ViewerNavigationToolbarClassNames.GlassPanel);
            }

            ConfigureButtons(buttons);
            ConfigureIcons(icons);
        }

        private static Button CreateButton(string buttonName, string iconName)
        {
            Button button = new Button
            {
                name = buttonName,
                text = string.Empty
            };
            button.Add(CreateIcon(iconName));
            return button;
        }

        private static VisualElement CreateIcon(string name)
        {
            return new VisualElement { name = name };
        }

        private static void ConfigureButtons(Button[] buttons)
        {
            if (buttons == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                button.text = string.Empty;
                button.pickingMode = PickingMode.Position;
                button.AddToClassList(
                    ViewerNavigationToolbarClassNames.Button);
                button.AddToClassList(
                    ViewerNavigationToolbarClassNames.Inactive);
            }
        }

        private static void ConfigureIcons(VisualElement[] icons)
        {
            if (icons == null)
            {
                return;
            }

            for (int i = 0; i < icons.Length; i++)
            {
                VisualElement icon = icons[i];
                if (icon == null)
                {
                    continue;
                }

                icon.pickingMode = PickingMode.Ignore;
                icon.AddToClassList(
                    ViewerNavigationToolbarClassNames.Icon);
            }
        }

        private static void ConfigureFullScreen(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            element.style.display = DisplayStyle.Flex;
            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.right = 0f;
            element.style.top = 0f;
            element.style.bottom = 0f;
            element.style.width = Length.Percent(100f);
            element.style.height = Length.Percent(100f);
            element.style.minHeight = Length.Percent(100f);
            element.style.flexGrow = 1f;
            element.style.flexShrink = 0f;
        }
    }
}
