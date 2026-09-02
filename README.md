# Deucarian Viewer Navigation Experience

`com.deucarian.viewer-navigation` composes the existing Deucarian camera, Input System,
pointer-capture, UI, theming, logging, and diagnostics packages into a canonical viewer
navigation experience.

Current package version: `0.1.14`. Unity `2022.3` or newer is supported.

It owns the authoritative Orbit/Fly/top-down state, cancellable camera transitions,
reference bounds and pivot wiring, origin capture, UI input blocking, a navigation
toolbar, and an optional six-face view cube. The shared toolbar owns the canonical
UXML, USS, icon layout, and interactions for Orbit, Fly,
Recenter, and Top view. Its colors, typography, density,
active/hover/pressed/focus states, runtime tooltips, and provider style overrides are
resolved through Deucarian UI and Theming. Its panel-shared movement-key guard keeps
viewer controls out of focused UI without suppressing accessibility or gamepad focus
navigation. Deucarian UI exclusively assigns the canonical PanelSettings and semantic
`PrimaryControls` surface role; this package never creates a private panel or chooses a
numeric sorting order. The view cube is disabled by default and can be enabled per
`ViewerNavigationSettings`. It does not own camera math, raw command routing, model
loading, browser transport, or application selection behavior.

## Runtime composition

Create the canonical reference composition from an application composition root and
register reference bounds only after the model has completed placement:

```csharp
ViewerNavigationReferenceCompositionProfile composition =
    ViewerNavigationReferenceComposition.Resolve();
ViewerNavigationInstaller navigation =
    composition.Compose(transform, viewerCamera);
navigation.BeginReferenceLoad();
ViewerNavigationReferenceCenteringResult centering =
    ViewerNavigationReferenceCentering.CenterMeshRendererBoundsAtWorldOrigin(
        loadedModelRoot.transform,
        includeInactive: true);
navigation.RegisterReference(loadedModelRoot, frame: true, captureOrigin: true);
```

`Resolve()` supplies the packaged settings, UI input blocker, MeshRenderer-only bounds
strategy, non-null runtime animation policy, and canonical dark Frosted Glass theme as
one reusable profile. The default policy also honors WebGL
`prefers-reduced-motion`, so every consumer gets the same accessibility behavior.
`Compose()` installs the matching theme provider before the toolbar is initialized.
Whole-viewer shells should pass their authoritative `DeucarianThemeProvider` to the
three-argument overload so every viewer document resolves one theme instance and
navigation does not create a child provider. A host can still pass a
`ViewerNavigationAnimationPolicy` for a deliberate application
override without forking the preset's timing or curves. Use `WithPreset(settings)` for
an intentional navigation-settings variation
while retaining the exact shared input, bounds, animation, and theme objects.
Reinitializing an installer is supported; it detaches old event subscriptions and
cancels active camera transitions before applying the replacement dependencies.
`CenterMeshRendererBoundsAtWorldOrigin` is an explicit placement step for viewers
that use a world-origin model convention. It includes inactive MeshRenderers without
activating them and does not register a reference, move the camera, or alter
selection-owned visibility.

## Public contract

- `ViewerNavigationInstaller` owns scene composition and late model registration.
- `ViewerNavigationReferenceComposition` resolves the canonical settings and runtime
  policies, including the reference theme family and mode, as one reusable profile.
- `ViewerNavigationController` is the single authoritative owner of navigation mode,
  top-down state, reference bounds, origin, and active transition state.
- `ViewerNavigationCommand` and
  `ViewerNavigationController.TryExecuteCommand(...)` provide the
  transport-neutral host-command adapter for that same controller. Wire parsing and
  command routing remain application-owned.
- `ViewerNavigationSnapshot` is the immutable state notification contract.
- `IViewerNavigationMotionProfile` supplies application-specific timing and easing.
- `ViewerNavigationMotionPreferences` provides the shared runtime and WebGL reduced-
  motion decision used by the default reference policy.
- `IViewerNavigationAnimationPolicy` lets a host deliberately override the shared
  motion gate without forking its timing or curves.
- `ViewerNavigationUiInputBlocker` applies the shared EventSystem and UI Toolkit input
  policy.
- `ViewerNavigationMeshBoundsStrategy` preserves the reference MeshRenderer-only bounds
  policy.
- `ViewerNavigationReferenceCentering` explicitly centers that same MeshRenderer-only
  bounds policy at the world origin and returns immutable placement evidence.
- `ViewerNavigationMovementKeyGuard.Bind(root, movementKeyState)` applies one reference-
  counted movement-key policy to any viewer UI Toolkit document. Its optional state
  delegate bridges the first-frame UI event ordering gap without reading input devices
  outside the Input System integration.
- `ViewerNavigationToolbarPresenter` exposes the composed `Document`, `Root`, and
  `ToolbarElement` for integration and parity checks while retaining ownership of its
  assets, theme bindings, input behavior, and element hierarchy.
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

Open **Deucarian Control Center > Experience > Viewer Navigation** to inspect or install the scene-level
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
