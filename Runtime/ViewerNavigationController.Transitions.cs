using System;
using System.Collections;
using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    public sealed partial class ViewerNavigationController
    {
        private Coroutine activeTransitionRoutine;
        private uint transitionGeneration;

        public bool CancelTransition()
        {
            transitionGeneration++;
            if (activeTransitionRoutine != null)
            {
                StopCoroutine(activeTransitionRoutine);
                activeTransitionRoutine = null;
            }

            bool canceled = state.EndTransition();
            if (canceled)
            {
                ApplyNavigationMode();
                ViewerNavigationLog.Navigation.Debug(
                    "Active camera transition was superseded.",
                    this);
            }

            return canceled;
        }

        private bool MoveCameraToPose(
            DeucarianCameraPose targetPose,
            Bounds bounds,
            Vector3 pivot,
            ViewerNavigationTransitionKind kind,
            bool animate,
            bool topDownAtEnd)
        {
            if (navigationCamera == null || !IsFinite(targetPose.Position))
            {
                return false;
            }

            CancelTransition();
            DeucarianCameraPose startPose =
                DeucarianCameraPose.Capture(navigationCamera);
            bool enteringOrthographic =
                !startPose.Orthographic && targetPose.Orthographic;
            bool exitingOrthographic =
                startPose.Orthographic && !targetPose.Orthographic;
            DeucarianCameraPose animationStartPose = exitingOrthographic
                ? DeucarianCameraFraming.CreateVisibleTopDownTransitionPose(
                    startPose,
                    pivot,
                    targetPose.FieldOfView)
                : startPose;
            DeucarianCameraPose animationTargetPose = enteringOrthographic
                ? DeucarianCameraFraming.CreateVisibleTopDownTransitionPose(
                    targetPose,
                    pivot,
                    startPose.FieldOfView)
                : targetPose;
            float distance = Vector3.Distance(
                animationStartPose.Position,
                animationTargetPose.Position);
            float duration = animate && motionProfile != null &&
                             motionProfile.AnimateTransitions
                ? motionProfile.CalculateTransitionDuration(distance)
                : 0f;
            uint generation = ++transitionGeneration;
            state.BeginTransition(kind);

            if (!Application.isPlaying || duration <= 0f)
            {
                CommitCameraMove(
                    generation,
                    targetPose,
                    bounds,
                    pivot,
                    topDownAtEnd);
                return true;
            }

            activeTransitionRoutine = StartCoroutine(
                AnimateCameraMove(
                    generation,
                    startPose,
                    animationStartPose,
                    animationTargetPose,
                    targetPose,
                    bounds,
                    pivot,
                    duration,
                    topDownAtEnd,
                    exitingOrthographic));
            return true;
        }

        private IEnumerator AnimateCameraMove(
            uint generation,
            DeucarianCameraPose capturedStartPose,
            DeucarianCameraPose animationStartPose,
            DeucarianCameraPose animationTargetPose,
            DeucarianCameraPose committedTargetPose,
            Bounds bounds,
            Vector3 pivot,
            float duration,
            bool topDownAtEnd,
            bool exitingOrthographic)
        {
            if (exitingOrthographic)
            {
                PreparePerspectiveTransitionStart(
                    capturedStartPose,
                    animationStartPose,
                    pivot);
                state.SetTopDown(false);
                ApplyNavigationMode();
            }

            float elapsed = 0f;
            while (generation == transitionGeneration &&
                   navigationCamera != null &&
                   elapsed < duration)
            {
                float normalized = Mathf.Clamp01(elapsed / duration);
                float movement = motionProfile != null
                    ? motionProfile.EvaluateMovement(normalized)
                    : normalized;
                float rotation = motionProfile != null
                    ? motionProfile.EvaluateRotation(normalized)
                    : normalized;
                ApplyTransitionFrame(
                    animationStartPose,
                    animationTargetPose,
                    movement,
                    rotation);
                DeucarianCameraFraming.ConfigureClipPlanes(
                    navigationCamera,
                    bounds);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (generation == transitionGeneration)
            {
                CommitCameraMove(
                    generation,
                    committedTargetPose,
                    bounds,
                    pivot,
                    topDownAtEnd);
            }
        }

        private void ApplyTransitionFrame(
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

        private void PreparePerspectiveTransitionStart(
            DeucarianCameraPose orthographicStartPose,
            DeucarianCameraPose visiblePerspectiveStartPose,
            Vector3 pivot)
        {
            DeucarianCameraPose hiddenPerspectiveMatch =
                DeucarianCameraFraming
                    .CreatePerspectiveMatchPoseForOrthographicSwitch(
                        orthographicStartPose,
                        pivot,
                        ResolveTransitionMatchFieldOfView());
            hiddenPerspectiveMatch.ApplyTo(navigationCamera);
            visiblePerspectiveStartPose.ApplyTo(navigationCamera);
        }

        private void CommitCameraMove(
            uint generation,
            DeucarianCameraPose targetPose,
            Bounds bounds,
            Vector3 pivot,
            bool topDownAtEnd)
        {
            if (generation != transitionGeneration || navigationCamera == null)
            {
                return;
            }

            if (!navigationCamera.orthographic && targetPose.Orthographic)
            {
                DeucarianCameraPose hiddenPerspectiveMatch =
                    DeucarianCameraFraming
                        .CreatePerspectiveMatchPoseForOrthographicSwitch(
                            targetPose,
                            pivot,
                            ResolveTransitionMatchFieldOfView());
                hiddenPerspectiveMatch.ApplyTo(navigationCamera);
            }

            targetPose.ApplyTo(navigationCamera);
            DeucarianCameraFraming.ConfigureClipPlanes(navigationCamera, bounds);
            if (navigationRig != null)
            {
                navigationRig.SetPivot(pivot);
                navigationRig.SyncNavigationState();
            }

            state.SetTopDown(topDownAtEnd);
            activeTransitionRoutine = null;
            state.EndTransition();
            ApplyNavigationMode();
        }

        private float ResolveTransitionMatchFieldOfView()
        {
            float value = motionProfile != null
                ? motionProfile.TransitionMatchFieldOfView
                : ViewerNavigationSettings.DefaultTransitionMatchFieldOfView;
            return float.IsNaN(value) || float.IsInfinity(value)
                ? ViewerNavigationSettings.DefaultTransitionMatchFieldOfView
                : value;
        }
    }
}
