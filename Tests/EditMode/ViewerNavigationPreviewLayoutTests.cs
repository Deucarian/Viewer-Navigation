using System.Collections;
using Deucarian.Editor;
using Deucarian.ViewerNavigation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationPreviewLayoutTests
    {
        [UnityTest]
        public IEnumerator PreviewToolbarWrapsWithoutClippingOrOverlappingButtons()
        {
            var host = ScriptableObject.CreateInstance<PreviewLayoutHost>();
            using (var preview = new ViewerNavigationPreview(() => null))
            {
                try
                {
                    host.Show();
                    var root = DeucarianEditorInspector.CreateToolkit();
                    host.rootVisualElement.Add(root);
                    root.Add(preview.Root);
                    foreach (int width in new[] { 300, 480, 900 })
                    {
                        host.position = new Rect(30, 30, width, 700);
                        for (int i = 0; i < 5; i++) yield return null;
                        var bar = root.Q("viewer-preview-toolbar");
                        var buttons = bar.Query<Button>().ToList();
                        Assert.AreEqual(5, buttons.Count);
                        for (int i = 0; i < buttons.Count; i++)
                        {
                            var rect = buttons[i].worldBound;
                            Assert.Greater(rect.width, 100);
                            Assert.GreaterOrEqual(rect.xMin, preview.Root.worldBound.xMin);
                            Assert.LessOrEqual(rect.xMax, preview.Root.worldBound.xMax + 1);
                            for (int j = i + 1; j < buttons.Count; j++)
                                Assert.IsFalse(rect.Overlaps(buttons[j].worldBound));
                        }
                    }
                }
                finally { host.Close(); }
            }
        }
        private sealed class PreviewLayoutHost : EditorWindow { }
    }
}
