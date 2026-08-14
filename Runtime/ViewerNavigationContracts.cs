using System;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    public enum ViewerNavigationMode
    {
        Orbit,
        Fly
    }

    public enum ViewerViewFace
    {
        Top,
        Bottom,
        Front,
        Back,
        Left,
        Right
    }

    public enum ViewerNavigationTransitionKind
    {
        None,
        Frame,
        ReturnToOrigin,
        EnterTopDown,
        ExitTopDown,
        ViewFace
    }

    public interface IViewerNavigationInputBlocker
    {
        bool IsPointerInputBlocked(Vector2 screenPosition);
        bool IsKeyboardInputBlocked();
    }

    public interface IViewerNavigationMotionProfile
    {
        bool AnimateTransitions { get; }
        float TransitionMatchFieldOfView { get; }
        float CalculateTransitionDuration(float distance);
        float EvaluateMovement(float normalizedTime);
        float EvaluateRotation(float normalizedTime);
    }

    public interface IViewerNavigationAnimationPolicy
    {
        bool ShouldAnimate { get; }
    }

    public readonly struct ViewerNavigationSnapshot : IEquatable<ViewerNavigationSnapshot>
    {
        public ViewerNavigationSnapshot(
            ViewerNavigationMode mode,
            bool isTopDown,
            bool hasReferenceBounds,
            bool hasOrigin,
            bool isTransitioning,
            ViewerNavigationTransitionKind transitionKind,
            uint revision)
        {
            Mode = mode;
            IsTopDown = isTopDown;
            HasReferenceBounds = hasReferenceBounds;
            HasOrigin = hasOrigin;
            IsTransitioning = isTransitioning;
            TransitionKind = transitionKind;
            Revision = revision;
        }

        public ViewerNavigationMode Mode { get; }
        public bool IsTopDown { get; }
        public bool HasReferenceBounds { get; }
        public bool HasOrigin { get; }
        public bool IsTransitioning { get; }
        public ViewerNavigationTransitionKind TransitionKind { get; }
        public uint Revision { get; }

        public bool Equals(ViewerNavigationSnapshot other)
        {
            return Mode == other.Mode &&
                   IsTopDown == other.IsTopDown &&
                   HasReferenceBounds == other.HasReferenceBounds &&
                   HasOrigin == other.HasOrigin &&
                   IsTransitioning == other.IsTransitioning &&
                   TransitionKind == other.TransitionKind &&
                   Revision == other.Revision;
        }

        public override bool Equals(object obj) =>
            obj is ViewerNavigationSnapshot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Mode;
                hash = hash * 397 ^ IsTopDown.GetHashCode();
                hash = hash * 397 ^ HasReferenceBounds.GetHashCode();
                hash = hash * 397 ^ HasOrigin.GetHashCode();
                hash = hash * 397 ^ IsTransitioning.GetHashCode();
                hash = hash * 397 ^ (int)TransitionKind;
                hash = hash * 397 ^ (int)Revision;
                return hash;
            }
        }
    }
}
