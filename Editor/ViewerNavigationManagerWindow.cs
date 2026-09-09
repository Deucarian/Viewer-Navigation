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
                DeucarianEditorWindowPages.GetStandalone<ViewerNavigationManagerWindow>("Viewer Navigation");
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

        public static IDeucarianEditorPage CreatePage() =>
            DeucarianEditorImGuiPage.Create<ViewerNavigationManagerWindow>(DeucarianToolIds.ViewerNavigation, window => window.OnGUI());

        private void OnGUI()
        {
            using (DeucarianEditorWorkbenchPanelScope page =
                   DeucarianEditorWorkbenchGUI.BeginSettingsPage(this,
                       GUILayout.ExpandHeight(true)))
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DeucarianEditorChrome.DrawPackageHeader(this,
                    "Viewer Navigation",
                    "Canonical Orbit, Fly, top-down, origin, and view-cube experience.");
                DrawOwnership();
                DrawSelectedObject();
                DrawRuntimeState();
                DeucarianEditorChrome.DrawFooterVersion(this,
                    "com.deucarian.viewer-navigation");
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawOwnership()
        {
            DeucarianEditorChrome.DrawSectionHeader("Package Boundary");
            DeucarianEditorChrome.BeginSection();
            DeucarianEditorTextGUI.HelpBox(
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
                DeucarianEditorTextGUI.HelpBox(
                    "Select the application composition-root GameObject.",
                    MessageType.Info);
            }
            else
            {
                ViewerNavigationInstaller installer =
                    selected.GetComponent<ViewerNavigationInstaller>();
                if (installer == null)
                {
                    DeucarianEditorTextGUI.HelpBox(
                        selected.name + " has no Viewer Navigation installer.",
                        MessageType.Warning);
                    if (DeucarianEditorActionGUI.Button(
                            "Add Viewer Navigation Installer",
                            DeucarianEditorWorkbenchGUI.PrimaryButtonStyle))
                    {
                        installer = Undo.AddComponent<ViewerNavigationInstaller>(selected);
                        Selection.activeObject = installer;
                    }
                }
                else
                {
                    DeucarianEditorInputGUI.ObjectField(
                        "Installer",
                        installer,
                        typeof(ViewerNavigationInstaller),
                        true);
                    if (DeucarianEditorActionGUI.Button(
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
                DeucarianEditorTextGUI.HelpBox(
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
