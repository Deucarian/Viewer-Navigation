using System.Runtime.InteropServices;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    /// <summary>
    /// Resolves the shared runtime motion preferences used by reference viewers.
    /// </summary>
    public static class ViewerNavigationMotionPreferences
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int
            DeucarianViewerNavigationPrefersReducedMotion();
#endif

        /// <summary>
        /// True when the runtime platform asks viewers to minimize non-essential
        /// motion. This maps to prefers-reduced-motion in WebGL and is false on
        /// platforms without that browser preference.
        /// </summary>
        public static bool PrefersReducedMotion =>
            QueryPrefersReducedMotion();

        /// <summary>
        /// True when runtime navigation transitions should animate.
        /// </summary>
        public static bool ShouldAnimate => ResolveShouldAnimate(
            Application.isPlaying,
            PrefersReducedMotion);

        internal static bool ResolveShouldAnimate(
            bool isPlaying,
            bool prefersReducedMotion)
        {
            return isPlaying && !prefersReducedMotion;
        }

        private static bool QueryPrefersReducedMotion()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return DeucarianViewerNavigationPrefersReducedMotion() == 1;
#else
            return false;
#endif
        }
    }
}
