using System;

namespace Deucarian.ViewerNavigation
{
    /// <summary>
    /// Shared animation gate with an injectable policy function.
    /// </summary>
    public sealed class ViewerNavigationAnimationPolicy : IViewerNavigationAnimationPolicy
    {
        private readonly Func<bool> shouldAnimate;

        public ViewerNavigationAnimationPolicy(Func<bool> shouldAnimate = null)
        {
            this.shouldAnimate = shouldAnimate;
        }

        public bool ShouldAnimate => shouldAnimate == null || shouldAnimate();
    }
}
