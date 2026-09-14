using System.Collections;
using Deucarian.Theming;
using Deucarian.UI;
using Deucarian.ViewerNavigation.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationControlAlignmentTests
    {
        [UnityTest]
        public IEnumerator NavigationIconsStayCenteredAfterConsumerChromeAndSelection()
        {
            var window = ScriptableObject.CreateInstance<AlignmentWindow>();
            window.Show();
            try
            {
                var root = window.rootVisualElement;
                var toolbar = new VisualElement(); root.Add(toolbar);
                var buttons = new Button[4]; var icons = new VisualElement[4];
                for (int i = 0; i < 4; i++)
                {
                    buttons[i] = new Button(); icons[i] = new VisualElement();
                    buttons[i].Add(icons[i]); toolbar.Add(buttons[i]);
                }
                var theme = DeucarianViewerReferenceThemePreset.Resolve().DarkTheme;
                ViewerNavigationToolbarChrome.Apply(root, toolbar, buttons, icons, theme, theme.VisualStyle);
                var palette = ViewerNavigationToolbarTheme.ResolvePalette(theme);
                for (int i = 0; i < 4; i++)
                    DeucarianControlIslandTheme.ApplyIconButtonState(buttons[i], icons[i], theme,
                        new DeucarianIconButtonVisualState(true, true, i == 0, false, false, false));
                for (int i = 0; i < 6; i++) yield return null;
                for (int i = 0; i < 4; i++)
                    Assert.Less(Vector2.Distance(buttons[i].worldBound.center, icons[i].worldBound.center), .75f);
                Assert.IsTrue(palette.AutoContrast);
                Assert.GreaterOrEqual(DeucarianForegroundContrast.Ratio(
                    icons[0].style.unityBackgroundImageTintColor.value, buttons[0].style.backgroundColor.value), 3f);
            }
            finally { window.Close(); Object.DestroyImmediate(window); }
        }

        private sealed class AlignmentWindow : EditorWindow { }
    }
}
