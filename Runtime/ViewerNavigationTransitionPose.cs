using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    internal static class ViewerNavigationTransitionPose
    {
        public static void ResolveAnimationPoses(DeucarianCameraPose start, DeucarianCameraPose target,
            Vector3 pivot, out DeucarianCameraPose animationStart, out DeucarianCameraPose animationTarget)
        {
            animationStart = start.Orthographic && !target.Orthographic
                ? DeucarianCameraFraming.CreateVisibleTopDownTransitionPose(start, pivot, target.FieldOfView)
                : start;
            animationTarget = !start.Orthographic && target.Orthographic
                ? DeucarianCameraFraming.CreateVisibleTopDownTransitionPose(target, pivot, start.FieldOfView)
                : target;
        }

        public static void Apply(
            Camera navigationCamera,
            DeucarianCameraPose start,
            DeucarianCameraPose target,
            float movement,
            float rotation)
        {
            DeucarianCameraPose frame = new DeucarianCameraPose(
                Vector3.LerpUnclamped(start.Position, target.Position, movement),
                Quaternion.Slerp(start.Rotation, target.Rotation, rotation),
                start.Orthographic,
                Mathf.Lerp(start.OrthographicSize, target.OrthographicSize, movement),
                Mathf.Lerp(start.FieldOfView, target.FieldOfView, movement));
            frame.ApplyTo(navigationCamera);
        }

    }
}
