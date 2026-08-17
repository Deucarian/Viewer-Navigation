# Viewer Navigation Bootstrap

1. Add `ViewerNavigationSampleBootstrap` to an empty scene object.
2. Assign the viewer camera, a model root containing renderers, and optionally a
   `ViewerNavigationSettings` asset.
3. Enter Play Mode.

The sample initializes through an explicit composition root, registers the reference
only after it is available, frames it, captures origin, and presents the shared
Report Viewer-proven icon toolbar and an optional six-face view cube, which is
disabled by default. It contains no
report, activity, backend, or browser DTOs.
