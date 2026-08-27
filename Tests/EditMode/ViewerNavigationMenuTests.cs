using Deucarian.ViewerNavigation.Editor;
using NUnit.Framework;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationMenuTests
    {
        [Test]
        public void PackageExposesDirectCapabilityMenu()
        {
            Assert.AreEqual(
                "Tools/Deucarian/Viewer Navigation",
                ViewerNavigationManagerWindow.MenuPath);
        }
    }
}
