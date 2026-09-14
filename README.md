# Deucarian Viewer Navigation Experience

## Asset selection and project defaults

The preview profile starts with the bundled reference preset until you select another profile. Choose includes project and package assets; Create/Customize makes an editable project copy. Scene object and Camera remain explicit scene selections rather than guessing objects. Profile changes update the preview; applying a setup to scene objects remains a separate action.

## Typed definition workflow

Mode selection is an enum shared by C# and the Inspector. The existing viewer controller remains the only navigation state owner.

Start with the [Definition Workflow walkthrough](Documentation~/DefinitionWorkflow.md).
Import **Definition Workflow** in Package Manager for a configured sample scene
and short caller scripts. The sample keeps typed contracts and service setup explicit, with reusable
components for scene callers.


`com.deucarian.viewer-navigation` composes the existing Deucarian camera, Input System,
pointer-capture, UI, theming, logging, and diagnostics packages into a canonical viewer
navigation experience.

Current package version: `0.4.5`. Unity `2022.3` or newer is supported.

For multi-scene applications, create one `PointerCaptureScope` in application
startup and call `viewer.ConfigurePointerCapture(scope.OpenSession())` before
`Initialize`. Navigation disposes only its borrowed session on destruction; the
application disposes the scope on shutdown. Existing callers remain supported
through Pointer Capture's documented scene-scoped compatibility adapter. The
Definition Workflow sample demonstrates the explicit composition route.

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

The embedded preview is interactive in Edit Mode and uses the actual
`ViewerNavigationController`, its motion profile, and Camera Navigation's Orbit/Fly
controllers. Drag to look/orbit, Shift-drag or middle-drag to pan, and scroll to
zoom. Click the preview before using WASD and Q/E; Escape or focus loss releases
input. Orbit, Fly, Top, Frame, Reset, and the optional view cube drive the same
camera actions as runtime. Wheel zoom damps between events and framing/reset
transition from the current pose instead of jumping. Toolbar/view-cube toggles
apply immediately; disabling the view cube does not hide the navigable scene.

Choose a project-owned profile to tune it. Without one, the preview uses the
packaged reference preset without modifying it. Current motion-curve and controls
values are read live. The camera lives in a disposable preview scene; scene
objects and runtime input actions remain untouched. **Apply to scene** is a
separate, explicit Undo-aware action.

Pointer sensitivity and wheel normalization follow the profile's live input
settings. The focused editor gestures described above are not a simulation of
custom runtime key/button bindings. Replacing a profile's controls, framing, or
input asset refreshes the existing preview; presentation-only edits do not
interrupt a move. Starting a gesture during framing cancels at the current pose
without resuming an old zoom target.

`ViewerNavigationController.SetManualUpdates(true)` allows a deterministic host
to advance transitions with `Tick(deltaTime)` instead of a coroutine. This is how
the preview runs outside Play Mode. Only one clock runs at a time, and changing
clock cancels the active move. Normal runtime defaults still animate automatically;
explicit instant moves and the reduced-motion policy remain supported.

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

## Simple typed usage

See [Simple usage](Documentation~/SimpleUsage.md) for the short caller, Inspector selections and one-time scoped setup.

## Preview versus scene setup

The editor preview renders a real Unity Cube. `Preview profile` changes its live controls and motion values. The wrapping toolbar and view cube operate only on that isolated preview.

`Apply to a scene` is separate: assign an Installer object, Scene camera and your profile, then apply with Undo. Choosing a scene object does not replace the preview profile. Scene application is disabled in Play Mode.

The importable `ViewerNavigationDemo.unity` scene uses one Cube and the runtime toolbar. Its material requires URP (declared by this package); assign a URP asset in Graphics Settings and check Quality overrides. The Input System integration keeps Active Input Handling set to Both, with a one-time editor restart needed if native input was disabled.
