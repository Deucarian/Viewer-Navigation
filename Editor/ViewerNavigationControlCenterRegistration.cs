using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.ViewerNavigation.Editor
{
    [InitializeOnLoad]
    internal static class ViewerNavigationControlCenterRegistration
    {
        private const string PackageId = "com.deucarian.viewer-navigation";
        private static readonly IDisposable ToolRegistration;
        private static readonly IDisposable CardRegistration;

        static ViewerNavigationControlCenterRegistration()
        {
            ToolRegistration = DeucarianToolRegistry.Register(
                new DeucarianToolDescriptor(
                    DeucarianToolIds.ViewerNavigation,
                    "Viewer Navigation",
                    "Inspect and install the package-owned viewer navigation composition.",
                    DeucarianControlCenterArea.Experience,
                    ViewerNavigationManagerWindow.OpenWindow,
                    PackageId,
                    searchTerms: new[] { "viewer", "navigation", "toolbar", "view cube" },
                    order: 110));

            CardRegistration = DeucarianControlCenterRegistry.RegisterCardProvider(
                new ViewerNavigationCardProvider());
        }

        private sealed class ViewerNavigationCardProvider :
            IDeucarianControlCenterCardProvider
        {
            public string Id => PackageId + ".control-center";

            public IEnumerable<DeucarianControlCenterCard> Capture(
                DeucarianControlCenterContext context)
            {
                ViewerNavigationInstaller[] installers =
                    UnityEngine.Object.FindObjectsByType<ViewerNavigationInstaller>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                ViewerNavigationController[] controllers =
                    EditorApplication.isPlaying
                        ? UnityEngine.Object.FindObjectsByType<ViewerNavigationController>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None)
                        : Array.Empty<ViewerNavigationController>();

                return new[]
                {
                    CreateCard(
                        installers.Length,
                        EditorApplication.isPlaying,
                        controllers)
                };
            }

            private static DeucarianControlCenterCard CreateCard(
                int installerCount,
                bool isPlaying,
                ViewerNavigationController[] controllers)
            {
                int controllerCount = controllers?.Length ?? 0;
                DeucarianControlCenterStatus status;
                string statusText;
                if (isPlaying)
                {
                    status = controllerCount == 1
                        ? DeucarianControlCenterStatus.Success
                        : DeucarianControlCenterStatus.Warning;
                    statusText = controllerCount == 1
                        ? "Viewer navigation active"
                        : controllerCount == 0
                            ? "Controller unavailable"
                            : "Multiple controllers";
                }
                else
                {
                    status = installerCount == 1
                        ? DeucarianControlCenterStatus.Success
                        : DeucarianControlCenterStatus.Warning;
                    statusText = installerCount == 1
                        ? "Scene composition present"
                        : installerCount == 0
                            ? "Scene setup required"
                            : "Multiple scene installers";
                }

                List<string> details = new List<string>
                {
                    "Scene installers: " + installerCount,
                    isPlaying
                        ? "Runtime controllers: " + controllerCount
                        : "Runtime controller state: available in Play Mode"
                };
                if (isPlaying && controllerCount == 1)
                {
                    ViewerNavigationSnapshot snapshot = controllers[0].Snapshot;
                    details.Add("Navigation mode: " + snapshot.Mode);
                    details.Add(
                        snapshot.HasReferenceBounds
                            ? "Reference bounds: available"
                            : "Reference bounds: unavailable");
                    details.Add(
                        snapshot.IsTransitioning
                            ? "Transition: active"
                            : "Transition: idle");
                }

                return new DeucarianControlCenterCard(
                    PackageId + ".workflow",
                    DeucarianControlCenterArea.Experience,
                    "Viewer Navigation",
                    "Canonical scene composition and current runtime state.",
                    PackageId,
                    status,
                    statusText,
                    order: 110,
                    details: details,
                    actions: new[]
                    {
                        new DeucarianControlCenterAction(
                            PackageId + ".open",
                            "Open Viewer Navigation",
                            ViewerNavigationManagerWindow.OpenWindow)
                    },
                    searchTerms: new[]
                    {
                        "viewer", "navigation", "orbit", "fly",
                        "installer", "controller", "scene"
                    });
            }
        }
    }
}