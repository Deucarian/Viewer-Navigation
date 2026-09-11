using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.ViewerNavigation.Editor
{
    public sealed class ViewerNavigationManagerWindow : EditorWindow
    {
        private DeucarianEditorPageSession session;
        public static void OpenWindow() => DeucarianEditorToolWindow.Open(DeucarianToolIds.ViewerNavigation);
        public static IDeucarianEditorPage CreatePage() => new ViewerNavigationPage().Page;
        private void CreateGUI()
        {
            session?.Dispose();
            session = new DeucarianEditorPageSession(this, DeucarianToolIds.ViewerNavigation, CreatePage());
        }
        private void OnDisable() { session?.Dispose(); session = null; }
    }
}
