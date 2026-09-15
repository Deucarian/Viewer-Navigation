using Deucarian.CameraNavigation;
using Deucarian.CameraNavigation.InputSystemIntegration;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    [CreateAssetMenu(
        fileName = "ViewerNavigationSettings",
        menuName = "Deucarian/Viewer/Navigation Settings")]
    public sealed class ViewerNavigationSettings :
        ScriptableObject,
        IViewerNavigationMotionProfile
    {
        public const string ReferencePresetResourcesPath =
            "Deucarian/ViewerNavigationReferencePreset";
        public const float DefaultTransitionMatchFieldOfView = 0.1f;
        public const float DefaultReferencePadding = 1.25f;

        [Header("Dependencies")]
        [SerializeField] private DeucarianCameraNavigationControls controls;
        [SerializeField] private DeucarianInputSystemNavigationSettings inputSettings;
        [SerializeField] private DeucarianCameraFramingSettings framingSettings;

        [Header("Motion")]
        [SerializeField] private bool animateTransitions = true;
        [SerializeField, Range(0.1f, 30f)] private float transitionMatchFieldOfView =
            DefaultTransitionMatchFieldOfView;
        [SerializeField] private AnimationCurve movementCurve =
            AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve rotationCurve =
            AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Reference")]
        [SerializeField, Min(1f)] private float referencePadding =
            DefaultReferencePadding;

        [Header("Presentation")]
        [SerializeField] private bool showToolbar = true;
        [SerializeField, Tooltip("Show the optional six-face view cube. Disabled by default.")]
        private bool showViewCube;

        public DeucarianCameraNavigationControls Controls => controls;
        public DeucarianInputSystemNavigationSettings InputSettings => inputSettings;
        public DeucarianCameraFramingSettings FramingSettings => framingSettings;
        public bool AnimateTransitions => animateTransitions;
        public float TransitionMatchFieldOfView =>
            Mathf.Clamp(transitionMatchFieldOfView, 0.1f, 30f);
        public float ReferencePadding => Mathf.Max(1f, referencePadding);
        public bool ShowToolbar => showToolbar;
        public bool ShowViewCube => showViewCube;

        public static ViewerNavigationSettings LoadReferencePreset()
        {
            return Resources.Load<ViewerNavigationSettings>(
                ReferencePresetResourcesPath);
        }

        public float EvaluateMovement(float normalizedTime) =>
            Evaluate(movementCurve, normalizedTime);

        public float EvaluateRotation(float normalizedTime) =>
            Evaluate(rotationCurve, normalizedTime);

        private static float Evaluate(AnimationCurve curve, float time)
        {
            float t = Mathf.Clamp01(time);
            if (t <= 0f || t >= 1f || curve == null || curve.length == 0)
            {
                return t;
            }

            return Mathf.Clamp01(curve.Evaluate(t));
        }

        private void OnValidate()
        {
            transitionMatchFieldOfView = Mathf.Clamp(transitionMatchFieldOfView, 0.1f, 30f);
            referencePadding = Mathf.Max(1f, referencePadding);
        }
    }

    internal sealed class DefaultViewerNavigationMotionProfile :
        IViewerNavigationMotionProfile
    {
        public bool AnimateTransitions => true;
        public float TransitionMatchFieldOfView =>
            ViewerNavigationSettings.DefaultTransitionMatchFieldOfView;

        public float EvaluateMovement(float normalizedTime) =>
            Mathf.Clamp01(normalizedTime);

        public float EvaluateRotation(float normalizedTime) =>
            Mathf.Clamp01(normalizedTime);
    }

    internal sealed class PolicyAwareViewerNavigationMotionProfile :
        IViewerNavigationMotionProfile
    {
        private readonly IViewerNavigationMotionProfile profile;
        private readonly IViewerNavigationAnimationPolicy policy;

        public PolicyAwareViewerNavigationMotionProfile(
            IViewerNavigationMotionProfile motionProfile,
            IViewerNavigationAnimationPolicy animationPolicy)
        {
            profile = motionProfile;
            policy = animationPolicy;
        }

        public bool AnimateTransitions =>
            profile != null &&
            profile.AnimateTransitions &&
            (policy == null || policy.ShouldAnimate);

        public float TransitionMatchFieldOfView => profile != null
            ? profile.TransitionMatchFieldOfView
            : ViewerNavigationSettings.DefaultTransitionMatchFieldOfView;

        public float EvaluateMovement(float normalizedTime) =>
            profile != null
                ? profile.EvaluateMovement(normalizedTime)
                : Mathf.Clamp01(normalizedTime);

        public float EvaluateRotation(float normalizedTime) =>
            profile != null
                ? profile.EvaluateRotation(normalizedTime)
                : Mathf.Clamp01(normalizedTime);
    }
}
