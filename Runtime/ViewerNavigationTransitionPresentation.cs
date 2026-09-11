using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    /// <summary>Applies one transition frame or projection switch; it never owns navigation state.</summary>
    internal static class ViewerNavigationTransitionPresentation
    {
        internal static void PreparePerspectiveStart(Camera camera, DeucarianCameraPose orthographicStart,
            DeucarianCameraPose visiblePerspectiveStart, Vector3 pivot, IViewerNavigationMotionProfile profile)
        {
            var match = DeucarianCameraFraming.CreatePerspectiveMatchPoseForOrthographicSwitch(
                orthographicStart, pivot, ViewerNavigationConfigurationResolver.ResolveTransitionMatchFieldOfView(profile));
            match.ApplyTo(camera);
            visiblePerspectiveStart.ApplyTo(camera);
        }

        internal static void Commit(Camera camera, DeucarianCameraPose target,
            Bounds bounds, Vector3 pivot, IViewerNavigationMotionProfile profile)
        {
            if (!camera.orthographic && target.Orthographic)
            {
                var match = DeucarianCameraFraming.CreatePerspectiveMatchPoseForOrthographicSwitch(
                    target, pivot, ViewerNavigationConfigurationResolver.ResolveTransitionMatchFieldOfView(profile));
                match.ApplyTo(camera);
            }
            target.ApplyTo(camera);
            DeucarianCameraFraming.ConfigureClipPlanes(camera, bounds);
        }

    }
}
