using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    public sealed partial class ViewerNavigationController
    {
        /// <summary>
        /// Restores an authored camera pose through the normal cancellable
        /// navigation transition, without deriving a new position from bounds.
        /// Optional duration is measured at default global sensitivity.
        /// </summary>
        public bool TryRestorePose(DeucarianCameraPose pose, Vector3 pivot,
            out string message, bool animate = true, float? durationSeconds = null)
        {
            if (!ViewerNavigationPoseValidation.IsValid(pose, pivot))
            {
                message = "Camera pose is invalid.";
                return false;
            }

            bool accepted = MoveCameraToPose(pose, ResolveNavigationBounds(), pivot,
                ViewerNavigationTransitionKind.Frame, animate, false, durationSeconds: durationSeconds);
            message = accepted ? "Camera pose accepted." : "Camera pose was not accepted.";
            return accepted;
        }
    }
}
