using System;
using Deucarian.CameraNavigation;
using Deucarian.CameraNavigation.InputSystemIntegration;
using Deucarian.Diagnostics;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    [DisallowMultipleComponent]
    public sealed partial class ViewerNavigationController : MonoBehaviour
    {
        [SerializeField] private Camera navigationCamera;
        [SerializeField] private ViewerNavigationSettings settings;

        private readonly ViewerNavigationStateStore state =
            new ViewerNavigationStateStore();
        private DeucarianInputSystemCameraNavigationRig navigationRig;
        private ViewerNavigationInteractionGate interactionGate;
        private DeucarianCameraNavigationControls controls;
        private DeucarianInputSystemNavigationSettings inputSettings;
        private IDeucarianCameraFramingSettings framingSettings;
        private IDeucarianFramingBoundsStrategy<GameObject>
            referenceBoundsStrategy = DeucarianRendererBoundsStrategy.Instance;
        private IViewerNavigationMotionProfile motionProfile;
        private IViewerNavigationInputBlocker externalInputBlocker;
        private DiagnosticProviderRegistration diagnosticRegistration;
        private ViewerNavigationSnapshot lastSnapshot;
        private bool stateSubscribed;
        private bool gateSubscribed;
        private bool initialized;

        public event Action<ViewerNavigationSnapshot> StateChanged;
        public event Action<ViewerNavigationMode> ModeChanged;
        public event Action<bool> TopDownChanged;

        public Camera Camera => navigationCamera;
        public ViewerNavigationSnapshot Snapshot => state.Snapshot;
        public ViewerNavigationMode Mode => state.Snapshot.Mode;
        public bool IsTopDown => state.Snapshot.IsTopDown;
        public bool HasReferenceBounds => state.Snapshot.HasReferenceBounds;
        public bool HasOrigin => state.Snapshot.HasOrigin;
        public bool IsTransitioning => state.Snapshot.IsTransitioning;
        public ViewerNavigationTransitionKind ActiveTransition =>
            state.Snapshot.TransitionKind;
        public Bounds ReferenceBounds => referenceBounds;
        public Vector3 Pivot => navigationRig != null
            ? navigationRig.OrbitPivot
            : referenceBounds.center;
        public DeucarianCameraNavigationControls Controls => controls;
        public IDeucarianCameraFramingSettings FramingSettings => framingSettings;
        public IDeucarianFramingBoundsStrategy<GameObject>
            ReferenceBoundsStrategy => referenceBoundsStrategy;
        public IViewerNavigationMotionProfile MotionProfile => motionProfile;
        public ViewerNavigationInteractionGate InteractionGate => interactionGate;

        public void Initialize(
            Camera camera,
            ViewerNavigationSettings configuration,
            IViewerNavigationInputBlocker inputBlocker = null,
            IDeucarianFramingBoundsStrategy<GameObject>
                navigationReferenceBoundsStrategy = null,
            IViewerNavigationAnimationPolicy animationPolicy = null)
        {
            settings = configuration;
            IViewerNavigationMotionProfile motionProfile = configuration;
            if (motionProfile != null && animationPolicy != null)
            {
                motionProfile = new PolicyAwareViewerNavigationMotionProfile(
                    motionProfile,
                    animationPolicy);
            }

            Initialize(
                camera,
                ResolveControls(configuration),
                configuration != null ? configuration.InputSettings : null,
                motionProfile,
                ResolveFramingSettings(configuration),
                inputBlocker,
                navigationReferenceBoundsStrategy);
            settings = configuration;
        }

        private static DeucarianCameraNavigationControls ResolveControls(
            ViewerNavigationSettings configuration)
        {
            return configuration != null && configuration.Controls != null
                ? configuration.Controls
                : Resources.Load<DeucarianCameraNavigationControls>(
                    DeucarianCameraNavigationControls.CanonicalResourcesPath);
        }

        private static IDeucarianCameraFramingSettings ResolveFramingSettings(
            ViewerNavigationSettings configuration)
        {
            return configuration != null && configuration.FramingSettings != null
                ? configuration.FramingSettings
                : Resources.Load<DeucarianCameraFramingSettings>(
                    DeucarianCameraFramingSettings.CanonicalResourcesPath);
        }

        public void Initialize(
            Camera camera,
            DeucarianCameraNavigationControls navigationControls = null,
            DeucarianInputSystemNavigationSettings navigationInputSettings = null,
            IViewerNavigationMotionProfile navigationMotionProfile = null,
            IDeucarianCameraFramingSettings navigationFramingSettings = null,
            IViewerNavigationInputBlocker inputBlocker = null,
            IDeucarianFramingBoundsStrategy<GameObject>
                navigationReferenceBoundsStrategy = null)
        {
            CancelTransition();
            UnsubscribeGate();

            settings = null;
            navigationCamera = camera;
            controls = navigationControls;
            inputSettings = navigationInputSettings;
            motionProfile = navigationMotionProfile ??
                            new DefaultViewerNavigationMotionProfile();
            framingSettings = navigationFramingSettings;
            referenceBoundsStrategy = navigationReferenceBoundsStrategy ??
                                      DeucarianRendererBoundsStrategy.Instance;
            externalInputBlocker = inputBlocker;

            EnsureStateSubscription();
            EnsureRuntimeComponents();
            ConfigureRuntimeComponents();
            initialized = true;
            SubscribeGate();
            RegisterDiagnostics();
            lastSnapshot = state.Snapshot;
        }

        public bool SetNavigationMode(ViewerNavigationMode mode)
        {
            if (mode != ViewerNavigationMode.Orbit && mode != ViewerNavigationMode.Fly)
            {
                return false;
            }

            if (Mode == mode)
            {
                return false;
            }

            CancelTransition();
            state.SetMode(mode);
            ApplyNavigationMode();
            ViewerNavigationLog.Navigation.Info(
                "Navigation mode changed to " + mode + ".",
                this);
            return true;
        }

        public bool SetGlobalSensitivity(float sensitivity)
        {
            if (controls == null ||
                float.IsNaN(sensitivity) ||
                float.IsInfinity(sensitivity))
            {
                return false;
            }

            controls.GlobalSensitivity = sensitivity;
            return true;
        }

        private void Awake()
        {
            EnsureStateSubscription();
        }

        private void OnEnable()
        {
            EnsureStateSubscription();
            if (initialized)
            {
                SubscribeGate();
                RegisterDiagnostics();
            }
        }

        private void OnDisable()
        {
            CancelTransition();
            UnsubscribeGate();
            diagnosticRegistration?.Dispose();
            diagnosticRegistration = null;
        }

        private void OnDestroy()
        {
            UnsubscribeGate();
            if (stateSubscribed)
            {
                state.Changed -= OnStateChanged;
                stateSubscribed = false;
            }

            diagnosticRegistration?.Dispose();
            diagnosticRegistration = null;
        }

        private void EnsureRuntimeComponents()
        {
            navigationRig = GetComponent<DeucarianInputSystemCameraNavigationRig>();
            if (navigationRig == null)
            {
                navigationRig = gameObject.AddComponent<DeucarianInputSystemCameraNavigationRig>();
            }

            interactionGate = GetComponent<ViewerNavigationInteractionGate>();
            if (interactionGate == null)
            {
                interactionGate = gameObject.AddComponent<ViewerNavigationInteractionGate>();
            }
        }

        private void ConfigureRuntimeComponents()
        {
            interactionGate.Configure(
                externalInputBlocker,
                navigationRig.ActionStateSource);
            navigationRig.NavigationCamera = navigationCamera;
            navigationRig.Controls = controls;
            navigationRig.InputSettings = inputSettings;
            navigationRig.SetInputBlocker(interactionGate);
            ApplyNavigationMode();
            SyncReferenceToRig();
        }

        private void ApplyNavigationMode()
        {
            if (navigationRig != null)
            {
                navigationRig.SetMode(
                    IsTopDown || Mode == ViewerNavigationMode.Orbit
                        ? DeucarianInputSystemNavigationMode.Orbit
                        : DeucarianInputSystemNavigationMode.Fly);
                navigationRig.SyncNavigationState();
            }

            interactionGate?.SetNavigationState(Mode, IsTopDown);
        }

        private void EnsureStateSubscription()
        {
            if (stateSubscribed)
            {
                return;
            }

            lastSnapshot = state.Snapshot;
            state.Changed += OnStateChanged;
            stateSubscribed = true;
        }

        private void OnStateChanged(ViewerNavigationSnapshot snapshot)
        {
            ViewerNavigationSnapshot previous = lastSnapshot;
            lastSnapshot = snapshot;
            StateChanged?.Invoke(snapshot);
            if (previous.Mode != snapshot.Mode)
            {
                ModeChanged?.Invoke(snapshot.Mode);
            }

            if (previous.IsTopDown != snapshot.IsTopDown)
            {
                TopDownChanged?.Invoke(snapshot.IsTopDown);
            }
        }

        private void SubscribeGate()
        {
            if (gateSubscribed || interactionGate == null || !isActiveAndEnabled)
            {
                return;
            }

            interactionGate.NavigationInputStarted += OnNavigationInputStarted;
            gateSubscribed = true;
        }

        private void UnsubscribeGate()
        {
            if (!gateSubscribed || interactionGate == null)
            {
                return;
            }

            interactionGate.NavigationInputStarted -= OnNavigationInputStarted;
            gateSubscribed = false;
        }

        private void OnNavigationInputStarted()
        {
            CancelTransition();
        }

        private void RegisterDiagnostics()
        {
            if (diagnosticRegistration == null && initialized)
            {
                diagnosticRegistration = DiagnosticProviderRegistry.Register(
                    new ViewerNavigationDiagnosticProvider(this));
            }
        }
    }
}
