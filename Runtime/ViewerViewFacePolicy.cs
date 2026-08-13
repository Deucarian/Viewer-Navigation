using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    public static class ViewerViewFacePolicy
    {
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
