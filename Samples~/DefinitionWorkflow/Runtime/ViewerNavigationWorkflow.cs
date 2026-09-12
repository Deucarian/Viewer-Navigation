using System;
using UnityEngine;

namespace Deucarian.ViewerNavigation.Samples.DefinitionWorkflow
{
    /// <summary>Small caller example. The configured scene hosts own services and resource lifetimes.</summary>
    public sealed class ViewerNavigationWorkflow : MonoBehaviour
    {
        [SerializeField] private ViewerNavigationHost host;
        [SerializeField] private ViewerNavigationTrigger trigger;
        private string status = "Ready. Choose an action below.";
        public string Status => status;
        public void Orbit() { host.SetMode(ViewerNavigationMode.Orbit); status = "Orbit mode selected."; }
        public void Fly() { host.SetMode(ViewerNavigationMode.Fly); status = "Fly mode selected."; }
        public void ApplyComponent() { trigger.ApplyMode(); status = "Applied the component's typed mode selection."; }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24, 24, Math.Min(540, Screen.width - 48), Screen.height - 48), GUI.skin.box);
            GUILayout.Label("Viewer-Navigation — definition workflow");
            GUILayout.Label("Mode selection is an enum shared by C# and the Inspector. The existing viewer controller remains the only navigation state owner.");
            GUILayout.Space(12);
            if (GUILayout.Button("Orbit mode", GUILayout.Height(32))) { try { Orbit(); } catch (Exception error) { status = error.Message; } }
            if (GUILayout.Button("Fly mode", GUILayout.Height(32))) { try { Fly(); } catch (Exception error) { status = error.Message; } }
            if (GUILayout.Button("Apply component mode", GUILayout.Height(32))) { try { ApplyComponent(); } catch (Exception error) { status = error.Message; } }
            GUILayout.Space(12);
            GUILayout.Label(status);
            GUILayout.EndArea();
        }
    }
}
