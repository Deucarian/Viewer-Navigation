using System.Linq;
using Deucarian.Editor;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ControlCenterRegistrationTests
    {
        private const string PackageId =
            "com.deucarian.viewer-navigation";

        [Test]
        public void ReturningToThePageKeepsRuntimeDetailsExpanded()
        {
            Assert.IsTrue(DeucarianToolRegistry.TryGet(DeucarianToolIds.ViewerNavigation, out var tool));
            using (var page = tool.CreatePage())
            {
                var details = page.Root.Query<Foldout>().ToList().Single(f => f.text == "Runtime state"); details.value = true;
                page.Deactivate(); page.Activate(null);
                Assert.IsTrue(page.Root.Contains(details));
                Assert.IsTrue(details.value);
            }
        }

        [Test]
        public void PackageRegistersStableToolAndCard()
        {
            Assert.That(
                DeucarianToolRegistry.TryGet(
                    DeucarianToolIds.ViewerNavigation,
                    out DeucarianToolDescriptor tool),
                Is.True);
            Assert.That(tool.OwningPackage, Is.EqualTo(PackageId));

            DeucarianControlCenterSnapshot snapshot =
                DeucarianControlCenterSnapshotBuilder.Capture(true);
            Assert.That(
                snapshot.Cards.Any(
                    card => card.OwningPackage == PackageId),
                Is.True);
            DeucarianControlCenterCard card = snapshot.Cards.Single(
                candidate => candidate.Id == PackageId + ".workflow");
            Assert.That(
                card.Details.Any(detail =>
                    detail.StartsWith("Scene installers:")),
                Is.True);
            Assert.That(card.StatusText, Is.Not.EqualTo("Edit Mode"));
            Assert.That(card.StatusText, Is.Not.EqualTo("Play Mode"));
        }
    }
}
