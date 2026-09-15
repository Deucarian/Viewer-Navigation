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
        private CameraMoveOperation activeMoveOperation;
        private readonly ViewerNavigationTransitionRigOwnership transitionRigOwnership = new ViewerNavigationTransitionRigOwnership();
        public bool UsesManualUpdates { get; private set; }

        /// <summary>Use an explicit clock instead of a coroutine; changing clock cancels the current move.</summary>
        public void SetManualUpdates(bool enabled)
        {
            if (UsesManualUpdates == enabled) return;
            UsesManualUpdates = enabled;
            CancelTransition();
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

            FinishMoveOperation(CameraMoveResult.Cancelled);
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
            bool topDownAtEnd,
            CameraMoveOperation operation = null,
            float? durationSeconds = null)
        {
            if (durationSeconds.HasValue && (durationSeconds.Value < 0f ||
                float.IsNaN(durationSeconds.Value) || float.IsInfinity(durationSeconds.Value)))
                return false;
            if (navigationCamera == null || !isActiveAndEnabled || !ViewerNavigationPoseValidation.IsFinite(targetPose.Position))
            {
                return false;
            }

            uint expectedAfterCancel = transitionGeneration + 1;
            CancelTransition();
            if (transitionGeneration != expectedAfterCancel)
            {
                operation?.Complete(CameraMoveResult.Cancelled);
                return false;
            }
            DeucarianCameraPose startPose =
                DeucarianCameraPose.Capture(navigationCamera);
            bool exitingOrthographic =
                startPose.Orthographic && !targetPose.Orthographic;
            ViewerNavigationTransitionPose.ResolveAnimationPoses(startPose, targetPose, pivot,
                out DeucarianCameraPose animationStartPose, out DeucarianCameraPose animationTargetPose);
            float duration = animate && motionProfile != null && motionProfile.AnimateTransitions
                ? ViewerNavigationTransitionTiming.ResolveDuration(animationStartPose, animationTargetPose,
                    motionProfile, controls?.GlobalSensitivity ?? DeucarianCameraNavigationControls.DefaultGlobalSensitivity,
                    durationSeconds)
                : 0f;
            uint generation = ++transitionGeneration;
            activeMoveOperation = operation;
            state.BeginTransition(kind);
            if (generation != transitionGeneration) return true;

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
                ViewerNavigationTransitionPose.Apply(
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
            if (generation != transitionGeneration)
            {
                return;
            }
            if (navigationCamera == null)
            {
                activeTransitionRoutine = null;
                manualTransition = null;
                FinishMoveOperation(CameraMoveResult.InvalidTarget);
                state.EndTransition();
                return;
            }

            ViewerNavigationTransitionPresentation.Commit(navigationCamera, targetPose, bounds, pivot, motionProfile);
            if (navigationRig != null)
            {
                navigationRig.SetPivot(pivot);
                navigationRig.SyncNavigationState();
            }

            activeTransitionRoutine = null;
            manualTransition = null;
            FinishMoveOperation(CameraMoveResult.Completed);
            state.SetTopDown(topDownAtEnd);
            if (generation != transitionGeneration) return;
            state.EndTransition();
            ApplyNavigationMode();
        }

        private void FinishMoveOperation(CameraMoveResult result)
        {
            var operation = activeMoveOperation;
            activeMoveOperation = null;
            operation?.Complete(result);
        }

    }
}
