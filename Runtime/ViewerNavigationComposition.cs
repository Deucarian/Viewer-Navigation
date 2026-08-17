using System;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    /// <summary>
    /// Shared default composition values used by multiple consumers to keep navigation feel
    /// identical.
    /// </summary>
    public static class ViewerNavigationReferenceComposition
    {
        /// <summary>
        /// Compose the canonical reusable preset and helpers used by Report Viewer,
        /// Activity Viewer, and template consumers.
        /// </summary>
        public static ViewerNavigationReferenceCompositionProfile Resolve(
            IViewerNavigationAnimationPolicy animationPolicy = null)
        {
            return new ViewerNavigationReferenceCompositionProfile(
                ViewerNavigationSettings.LoadReferencePreset(),
                new ViewerNavigationUiInputBlocker(),
                new DeucarianMeshBoundsStrategy(),
                animationPolicy);
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
            IViewerNavigationAnimationPolicy animationPolicy)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            Preset = preset;
            InputBlocker = inputBlocker;
            BoundsStrategy = boundsStrategy;
            AnimationPolicy = animationPolicy;
        }

        public ViewerNavigationSettings Preset { get; }
        public IViewerNavigationInputBlocker InputBlocker { get; }
        public IDeucarianFramingBoundsStrategy<GameObject> BoundsStrategy { get; }
        public IViewerNavigationAnimationPolicy AnimationPolicy { get; }

        public ViewerNavigationInstaller Compose(Transform parent, Camera camera) =>
            ViewerNavigationInstaller.Create(
                parent,
                camera,
                Preset,
                InputBlocker,
                BoundsStrategy,
                AnimationPolicy);
    }
}
