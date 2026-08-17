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
            UsesSharedMotionPreference = shouldAnimate == null;
            this.shouldAnimate = shouldAnimate ??
                (() => ViewerNavigationMotionPreferences.ShouldAnimate);
        }

        /// <summary>
        /// True when this policy uses the package-owned runtime and browser
        /// accessibility preference rather than a host override.
        /// </summary>
        public bool UsesSharedMotionPreference { get; }

        public bool ShouldAnimate => shouldAnimate();
    }
}
