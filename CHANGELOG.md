# Changelog

## 0.1.7 - 2026-08-17

- Moved the complete canonical reference navigation toolbar into package-owned UXML,
  USS, and transparent scale-with-screen-size PanelSettings assets.
- Matched the proven control-island layout, semantic accent selection, icon swapping,
  tooltips, pointer picking, focus behavior, and motion while keeping every name and
  dependency consumer-neutral.
- Added panel-shared movement-key suppression so WASD, arrows, Q/E, and page movement
  never activate focused UI controls while accessibility and gamepad navigation remain
  available.
- Made Theming the source of theme, style, and typography and UI the source of glass,
  density, chrome, icon interaction, and motion primitives.
- Added package-level presentation, asset-ownership, provider-style, input, and
  movement-key regressions for all viewer consumers.

## 0.1.6 - 2026-08-17

- Made the default reference animation policy honor the browser's generic
  `prefers-reduced-motion` preference in WebGL as well as runtime state.
- Added package-owned, defensive WebGL motion-preference interop so Report Viewer,
  Activity Viewer, and reusable templates no longer need consumer-specific gates.
- Added deterministic policy coverage for every play-mode and reduced-motion state.

## 0.1.5 - 2026-08-17

- Added one canonical reference composition for the packaged navigation settings,
  animation policy, UI input blocker, MeshRenderer-only bounds strategy, and shared
  reference-viewer theme profile.
- Made the default reference animation policy explicit, non-null, and runtime-only while
  retaining an injectable accessibility gate for host applications.
- Reference composition now installs the canonical dark Frosted Glass theme provider
  before toolbar initialization, while non-reference installer overloads remain
  unthemed.
- Added focused regressions for composition injection, motion-policy reevaluation,
  reference bounds, and UI input classification.

## 0.1.4 - 2026-08-17

- Restored the polished Report Viewer navigation toolbar as shared package behavior:
  Orbit, Fly, Recenter, and Top-view icons replace the temporary letter buttons.
- Restored theme-driven selected, hover, pressed, focus, and disabled presentation,
  orthographic/perspective icon swapping, and runtime/keyboard tooltips.
- Added package regressions that require the real icon resources and continue to keep
  the optional view cube disabled by default.

## 0.1.3 - 2026-08-17

- Made the optional view cube opt-in. New settings, the packaged reference preset,
  and presenter initialization without settings now keep it hidden by default.
- Kept the existing package-level presentation toggle so individual viewers can
  explicitly enable the six-face cube when their product calls for it.
- Recomposition now clears an existing cube when the setting is switched off.

## 0.1.2 - 2026-08-14

- Added the canonical reference-viewer navigation preset using the tuning proven by
  Report Viewer for Orbit, Fly, framing, transitions, toolbar, and view cube.
- Made the settings-based installer resolve that preset when a consumer does not
  provide an intentional override, so Report Viewer, Activity Viewer, and new viewer
  templates share one default experience rather than parallel local defaults.

## 0.1.1 - 2026-08-14

- Coordinated configured drag actions with the Pointer Capture Requested, Active,
  Rejected, and Lost lifecycle; navigation now remains blocked until capture is active
  and a lost or rejected held gesture cannot resume without release and rearming.
- Added an injectable renderer-bounds strategy so consumers can preserve their proven
  model framing policy without retaining a second navigation engine.

## 0.1.0 - 2026-08-13

- Added the viewer navigation composition root and authoritative state owner.
- Added Orbit, Fly, top-down, return-to-origin, framing, and cancellable transitions.
- Added pointer capture and explicit UI input-blocking coordination.
- Added a camera-following six-face view cube and themed runtime toolbar.
- Added Diagnostics, Deucarian Editor tooling, samples, and lifecycle/state tests.
- Routed viewer-level action observation through the configurable Input System
  integration, including remapped-control and input-blocking coverage.
- Preserved the active theme palette while camera orientation updates the view
  cube, and exposed the captured origin as read-only navigation state.
