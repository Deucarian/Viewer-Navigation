# Viewer Navigation Bootstrap

Open **ViewerNavigationDemo.unity** and press Play. A camera, Unity Cube reference
and **SampleViewerSettings.asset** are already assigned on **Viewer demo**.
Use the generated package toolbar to switch views and modes. Inspect the sample
settings to change transition timing or enable the optional view cube.

The Cube material targets URP, declared as a package dependency. Assign a URP
pipeline asset in Graphics Settings and check Quality overrides; installing the
dependency alone does not activate it. For another pipeline, replace the material;
the navigation behavior itself is pipeline-agnostic.

The Input System integration keeps Active Input Handling = Both in Player
Settings. Restart Unity once if native input was previously disabled. The scene's
instruction canvas uses a built-in input module; the toolbar is package-owned UI Toolkit.

To compose another scene manually:

1. Add `ViewerNavigationSampleBootstrap` to an empty scene object.
2. Assign the viewer camera, a model root containing renderers, and optionally a
   `ViewerNavigationSettings` asset.
3. Enter Play Mode.

The sample initializes through an explicit composition root, registers the reference
only after it is available, frames it, captures origin, and presents the shared
Report Viewer-proven icon toolbar and an optional six-face view cube, which is
disabled by default. It contains no
report, activity, backend, or browser DTOs.
