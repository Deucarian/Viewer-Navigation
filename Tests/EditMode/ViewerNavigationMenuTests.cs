using Deucarian.ViewerNavigation.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationMenuTests
    {
        [Test]
        public void StandaloneOpenApiHasNoNormalToolsMenuEntry()
        {
            var method = typeof(ViewerNavigationManagerWindow)
                .GetMethod(nameof(ViewerNavigationManagerWindow.OpenWindow));

            Assert.That(method, Is.Not.Null);
            Assert.That(
                method.GetCustomAttributes(typeof(MenuItem), false),
                Is.Empty);
        }
    }
}
