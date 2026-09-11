using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    internal static class ViewerNavigationTransitionPose
    {
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
