using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    internal static class ViewerNavigationTransitionTiming
    {
        internal static float ResolveDuration(DeucarianCameraPose start, DeucarianCameraPose target,
            IViewerNavigationMotionProfile motionProfile, float sensitivity)
        {
            // A stationary camera can still make a large turn or zoom. Express that work
            // in the same profile units used for framing and returning to the origin.
            const float defaultAngularSpeed = 90f;
            float angularDistance = Mathf.Max(Quaternion.Angle(start.Rotation, target.Rotation),
                Mathf.Abs(start.FieldOfView - target.FieldOfView));
            float equivalentDistance = angularDistance *
                ViewerNavigationSettings.DefaultTransitionSpeed / defaultAngularSpeed;
            float distance = Mathf.Max(Vector3.Distance(start.Position, target.Position), equivalentDistance);
            if (start.Orthographic && target.Orthographic)
                distance = Mathf.Max(distance, Mathf.Abs(start.OrthographicSize - target.OrthographicSize));
            return motionProfile.CalculateTransitionDuration(distance) *
                DeucarianCameraNavigationControls.DefaultGlobalSensitivity / Mathf.Max(0.01f, sensitivity);
        }

    }
}
