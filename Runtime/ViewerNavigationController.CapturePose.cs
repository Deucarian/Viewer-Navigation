using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    public sealed partial class ViewerNavigationController
    {
        /// <summary>
        /// Restores an authored camera pose through the normal cancellable
        /// navigation transition, without deriving a new position from bounds.
        /// </summary>
        public bool TryRestorePose(DeucarianCameraPose pose, Vector3 pivot,
            out string message, bool animate = true)
            => TryRestorePoseCore(pose, pivot, out message, animate, null);

        /// <summary>Uses a per-action duration at default sensitivity, preserving navigation policy and curves.</summary>
        public bool TryRestorePose(DeucarianCameraPose pose, Vector3 pivot,
            out string message, float durationSeconds, bool animate = true)
            => TryRestorePoseCore(pose, pivot, out message, animate, durationSeconds);

        private bool TryRestorePoseCore(DeucarianCameraPose pose, Vector3 pivot,
            out string message, bool animate, float? durationSeconds)
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
