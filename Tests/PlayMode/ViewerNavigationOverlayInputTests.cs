using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationOverlayInputTests
    {
        [UnityTest]
        public IEnumerator TransientOverlayBlocksSceneInputUntilDisabled()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.scale = 1f;
            var carrier = new GameObject("TransientInputTest");
            carrier.hideFlags = HideFlags.DontSave;
            var document = carrier.AddComponent<UIDocument>();
            document.panelSettings = settings;
            var root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.left = root.style.top = 0;
            root.style.right = root.style.bottom = 0;
            var button = new Button();
            button.style.position = Position.Absolute;
            button.style.left = button.style.top = 24;
            button.style.width = 80;
            button.style.height = 40;
            root.Add(button);
            try
            {
                for (int i = 0; i < 6; i++) yield return null;
                Assert.Greater(button.worldBound.width, 0);
                Vector2 point = button.worldBound.center;
                Vector2 screenPoint = new Vector2(point.x, Screen.height - point.y);
                Assert.AreSame(button, root.panel.Pick(point));
                var blocker = new ViewerNavigationUiInputBlocker();
                Assert.IsTrue(blocker.IsPointerInputBlocked(screenPoint),
                    "Transient menu documents must prevent a click from selecting the scene behind them.");
                document.enabled = false;
                yield return null;
                Assert.IsFalse(blocker.IsPointerInputBlocked(screenPoint),
                    "Disabled overlays must stop blocking scene input.");
            }
            finally
            {
                Object.DestroyImmediate(carrier);
                Object.DestroyImmediate(settings);
            }
        }
    }
}
