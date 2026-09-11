using System;
using Deucarian.CameraNavigation;
using Deucarian.CameraNavigation.Editor;
using Deucarian.CameraNavigation.InputSystemIntegration;
using Deucarian.Editor;
using Deucarian.ViewerNavigation.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Controls = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.ViewerNavigation.Editor
{
    internal sealed class ViewerNavigationPreview : IDisposable
    {
        private readonly Func<ViewerNavigationSettings> readSettings;
        private readonly ViewerNavigationSettings referenceSettings;
        private readonly DeucarianCameraNavigationPreview cameraPreview;
        private readonly ViewerNavigationController controller;
        private readonly VisualElement toolbar;
        private readonly ViewerViewCubeElement cube;
        private readonly Button orbit, fly, topDown;
        private ViewerNavigationSettings configuration;
        private DeucarianCameraNavigationControls configuredControls;
        private DeucarianCameraFramingSettings configuredFraming;
        private DeucarianInputSystemNavigationSettings configuredInput;
        private bool disposed;

        internal VisualElement Root { get; }
        internal Camera Camera => cameraPreview.Camera;
        internal ViewerNavigationController Controller => controller;
        internal DeucarianCameraNavigationPreview Navigation => cameraPreview;

        internal ViewerNavigationPreview(Func<ViewerNavigationSettings> settings)
        {
            readSettings = settings;
            referenceSettings = ViewerNavigationSettings.LoadReferencePreset();
            configuration = ResolveSettings();
            cameraPreview = new DeucarianCameraNavigationPreview(
                () => ResolveSettings()?.Controls,
                () => ResolveSettings()?.InputSettings?.PointerDeltaScale ?? DeucarianInputSystemNavigationSettings.DefaultPointerDeltaScale,
                () => ResolveSettings()?.InputSettings?.ScrollNormalization ?? DeucarianInputSystemNavigationSettings.DefaultScrollNormalization);
            // The preview owns its gesture adapter. Never enable global runtime input actions here.
            cameraPreview.Camera.gameObject.SetActive(false);
            cameraPreview.Camera.gameObject.AddComponent<DeucarianInputSystemCameraNavigationRig>();
            foreach (var behaviour in cameraPreview.Camera.GetComponents<MonoBehaviour>())
                behaviour.enabled = false;
            controller = cameraPreview.Camera.gameObject.AddComponent<ViewerNavigationController>();
            controller.Initialize(cameraPreview.Camera, configuration);
            CacheDependencies();
            controller.SetManualUpdates(true);
            controller.SetReferenceBounds(cameraPreview.Bounds, cameraPreview.Pivot);
            controller.CaptureOrigin();
            cameraPreview.Camera.gameObject.SetActive(true);
            cameraPreview.InputStarted += OnInputStarted;
            Root = Controls.Region("viewer-controls-preview", "dw-spatial-specimen");
            Root.Add(cameraPreview.View);
            cube = new ViewerViewCubeElement();
            cube.style.top = 12; cube.style.right = 12;
            cube.FaceSelected += face => Run(() => controller.NavigateToFace(face));
            Root.Add(cube);
            toolbar = Controls.Actions();
            toolbar.name = "viewer-preview-toolbar";
            toolbar.AddToClassList("dw-scene-preview-toolbar");
            Root.Add(toolbar);
            orbit = Button("Orbit", DeucarianEditorIconIds.Orbit, () => SetMode(ViewerNavigationMode.Orbit));
            fly = Button("Fly", DeucarianEditorIconIds.Send, () => SetMode(ViewerNavigationMode.Fly));
            topDown = Button("Top", DeucarianEditorIconIds.Monitor, () => Run(() => controller.ToggleTopDown()));
            Button("Frame", DeucarianEditorIconIds.Fit, () => Run(() => controller.FrameReference()));
            Button("Reset", DeucarianEditorIconIds.Home, () => Run(() => controller.ReturnToOrigin()));
            RefreshPresentation();
            Root.RegisterCallback<GeometryChangedEvent>(_ => cameraPreview.RefreshView(true));
        }

        private ViewerNavigationSettings ResolveSettings() => readSettings() ?? referenceSettings;

        private void CacheDependencies()
        {
            configuredControls = configuration?.Controls;
            configuredFraming = configuration?.FramingSettings;
            configuredInput = configuration?.InputSettings;
        }

        private Button Button(string label, string icon, Action action)
        {
            var button = Controls.IconButton(label, icon, action);
            button.name = "preview-" + label.ToLowerInvariant();
            button.tooltip = label + " the isolated preview (scene cameras are unchanged)";
            toolbar.Add(button);
            return button;
        }

        private void SetMode(ViewerNavigationMode mode)
        {
            Run(() => controller.SetNavigationMode(mode));
            cameraPreview.FlyMode = mode == ViewerNavigationMode.Fly && !controller.IsTopDown;
        }

        internal void Run(Func<bool> action)
        {
            if (disposed) return;
            cameraPreview.StopMotion();
            controller.SetReferenceBounds(cameraPreview.Bounds, cameraPreview.Pivot);
            action();
            RefreshPresentation();
        }

        internal void Update(float deltaTime)
        {
            if (disposed) return;
            var current = ResolveSettings();
            if (configuration != current || configuredControls != current?.Controls ||
                configuredFraming != current?.FramingSettings || configuredInput != current?.InputSettings)
            {
                configuration = current;
                controller.Initialize(Camera, configuration);
                controller.SetReferenceBounds(cameraPreview.Bounds, cameraPreview.Pivot);
                cameraPreview.SyncNavigationState(cameraPreview.Pivot);
                CacheDependencies();
            }
            cameraPreview.Update(deltaTime, suppressIdleMotion: controller.IsTransitioning);
            bool wasTransitioning = controller.IsTransitioning;
            controller.Tick(Mathf.Min(deltaTime, 0.05f));
            if (wasTransitioning && !controller.IsTransitioning)
                cameraPreview.SyncNavigationState(controller.Pivot);
            cameraPreview.FlyMode = controller.Mode == ViewerNavigationMode.Fly && !controller.IsTopDown;
            cameraPreview.RefreshView();
            RefreshPresentation();
        }

        private void RefreshPresentation()
        {
            Controls.Show(toolbar, configuration == null || configuration.ShowToolbar);
            Controls.Show(cube, configuration != null && configuration.ShowViewCube);
            cube.UpdateOrientation(Camera.transform.rotation);
            orbit.EnableInClassList("dw-primary", controller.Mode == ViewerNavigationMode.Orbit);
            fly.EnableInClassList("dw-primary", controller.Mode == ViewerNavigationMode.Fly);
            topDown.EnableInClassList("dw-primary", controller.IsTopDown);
        }

        private void OnInputStarted()
        {
            if (controller.CancelTransition())
                cameraPreview.SyncNavigationState(cameraPreview.Pivot);
        }

        internal void Pause()
        {
            if (disposed) return;
            controller.CancelTransition();
            cameraPreview.StopMotion();
        }

        public void Dispose()
        {
            if (disposed) return;
            Pause();
            disposed = true;
            cameraPreview.InputStarted -= OnInputStarted;
            cameraPreview.Dispose();
        }
    }
}
