# Deucarian Viewer Navigation

`com.deucarian.viewer-navigation` composes the existing Deucarian camera, Input System,
pointer-capture, UI, theming, logging, and diagnostics packages into a canonical viewer
navigation experience.

Current package version: `0.1.4`. Unity `2022.3` or newer is supported.

It owns the authoritative Orbit/Fly/top-down state, cancellable camera transitions,
reference bounds and pivot wiring, origin capture, UI input blocking, a navigation
toolbar, and an optional six-face view cube. The shared toolbar uses the proven Report
Viewer icon layout and interactions for Orbit, Fly, Recenter, and Top view; its colors,
density, active/hover/pressed/focus states, and runtime tooltips are resolved through
Deucarian UI and Theming. The view cube is disabled by default and can be enabled per
`ViewerNavigationSettings`. It does not own camera math, raw command routing, model
loading, browser transport, or application selection behavior.

## Runtime composition

Create a `ViewerNavigationInstaller` from an application composition root, pass the
camera and project-owned settings explicitly, and register reference bounds only after
the model has completed placement:

```csharp
ViewerNavigationInstaller navigation = ViewerNavigationInstaller.Create(
    transform,
    viewerCamera,
    navigationControls,
    inputSettings,
    motionProfile,
    framingSettings,
    inputBlocker,
    referenceBoundsStrategy);
navigation.BeginReferenceLoad();
navigation.RegisterReference(loadedModelRoot, frame: true, captureOrigin: true);
```

The overload taking `ViewerNavigationSettings` is useful for scene-authored projects.
Passing `null` selects the packaged canonical reference preset, whose navigation and
framing tuning is the Report Viewer-proven default. Supply another settings asset only
for an intentional product variation.
The dependency-explicit overload above is preferred for application composition roots
because each policy can be supplied and tested independently. Reinitializing an
installer is supported; it detaches old event subscriptions and cancels active camera
transitions before applying the replacement dependencies.

## Public contract

- `ViewerNavigationInstaller` owns scene composition and late model registration.
- `ViewerNavigationController` is the single authoritative owner of navigation mode,
  top-down state, reference bounds, origin, and active transition state.
- `ViewerNavigationSnapshot` is the immutable state notification contract.
- `IViewerNavigationMotionProfile` supplies application-specific timing and easing.
- `IViewerNavigationAnimationPolicy` lets a host gate the shared motion preset for an
  accessibility preference without forking its timing or curves.
- `IViewerNavigationInputBlocker` lets a host block input without coupling this package
  to application UI.
- `IDeucarianFramingBoundsStrategy<GameObject>` lets a host preserve its proven
  model-bounds policy while the shared controller remains the only navigation owner.
- `ViewerViewFacePolicy` maps six canonical cube faces to model-relative directions.

`SetNavigationMode`, `SetTopDown`, `NavigateToFace`, `FrameReference`, and
`ReturnToOrigin` all supersede an active transition. Pointer or keyboard navigation
also cancels the transition before the lower-level navigation rig consumes that input.

Visibility and selection updates must not call `RegisterReference`, `FrameReference`,
or another camera action. They can safely change model visibility without altering the
authoritative navigation state.

The top face uses the top-down orthographic policy. The other five faces use canonical
model-relative perspective views. Every action supersedes the currently active camera
transition.

## Installation

Install through the Deucarian Package Installer after the package is listed in the
Package Registry. For local development, add the repository as a Unity Package Manager
file dependency:

```json
"com.deucarian.viewer-navigation": "file:../Viewer-Navigation"
```

Keep the exact dependency versions declared in `package.json`; package consumers should
not copy the underlying navigation, pointer, toolbar, or view-cube implementation.

## Sample

Import **Viewer Navigation Bootstrap** from Unity Package Manager. The sample uses an
explicit composition root and registers a renderer-backed model after initialization.
It intentionally contains no Report Viewer, Activity Viewer, browser, or backend DTOs.

## Editor

Open `Tools > Deucarian > Viewer > Navigation` to inspect or install the scene-level
composition component. The editor surface uses `com.deucarian.editor`.

## Diagnostics

Each initialized controller registers a provider with `com.deucarian.diagnostics` and
unregisters it on disable or destruction. Reports include mode, top-down, reference,
origin, transition, and pointer-capture state without payload or authentication data.

## Validation

The package has EditMode coverage for state transitions, bounds/origin behavior,
view-cube mappings, and idempotency, plus PlayMode coverage for transition supersession
and lifecycle cancellation. The shared package-validation workflow runs on pull
requests and pushes to `develop` and `main`.

## Troubleshooting

- No navigation input: verify the assigned camera and Input System settings, then check
  whether the host input blocker or a focused UI control is intentionally blocking it.
- Home does nothing: capture the origin after model placement with
  `RegisterReference(..., captureOrigin: true)` or call `CaptureOrigin` explicitly.
- No reference framing: the registered root must satisfy the configured bounds strategy,
  or the host must call `SetReferenceBounds` with finite, non-zero bounds.
- Toolbar or cube missing: enable the corresponding presentation options on
  `ViewerNavigationSettings`; the dependency-explicit overload uses both by default.

## License

Released under the MIT License. See `LICENSE.md`.
