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
        private IEnumerator manualTransition;
        private float manualDeltaTime;
        public bool UsesManualUpdates { get; private set; }

        /// <summary>Use an explicit clock instead of a coroutine; changing clock cancels the current move.</summary>
        public void SetManualUpdates(bool enabled)
        {
            if (UsesManualUpdates == enabled) return;
            CancelTransition();
            UsesManualUpdates = enabled;
        }

        public void Tick(float deltaTime)
        {
            if (!UsesManualUpdates || manualTransition == null || deltaTime <= 0 ||
                float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            manualDeltaTime = deltaTime;
            var routine = manualTransition;
            if (!routine.MoveNext() && ReferenceEquals(routine, manualTransition)) manualTransition = null;
        }

        public bool CancelTransition()
        {
            transitionGeneration++;
            (manualTransition as IDisposable)?.Dispose();
            manualTransition = null;
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

            if ((!Application.isPlaying && !UsesManualUpdates) || duration <= 0f)
            {
                CommitCameraMove(
                    generation,
                    targetPose,
                    bounds,
                    pivot,
                    topDownAtEnd);
                return true;
            }

            var routine = AnimateCameraMove(
                    generation,
                    startPose,
                    animationStartPose,
                    animationTargetPose,
                    targetPose,
                    bounds,
                    pivot,
                    duration,
                    topDownAtEnd,
                    exitingOrthographic);
            if (UsesManualUpdates) manualTransition = routine;
            else activeTransitionRoutine = StartCoroutine(routine);
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
                ViewerNavigationTransitionPresentation.PreparePerspectiveStart(
                    navigationCamera,
                    capturedStartPose,
                    animationStartPose,
                    pivot,
                    motionProfile);
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
                ViewerNavigationTransitionPresentation.ApplyFrame(
                    navigationCamera,
                    animationStartPose,
                    animationTargetPose,
                    movement,
                    rotation);
                DeucarianCameraFraming.ConfigureClipPlanes(
                    navigationCamera,
                    bounds);
                elapsed += UsesManualUpdates ? manualDeltaTime : Time.unscaledDeltaTime;
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

            ViewerNavigationTransitionPresentation.Commit(navigationCamera, targetPose, bounds, pivot, motionProfile);
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

    }
}
