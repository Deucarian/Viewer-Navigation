using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    /// <summary>Applies one transition frame or projection switch; it never owns navigation state.</summary>
    internal static class ViewerNavigationTransitionPresentation
    {
        internal static void ApplyFrame(Camera camera, DeucarianCameraPose start,
            DeucarianCameraPose target, float movement, float rotation)
        {
            var frame = new DeucarianCameraPose(
                Vector3.LerpUnclamped(start.Position, target.Position, movement),
                Quaternion.Slerp(start.Rotation, target.Rotation, rotation),
                start.Orthographic,
                Mathf.Lerp(start.OrthographicSize, target.OrthographicSize, movement),
                Mathf.Lerp(start.FieldOfView, target.FieldOfView, movement));
            frame.ApplyTo(camera);
        }

        internal static void PreparePerspectiveStart(Camera camera, DeucarianCameraPose orthographicStart,
            DeucarianCameraPose visiblePerspectiveStart, Vector3 pivot, IViewerNavigationMotionProfile profile)
        {
            var match = DeucarianCameraFraming.CreatePerspectiveMatchPoseForOrthographicSwitch(
                orthographicStart, pivot, MatchFieldOfView(profile));
            match.ApplyTo(camera);
            visiblePerspectiveStart.ApplyTo(camera);
        }

        internal static void Commit(Camera camera, DeucarianCameraPose target,
            Bounds bounds, Vector3 pivot, IViewerNavigationMotionProfile profile)
        {
            if (!camera.orthographic && target.Orthographic)
            {
                var match = DeucarianCameraFraming.CreatePerspectiveMatchPoseForOrthographicSwitch(
                    target, pivot, MatchFieldOfView(profile));
                match.ApplyTo(camera);
            }
            target.ApplyTo(camera);
            DeucarianCameraFraming.ConfigureClipPlanes(camera, bounds);
        }

        private static float MatchFieldOfView(IViewerNavigationMotionProfile profile)
        {
            float value = profile != null ? profile.TransitionMatchFieldOfView
                : ViewerNavigationSettings.DefaultTransitionMatchFieldOfView;
            return float.IsNaN(value) || float.IsInfinity(value)
                ? ViewerNavigationSettings.DefaultTransitionMatchFieldOfView : value;
        }
    }
}
