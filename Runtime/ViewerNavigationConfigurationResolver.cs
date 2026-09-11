using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    internal static class ViewerNavigationConfigurationResolver
    {
        public static DeucarianCameraNavigationControls ResolveControls(
            ViewerNavigationSettings configuration)
        {
            return configuration != null && configuration.Controls != null
                ? configuration.Controls
                : Resources.Load<DeucarianCameraNavigationControls>(
                    DeucarianCameraNavigationControls.CanonicalResourcesPath);
        }

        public static IDeucarianCameraFramingSettings ResolveFramingSettings(
            ViewerNavigationSettings configuration)
        {
            return configuration != null && configuration.FramingSettings != null
                ? configuration.FramingSettings
                : Resources.Load<DeucarianCameraFramingSettings>(
                    DeucarianCameraFramingSettings.CanonicalResourcesPath);
        }

        public static float ResolveTransitionMatchFieldOfView(IViewerNavigationMotionProfile motionProfile)
        {
            float value = motionProfile != null
                ? motionProfile.TransitionMatchFieldOfView
                : ViewerNavigationSettings.DefaultTransitionMatchFieldOfView;
            return float.IsNaN(value) || float.IsInfinity(value)
                ? ViewerNavigationSettings.DefaultTransitionMatchFieldOfView
                : value;
        }
    }
}
