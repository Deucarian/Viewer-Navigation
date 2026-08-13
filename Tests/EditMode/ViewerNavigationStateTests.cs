using NUnit.Framework;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationStateTests
    {
        [Test]
        public void ModeIsAuthoritativeAndIdempotent()
        {
            ViewerNavigationStateStore store = new ViewerNavigationStateStore();
            int changes = 0;
            store.Changed += _ => changes++;

            Assert.That(store.SetMode(ViewerNavigationMode.Fly), Is.True);
            Assert.That(store.SetMode(ViewerNavigationMode.Fly), Is.False);

            Assert.That(store.Snapshot.Mode, Is.EqualTo(ViewerNavigationMode.Fly));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void ResetReferenceClearsOnlyReferenceLifecycleState()
        {
            ViewerNavigationStateStore store = new ViewerNavigationStateStore();
            store.SetMode(ViewerNavigationMode.Fly);
            store.SetReferenceBounds(true);
            store.SetOrigin(true);
            store.SetTopDown(true);
            store.BeginTransition(ViewerNavigationTransitionKind.Frame);

            store.ResetReference();

            ViewerNavigationSnapshot snapshot = store.Snapshot;
            Assert.That(snapshot.Mode, Is.EqualTo(ViewerNavigationMode.Fly));
            Assert.That(snapshot.HasReferenceBounds, Is.False);
            Assert.That(snapshot.HasOrigin, Is.False);
            Assert.That(snapshot.IsTopDown, Is.False);
            Assert.That(snapshot.IsTransitioning, Is.False);
        }

        [Test]
        public void TransitionStatePublishesStartAndCompletion()
        {
            ViewerNavigationStateStore store = new ViewerNavigationStateStore();
            uint initial = store.Snapshot.Revision;

            store.BeginTransition(ViewerNavigationTransitionKind.ViewFace);
            Assert.That(store.Snapshot.IsTransitioning, Is.True);
            Assert.That(
                store.Snapshot.TransitionKind,
                Is.EqualTo(ViewerNavigationTransitionKind.ViewFace));

            Assert.That(store.EndTransition(), Is.True);
            Assert.That(store.EndTransition(), Is.False);
            Assert.That(store.Snapshot.IsTransitioning, Is.False);
            Assert.That(store.Snapshot.Revision, Is.EqualTo(initial + 2));
        }
    }
}
