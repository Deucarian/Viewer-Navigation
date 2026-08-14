using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    public sealed partial class ViewerNavigationController
    {
        private Bounds referenceBounds = new Bounds(Vector3.zero, Vector3.one * 10f);
        private DeucarianCameraPose originPose;
        private DeucarianCameraPose topDownReturnPose;
        private bool hasTopDownReturnPose;

        public void BeginReferenceLoad()
        {
            CancelTransition();
            state.ResetReference();
            hasTopDownReturnPose = false;
            referenceBounds = new Bounds(Vector3.zero, Vector3.one * 10f);
            if (navigationRig != null)
            {
                navigationRig.ClearReferenceBounds();
                navigationRig.SetPivot(Vector3.zero);
            }

            ApplyNavigationMode();
        }

        public bool RegisterReference(
            GameObject referenceRoot,
            bool frame = true,
            bool captureOrigin = true)
        {
            if (!DeucarianCameraFraming.TryCalculateRendererBounds(
                    referenceRoot,
                    out Bounds bounds))
            {
                if (captureOrigin)
                {
                    CaptureOrigin();
                }

                ViewerNavigationLog.Navigation.Warning(
                    "Reference registration found no renderer bounds.",
                    this);
                return false;
            }

            SetReferenceBounds(bounds, bounds.center);
            if (frame)
            {
                FrameReference(false);
            }

            if (captureOrigin)
            {
                CaptureOrigin();
            }

            return true;
        }

        public bool SetReferenceBounds(Bounds bounds, Vector3 pivot)
        {
            if (!IsFinite(bounds.center) ||
                !IsFinite(bounds.size) ||
                !IsFinite(pivot) ||
                bounds.size.sqrMagnitude <= 0.00000001f)
            {
                return false;
            }

            referenceBounds = bounds;
            state.SetReferenceBounds(true);
            if (navigationRig != null)
            {
                navigationRig.SetReferenceBounds(bounds);
                navigationRig.SetPivot(pivot);
            }

            return true;
        }

        public bool CaptureOrigin()
        {
            if (navigationCamera == null)
            {
                return false;
            }

            originPose = DeucarianCameraPose.Capture(navigationCamera);
            state.SetOrigin(true);
            return true;
        }

        public bool RefreshOrigin() => CaptureOrigin();

        public bool TryGetOrigin(out DeucarianCameraPose pose)
        {
            pose = originPose;
            return HasOrigin;
        }

        public bool FrameReference(bool animate = true)
        {
            if (navigationCamera == null || !HasReferenceBounds)
            {
                return false;
            }

            Vector3 preferredForward =
                referenceBounds.center - navigationCamera.transform.position;
            DeucarianCameraPose pose = DeucarianCameraFraming.CreatePerspectiveFramePose(
                referenceBounds,
                navigationCamera,
                preferredForward,
                ResolveReferencePadding());
            return MoveCameraToPose(
                pose,
                referenceBounds,
                referenceBounds.center,
                ViewerNavigationTransitionKind.Frame,
                animate,
                false);
        }

        public bool ReturnToOrigin(bool animate = true)
        {
            if (navigationCamera == null)
            {
                return false;
            }

            if (!HasOrigin && !CaptureOrigin())
            {
                return false;
            }

            return MoveCameraToPose(
                originPose,
                ResolveNavigationBounds(),
                HasReferenceBounds ? referenceBounds.center : Vector3.zero,
                ViewerNavigationTransitionKind.ReturnToOrigin,
                animate,
                false);
        }

        public bool SetTopDown(bool enabled, bool animate = true)
        {
            bool targetsSameState =
                IsTransitioning &&
                ((enabled && ActiveTransition == ViewerNavigationTransitionKind.EnterTopDown) ||
                 (!enabled && ActiveTransition == ViewerNavigationTransitionKind.ExitTopDown));
            if (targetsSameState || (!IsTransitioning && IsTopDown == enabled))
            {
                return false;
            }

            Bounds bounds = ResolveNavigationBounds();
            if (enabled)
            {
                topDownReturnPose = DeucarianCameraPose.Capture(navigationCamera);
                hasTopDownReturnPose = navigationCamera != null;
                DeucarianCameraPose topDown = DeucarianCameraFraming.CreateTopDownPose(
                    bounds,
                    navigationCamera,
                    Pivot,
                    ResolveReferencePadding());
                return MoveCameraToPose(
                    topDown,
                    bounds,
                    Pivot,
                    ViewerNavigationTransitionKind.EnterTopDown,
                    animate,
                    true);
            }

            DeucarianCameraPose perspective = hasTopDownReturnPose
                ? topDownReturnPose
                : DeucarianCameraFraming.CreatePerspectiveFramePose(
                    bounds,
                    navigationCamera,
                    new Vector3(-1f, -0.65f, 1f),
                    ResolveReferencePadding());
            return MoveCameraToPose(
                perspective,
                bounds,
                Pivot,
                ViewerNavigationTransitionKind.ExitTopDown,
                animate,
                false);
        }

        public bool ToggleTopDown(bool animate = true) =>
            SetTopDown(!IsTopDown, animate);

        public bool NavigateToFace(ViewerViewFace face, bool animate = true)
        {
            if (face == ViewerViewFace.Top)
            {
                return SetTopDown(true, animate);
            }

            return NavigateToDirection(
                ViewerViewFacePolicy.GetDirectionFromTargetToCamera(face),
                animate);
        }

        public bool NavigateToDirection(
            Vector3 directionFromTargetToCamera,
            bool animate = true)
        {
            if (navigationCamera == null ||
                !IsFinite(directionFromTargetToCamera) ||
                directionFromTargetToCamera.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Bounds bounds = ResolveNavigationBounds();
            Vector3 pivot = Pivot;
            Bounds framingBounds = CreatePivotCenteredBounds(bounds, pivot);
            DeucarianCameraPose pose = DeucarianCameraFraming.CreateViewDirectionPose(
                framingBounds,
                navigationCamera,
                directionFromTargetToCamera,
                ResolveReferencePadding());
            return MoveCameraToPose(
                pose,
                bounds,
                pivot,
                ViewerNavigationTransitionKind.ViewFace,
                animate,
                false);
        }

        private static Bounds CreatePivotCenteredBounds(Bounds bounds, Vector3 pivot)
        {
            Vector3 centerOffset = bounds.center - pivot;
            Vector3 extents = bounds.extents + new Vector3(
                Mathf.Abs(centerOffset.x),
                Mathf.Abs(centerOffset.y),
                Mathf.Abs(centerOffset.z));
            return new Bounds(pivot, extents * 2f);
        }

        public bool TryFrame(
            DeucarianCameraFramingTarget target,
            out string message,
            bool animate = true)
        {
            if (!DeucarianCameraFraming.TryCreateCurrentProjectionFramePose(
                    target,
                    navigationCamera,
                    framingSettings,
                    out DeucarianCameraPose pose))
            {
                message = "Camera or framing target is invalid.";
                return false;
            }

            bool accepted = MoveCameraToPose(
                pose,
                target.Bounds,
                target.FocusPoint,
                ViewerNavigationTransitionKind.Frame,
                animate,
                IsTopDown);
            message = accepted ? "Framing accepted." : "Framing was not accepted.";
            return accepted;
        }

        private float ResolveReferencePadding()
        {
            return settings != null
                ? settings.ReferencePadding
                : ViewerNavigationSettings.DefaultReferencePadding;
        }

        private Bounds ResolveNavigationBounds()
        {
            return HasReferenceBounds
                ? referenceBounds
                : new Bounds(Vector3.zero, Vector3.one * 10f);
        }

        private void SyncReferenceToRig()
        {
            if (navigationRig == null)
            {
                return;
            }

            if (HasReferenceBounds)
            {
                navigationRig.SetReferenceBounds(referenceBounds);
                navigationRig.SetPivot(referenceBounds.center);
            }
            else
            {
                navigationRig.ClearReferenceBounds();
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                   !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}
