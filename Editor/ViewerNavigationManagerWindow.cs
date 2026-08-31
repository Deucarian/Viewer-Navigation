using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.ViewerNavigation.Editor
{
    public sealed class ViewerNavigationManagerWindow : EditorWindow
    {
        private Vector2 scrollPosition;

        public static void OpenWindow()
        {
            ViewerNavigationManagerWindow window =
                GetWindow<ViewerNavigationManagerWindow>("Viewer Navigation");
            window.minSize = new Vector2(520f, 480f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += RepaintWhilePlaying;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintWhilePlaying;
        }

        private void OnGUI()
        {
            using (DeucarianEditorWorkbenchPanelScope page =
                   DeucarianEditorWorkbenchGUI.BeginSettingsPage(
                       GUILayout.ExpandHeight(true)))
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DeucarianEditorChrome.DrawPackageHeader(
                    "Viewer Navigation",
                    "Canonical Orbit, Fly, top-down, origin, and view-cube experience.");
                DrawOwnership();
                DrawSelectedObject();
                DrawRuntimeState();
                DeucarianEditorChrome.DrawFooterVersion(
                    "com.deucarian.viewer-navigation");
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawOwnership()
        {
            DeucarianEditorChrome.DrawSectionHeader("Package Boundary");
            DeucarianEditorChrome.BeginSection();
            EditorGUILayout.HelpBox(
                "This package composes navigation state, transitions, pointer/UI arbitration, " +
                "toolbar, and view cube. Camera math remains in Camera Navigation; selection, " +
                "model loading, and browser commands remain application concerns.",
                MessageType.Info);
            DeucarianEditorChrome.EndSection();
        }

        private static void DrawSelectedObject()
        {
            DeucarianEditorChrome.DrawSectionHeader("Selected Composition Root");
            DeucarianEditorChrome.BeginSection();
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorGUILayout.HelpBox(
                    "Select the application composition-root GameObject.",
                    MessageType.Info);
            }
            else
            {
                ViewerNavigationInstaller installer =
                    selected.GetComponent<ViewerNavigationInstaller>();
                if (installer == null)
                {
                    EditorGUILayout.HelpBox(
                        selected.name + " has no Viewer Navigation installer.",
                        MessageType.Warning);
                    if (GUILayout.Button(
                            "Add Viewer Navigation Installer",
                            DeucarianEditorWorkbenchGUI.PrimaryButtonStyle))
                    {
                        installer = Undo.AddComponent<ViewerNavigationInstaller>(selected);
                        Selection.activeObject = installer;
                    }
                }
                else
                {
                    EditorGUILayout.ObjectField(
                        "Installer",
                        installer,
                        typeof(ViewerNavigationInstaller),
                        true);
                    if (GUILayout.Button(
                            "Select Installer",
                            DeucarianEditorWorkbenchGUI.SecondaryButtonStyle))
                    {
                        Selection.activeObject = installer;
                        EditorGUIUtility.PingObject(installer);
                    }
                }
            }

            DeucarianEditorChrome.EndSection();
        }

        private static void DrawRuntimeState()
        {
            DeucarianEditorChrome.DrawSectionHeader("Runtime State");
            DeucarianEditorChrome.BeginSection();
            ViewerNavigationController controller =
                Object.FindFirstObjectByType<ViewerNavigationController>();
            if (!EditorApplication.isPlaying || controller == null)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode with an initialized controller to inspect live state.",
                    MessageType.Info);
                DeucarianEditorChrome.EndSection();
                return;
            }

            ViewerNavigationSnapshot snapshot = controller.Snapshot;
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow("Mode", snapshot.Mode.ToString());
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Top Down",
                snapshot.IsTopDown.ToString());
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Reference Bounds",
                snapshot.HasReferenceBounds.ToString());
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Origin Captured",
                snapshot.HasOrigin.ToString());
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Transition",
                snapshot.IsTransitioning
                    ? snapshot.TransitionKind.ToString()
                    : "Idle");
            DeucarianEditorChrome.EndSection();
        }

        private void RepaintWhilePlaying()
        {
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }
    }
}
