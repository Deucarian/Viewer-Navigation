using System;

namespace Deucarian.ViewerNavigation
{
    public sealed class ViewerNavigationStateStore
    {
        private ViewerNavigationMode mode = ViewerNavigationMode.Orbit;
        private bool isTopDown;
        private bool hasReferenceBounds;
        private bool hasOrigin;
        private bool isTransitioning;
        private ViewerNavigationTransitionKind transitionKind;
        private uint revision;

        public event Action<ViewerNavigationSnapshot> Changed;

        public ViewerNavigationSnapshot Snapshot => new ViewerNavigationSnapshot(
            mode,
            isTopDown,
            hasReferenceBounds,
            hasOrigin,
            isTransitioning,
            transitionKind,
            revision);

        public bool SetMode(ViewerNavigationMode value)
        {
            if (mode == value)
            {
                return false;
            }

            mode = value;
            Publish();
            return true;
        }

        public bool SetTopDown(bool value)
        {
            if (isTopDown == value)
            {
                return false;
            }

            isTopDown = value;
            Publish();
            return true;
        }

        public bool SetReferenceBounds(bool value)
        {
            if (hasReferenceBounds == value)
            {
                return false;
            }

            hasReferenceBounds = value;
            Publish();
            return true;
        }

        public bool SetOrigin(bool value)
        {
            if (hasOrigin == value)
            {
                return false;
            }

            hasOrigin = value;
            Publish();
            return true;
        }

        public void BeginTransition(ViewerNavigationTransitionKind kind)
        {
            isTransitioning = kind != ViewerNavigationTransitionKind.None;
            transitionKind = kind;
            Publish();
        }

        public bool EndTransition()
        {
            if (!isTransitioning && transitionKind == ViewerNavigationTransitionKind.None)
            {
                return false;
            }

            isTransitioning = false;
            transitionKind = ViewerNavigationTransitionKind.None;
            Publish();
            return true;
        }

        public void ResetReference()
        {
            bool changed = hasReferenceBounds || hasOrigin || isTopDown || isTransitioning;
            hasReferenceBounds = false;
            hasOrigin = false;
            isTopDown = false;
            isTransitioning = false;
            transitionKind = ViewerNavigationTransitionKind.None;
            if (changed)
            {
                Publish();
            }
        }

        private void Publish()
        {
            revision++;
            Changed?.Invoke(Snapshot);
        }
    }
}
