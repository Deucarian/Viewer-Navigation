using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Controls = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.ViewerNavigation.Editor
{
    internal sealed class ViewerNavigationPage
    {
        private readonly DeucarianEditorWorkspace workspace;
        private readonly List<DeucarianEditorSerializedForm> bindings = new List<DeucarianEditorSerializedForm>();
        private ViewerNavigationInstaller installer;
        private ViewerNavigationSettings settings;
        private Camera camera;
        private GameObject target;
        private ViewerNavigationController liveController;
        private DeucarianEditorWorkspaceForm runtime;
        private bool wasPlaying;
        private bool sceneSetupExpanded;
        private Button apply;
        private readonly ViewerNavigationPreview preview;
        private readonly DeucarianEditorAssetField profilePicker, targetPicker, cameraPicker;
        private double previousTime;
        public IDeucarianEditorPage Page { get; }

        internal ViewerNavigationPage()
        {
            target = Selection.activeGameObject;
            ReadTarget(true);
            settings = settings != null ? settings : ViewerNavigationSettings.LoadReferencePreset();
            preview = new ViewerNavigationPreview(() => settings);
            profilePicker = new DeucarianEditorAssetField("viewer-profile", typeof(ViewerNavigationSettings), () => settings,
                value => { settings = value as ViewerNavigationSettings; Render(); },
                create: () => { CreateProfile(); return settings; }, customize: DeucarianEditorAssetCatalog.CopyToProject,
                defaultValue: ViewerNavigationSettings.LoadReferencePreset);
            targetPicker = new DeucarianEditorAssetField("viewer-target", typeof(GameObject), () => target,
                value => { target = value as GameObject; ReadTarget(); Render(); }, allowSceneObjects: true);
            cameraPicker = new DeucarianEditorAssetField("viewer-camera", typeof(Camera), () => camera,
                value => camera = value as Camera, allowSceneObjects: true);
            var root = new VisualElement();
            workspace = new DeucarianEditorWorkspace(root, Application.productName);
            workspace.Title.text = "Viewer navigation";
            workspace.Subtitle.text = "Test viewer controls and tune smooth camera navigation.";
            Controls.Show(workspace.Scope, false);
            Controls.Show(workspace.Tabs, false);
            DeucarianEditorWorkspaceNavigation.Populate(workspace, DeucarianToolIds.ViewerNavigation);
            Page = new DeucarianEditorPage(root, activate: _ => { previousTime = EditorApplication.timeSinceStartup; Update(); },
                deactivate: preview.Pause, update: _ => Update(), dispose: Dispose);
            Render();
        }

        private void ReadTarget(bool readProfile = false)
        {
            installer = target != null ? target.GetComponent<ViewerNavigationInstaller>() : null;
            if (installer == null) { camera = target != null ? target.GetComponent<Camera>() : null; return; }
            using (var serialized = new SerializedObject(installer))
            {
                if (readProfile) settings = serialized.FindProperty("settings").objectReferenceValue as ViewerNavigationSettings;
                camera = serialized.FindProperty("navigationCamera").objectReferenceValue as Camera;
            }
        }

        private void Render()
        {
            ClearBindings();
            workspace.Content.Clear();
            wasPlaying = EditorApplication.isPlaying;
            liveController = installer != null ? installer.Controller : null;
            if (liveController == null && wasPlaying)
                liveController = Object.FindFirstObjectByType<ViewerNavigationController>();
            var scroll = Controls.Scroll("viewer-navigation-settings");
            workspace.Content.Add(scroll);
            var card = new DeucarianEditorFeatureSection("viewer-controls", "Navigation controls",
                "Try the controls on a Unity Cube. Profile changes apply to this preview immediately.", DeucarianEditorIconIds.Center);
            scroll.Add(card.Root);
            var fields = new VisualElement();
            var split = Controls.Split(fields, preview.Root); split.AddToClassList("dw-spatial-split"); card.Details.Add(split);
            fields.Add(Controls.Field("Preview profile", profilePicker.Root)); profilePicker.Refresh();
            fields.Add(Controls.Divider());
            if (settings == null)
            {
                fields.Add(Controls.Label("Using the bundled defaults. Create a profile to save your own settings.", "dw-muted"));
                fields.Add(Controls.Button("Create controls profile", CreateProfile));
            }
            else
            {
                var form = Bind(fields, settings);
                form.Property("showToolbar", "Toolbar");
                form.Property("showViewCube", "View cube");
                var advanced = new Foldout { text = "Motion and input settings", value = false };
                advanced.AddToClassList("dw-foldout");
                fields.Add(advanced);
                Bind(advanced, settings).Remaining("showToolbar", "showViewCube");
            }
            var select = Controls.Button("Select controls", () => { Selection.activeObject = settings; EditorGUIUtility.PingObject(settings); });
            select.SetEnabled(settings != null);
            card.Actions.Add(select);
            card.Details.Add(Controls.Label("Drag to look or orbit · Shift-drag to pan · Scroll to zoom · Click, then WASD + Q/E to move", "dw-muted"));
            workspace.FooterLeading.text = "Interactive preview · Your scene cameras stay unchanged";
            BuildSceneSetup(scroll);
            BuildRuntime(scroll);
            previousTime = EditorApplication.timeSinceStartup;
            Update();
        }

        private void BuildSceneSetup(VisualElement parent)
        {
            var setup = new Foldout { name = "viewer-scene-setup", text = "Apply to a scene", value = sceneSetupExpanded };
            setup.RegisterValueChangedCallback(evt => sceneSetupExpanded = evt.newValue);
            setup.AddToClassList("dw-foldout");
            parent.Add(setup);
            setup.Add(Controls.Label("Optional scene setup. These references do not change the isolated preview above.", "dw-muted"));
            setup.Add(Controls.Field("Installer object", targetPicker.Root)); targetPicker.Refresh();
            setup.Add(Controls.Field("Scene camera", cameraPicker.Root)); cameraPicker.Refresh();
            apply = Controls.Button(installer == null ? "Add navigation to object" : "Apply profile to scene", Apply, true);
            apply.name = "viewer-apply-scene";
            apply.tooltip = "Assign the preview profile and camera with Undo. Does not change the preview camera.";
            setup.Add(apply);
        }

        private void BuildRuntime(VisualElement parent)
        {
            var details = new Foldout { text = "Runtime state", value = false };
            details.AddToClassList("dw-foldout");
            parent.Add(details);
            runtime = new DeucarianEditorWorkspaceForm(details);
            runtime.ReadOnly("viewer-mode", "Mode", () => !IsLive ? "Start Play Mode with an initialized controller" : liveController.Snapshot.Mode.ToString());
            runtime.ReadOnly("viewer-top-down", "Top down", () => !IsLive ? "—" : Yes(liveController.Snapshot.IsTopDown));
            runtime.ReadOnly("viewer-bounds", "Reference bounds", () => !IsLive ? "—" : Yes(liveController.Snapshot.HasReferenceBounds));
            runtime.ReadOnly("viewer-origin", "Origin captured", () => !IsLive ? "—" : Yes(liveController.Snapshot.HasOrigin));
            runtime.ReadOnly("viewer-transition", "Transition", () => !IsLive ? "—" : liveController.Snapshot.IsTransitioning
                ? liveController.Snapshot.TransitionKind.ToString() : "Idle");
            var select = Controls.Button("Select installer", () => { Selection.activeObject = installer; EditorGUIUtility.PingObject(installer); });
            select.SetEnabled(installer != null);
            details.Add(select);
        }

        private bool IsLive => EditorApplication.isPlaying && liveController != null;
        private static string Yes(bool value) => value ? "Yes" : "No";

        private void Apply()
        {
            if (EditorApplication.isPlaying || target == null || EditorUtility.IsPersistent(target) || settings == null || camera == null) return;
            if (installer == null) installer = Undo.AddComponent<ViewerNavigationInstaller>(target);
            using (var value = new SerializedObject(installer))
            {
                value.FindProperty("settings").objectReferenceValue = settings;
                value.FindProperty("navigationCamera").objectReferenceValue = camera;
                value.ApplyModifiedProperties();
            }
            Render();
        }

        private void CreateProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create navigation controls", "ViewerNavigationSettings", "asset", "Choose where to save the controls profile.");
            if (string.IsNullOrEmpty(path)) return;
            var preset = ViewerNavigationSettings.LoadReferencePreset();
            settings = preset != null ? Object.Instantiate(preset) : ScriptableObject.CreateInstance<ViewerNavigationSettings>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            Render();
        }

        private void Update()
        {
            if ((!ReferenceEquals(target, null) && target == null) || (!ReferenceEquals(settings, null) && settings == null)
                || (!ReferenceEquals(camera, null) && camera == null))
            {
                if (target == null) { target = null; installer = null; }
                if (settings == null) settings = null;
                if (camera == null) camera = null;
                Render(); return;
            }
            if (wasPlaying != EditorApplication.isPlaying) { Render(); return; }
            if (EditorApplication.isPlaying && installer != null) liveController = installer.Controller;
            runtime?.Refresh();
            double now = EditorApplication.timeSinceStartup;
            preview.Update((float)(now - previousTime));
            previousTime = now;
            apply?.SetEnabled(!EditorApplication.isPlaying && target != null && !EditorUtility.IsPersistent(target) && settings != null && camera != null);
        }

        private DeucarianEditorSerializedForm Bind(VisualElement root, Object value)
        { var binding = new DeucarianEditorSerializedForm(root, value); bindings.Add(binding); return binding; }
        private void ClearBindings() { foreach (var binding in bindings) binding.Dispose(); bindings.Clear(); }
        private void Dispose() { ClearBindings(); preview.Dispose(); workspace.Dispose(); }
    }
}
