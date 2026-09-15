using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    internal static class ViewerNavigationPoseValidation
    {
        internal static bool IsFinite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        internal static bool IsValid(DeucarianCameraPose pose, Vector3 pivot)
        {
            Quaternion rotation = pose.Rotation;
            return IsFinite(pose.Position) && IsFinite(pivot) &&
                IsFinite(new Vector3(rotation.x, rotation.y, rotation.z)) &&
                !float.IsNaN(rotation.w) && !float.IsInfinity(rotation.w) &&
                Mathf.Abs(Quaternion.Dot(rotation, rotation) - 1f) <= 0.001f &&
                pose.FieldOfView > 0f && pose.FieldOfView < 180f &&
                !float.IsInfinity(pose.OrthographicSize) && pose.OrthographicSize > 0f;
        }
    }
}
