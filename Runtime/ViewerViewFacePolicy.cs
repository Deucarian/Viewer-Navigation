using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    public static class ViewerViewFacePolicy
    {
        internal static Bounds CreatePivotCenteredBounds(Bounds bounds, Vector3 pivot)
        {
            Vector3 centerOffset = bounds.center - pivot;
            Vector3 extents = bounds.extents + new Vector3(
                Mathf.Abs(centerOffset.x),
                Mathf.Abs(centerOffset.y),
                Mathf.Abs(centerOffset.z));
            return new Bounds(pivot, extents * 2f);
        }

        public static Vector3 GetDirectionFromTargetToCamera(ViewerViewFace face)
        {
            switch (face)
            {
                case ViewerViewFace.Top:
                    return Vector3.up;
                case ViewerViewFace.Bottom:
                    return Vector3.down;
                case ViewerViewFace.Front:
                    return Vector3.back;
                case ViewerViewFace.Back:
                    return Vector3.forward;
                case ViewerViewFace.Left:
                    return Vector3.left;
                case ViewerViewFace.Right:
                    return Vector3.right;
                default:
                    return Vector3.back;
            }
        }

        public static string GetLabel(ViewerViewFace face)
        {
            switch (face)
            {
                case ViewerViewFace.Top:
                    return "TOP";
                case ViewerViewFace.Bottom:
                    return "BOT";
                case ViewerViewFace.Front:
                    return "F";
                case ViewerViewFace.Back:
                    return "B";
                case ViewerViewFace.Left:
                    return "L";
                case ViewerViewFace.Right:
                    return "R";
                default:
                    return "?";
            }
        }
    }
}
