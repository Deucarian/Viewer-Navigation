using System;
using Deucarian.CameraNavigation;
using Deucarian.Theming;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    /// <summary>
    /// Resolves the canonical reusable navigation preset and its runtime policies.
    /// </summary>
    public static class ViewerNavigationReferenceComposition
    {
        /// <summary>
        /// Compose the canonical reusable preset and helpers used by reference-viewer
        /// consumers.
        /// </summary>
        public static ViewerNavigationReferenceCompositionProfile Resolve(
            IViewerNavigationAnimationPolicy animationPolicy = null)
        {
            DeucarianViewerReferenceThemeProfile themeProfile =
                DeucarianViewerReferenceThemePreset.Resolve();
            return new ViewerNavigationReferenceCompositionProfile(
                ViewerNavigationSettings.LoadReferencePreset(),
                new ViewerNavigationUiInputBlocker(),
                new ViewerNavigationMeshBoundsStrategy(),
                animationPolicy ?? new ViewerNavigationAnimationPolicy(),
                themeProfile,
                DeucarianViewerReferenceThemePreset.DefaultMode);
        }
    }

    /// <summary>
    /// A reusable profile for reference navigation composition.
    /// </summary>
    public readonly struct ViewerNavigationReferenceCompositionProfile
    {
        public ViewerNavigationReferenceCompositionProfile(
            ViewerNavigationSettings preset,
            IViewerNavigationInputBlocker inputBlocker,
            IDeucarianFramingBoundsStrategy<GameObject> boundsStrategy,
            IViewerNavigationAnimationPolicy animationPolicy,
            DeucarianViewerReferenceThemeProfile themeProfile,
            DeucarianThemeMode themeMode)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            if (themeProfile == null)
            {
                throw new ArgumentNullException(nameof(themeProfile));
            }

            Preset = preset;
            InputBlocker = inputBlocker;
            BoundsStrategy = boundsStrategy;
            AnimationPolicy = animationPolicy;
            ThemeProfile = themeProfile;
            ThemeMode = themeMode;
        }

        public ViewerNavigationSettings Preset { get; }
        public IViewerNavigationInputBlocker InputBlocker { get; }
        public IDeucarianFramingBoundsStrategy<GameObject> BoundsStrategy { get; }
        public IViewerNavigationAnimationPolicy AnimationPolicy { get; }
        public DeucarianViewerReferenceThemeProfile ThemeProfile { get; }
        public DeucarianThemeMode ThemeMode { get; }

        /// <summary>
        /// Returns the same reference composition with an intentional settings override.
        /// Runtime policy and theme object identities are preserved.
        /// </summary>
        public ViewerNavigationReferenceCompositionProfile WithPreset(
            ViewerNavigationSettings preset)
        {
            return new ViewerNavigationReferenceCompositionProfile(
                preset,
                InputBlocker,
                BoundsStrategy,
                AnimationPolicy,
                ThemeProfile,
                ThemeMode);
        }

        public ViewerNavigationInstaller Compose(Transform parent, Camera camera) =>
            ViewerNavigationInstaller.CreateWithReferenceComposition(
                parent,
                camera,
                this);
    }
}
